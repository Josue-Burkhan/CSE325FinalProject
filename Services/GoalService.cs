using Microsoft.EntityFrameworkCore;
using CSE325FinalProject.Data;
using CSE325FinalProject.Models;
using CSE325FinalProject.Models.DTOs;

namespace CSE325FinalProject.Services;

public interface IGoalService
{
    Task<List<GoalDto>> GetSkillGoalsAsync(int skillId, int userId);
    Task<List<GoalDto>> GetSkillGoalsForDetailsAsync(int skillId, int? userId);
    Task<GoalDto?> GetGoalByIdAsync(int goalId, int userId);
    Task<GoalDto> CreateGoalAsync(int skillId, int userId, CreateGoalRequest request);
    Task<GoalDto?> UpdateGoalAsync(int goalId, int userId, UpdateGoalRequest request);
    Task<bool> DeleteGoalAsync(int goalId, int userId);
    Task UpdateGoalProgressAsync(int goalId);
}

public class GoalService : IGoalService
{
    private readonly ApplicationDbContext _context;
    private readonly ISkillService _skillService;
    
    public GoalService(ApplicationDbContext context, ISkillService skillService)
    {
        _context = context;
        _skillService = skillService;
    }
    
    public async Task<List<GoalDto>> GetSkillGoalsAsync(int skillId, int userId)
    {
        var goals = await _context.Goals
            .Include(g => g.Skill)
            .Include(g => g.Milestones)
            .Where(g => g.SkillId == skillId && g.UserId == userId)
            .OrderBy(g => g.SortOrder)
            .ToListAsync();
        
        return goals.Select(MapToDto).ToList();
    }
    
    public async Task<List<GoalDto>> GetSkillGoalsForDetailsAsync(int skillId, int? userId)
    {
        // First checks if the user has access to view the skill
        var skill = await _skillService.GetSkillForDetailsAsync(skillId, userId);
        if (skill == null) return new List<GoalDto>();
        
        // Retrieves goals including milestones for detailed display
        var goals = await _context.Goals
            .Include(g => g.Skill)
            .Include(g => g.Milestones)
            .Where(g => g.SkillId == skillId)
            .OrderBy(g => g.SortOrder)
            .ToListAsync();
        
        return goals.Select(MapToDto).ToList();
    }
    
    public async Task<GoalDto?> GetGoalByIdAsync(int goalId, int userId)
    {
        var goal = await _context.Goals
            .Include(g => g.Skill)
            .Include(g => g.Milestones.OrderBy(m => m.SortOrder))
            .Include(g => g.ProgressLogs)
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);
        
