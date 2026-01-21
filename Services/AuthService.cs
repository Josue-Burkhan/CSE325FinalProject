using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CSE325FinalProject.Data;
using CSE325FinalProject.Models;
using CSE325FinalProject.Models.DTOs;

namespace CSE325FinalProject.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, string? ipAddress, string? userAgent);
    Task<bool> LogoutAsync(int userId, string refreshToken);
    Task<bool> LogoutAllSessionsAsync(int userId);
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<bool> ValidateSessionAsync(string refreshToken);
    Task<UserSession?> GetSessionByRefreshTokenAsync(string refreshToken);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    
    // Token expiration settings
    private const int AccessTokenExpirationMinutes = 30; // 30 minutes
    private const int RefreshTokenExpirationDays = 30;   // 30 days
    
    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }
    
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if email exists
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
        {
            return new AuthResponse { Success = false, Message = "Email already registered" };
        }
        
        // Create user
        var user = new User
        {
            Email = request.Email.ToLower().Trim(),
            PasswordHash = HashPassword(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        // Create default sharing settings
        var sharingSettings = new UserSharingSettings
        {
            UserId = user.Id,
            IsProfilePublic = false,
            PublicUsername = GenerateUsername(request.FirstName, request.LastName)
        };
        
        _context.UserSharingSettings.Add(sharingSettings);
        await _context.SaveChangesAsync();
        
        return new AuthResponse
        {
            Success = true,
            Message = "Registration successful! Please log in.",
            User = MapToDto(user)
        };
    }
    
    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.IsActive);
        
        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            return new AuthResponse { Success = false, Message = "Invalid email or password" };
        }
        
        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        
        // Generate tokens
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        
        // Create session with refresh token
        var session = new UserSession
        {
            UserId = user.Id,
            SessionToken = refreshToken,
            DeviceName = ParseDeviceName(userAgent),
            DeviceType = ParseDeviceType(userAgent),
            Browser = ParseBrowser(userAgent),
            IpAddress = ipAddress,
            IsCurrent = true,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow
        };
        
        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();
        
        return new AuthResponse
        {
            Success = true,
            Message = "Login successful",
            User = MapToDto(user),
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes)
        };
    }
    
    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string? ipAddress, string? userAgent)
    {
        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionToken == refreshToken && s.ExpiresAt > DateTime.UtcNow);
        
        if (session == null || session.User == null || !session.User.IsActive)
        {
            return new AuthResponse { Success = false, Message = "Invalid or expired refresh token" };
        }
        
        var user = session.User;
        
        // Generate new access token
        var accessToken = GenerateAccessToken(user);
        
        // Optionally rotate refresh token for extra security
        var newRefreshToken = GenerateRefreshToken();
        session.SessionToken = newRefreshToken;
        session.LastActiveAt = DateTime.UtcNow;
        session.ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays); // Extend session
        session.IpAddress = ipAddress ?? session.IpAddress;
        
        await _context.SaveChangesAsync();
        
        return new AuthResponse
        {
            Success = true,
            Message = "Token refreshed",
            User = MapToDto(user),
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes)
        };
    }
    
    public async Task<bool> LogoutAsync(int userId, string refreshToken)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionToken == refreshToken);
        
        if (session != null)
        {
            _context.UserSessions.Remove(session);
            await _context.SaveChangesAsync();
            return true;
        }
        
        return false;
    }
    
    public async Task<bool> LogoutAllSessionsAsync(int userId)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId)
            .ToListAsync();
        
        _context.UserSessions.RemoveRange(sessions);
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }
    
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }
    
    public async Task<bool> ValidateSessionAsync(string refreshToken)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.SessionToken == refreshToken && s.ExpiresAt > DateTime.UtcNow);
        
        if (session != null)
        {
            session.LastActiveAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        
        return false;
    }
    
    public async Task<UserSession?> GetSessionByRefreshTokenAsync(string refreshToken)
    {
        return await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionToken == refreshToken && s.ExpiresAt > DateTime.UtcNow);
    }
    
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }
    
    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
    
    // JWT Token Generation
    private string GenerateAccessToken(User user)
    {
        var jwtSecret = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = _configuration["Jwt:Issuer"] ?? "SkillTracker";
        var audience = _configuration["Jwt:Audience"] ?? "SkillTrackerUsers";
        
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim("fullName", user.FullName),
            new Claim("initials", user.Initials),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
    
    // Private helpers
    private string GenerateUsername(string firstName, string lastName)
    {
        var baseUsername = $"{firstName.ToLower()}-{lastName.ToLower()}".Replace(" ", "-");
        var random = new Random().Next(100, 999);
        return $"{baseUsername}-{random}";
    }
    
    private string ParseDeviceName(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown Device";
        
        if (userAgent.Contains("Macintosh")) return "Mac";
        if (userAgent.Contains("Windows")) return "Windows PC";
        if (userAgent.Contains("iPhone")) return "iPhone";
        if (userAgent.Contains("iPad")) return "iPad";
        if (userAgent.Contains("Android")) return "Android Device";
        if (userAgent.Contains("Linux")) return "Linux";
        
        return "Unknown Device";
    }
    
    private string ParseDeviceType(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "unknown";
        
        if (userAgent.Contains("Mobile") || userAgent.Contains("iPhone") || userAgent.Contains("Android"))
            return "mobile";
        if (userAgent.Contains("iPad") || userAgent.Contains("Tablet"))
            return "tablet";
        
        return "desktop";
    }
    
    private string ParseBrowser(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";
        
        if (userAgent.Contains("Chrome") && !userAgent.Contains("Edge")) return "Chrome";
        if (userAgent.Contains("Firefox")) return "Firefox";
        if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome")) return "Safari";
        if (userAgent.Contains("Edge")) return "Edge";
        
        return "Unknown";
    }
    
    private UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Initials = user.Initials,
            JobTitle = user.JobTitle,
            AvatarUrl = user.AvatarUrl,
            ThemePreference = user.ThemePreference
        };
    }
}
