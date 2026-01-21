using Microsoft.AspNetCore.Mvc;
using CSE325FinalProject.Models.DTOs;
using CSE325FinalProject.Services;

namespace CSE325FinalProject.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    
    /// <summary>
    /// Register a new user
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
    /// Login user and get tokens
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
        
        // Set refresh token as HttpOnly cookie (more secure)
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
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest? request)
    {
        // Try to get refresh token from cookie first, then from body
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
            // Clear invalid cookie
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
    /// Logout and invalidate tokens
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
    /// Get current user info from token
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
    /// Validate if refresh token is still valid
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
}
