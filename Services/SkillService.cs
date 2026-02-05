using Microsoft.EntityFrameworkCore;
using CSE325FinalProject.Data;
using CSE325FinalProject.Models;
using CSE325FinalProject.Models.DTOs;

namespace CSE325FinalProject.Services;

public interface ISkillService
{
    Task<List<SkillDto>> GetUserSkillsAsync(int userId, string? visibility = null, string? status = null);
    Task<SkillDto?> GetSkillByIdAsync(int skillId, int userId);
    Task<SkillDto?> GetSkillForDetailsAsync(int skillId, int? userId);
    Task<SkillDto?> GetSkillBySlugAsync(string slug);
    Task<SkillDto> CreateSkillAsync(int userId, CreateSkillRequest request);
    Task<SkillDto?> UpdateSkillAsync(int skillId, int userId, UpdateSkillRequest request);
    Task<bool> DeleteSkillAsync(int skillId, int userId);
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task UpdateSkillProgressAsync(int skillId);
}

public class SkillService : ISkillService
{
    private readonly ApplicationDbContext _context;
    
    public SkillService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<SkillDto>> GetUserSkillsAsync(int userId, string? visibility = null, string? status = null)
    {
        var query = _context.Skills
            .Include(s => s.Category)
            .Include(s => s.User)
            .Include(s => s.Goals)
            .Include(s => s.ProgressLogs)
            .Where(s => s.UserId == userId);
        
        if (!string.IsNullOrEmpty(visibility))
            query = query.Where(s => s.Visibility == visibility);
        
        if (!string.IsNullOrEmpty(status))
            query = query.Where(s => s.Status == status);
        
        var skills = await query
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();
        
        return skills.Select(MapToDto).ToList();
    }
    
    public async Task<SkillDto?> GetSkillByIdAsync(int skillId, int userId)
    {
        var skill = await _context.Skills
            .Include(s => s.User)
            .Include(s => s.Category)
            .Include(s => s.Goals)
                .ThenInclude(g => g.Milestones)
            .Include(s => s.ProgressLogs)
            .FirstOrDefaultAsync(s => s.Id == skillId && s.UserId == userId);
        
        return skill != null ? MapToDto(skill) : null;
    }
    
    public async Task<SkillDto?> GetSkillForDetailsAsync(int skillId, int? userId)
    {
        var skill = await _context.Skills
            .Include(s => s.User)
            .Include(s => s.Category)
            .Include(s => s.Goals)
                .ThenInclude(g => g.Milestones)
            .Include(s => s.ProgressLogs)
            .FirstOrDefaultAsync(s => s.Id == skillId);
        
        if (skill == null) return null;
        
        // Check access: Public OR Owner
        var isOwner = userId.HasValue && skill.UserId == userId.Value;
        var isPublic = skill.Visibility == "public";
        
        if (!isPublic && !isOwner) return null;
        
        return MapToDto(skill);
    }
    
    public async Task<SkillDto?> GetSkillBySlugAsync(string slug)
    {
        var skill = await _context.Skills
            .Include(s => s.Category)
            .Include(s => s.Goals)
            .FirstOrDefaultAsync(s => s.PublicSlug == slug && s.Visibility == "public");
        
        return skill != null ? MapToDto(skill) : null;
    }
    
