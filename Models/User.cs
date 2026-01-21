using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSE325FinalProject.Models;

/// <summary>
/// User account entity
/// </summary>
public class User
{
    [Key]
    public int Id { get; set; }
    
    [Required, MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [MaxLength(150)]
    public string? JobTitle { get; set; }
    
    public string? Bio { get; set; }
    
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
    
    public bool IsEmailVerified { get; set; } = false;
    public bool IsActive { get; set; } = true;
    
    [MaxLength(20)]
    public string ThemePreference { get; set; } = "light";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public virtual ICollection<Skill> Skills { get; set; } = new List<Skill>();
    public virtual ICollection<Goal> Goals { get; set; } = new List<Goal>();
    public virtual ICollection<ProgressLog> ProgressLogs { get; set; } = new List<ProgressLog>();
    public virtual UserSharingSettings? SharingSettings { get; set; }
    
    // Computed properties
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
    
    [NotMapped]
    public string Initials => $"{FirstName?.FirstOrDefault()}{LastName?.FirstOrDefault()}".ToUpper();
}

/// <summary>
/// Active user sessions for device tracking
/// </summary>
public class UserSession
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required, MaxLength(500)]
    public string SessionToken { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? DeviceName { get; set; }
    
    [MaxLength(20)]
    public string DeviceType { get; set; } = "unknown";
    
    [MaxLength(100)]
    public string? Browser { get; set; }
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    [MaxLength(200)]
    public string? Location { get; set; }
    
    public bool IsCurrent { get; set; } = false;
    
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}

/// <summary>
/// Password reset tokens
/// </summary>
public class PasswordResetToken
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required, MaxLength(255)]
    public string Token { get; set; } = string.Empty;
    
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}

/// <summary>
/// User sharing and visibility settings
/// </summary>
public class UserSharingSettings
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public bool IsProfilePublic { get; set; } = false;
    
    [MaxLength(100)]
    public string? PublicUsername { get; set; }
    
    public bool ShowSkills { get; set; } = true;
    public bool ShowProgress { get; set; } = true;
    public bool ShowGoals { get; set; } = false;
    public bool ShowStatistics { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
