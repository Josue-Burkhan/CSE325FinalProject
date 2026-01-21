using Microsoft.AspNetCore.Mvc;
using CSE325FinalProject.Models.DTOs;
using CSE325FinalProject.Services;

namespace CSE325FinalProject.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IAiPlanService _aiPlanService;
    private readonly IAuthService _authService;
    
    public AiController(IAiPlanService aiPlanService, IAuthService authService)
    {
        _aiPlanService = aiPlanService;
        _authService = authService;
    }
    
    private async Task<int?> GetUserIdAsync()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken)) return null;
        
        var session = await _authService.GetSessionByRefreshTokenAsync(refreshToken);
        return session?.UserId;
    }
    
    /// <summary>
    /// Generate AI learning plan
    /// </summary>
    [HttpPost("generate-plan")]
    public async Task<ActionResult<ApiResponse<AiPlanResponse>>> GeneratePlan([FromBody] AiPlanRequest request)
    {
        var userId = await GetUserIdAsync();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<AiPlanResponse>.Fail("Not authenticated"));
        
        if (string.IsNullOrWhiteSpace(request.GoalDescription))
        {
            return BadRequest(ApiResponse<AiPlanResponse>.Fail("Goal description is required"));
        }
        
        var result = await _aiPlanService.GeneratePlanAsync(userId.Value, request);
        
        if (!result.Success)
        {
            return BadRequest(ApiResponse<AiPlanResponse>.Fail(result.ErrorMessage ?? "Failed to generate plan"));
        }
        
        return Ok(ApiResponse<AiPlanResponse>.Ok(result));
    }
    
    /// <summary>
    /// Apply AI generated plan (create skill with goals)
    /// </summary>
    [HttpPost("apply-plan")]
    public async Task<ActionResult<ApiResponse<SkillDto>>> ApplyPlan([FromBody] AiGeneratedSkillPlan plan)
    {
        var userId = await GetUserIdAsync();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<SkillDto>.Fail("Not authenticated"));
        
        if (string.IsNullOrWhiteSpace(plan.SkillName) || !plan.Goals.Any())
        {
            return BadRequest(ApiResponse<SkillDto>.Fail("Invalid plan data"));
        }
        
        var skill = await _aiPlanService.ApplyPlanAsync(userId.Value, plan);
        
        return Ok(ApiResponse<SkillDto>.Ok(new SkillDto
        {
            Id = skill.Id,
            Name = skill.Name,
            Description = skill.Description,
            BigGoal = skill.BigGoal,
            Status = skill.Status,
            GoalsCount = plan.Goals.Count
        }, "Plan applied successfully! Your skill has been created."));
    }
}
