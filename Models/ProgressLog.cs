using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSE325FinalProject.Models;

/// <summary>
/// Progress log entries for tracking daily activity
/// </summary>
public class ProgressLog
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public int SkillId { get; set; }
    public int? GoalId { get; set; }
    
    [MaxLength(300)]
    public string? Title { get; set; }
    
    public string? Description { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal HoursLogged { get; set; }
    
    [Required]
    public DateTime LogDate { get; set; }
    
    public int? QualityRating { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
    
    [ForeignKey("SkillId")]
    public virtual Skill? Skill { get; set; }
    
    [ForeignKey("GoalId")]
    public virtual Goal? Goal { get; set; }
    
    public virtual ICollection<MilestoneCompletion> MilestoneCompletions { get; set; } = new List<MilestoneCompletion>();
}

/// <summary>
/// Junction table for milestone completions in progress logs
/// </summary>
public class MilestoneCompletion
{
    [Key]
    public int Id { get; set; }
    
    public int ProgressLogId { get; set; }
    public int MilestoneId { get; set; }
    
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey("ProgressLogId")]
    public virtual ProgressLog? ProgressLog { get; set; }
    
    [ForeignKey("MilestoneId")]
    public virtual Milestone? Milestone { get; set; }
}

/// <summary>
/// Skill schedule configuration
/// </summary>
public class SkillSchedule
{
    [Key]
    public int Id { get; set; }
    
    public int SkillId { get; set; }
    
    [MaxLength(20)]
    public string Frequency { get; set; } = "weekly";
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal HoursPerPeriod { get; set; } = 5;
    
    [MaxLength(20)]
    public string PreferredTimeSlot { get; set; } = "any";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey("SkillId")]
    public virtual Skill? Skill { get; set; }
    
    public virtual ICollection<ScheduleDay> ScheduleDays { get; set; } = new List<ScheduleDay>();
}

/// <summary>
/// Specific days in a skill schedule
/// </summary>
public class ScheduleDay
{
    [Key]
    public int Id { get; set; }
    
    public int SkillScheduleId { get; set; }
    
    [Required, MaxLength(20)]
    public string DayOfWeek { get; set; } = string.Empty;
    
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    
    [MaxLength(100)]
    public string? Label { get; set; }
    
    [ForeignKey("SkillScheduleId")]
    public virtual SkillSchedule? SkillSchedule { get; set; }
}

/// <summary>
/// AI generated plans storage
/// </summary>
public class AiGeneratedPlan
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public int? SkillId { get; set; }
    
    [Required]
    public string GoalDescription { get; set; } = string.Empty;
    
    public DateTime? TargetDate { get; set; }
    
    [MaxLength(20)]
    public string? DedicationFrequency { get; set; }
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal? DedicationHours { get; set; }
    
    [MaxLength(50)]
    public string? PreferredTimeSlot { get; set; }
    
    public string? Clarification1Question { get; set; }
    public string? Clarification1Answer { get; set; }
    public string? Clarification2Question { get; set; }
    public string? Clarification2Answer { get; set; }
    
    [Column(TypeName = "json")]
    public string? AiResponseJson { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "pending";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
    
    [ForeignKey("SkillId")]
    public virtual Skill? Skill { get; set; }
}

/// <summary>
/// Daily aggregated statistics
/// </summary>
public class DailyStat
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public DateTime StatDate { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalHoursLogged { get; set; } = 0;
    
    public int SkillsPracticed { get; set; } = 0;
    public int GoalsCompleted { get; set; } = 0;
    public int MilestonesCompleted { get; set; } = 0;
    public int LogsCount { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}

/// <summary>
/// Weekly aggregated statistics
/// </summary>
public class WeeklyStat
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public DateTime WeekStart { get; set; }
    public int WeekNumber { get; set; }
    public int Year { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalHoursLogged { get; set; } = 0;
    
    public int SkillsPracticed { get; set; } = 0;
    public int GoalsCompleted { get; set; } = 0;
    public int ActiveDays { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}

/// <summary>
/// User notifications
/// </summary>
public class Notification
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required, MaxLength(30)]
    public string Type { get; set; } = "system";
    
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    public string? Message { get; set; }
    
    [MaxLength(500)]
    public string? ActionUrl { get; set; }
    
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
