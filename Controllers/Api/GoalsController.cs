using Microsoft.AspNetCore.Mvc;
using CSE325FinalProject.Models.DTOs;
using CSE325FinalProject.Services;

namespace CSE325FinalProject.Controllers.Api;

[ApiController]
[Route("api/skills/{skillId:int}/[controller]")]
public class GoalsController : ControllerBase
{
    private readonly IGoalService _goalService;
    
    public GoalsController(IGoalService goalService)
    {
        _goalService = goalService;
    }
    
    private int? GetUserId() => HttpContext.Session.GetInt32("UserId");
    
    /// <summary>
    /// Get all goals for a skill
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<GoalDto>>>> GetGoals(int skillId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<List<GoalDto>>.Fail("Not authenticated"));
        
        var goals = await _goalService.GetSkillGoalsAsync(skillId, userId.Value);
        return Ok(ApiResponse<List<GoalDto>>.Ok(goals));
    }
    
    /// <summary>
    /// Get goal by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<GoalDto>>> GetGoal(int skillId, int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<GoalDto>.Fail("Not authenticated"));
        
        var goal = await _goalService.GetGoalByIdAsync(id, userId.Value);
        
        if (goal == null || goal.SkillId != skillId)
            return NotFound(ApiResponse<GoalDto>.Fail("Goal not found"));
        
        return Ok(ApiResponse<GoalDto>.Ok(goal));
    }
    
    /// <summary>
    /// Create new goal
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<GoalDto>>> CreateGoal(int skillId, [FromBody] CreateGoalRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<GoalDto>.Fail("Not authenticated"));
        
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(ApiResponse<GoalDto>.Fail("Validation failed", errors));
        }
        
        try
        {
            var goal = await _goalService.CreateGoalAsync(skillId, userId.Value, request);
            return CreatedAtAction(nameof(GetGoal), new { skillId, id = goal.Id }, 
                ApiResponse<GoalDto>.Ok(goal, "Goal created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<GoalDto>.Fail(ex.Message));
        }
    }
    
    /// <summary>
    /// Update goal
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<GoalDto>>> UpdateGoal(int skillId, int id, [FromBody] UpdateGoalRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<GoalDto>.Fail("Not authenticated"));
        
        var goal = await _goalService.UpdateGoalAsync(id, userId.Value, request);
        
        if (goal == null)
            return NotFound(ApiResponse<GoalDto>.Fail("Goal not found"));
        
        return Ok(ApiResponse<GoalDto>.Ok(goal, "Goal updated successfully"));
    }
    
    /// <summary>
    /// Delete goal
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteGoal(int skillId, int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<bool>.Fail("Not authenticated"));
        
        var result = await _goalService.DeleteGoalAsync(id, userId.Value);
        
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Goal not found"));
        
        return Ok(ApiResponse<bool>.Ok(true, "Goal deleted successfully"));
    }
}

/// <summary>
/// Standalone goals API for direct access
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AllGoalsController : ControllerBase
{
    private readonly IGoalService _goalService;
    
    public AllGoalsController(IGoalService goalService)
    {
        _goalService = goalService;
    }
    
    private int? GetUserId() => HttpContext.Session.GetInt32("UserId");
    
    /// <summary>
    /// Get goal by ID (direct access)
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<GoalDto>>> GetGoal(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<GoalDto>.Fail("Not authenticated"));
        
        var goal = await _goalService.GetGoalByIdAsync(id, userId.Value);
        
        if (goal == null)
            return NotFound(ApiResponse<GoalDto>.Fail("Goal not found"));
        
        return Ok(ApiResponse<GoalDto>.Ok(goal));
    }
    
    /// <summary>
    /// Update goal status directly
    /// </summary>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<ApiResponse<GoalDto>>> UpdateGoalStatus(int id, [FromBody] UpdateGoalRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<GoalDto>.Fail("Not authenticated"));
        
        var goal = await _goalService.UpdateGoalAsync(id, userId.Value, new UpdateGoalRequest { Status = request.Status });
        
        if (goal == null)
            return NotFound(ApiResponse<GoalDto>.Fail("Goal not found"));
        
        return Ok(ApiResponse<GoalDto>.Ok(goal));
    }
}
