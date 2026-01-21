using Microsoft.EntityFrameworkCore;
using CSE325FinalProject.Models;

namespace CSE325FinalProject.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    // User & Auth
    public DbSet<User> Users { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<UserSharingSettings> UserSharingSettings { get; set; }
    
    // Skills & Goals
    public DbSet<Category> Categories { get; set; }
    public DbSet<Skill> Skills { get; set; }
    public DbSet<Goal> Goals { get; set; }
    public DbSet<Milestone> Milestones { get; set; }
    
    // Progress Tracking
    public DbSet<ProgressLog> ProgressLogs { get; set; }
    public DbSet<MilestoneCompletion> MilestoneCompletions { get; set; }
    
    // Schedules
    public DbSet<SkillSchedule> SkillSchedules { get; set; }
    public DbSet<ScheduleDay> ScheduleDays { get; set; }
    
    // AI
    public DbSet<AiGeneratedPlan> AiGeneratedPlans { get; set; }
    
    // Stats
    public DbSet<DailyStat> DailyStats { get; set; }
    public DbSet<WeeklyStat> WeeklyStats { get; set; }
    
    // Notifications
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // ==========================================
        // Table mapping to match SQL schema
        // ==========================================
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<UserSession>().ToTable("user_sessions");
        modelBuilder.Entity<PasswordResetToken>().ToTable("password_reset_tokens");
        modelBuilder.Entity<UserSharingSettings>().ToTable("user_sharing_settings");
        modelBuilder.Entity<Category>().ToTable("categories");
        modelBuilder.Entity<Skill>().ToTable("skills");
        modelBuilder.Entity<Goal>().ToTable("goals");
        modelBuilder.Entity<Milestone>().ToTable("milestones");
        modelBuilder.Entity<ProgressLog>().ToTable("progress_logs");
        modelBuilder.Entity<MilestoneCompletion>().ToTable("milestone_completions");
        modelBuilder.Entity<SkillSchedule>().ToTable("skill_schedules");
        modelBuilder.Entity<ScheduleDay>().ToTable("schedule_days");
        modelBuilder.Entity<AiGeneratedPlan>().ToTable("ai_generated_plans");
        modelBuilder.Entity<DailyStat>().ToTable("daily_stats");
        modelBuilder.Entity<WeeklyStat>().ToTable("weekly_stats");
        modelBuilder.Entity<Notification>().ToTable("notifications");
        
        // ==========================================
        // User configurations
        // ==========================================
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.JobTitle).HasColumnName("job_title");
            entity.Property(e => e.Bio).HasColumnName("bio");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.IsEmailVerified).HasColumnName("is_email_verified");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.ThemePreference).HasColumnName("theme_preference");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
        });
        
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.SessionToken).HasColumnName("session_token");
            entity.Property(e => e.DeviceName).HasColumnName("device_name");
            entity.Property(e => e.DeviceType).HasColumnName("device_type");
            entity.Property(e => e.Browser).HasColumnName("browser");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.Location).HasColumnName("location");
            entity.Property(e => e.IsCurrent).HasColumnName("is_current");
            entity.Property(e => e.LastActiveAt).HasColumnName("last_active_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
        
        modelBuilder.Entity<UserSharingSettings>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.PublicUsername).IsUnique();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.IsProfilePublic).HasColumnName("is_profile_public");
            entity.Property(e => e.PublicUsername).HasColumnName("public_username");
            entity.Property(e => e.ShowSkills).HasColumnName("show_skills");
            entity.Property(e => e.ShowProgress).HasColumnName("show_progress");
            entity.Property(e => e.ShowGoals).HasColumnName("show_goals");
            entity.Property(e => e.ShowStatistics).HasColumnName("show_statistics");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });
        
        // ==========================================
        // Category configurations
        // ==========================================
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Icon).HasColumnName("icon");
            entity.Property(e => e.Color).HasColumnName("color");
            entity.Property(e => e.IsSystem).HasColumnName("is_system");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
        
        // ==========================================
        // Skill configurations
        // ==========================================
        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasIndex(e => e.PublicSlug).IsUnique();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.BigGoal).HasColumnName("big_goal");
            entity.Property(e => e.MasteryPercentage).HasColumnName("mastery_percentage");
            entity.Property(e => e.TotalHoursLogged).HasColumnName("total_hours_logged");
            entity.Property(e => e.TargetHours).HasColumnName("target_hours");
            entity.Property(e => e.Visibility).HasColumnName("visibility");
            entity.Property(e => e.PublicSlug).HasColumnName("public_slug");
            entity.Property(e => e.TargetDate).HasColumnName("target_date");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Icon).HasColumnName("icon");
            entity.Property(e => e.Color).HasColumnName("color");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });
        
        // ==========================================
        // Goal configurations
        // ==========================================
        modelBuilder.Entity<Goal>(entity =>
        {
            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ProgressPercentage).HasColumnName("progress_percentage");
            entity.Property(e => e.TargetHours).HasColumnName("target_hours");
            entity.Property(e => e.LoggedHours).HasColumnName("logged_hours");
            entity.Property(e => e.TargetDate).HasColumnName("target_date");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.IsAiGenerated).HasColumnName("is_ai_generated");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });
        
        // ==========================================
        // Milestone configurations
        // ==========================================
        modelBuilder.Entity<Milestone>(entity =>
        {
            entity.Property(e => e.GoalId).HasColumnName("goal_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.IsCompleted).HasColumnName("is_completed");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.IsAiGenerated).HasColumnName("is_ai_generated");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });
        
        // ==========================================
        // Progress Log configurations
        // ==========================================
        modelBuilder.Entity<ProgressLog>(entity =>
        {
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.GoalId).HasColumnName("goal_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.HoursLogged).HasColumnName("hours_logged");
            entity.Property(e => e.LogDate).HasColumnName("log_date");
            entity.Property(e => e.QualityRating).HasColumnName("quality_rating");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });
        
        modelBuilder.Entity<MilestoneCompletion>(entity =>
        {
            entity.HasIndex(e => new { e.ProgressLogId, e.MilestoneId }).IsUnique();
            entity.Property(e => e.ProgressLogId).HasColumnName("progress_log_id");
            entity.Property(e => e.MilestoneId).HasColumnName("milestone_id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
        });
        
        // ==========================================
        // Schedule configurations
        // ==========================================
        modelBuilder.Entity<SkillSchedule>(entity =>
        {
            entity.HasIndex(e => e.SkillId).IsUnique();
            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.Frequency).HasColumnName("frequency");
            entity.Property(e => e.HoursPerPeriod).HasColumnName("hours_per_period");
            entity.Property(e => e.PreferredTimeSlot).HasColumnName("preferred_time_slot");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });
        
        modelBuilder.Entity<ScheduleDay>(entity =>
        {
            entity.HasIndex(e => new { e.SkillScheduleId, e.DayOfWeek }).IsUnique();
            entity.Property(e => e.SkillScheduleId).HasColumnName("skill_schedule_id");
            entity.Property(e => e.DayOfWeek).HasColumnName("day_of_week");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.Label).HasColumnName("label");
        });
        
        // ==========================================
        // AI Plan configurations
        // ==========================================
        modelBuilder.Entity<AiGeneratedPlan>(entity =>
        {
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.GoalDescription).HasColumnName("goal_description");
            entity.Property(e => e.TargetDate).HasColumnName("target_date");
            entity.Property(e => e.DedicationFrequency).HasColumnName("dedication_frequency");
            entity.Property(e => e.DedicationHours).HasColumnName("dedication_hours");
            entity.Property(e => e.PreferredTimeSlot).HasColumnName("preferred_time_slot");
            entity.Property(e => e.Clarification1Question).HasColumnName("clarification_1_question");
            entity.Property(e => e.Clarification1Answer).HasColumnName("clarification_1_answer");
            entity.Property(e => e.Clarification2Question).HasColumnName("clarification_2_question");
            entity.Property(e => e.Clarification2Answer).HasColumnName("clarification_2_answer");
            entity.Property(e => e.AiResponseJson).HasColumnName("ai_response_json");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });
        
        // ==========================================
        // Stats configurations
        // ==========================================
        modelBuilder.Entity<DailyStat>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.StatDate }).IsUnique();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.StatDate).HasColumnName("stat_date");
            entity.Property(e => e.TotalHoursLogged).HasColumnName("total_hours_logged");
            entity.Property(e => e.SkillsPracticed).HasColumnName("skills_practiced");
            entity.Property(e => e.GoalsCompleted).HasColumnName("goals_completed");
            entity.Property(e => e.MilestonesCompleted).HasColumnName("milestones_completed");
            entity.Property(e => e.LogsCount).HasColumnName("logs_count");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
        
        modelBuilder.Entity<WeeklyStat>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Year, e.WeekNumber }).IsUnique();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WeekStart).HasColumnName("week_start");
            entity.Property(e => e.WeekNumber).HasColumnName("week_number");
            entity.Property(e => e.Year).HasColumnName("year");
            entity.Property(e => e.TotalHoursLogged).HasColumnName("total_hours_logged");
            entity.Property(e => e.SkillsPracticed).HasColumnName("skills_practiced");
            entity.Property(e => e.GoalsCompleted).HasColumnName("goals_completed");
            entity.Property(e => e.ActiveDays).HasColumnName("active_days");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
        
        // ==========================================
        // Notification configurations
        // ==========================================
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.ActionUrl).HasColumnName("action_url");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.ReadAt).HasColumnName("read_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
    }
}
