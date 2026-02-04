using System.Security.Claims;
using CSE325FinalProject.Services;

namespace CSE325FinalProject.Middleware;

/// <summary>
/// Custom middleware that reads the refreshToken cookie and sets up HttpContext.User
/// This bridges the gap between the session-based auth and ASP.NET Core's auth system
/// </summary>
public class SessionAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionAuthMiddleware> _logger;

    public SessionAuthMiddleware(RequestDelegate next, ILogger<SessionAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
        // Skip if already authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        var refreshToken = context.Request.Cookies["refreshToken"];
        
        if (!string.IsNullOrEmpty(refreshToken))
        {
            try
            {
                var session = await authService.GetSessionByRefreshTokenAsync(refreshToken);
                
                if (session?.User != null)
                {
                    var claims = new[]
                    {
                        new Claim("userId", session.User.Id.ToString()),
                        new Claim(ClaimTypes.NameIdentifier, session.User.Id.ToString()),
                        new Claim(ClaimTypes.Name, session.User.FullName),
                        new Claim(ClaimTypes.Email, session.User.Email),
                        new Claim("firstName", session.User.FirstName),
                        new Claim("lastName", session.User.LastName),
                        new Claim("initials", session.User.Initials),
                        new Claim("avatarUrl", session.User.AvatarUrl ?? ""),
                        new Claim("themePreference", session.User.ThemePreference)
                    };

                    var identity = new ClaimsIdentity(claims, "SessionAuth");
                    context.User = new ClaimsPrincipal(identity);
                    
                    _logger.LogDebug("[SESSION_AUTH] User authenticated via session: {Email}", session.User.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SESSION_AUTH] Error authenticating user from session");
            }
        }

        await _next(context);
    }
}

public static class SessionAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionAuthMiddleware>();
    }
}