    public async Task<SkillDto> CreateSkillAsync(int userId, CreateSkillRequest request)
    {
        var skill = new Skill
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            BigGoal = request.BigGoal?.Trim(),
            TargetHours = request.TargetHours,
            TargetDate = request.TargetDate,
            Visibility = request.Visibility,
            Icon = request.Icon,
            Color = request.Color,
            Status = "in_progress",
            StartedAt = DateTime.UtcNow,
            PublicSlug = request.Visibility == "public" ? GenerateSlug(request.Name) : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();
        
        // Create schedule if provided
        if (request.Schedule != null)
        {
            var schedule = new SkillSchedule
            {
                SkillId = skill.Id,
                Frequency = request.Schedule.Frequency,
                HoursPerPeriod = request.Schedule.HoursPerPeriod,
                PreferredTimeSlot = request.Schedule.PreferredTimeSlot,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _context.SkillSchedules.Add(schedule);
            await _context.SaveChangesAsync();
            
            // Add schedule days
            if (request.Schedule.Days.Any())
            {
                foreach (var dayRequest in request.Schedule.Days)
                {
                    var scheduleDay = new ScheduleDay
                    {
                        SkillScheduleId = schedule.Id,
                        DayOfWeek = dayRequest.DayOfWeek,
                        StartTime = !string.IsNullOrEmpty(dayRequest.StartTime) 
                            ? TimeSpan.Parse(dayRequest.StartTime) : null,
                        EndTime = !string.IsNullOrEmpty(dayRequest.EndTime) 
                            ? TimeSpan.Parse(dayRequest.EndTime) : null,
                        Label = dayRequest.Label
                    };
                    _context.ScheduleDays.Add(scheduleDay);
                }
                await _context.SaveChangesAsync();
            }
        }
        
        // Create goals if provided
        if (request.Goals.Any())
        {
            int sortOrder = 0;
            foreach (var goalRequest in request.Goals)
            {
                var goal = new Goal
                {
                    SkillId = skill.Id,
                    UserId = userId,
                    Title = goalRequest.Title.Trim(),
                    Description = goalRequest.Description?.Trim(),
                    TargetHours = goalRequest.TargetHours,
                    TargetDate = goalRequest.TargetDate,
                    Status = "pending",
                    SortOrder = sortOrder++,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                _context.Goals.Add(goal);
                await _context.SaveChangesAsync();
                
                // Create milestones
                if (goalRequest.Milestones.Any())
                {
                    int milestoneOrder = 0;
                    foreach (var milestoneRequest in goalRequest.Milestones)
                    {
                        var milestone = new Milestone
                        {
                            GoalId = goal.Id,
                            UserId = userId,
                            Title = milestoneRequest.Title.Trim(),
                            Description = milestoneRequest.Description?.Trim(),
                            Category = milestoneRequest.Category,
                            SortOrder = milestoneOrder++,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.Milestones.Add(milestone);
                    }
                    await _context.SaveChangesAsync();
                }
            }
        }
        
        return (await GetSkillByIdAsync(skill.Id, userId))!;
    }
    
    public async Task<SkillDto?> UpdateSkillAsync(int skillId, int userId, UpdateSkillRequest request)
    {
        var skill = await _context.Skills.FirstOrDefaultAsync(s => s.Id == skillId && s.UserId == userId);
        
        if (skill == null) return null;
        
        if (request.Name != null) skill.Name = request.Name.Trim();
        if (request.Description != null) skill.Description = request.Description.Trim();
        if (request.BigGoal != null) skill.BigGoal = request.BigGoal.Trim();
        if (request.CategoryId != null) skill.CategoryId = request.CategoryId;
        if (request.TargetHours != null) skill.TargetHours = request.TargetHours;
        if (request.TargetDate != null) skill.TargetDate = request.TargetDate;
        if (request.Visibility != null)
        {
            skill.Visibility = request.Visibility;
            if (request.Visibility == "public" && string.IsNullOrEmpty(skill.PublicSlug))
            {
                skill.PublicSlug = GenerateSlug(skill.Name);
            }
        }
        if (request.Status != null)
        {
            skill.Status = request.Status;
            if (request.Status == "completed" && !skill.CompletedAt.HasValue)
            {
                skill.CompletedAt = DateTime.UtcNow;
            }
        }
        if (request.Icon != null) skill.Icon = request.Icon;
        if (request.Color != null) skill.Color = request.Color;
        
        skill.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return await GetSkillByIdAsync(skillId, userId);
    }
    
    public async Task<bool> DeleteSkillAsync(int skillId, int userId)
    {
        var skill = await _context.Skills.FirstOrDefaultAsync(s => s.Id == skillId && s.UserId == userId);
        
        if (skill == null) return false;
        
        _context.Skills.Remove(skill);
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();
        
        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Icon = c.Icon,
            Color = c.Color,
            IsSystem = c.IsSystem
        }).ToList();
    }
    
    public async Task UpdateSkillProgressAsync(int skillId)
    {
        var skill = await _context.Skills
            .Include(s => s.Goals)
            .Include(s => s.ProgressLogs)
            .FirstOrDefaultAsync(s => s.Id == skillId);
        
        if (skill == null) return;
        
        // Calculate total hours
        skill.TotalHoursLogged = skill.ProgressLogs.Sum(p => p.HoursLogged);
        
        // Calculate mastery percentage
        if (skill.TargetHours.HasValue && skill.TargetHours > 0)
        {
            skill.MasteryPercentage = Math.Min(100, (skill.TotalHoursLogged / skill.TargetHours.Value) * 100);
        }
        else if (skill.Goals.Any())
        {
            var completedGoals = skill.Goals.Count(g => g.Status == "completed");
            skill.MasteryPercentage = (completedGoals / (decimal)skill.Goals.Count) * 100;
        }
        
        skill.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
    }
    
    private string GenerateSlug(string name)
    {
        var slug = name.ToLower()
            .Replace(" ", "-")
            .Replace("_", "-");
        
        // Remove special characters
        slug = System.Text.RegularExpressions.Regex.Replace(slug, "[^a-z0-9-]", "");
        
        // Add random suffix to ensure uniqueness
        var random = new Random().Next(1000, 9999);
        return $"{slug}-{random}";
    }
    
    private SkillDto MapToDto(Skill skill)
    {
        return new SkillDto
        {
            Id = skill.Id,
            UserId = skill.UserId,
            OwnerName = skill.User != null ? $"{skill.User.FirstName} {skill.User.LastName}".Trim() : "Unknown User",
            Name = skill.Name,
            Description = skill.Description,
            BigGoal = skill.BigGoal,
            MasteryPercentage = skill.MasteryPercentage,
            TotalHoursLogged = skill.TotalHoursLogged,
            TargetHours = skill.TargetHours,
            Visibility = skill.Visibility,
            TargetDate = skill.TargetDate,
            Status = skill.Status,
            Icon = skill.Icon,
            Color = skill.Color,
            Category = skill.Category != null ? new CategoryDto
            {
                Id = skill.Category.Id,
                Name = skill.Category.Name,
                Description = skill.Category.Description,
                Icon = skill.Category.Icon,
                Color = skill.Category.Color,
                IsSystem = skill.Category.IsSystem
            } : null,
            GoalsCount = skill.Goals?.Count ?? 0,
            CompletedGoalsCount = skill.Goals?.Count(g => g.Status == "completed") ?? 0,
            LogsCount = skill.ProgressLogs?.Count ?? 0,
            Logs = skill.ProgressLogs?
                .OrderByDescending(l => l.LogDate)
                .Select(l => new ProgressLogDto
                {
                    Id = l.Id,
                    SkillId = l.SkillId,
                    SkillName = skill.Name,
                    GoalId = l.GoalId,
                    GoalTitle = l.Goal?.Title,
                    Title = l.Title,
                    Description = l.Description,
                    HoursLogged = l.HoursLogged,
                    LogDate = l.LogDate,
                    QualityRating = l.QualityRating,
                    CreatedAt = l.CreatedAt
                }).ToList() ?? new List<ProgressLogDto>()
        };
    }
}
