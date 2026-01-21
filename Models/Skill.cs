using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSE325FinalProject.Models;

/// <summary>
/// Skill categories (Programming, Languages, Music, etc.)
/// </summary>
public class Category
{
    [Key]
    public int Id { get; set; }
    
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [MaxLength(50)]
    public string? Icon { get; set; }
    
    [MaxLength(20)]
    public string? Color { get; set; }
    
    public bool IsSystem { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public virtual ICollection<Skill> Skills { get; set; } = new List<Skill>();
}

/// <summary>
/// Main skill tracking entity
/// </summary>
public class Skill
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public int? CategoryId { get; set; }
    
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string? BigGoal { get; set; }
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal MasteryPercentage { get; set; } = 0;
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalHoursLogged { get; set; } = 0;
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal? TargetHours { get; set; }
    
    [MaxLength(20)]
    public string Visibility { get; set; } = "private";
    
    [MaxLength(100)]
    public string? PublicSlug { get; set; }
    
    public DateTime? TargetDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    [MaxLength(30)]
    public string Status { get; set; } = "not_started";
    
    [MaxLength(50)]
    public string? Icon { get; set; }
    
    [MaxLength(20)]
    public string? Color { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
    
    [ForeignKey("CategoryId")]
    public virtual Category? Category { get; set; }
    
    public virtual ICollection<Goal> Goals { get; set; } = new List<Goal>();
    public virtual ICollection<ProgressLog> ProgressLogs { get; set; } = new List<ProgressLog>();
    public virtual SkillSchedule? Schedule { get; set; }
    
    // Computed
    [NotMapped]
    public bool IsPublic => Visibility == "public";
    
    [NotMapped]
    public int GoalsCount => Goals?.Count ?? 0;
    
    [NotMapped]
    public int CompletedGoalsCount => Goals?.Count(g => g.Status == "completed") ?? 0;
}

/// <summary>
/// Goals/objectives within a skill
/// </summary>
public class Goal
{
    [Key]
    public int Id { get; set; }
    
    public int SkillId { get; set; }
    public int UserId { get; set; }
    
    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal ProgressPercentage { get; set; } = 0;
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal? TargetHours { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal LoggedHours { get; set; } = 0;
    
    public DateTime? TargetDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    [MaxLength(30)]
    public string Status { get; set; } = "pending";
    
    public int Priority { get; set; } = 0;
    public int SortOrder { get; set; } = 0;
    
    public bool IsAiGenerated { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    [ForeignKey("SkillId")]
    public virtual Skill? Skill { get; set; }
    
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
    
    public virtual ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public virtual ICollection<ProgressLog> ProgressLogs { get; set; } = new List<ProgressLog>();
    
    // Computed
    [NotMapped]
    public int MilestonesCount => Milestones?.Count ?? 0;
    
    [NotMapped]
    public int CompletedMilestonesCount => Milestones?.Count(m => m.IsCompleted) ?? 0;
}

/// <summary>
/// Milestones/minimetas within goals
/// </summary>
public class Milestone
{
    [Key]
    public int Id { get; set; }
    
    public int GoalId { get; set; }
    public int UserId { get; set; }
    
    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [MaxLength(100)]
    public string? Category { get; set; }
    
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
    
    public int SortOrder { get; set; } = 0;
    
    public bool IsAiGenerated { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    [ForeignKey("GoalId")]
    public virtual Goal? Goal { get; set; }
    
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
