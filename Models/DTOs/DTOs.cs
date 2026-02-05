using System.ComponentModel.DataAnnotations;

namespace CSE325FinalProject.Models.DTOs;

// ==========================================
// AUTHENTICATION DTOs
// ==========================================

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;
    
    public bool RememberMe { get; set; } = false;
}

public class RegisterRequest
{
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Confirm password is required")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public UserDto? User { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? AvatarUrl { get; set; }
    public string ThemePreference { get; set; } = "light";
}

// ==========================================
// SKILL DTOs
// ==========================================

public class SkillDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BigGoal { get; set; }
    public decimal MasteryPercentage { get; set; }
    public decimal TotalHoursLogged { get; set; }
    public decimal? TargetHours { get; set; }
    public string Visibility { get; set; } = "private";
    public DateTime? TargetDate { get; set; }
    public string Status { get; set; } = "not_started";
    public string? Icon { get; set; }
    public string? Color { get; set; }
    
    public CategoryDto? Category { get; set; }
    public int GoalsCount { get; set; }
    public int CompletedGoalsCount { get; set; }
    public int LogsCount { get; set; }
    public List<ProgressLogDto> Logs { get; set; } = new();
}

public class CreateSkillRequest
{
    [Required(ErrorMessage = "Skill name is required")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    public string? BigGoal { get; set; }
    public int? CategoryId { get; set; }
    public decimal? TargetHours { get; set; }
    public DateTime? TargetDate { get; set; }
    public string Visibility { get; set; } = "private";
    public string? Icon { get; set; }
    public string? Color { get; set; }
    
    // Schedule
    public ScheduleRequest? Schedule { get; set; }
    
    // Sub-goals
    public List<CreateGoalRequest> Goals { get; set; } = new();
}

public class UpdateSkillRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? BigGoal { get; set; }
    public int? CategoryId { get; set; }
    public decimal? TargetHours { get; set; }
    public DateTime? TargetDate { get; set; }
    public string? Visibility { get; set; }
    public string? Status { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
}

public class ScheduleRequest
{
    public string Frequency { get; set; } = "weekly";
    public decimal HoursPerPeriod { get; set; } = 5;
    public string PreferredTimeSlot { get; set; } = "any";
    public List<ScheduleDayRequest> Days { get; set; } = new();
}

public class ScheduleDayRequest
{
    public string DayOfWeek { get; set; } = string.Empty;
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? Label { get; set; }
}

// ==========================================
// GOAL DTOs
// ==========================================

public class GoalDto
{
    public int Id { get; set; }
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal ProgressPercentage { get; set; }
    public decimal? TargetHours { get; set; }
    public decimal LoggedHours { get; set; }
    public DateTime? TargetDate { get; set; }
    public string Status { get; set; } = "pending";
    public bool IsAiGenerated { get; set; }
    public int MilestonesCount { get; set; }
    public int CompletedMilestonesCount { get; set; }
    public List<MilestoneDto> Milestones { get; set; } = new();
}

public class CreateGoalRequest
{
    [Required(ErrorMessage = "Goal title is required")]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    public decimal? TargetHours { get; set; }
    public DateTime? TargetDate { get; set; }
    public int SortOrder { get; set; } = 0;
    
    public List<CreateMilestoneRequest> Milestones { get; set; } = new();
}

public class UpdateGoalRequest
{
    [MaxLength(300)]
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal? TargetHours { get; set; }
    public DateTime? TargetDate { get; set; }
    public string? Status { get; set; }
    public int? SortOrder { get; set; }
}

// ==========================================
// MILESTONE DTOs
// ==========================================

public class MilestoneDto
{
    public int Id { get; set; }
    public int GoalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsAiGenerated { get; set; }
}

public class CreateMilestoneRequest
{
    [Required(ErrorMessage = "Milestone title is required")]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int SortOrder { get; set; } = 0;
}

// ==========================================
// PROGRESS LOG DTOs
// ==========================================

public class ProgressLogDto
{
    public int Id { get; set; }
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int? GoalId { get; set; }
    public string? GoalTitle { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal HoursLogged { get; set; }
    public DateTime LogDate { get; set; }
    public int? QualityRating { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<int> CompletedMilestoneIds { get; set; } = new();
}

public class CreateProgressLogRequest
{
    [Required]
    public int SkillId { get; set; }
    
    public int? GoalId { get; set; }
    
    [MaxLength(300)]
    public string? Title { get; set; }
    
    public string? Description { get; set; }
    
    [Required]
    [Range(0.01, 24, ErrorMessage = "Hours must be between 0.01 and 24")]
    public decimal HoursLogged { get; set; }
    
    [Required]
    public DateTime LogDate { get; set; }
    
    [Range(1, 5)]
    public int? QualityRating { get; set; }
    
    public List<int> CompletedMilestoneIds { get; set; } = new();
}

// ==========================================
// CATEGORY DTOs
// ==========================================

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsSystem { get; set; }
}

// ==========================================
// AI PLAN DTOs
// ==========================================

public class AiPlanRequest
{
    [Required]
    public string GoalDescription { get; set; } = string.Empty;
    
    public DateTime? TargetDate { get; set; }
    public string? Frequency { get; set; }
    public decimal? HoursPerPeriod { get; set; }
    public string? PreferredTimeSlot { get; set; }
    
    public List<ClarificationDto> Clarifications { get; set; } = new();
}

public class ClarificationDto
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

public class AiPlanResponse
{
    public bool Success { get; set; }
    public bool NeedsClarification { get; set; }
    public string? ClarificationQuestion { get; set; }
    public AiGeneratedSkillPlan? Plan { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AiGeneratedSkillPlan
{
    public string SkillName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BigGoal { get; set; }
    public string? Category { get; set; }
    public List<AiGeneratedGoal> Goals { get; set; } = new();
}

public class AiGeneratedGoal
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int WeekNumber { get; set; }
    public List<string> Milestones { get; set; } = new();
}

// ==========================================
// PROFILE DTOs
// ==========================================

public class UpdateProfileRequest
{
    [MaxLength(100)]
    public string? FirstName { get; set; }
    
    [MaxLength(100)]
    public string? LastName { get; set; }
    
    [MaxLength(150)]
    public string? JobTitle { get; set; }
    
    public string? Bio { get; set; }
    
    public string? ThemePreference { get; set; }
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string NewPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Confirm password is required")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class SharingSettingsDto
{
    public bool IsProfilePublic { get; set; }
    public string? PublicUsername { get; set; }
    public bool ShowSkills { get; set; }
    public bool ShowProgress { get; set; }
    public bool ShowGoals { get; set; }
    public bool ShowStatistics { get; set; }
}

// ==========================================
// STATISTICS DTOs
// ==========================================

public class DashboardStatsDto
{
    public int TotalSkills { get; set; }
    public int ActiveSkills { get; set; }
    public int CompletedGoals { get; set; }
    public int TotalGoals { get; set; }
    public decimal TotalHoursThisWeek { get; set; }
    public decimal TotalHoursAllTime { get; set; }
    public int CurrentStreak { get; set; }
    public decimal OverallProgress { get; set; }
}

public class WeeklyActivityDto
{
    public int WeekNumber { get; set; }
    public string WeekLabel { get; set; } = string.Empty;
    public decimal HoursLogged { get; set; }
}

// ==========================================
// COMMON RESPONSE
// ==========================================

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    
    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };
    
    public static ApiResponse<T> Fail(string message, List<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        Errors = errors ?? new List<string>()
    };
}

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
