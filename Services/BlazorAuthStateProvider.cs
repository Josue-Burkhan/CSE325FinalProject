using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using CSE325FinalProject.Models;

namespace CSE325FinalProject.Services;

/// <summary>
/// Blazor authentication state provider that integrates with the existing AuthService
/// </summary>
public class BlazorAuthStateProvider : AuthenticationStateProvider
{
    private readonly IAuthService _authService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BlazorAuthStateProvider(IAuthService authService, IHttpContextAccessor httpContextAccessor)
    {
        _authService = authService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return CreateAnonymousState();
            }

            var refreshToken = httpContext.Request.Cookies["refreshToken"];
            
            if (string.IsNullOrEmpty(refreshToken))
            {
                return CreateAnonymousState();
            }

            var session = await _authService.GetSessionByRefreshTokenAsync(refreshToken);
            
            if (session?.User == null)
            {
                return CreateAnonymousState();
            }

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

            var identity = new ClaimsIdentity(claims, "cookie");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return CreateAnonymousState();
        }
    }

    private static AuthenticationState CreateAnonymousState()
    {
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    /// <summary>
    /// Call this after login/logout to update the authentication state
    /// </summary>
    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
