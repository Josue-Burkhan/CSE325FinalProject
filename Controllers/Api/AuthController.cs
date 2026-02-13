using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using CSE325FinalProject.Models;
using CSE325FinalProject.Models.DTOs;
using CSE325FinalProject.Services;
using CSE325FinalProject.Data;

namespace CSE325FinalProject.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;
    
    public AuthController(IAuthService authService, ApplicationDbContext context)
    {
        _authService = authService;
        _context = context;
    }
    
    /// <summary>
    /// <summary>
    /// Registers a new user account with the provided details
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(ApiResponse<AuthResponse>.Fail("Validation failed", errors));
        }
        
        var result = await _authService.RegisterAsync(request);
        
        if (!result.Success)
        {
            return BadRequest(ApiResponse<AuthResponse>.Fail(result.Message ?? "Registration failed"));
        }
        
        return Ok(ApiResponse<AuthResponse>.Ok(result, result.Message));
    }
    
    /// <summary>
    /// <summary>
    /// Authenticates a user and issues access/refresh tokens
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(ApiResponse<AuthResponse>.Fail("Validation failed", errors));
        }
        
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();
        
        var result = await _authService.LoginAsync(request, ipAddress, userAgent);
        
        if (!result.Success)
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail(result.Message ?? "Login failed"));
        }
        
        // Sets the refresh token as a secure HttpOnly cookie
        if (!string.IsNullOrEmpty(result.RefreshToken))
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(30)
            };
            Response.Cookies.Append("refreshToken", result.RefreshToken, cookieOptions);
        }
        
        return Ok(ApiResponse<AuthResponse>.Ok(result, result.Message));
    }
    
    /// <summary>
    /// <summary>
    /// Renews the access token using a valid refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest? request)
    {
        // Attempts to retrieve the refresh token from the cookie first, then the request body
        var refreshToken = Request.Cookies["refreshToken"] ?? request?.RefreshToken;
        
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(ApiResponse<AuthResponse>.Fail("Refresh token is required"));
        }
        
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();
        
        var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress, userAgent);
        
        if (!result.Success)
        {
            // Clears the invalid cookie
            Response.Cookies.Delete("refreshToken");
            return Unauthorized(ApiResponse<AuthResponse>.Fail(result.Message ?? "Token refresh failed"));
        }
        
        // Update refresh token cookie
        if (!string.IsNullOrEmpty(result.RefreshToken))
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(30)
            };
            Response.Cookies.Append("refreshToken", result.RefreshToken, cookieOptions);
        }
        
        return Ok(ApiResponse<AuthResponse>.Ok(result));
    }
    
    /// <summary>
    /// <summary>
    /// Invalidates the current session and clears authentication cookies
    /// </summary>
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<bool>>> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var session = await _authService.GetSessionByRefreshTokenAsync(refreshToken);
            if (session != null)
            {
                await _authService.LogoutAsync(session.UserId, refreshToken);
            }
        }
        
        // Clear cookie
        Response.Cookies.Delete("refreshToken");
        
        return Ok(ApiResponse<bool>.Ok(true, "Logged out successfully"));
    }
    
    /// <summary>
    /// <summary>
    /// Retrieves the currently authenticated user's profile information
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
    {
        // First try to get user from refresh token cookie (for persistent sessions)
        var refreshToken = Request.Cookies["refreshToken"];
        
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var session = await _authService.GetSessionByRefreshTokenAsync(refreshToken);
            if (session?.User != null)
            {
                return Ok(ApiResponse<UserDto>.Ok(new UserDto
                {
                    Id = session.User.Id,
                    Email = session.User.Email,
                    FirstName = session.User.FirstName,
                    LastName = session.User.LastName,
                    FullName = session.User.FullName,
                    Initials = session.User.Initials,
                    JobTitle = session.User.JobTitle,
                    AvatarUrl = session.User.AvatarUrl,
                    ThemePreference = session.User.ThemePreference
                }));
            }
        }
        
        return Unauthorized(ApiResponse<UserDto>.Fail("Not authenticated"));
    }
    
    /// <summary>
    /// <summary>
    /// Validates if the current refresh token is active and valid
    /// </summary>
    [HttpGet("validate")]
    public async Task<ActionResult<ApiResponse<bool>>> ValidateSession()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Ok(ApiResponse<bool>.Ok(false, "No session"));
        }
        
        var isValid = await _authService.ValidateSessionAsync(refreshToken);
        return Ok(ApiResponse<bool>.Ok(isValid, isValid ? "Session valid" : "Session expired"));
    }

    [HttpGet("github/login")]
    public IActionResult LoginGitHub()
    {
        var redirectUrl = Url.Action("GitHubCallback", "Auth");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, "GitHub");
    }

    [HttpGet("github/callback")]
    public async Task<IActionResult> GitHubCallback()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
        {
            // Fallback: try to authenticate with default scheme if cookie didn't work (unlikely for external)
            // Actually, AddGitHub should have handled the handshake.
            // If we are here, something went wrong or we need to check the external cookie scheme if we set one.
            // But Program.cs set SignInScheme to "Cookies".
            return Redirect("/login?error=external-auth-failed");
        }

        var claims = authenticateResult.Principal.Claims;
        var emailClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email) 
                         ?? claims.FirstOrDefault(c => c.Type == "email");
        var nameClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name) 
                        ?? claims.FirstOrDefault(c => c.Type == "name")
                        ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

        if (emailClaim == null)
        {
            return Redirect("/login?error=email-missing");
        }

        var email = emailClaim.Value;
        var name = nameClaim?.Value ?? "GitHub User";
        
        // Split name into First/Last
        var names = name.Split(' ', 2);
        var firstName = names[0];
        var lastName = names.Length > 1 ? names[1] : "";

        // Check if user exists
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            // Create user
            user = new User
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random password
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ThemePreference = "light"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        
        // Re-issue cookie with application claims
        var appClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("initials", user.Initials),
            new Claim("avatarUrl", user.AvatarUrl ?? "")
        };
        
        var identity = new ClaimsIdentity(appClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Redirect("/");
    }
}
