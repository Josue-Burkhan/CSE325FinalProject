using Microsoft.AspNetCore.Mvc;
using CSE325FinalProject.Services;

namespace CSE325FinalProject.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    
    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }
    
    // Login Page
    public async Task<IActionResult> Login()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var isValid = await _authService.ValidateSessionAsync(refreshToken);
            if (isValid)
            {
                return RedirectToAction("Index", "Dashboard");
            }
        }
        
        return View();
    }
    
    // Register Page
    public async Task<IActionResult> Register()
    {
        // Check if already logged in via cookie
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var isValid = await _authService.ValidateSessionAsync(refreshToken);
            if (isValid)
            {
                return RedirectToAction("Index", "Dashboard");
            }
        }
        
        return View();
    }

    // Logout action
    public async Task<IActionResult> Logout()
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
        
        return RedirectToAction("Login");
    }
}
