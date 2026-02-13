using Microsoft.EntityFrameworkCore;
using CSE325FinalProject.Data;
using CSE325FinalProject.Models;
using CSE325FinalProject.Models.DTOs;

namespace CSE325FinalProject.Services;

public interface IProgressLogService
{
    Task<PagedResponse<ProgressLogDto>> GetUserLogsAsync(int userId, int page = 1, int pageSize = 10, int? skillId = null, int? goalId = null);
    Task<ProgressLogDto?> GetLogByIdAsync(int logId, int userId);
    Task<ProgressLogDto> CreateLogAsync(int userId, CreateProgressLogRequest request);
    Task<bool> DeleteLogAsync(int logId, int userId);
    Task<DashboardStatsDto> GetDashboardStatsAsync(int userId);
    Task<List<WeeklyActivityDto>> GetWeeklyActivityAsync(int userId, int weeks = 12);
}

public class ProgressLogService : IProgressLogService
{
    private readonly ApplicationDbContext _context;
    private readonly IGoalService _goalService;
    private readonly ISkillService _skillService;
    
    public ProgressLogService(ApplicationDbContext context, IGoalService goalService, ISkillService skillService)
    {
        _context = context;
        _goalService = goalService;
        _skillService = skillService;
    }
    
    public async Task<PagedResponse<ProgressLogDto>> GetUserLogsAsync(int userId, int page = 1, int pageSize = 10, int? skillId = null, int? goalId = null)
    {
        var query = _context.ProgressLogs
            .Include(p => p.Skill)
            .Include(p => p.Goal)
            .Include(p => p.MilestoneCompletions)
            .Where(p => p.UserId == userId);
        
        if (skillId.HasValue)
            query = query.Where(p => p.SkillId == skillId.Value);
        
        if (goalId.HasValue)
            query = query.Where(p => p.GoalId == goalId.Value);
        
        var totalItems = await query.CountAsync();
        
        var logs = await query
            .OrderByDescending(p => p.LogDate)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return new PagedResponse<ProgressLogDto>
        {
            Items = logs.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }
    
    public async Task<ProgressLogDto?> GetLogByIdAsync(int logId, int userId)
    {
        var log = await _context.ProgressLogs
            .Include(p => p.Skill)
            .Include(p => p.Goal)
            .Include(p => p.MilestoneCompletions)
            .FirstOrDefaultAsync(p => p.Id == logId && p.UserId == userId);
        
        return log != null ? MapToDto(log) : null;
    }
    
    public async Task<ProgressLogDto> CreateLogAsync(int userId, CreateProgressLogRequest request)
    {
        // Verify skill belongs to user
        var skill = await _context.Skills.FirstOrDefaultAsync(s => s.Id == request.SkillId && s.UserId == userId);
        if (skill == null)
        {
            throw new InvalidOperationException("Skill not found");
        }
        
        // Verify goal if provided
        if (request.GoalId.HasValue)
        {
            var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == request.GoalId && g.UserId == userId);
            if (goal == null)
            {
                throw new InvalidOperationException("Goal not found");
            }
        }
        
        var progressLog = new ProgressLog
        {
            UserId = userId,
            SkillId = request.SkillId,
            GoalId = request.GoalId,
            Title = request.Title?.Trim(),
            Description = request.Description?.Trim(),
            HoursLogged = request.HoursLogged,
            LogDate = request.LogDate.Date,
            QualityRating = request.QualityRating,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.ProgressLogs.Add(progressLog);
        await _context.SaveChangesAsync();
        
        // Mark milestones as completed
        if (request.CompletedMilestoneIds.Any())
        {
            foreach (var milestoneId in request.CompletedMilestoneIds)
            {
                var milestone = await _context.Milestones.FirstOrDefaultAsync(m => m.Id == milestoneId && m.UserId == userId);
                if (milestone != null && !milestone.IsCompleted)
                {
                    milestone.IsCompleted = true;
                    milestone.CompletedAt = DateTime.UtcNow;
                    milestone.UpdatedAt = DateTime.UtcNow;
                    
                    var completion = new MilestoneCompletion
                    {
                        ProgressLogId = progressLog.Id,
                        MilestoneId = milestoneId,
                        CompletedAt = DateTime.UtcNow
                    };
                    _context.MilestoneCompletions.Add(completion);
                }
            }
            await _context.SaveChangesAsync();
        }
        
        // Update goal progress
        if (request.GoalId.HasValue)
        {
            await _goalService.UpdateGoalProgressAsync(request.GoalId.Value);
        }
        
        // Update skill progress
        await _skillService.UpdateSkillProgressAsync(request.SkillId);
        
        // Update daily stats
        await UpdateDailyStatsAsync(userId, request.LogDate.Date);
        
        return (await GetLogByIdAsync(progressLog.Id, userId))!;
    }
    
    public async Task<bool> DeleteLogAsync(int logId, int userId)
    {
        var log = await _context.ProgressLogs.FirstOrDefaultAsync(p => p.Id == logId && p.UserId == userId);
        
        if (log == null) return false;
        
        var skillId = log.SkillId;
        var goalId = log.GoalId;
        var logDate = log.LogDate;
        
        _context.ProgressLogs.Remove(log);
        await _context.SaveChangesAsync();
        
        // Update goal progress
        if (goalId.HasValue)
        {
            await _goalService.UpdateGoalProgressAsync(goalId.Value);
        }
        
        // Update skill progress
        await _skillService.UpdateSkillProgressAsync(skillId);
        
        // Update daily stats
        await UpdateDailyStatsAsync(userId, logDate);
        
        return true;
    }
    
    public async Task<DashboardStatsDto> GetDashboardStatsAsync(int userId)
    {
        var skills = await _context.Skills.Where(s => s.UserId == userId).ToListAsync();
        var goals = await _context.Goals.Where(g => g.UserId == userId).ToListAsync();
        
        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
        var weekLogs = await _context.ProgressLogs
            .Where(p => p.UserId == userId && p.LogDate >= weekStart)
            .ToListAsync();
        
        var allLogs = await _context.ProgressLogs
            .Where(p => p.UserId == userId)
            .ToListAsync();
        
        // Calculate streak
        var streak = await CalculateStreakAsync(userId);
        
        // Overall progress
        var overallProgress = skills.Any() 
            ? skills.Average(s => s.MasteryPercentage) 
            : 0;
        
        return new DashboardStatsDto
        {
            TotalSkills = skills.Count,
            ActiveSkills = skills.Count(s => s.Status == "in_progress"),
            CompletedGoals = goals.Count(g => g.Status == "completed"),
            TotalGoals = goals.Count,
            TotalHoursThisWeek = weekLogs.Sum(l => l.HoursLogged),
            TotalHoursAllTime = allLogs.Sum(l => l.HoursLogged),
            CurrentStreak = streak,
            OverallProgress = overallProgress
        };
    }
    
    public async Task<List<WeeklyActivityDto>> GetWeeklyActivityAsync(int userId, int weeks = 12)
    {
        var result = new List<WeeklyActivityDto>();
        var today = DateTime.UtcNow.Date;
        
        // Align to Monday of current week
        var currentDayOfWeek = (int)today.DayOfWeek; // Sunday=0, Monday=1...
        var daysToSubtract = (currentDayOfWeek == 0) ? 6 : currentDayOfWeek - 1;
        var currentWeekStart = today.AddDays(-daysToSubtract);
        
        // Calculate start date for the whole period
        // providing 'weeks' number of full (or partial current) weeks
        var overallStart = currentWeekStart.AddDays(-(7 * (weeks - 1)));
        
        // Fetch all relevant logs in one query
        var logs = await _context.ProgressLogs
             .Where(p => p.UserId == userId && p.LogDate >= overallStart)
             .Select(p => new { p.LogDate, p.HoursLogged })
             .ToListAsync();

        for (int i = 0; i < weeks; i++)
        {
            var weekStart = overallStart.AddDays(i * 7);
            var weekEnd = weekStart.AddDays(6);
            
            var hoursLogged = logs
                .Where(l => l.LogDate >= weekStart && l.LogDate <= weekEnd)
                .Sum(l => l.HoursLogged);
            
            // Format label nicely
            string label;
            if (weekStart.Year != weekEnd.Year)
                label = $"{weekStart:MMM d} - {weekEnd:MMM d, yyyy}";
            else
                label = $"{weekStart:MMM d} - {weekEnd:MMM d}";

            result.Add(new WeeklyActivityDto
            {
                WeekNumber = i + 1, // Just an index for the chart
                WeekLabel = label,
                HoursLogged = hoursLogged
            });
        }
        
        return result;
    }
    
    private async Task UpdateDailyStatsAsync(int userId, DateTime date)
    {
        var logs = await _context.ProgressLogs
            .Where(p => p.UserId == userId && p.LogDate == date.Date)
            .ToListAsync();
        
        var existingStat = await _context.DailyStats
            .FirstOrDefaultAsync(s => s.UserId == userId && s.StatDate == date.Date);
        
        if (existingStat == null)
        {
            existingStat = new DailyStat
            {
                UserId = userId,
                StatDate = date.Date,
                CreatedAt = DateTime.UtcNow
            };
            _context.DailyStats.Add(existingStat);
        }
        
        existingStat.TotalHoursLogged = logs.Sum(l => l.HoursLogged);
        existingStat.SkillsPracticed = logs.Select(l => l.SkillId).Distinct().Count();
        existingStat.LogsCount = logs.Count;
        
        await _context.SaveChangesAsync();
    }
    
    private async Task<int> CalculateStreakAsync(int userId)
    {
        var dates = await _context.ProgressLogs
            .Where(p => p.UserId == userId)
            .Select(p => p.LogDate.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .Take(365)
            .ToListAsync();
        
        if (!dates.Any()) return 0;
        
        var streak = 0;
        var today = DateTime.UtcNow.Date;
        var checkDate = today;
        
        // Allow for missing today (check if yesterday counts)
        if (!dates.Contains(today))
        {
            checkDate = today.AddDays(-1);
            if (!dates.Contains(checkDate))
            {
                return 0;
            }
        }
        
        while (dates.Contains(checkDate))
        {
            streak++;
            checkDate = checkDate.AddDays(-1);
        }
        
        return streak;
    }
    
    private ProgressLogDto MapToDto(ProgressLog log)
    {
        return new ProgressLogDto
        {
            Id = log.Id,
            SkillId = log.SkillId,
            SkillName = log.Skill?.Name ?? "",
            GoalId = log.GoalId,
            GoalTitle = log.Goal?.Title,
            Title = log.Title,
            Description = log.Description,
            HoursLogged = log.HoursLogged,
            LogDate = log.LogDate,
            QualityRating = log.QualityRating,
            CreatedAt = log.CreatedAt,
            CompletedMilestoneIds = log.MilestoneCompletions?.Select(mc => mc.MilestoneId).ToList() ?? new List<int>()
        };
    }
}