        return goal != null ? MapToDto(goal) : null;
    }
    
    public async Task<GoalDto> CreateGoalAsync(int skillId, int userId, CreateGoalRequest request)
    {
        // Verify skill belongs to user
        var skill = await _context.Skills.FirstOrDefaultAsync(s => s.Id == skillId && s.UserId == userId);
        if (skill == null)
        {
            throw new InvalidOperationException("Skill not found");
        }
        
        // Determines the next sort order for the new goal
        var maxOrder = await _context.Goals
            .Where(g => g.SkillId == skillId)
            .MaxAsync(g => (int?)g.SortOrder) ?? -1;
        
        var goal = new Goal
        {
            SkillId = skillId,
            UserId = userId,
            Title = request.Title.Trim(),

            Description = request.Description?.Trim(),
            TargetHours = request.TargetHours,
            TargetDate = request.TargetDate,
            Status = "pending",
            SortOrder = maxOrder + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Goals.Add(goal);
        await _context.SaveChangesAsync();
        
        // Create milestones
        if (request.Milestones.Any())
        {
            int milestoneOrder = 0;
            foreach (var milestoneRequest in request.Milestones)
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
        
        return (await GetGoalByIdAsync(goal.Id, userId))!;
    }
    
    public async Task<GoalDto?> UpdateGoalAsync(int goalId, int userId, UpdateGoalRequest request)
    {
        var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);
        
        if (goal == null) return null;
        
        if (request.Title != null) goal.Title = request.Title.Trim();
        if (request.Description != null) goal.Description = request.Description.Trim();
        if (request.TargetHours != null) goal.TargetHours = request.TargetHours;
        if (request.TargetDate != null) goal.TargetDate = request.TargetDate;
        if (request.SortOrder != null) goal.SortOrder = request.SortOrder.Value;
        if (request.Status != null)
        {
            goal.Status = request.Status;
            if (request.Status == "in_progress" && !goal.StartedAt.HasValue)
            {
                goal.StartedAt = DateTime.UtcNow;
            }
            else if (request.Status == "completed" && !goal.CompletedAt.HasValue)
            {
                goal.CompletedAt = DateTime.UtcNow;
                goal.ProgressPercentage = 100;
            }
        }
        
        goal.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        // Triggers a recalculation of the parent skill's progress
        await _skillService.UpdateSkillProgressAsync(goal.SkillId);
        
        return await GetGoalByIdAsync(goalId, userId);
    }
    
    public async Task<bool> DeleteGoalAsync(int goalId, int userId)
    {
        var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);
        
        if (goal == null) return false;
        
        var skillId = goal.SkillId;
        
        _context.Goals.Remove(goal);
        await _context.SaveChangesAsync();
        
        // Update skill progress
        await _skillService.UpdateSkillProgressAsync(skillId);
        
        return true;
    }
    
    public async Task UpdateGoalProgressAsync(int goalId)
    {
        var goal = await _context.Goals
            .Include(g => g.Milestones)
            .Include(g => g.ProgressLogs)
            .FirstOrDefaultAsync(g => g.Id == goalId);
        
        if (goal == null) return;
        
        // Calculate logged hours
        goal.LoggedHours = goal.ProgressLogs.Sum(p => p.HoursLogged);
        
        // Calculate progress
        if (goal.Milestones.Any())
        {
            var completedMilestones = goal.Milestones.Count(m => m.IsCompleted);
            goal.ProgressPercentage = (completedMilestones / (decimal)goal.Milestones.Count) * 100;
        }
        else if (goal.TargetHours.HasValue && goal.TargetHours > 0)
        {
            goal.ProgressPercentage = Math.Min(100, (goal.LoggedHours / goal.TargetHours.Value) * 100);
        }
        
        // Automatically marks the goal as completed if progress reaches 100%
        if (goal.ProgressPercentage >= 100 && goal.Status != "completed")
        {
            goal.Status = "completed";
            goal.CompletedAt = DateTime.UtcNow;
        }
        
        goal.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        // Update parent skill
        await _skillService.UpdateSkillProgressAsync(goal.SkillId);
    }
    
    private GoalDto MapToDto(Goal goal)
    {
        return new GoalDto
        {
            Id = goal.Id,
            SkillId = goal.SkillId,
            SkillName = goal.Skill?.Name ?? "",
            Title = goal.Title,
            Description = goal.Description,
            ProgressPercentage = goal.ProgressPercentage,
            TargetHours = goal.TargetHours,
            LoggedHours = goal.LoggedHours,
            TargetDate = goal.TargetDate,
            Status = goal.Status,
            IsAiGenerated = goal.IsAiGenerated,
            MilestonesCount = goal.Milestones?.Count ?? 0,
            CompletedMilestonesCount = goal.Milestones?.Count(m => m.IsCompleted) ?? 0,
            Milestones = goal.Milestones?.Select(m => new MilestoneDto
            {
                Id = m.Id,
                GoalId = m.GoalId,
                Title = m.Title,
                Description = m.Description,
                Category = m.Category,
                IsCompleted = m.IsCompleted,
                CompletedAt = m.CompletedAt,
                IsAiGenerated = m.IsAiGenerated
            }).ToList() ?? new List<MilestoneDto>()
        };
    }
}
