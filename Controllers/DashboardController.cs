using Microsoft.AspNetCore.Mvc;
using CSE325FinalProject.Services;
using CSE325FinalProject.Models.DTOs;

namespace CSE325FinalProject.Controllers;

public class DashboardController : Controller
{
    private readonly IAuthService _authService;
    private readonly ISkillService _skillService;
    private readonly IProgressLogService _progressLogService;
    
    public DashboardController(
        IAuthService authService, 
        ISkillService skillService,
        IProgressLogService progressLogService)
    {
        _authService = authService;
        _skillService = skillService;
        _progressLogService = progressLogService;
    }
    
    // Helper to get current user
    private async Task<UserDto?> GetCurrentUserAsync()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken)) return null;
        
        var session = await _authService.GetSessionByRefreshTokenAsync(refreshToken);
        if (session?.User == null) return null;
        
        return new UserDto
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
        };
    }
    
    // Auth check - redirects to login if not authenticated
    private async Task<(bool isAuthenticated, UserDto? user)> RequireAuthAsync()
    {
        var user = await GetCurrentUserAsync();
        return (user != null, user);
    }
    
    // Dashboard Home - Skills Overview
    public async Task<IActionResult> Index()
    {
        var (isAuthenticated, user) = await RequireAuthAsync();
        if (!isAuthenticated) return RedirectToAction("Login", "Account");
        
        ViewBag.User = user;
        
        // Get user's skills
        var skills = await _skillService.GetUserSkillsAsync(user!.Id);
        ViewBag.Skills = skills;
        
        // Get dashboard stats
        var stats = await _progressLogService.GetDashboardStatsAsync(user.Id);
        ViewBag.Stats = stats;
        
        return View();
    }

    // All Skills List
    public async Task<IActionResult> Skills()
    {
        var (isAuthenticated, user) = await RequireAuthAsync();
        if (!isAuthenticated) return RedirectToAction("Login", "Account");
        
        ViewBag.User = user;
        
        var skills = await _skillService.GetUserSkillsAsync(user!.Id);
        ViewBag.Skills = skills;
        
        return View();
    }

    // Skill Detail View
    public async Task<IActionResult> SkillDetail(int? id)
    {
        var (isAuthenticated, user) = await RequireAuthAsync();
        if (!isAuthenticated) return RedirectToAction("Login", "Account");
        
        ViewBag.User = user;
        
        if (id.HasValue)
        {
            var skill = await _skillService.GetSkillByIdAsync(id.Value, user!.Id);
            ViewBag.Skill = skill;
        }
        
        return View();
    }

    // Goal Detail View (Progress logs and history)
    public async Task<IActionResult> GoalDetail(int? id)
    {
        var (isAuthenticated, user) = await RequireAuthAsync();
        if (!isAuthenticated) return RedirectToAction("Login", "Account");
        
        ViewBag.User = user;
        
        return View();
    }

    // Progress Chart / Reports
    public async Task<IActionResult> ProgressChart()
    {
        var (isAuthenticated, user) = await RequireAuthAsync();
        if (!isAuthenticated) return RedirectToAction("Login", "Account");
        
        ViewBag.User = user;
        
        var stats = await _progressLogService.GetDashboardStatsAsync(user!.Id);
        ViewBag.Stats = stats;
        
        var weeklyActivity = await _progressLogService.GetWeeklyActivityAsync(user.Id, 12);
        ViewBag.WeeklyActivity = weeklyActivity;
        
        return View();
    }

    // Sharing Settings
    public async Task<IActionResult> Sharing()
    {
        var (isAuthenticated, user) = await RequireAuthAsync();
        if (!isAuthenticated) return RedirectToAction("Login", "Account");
        
        ViewBag.User = user;
        
        return View();
    }

    // Public Profile View
    public IActionResult PublicView()
    {
        return View();
    }

    // Profile Settings
    public async Task<IActionResult> Profile()
    {
        var (isAuthenticated, user) = await RequireAuthAsync();
        if (!isAuthenticated) return RedirectToAction("Login", "Account");
        
        ViewBag.User = user;
        
        return View();
    }

    // Settings (same as Profile)
    public async Task<IActionResult> Settings()
    {
        var (isAuthenticated, user) = await RequireAuthAsync();
        if (!isAuthenticated) return RedirectToAction("Login", "Account");
        
        ViewBag.User = user;
        
        return View("Profile");
    }

    // Log Progress
    public async Task<IActionResult> LogProgress(int? skillId, int? goalId)
    {
        var (isAuthenticated, user) = await RequireAuthAsync();
        if (!isAuthenticated) return RedirectToAction("Login", "Account");
        
        ViewBag.User = user;
        ViewBag.SkillId = skillId;
        ViewBag.GoalId = goalId;
        
        var skills = await _skillService.GetUserSkillsAsync(user!.Id);
        ViewBag.Skills = skills;
        
        return View();
    }

    // Daily Log Success
    public async Task<IActionResult> DailyLogSuccess()
    {
        var (isAuthenticated, user) = await RequireAuthAsync();
        if (!isAuthenticated) return RedirectToAction("Login", "Account");
        
        ViewBag.User = user;
        
        return View();
    }

    // New Skill
    public async Task<IActionResult> NewSkill()
    {
        var (isAuthenticated, user) = await RequireAuthAsync();
        if (!isAuthenticated) return RedirectToAction("Login", "Account");
        
        ViewBag.User = user;
        
        var categories = await _skillService.GetCategoriesAsync();
        ViewBag.Categories = categories;
        
        return View();
    }
}
