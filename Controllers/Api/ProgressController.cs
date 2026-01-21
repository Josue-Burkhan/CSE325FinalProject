using Microsoft.AspNetCore.Mvc;
using CSE325FinalProject.Models.DTOs;
using CSE325FinalProject.Services;

namespace CSE325FinalProject.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ProgressController : ControllerBase
{
    private readonly IProgressLogService _progressLogService;
    
    public ProgressController(IProgressLogService progressLogService)
    {
        _progressLogService = progressLogService;
    }
    
    private int? GetUserId() => HttpContext.Session.GetInt32("UserId");
    
    /// <summary>
    /// Get progress logs with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<ProgressLogDto>>>> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? skillId = null,
        [FromQuery] int? goalId = null)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<PagedResponse<ProgressLogDto>>.Fail("Not authenticated"));
        
        var logs = await _progressLogService.GetUserLogsAsync(userId.Value, page, pageSize, skillId, goalId);
        return Ok(ApiResponse<PagedResponse<ProgressLogDto>>.Ok(logs));
    }
    
    /// <summary>
    /// Get progress log by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProgressLogDto>>> GetLog(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<ProgressLogDto>.Fail("Not authenticated"));
        
        var log = await _progressLogService.GetLogByIdAsync(id, userId.Value);
        
        if (log == null)
            return NotFound(ApiResponse<ProgressLogDto>.Fail("Log not found"));
        
        return Ok(ApiResponse<ProgressLogDto>.Ok(log));
    }
    
    /// <summary>
    /// Create progress log
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProgressLogDto>>> CreateLog([FromBody] CreateProgressLogRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<ProgressLogDto>.Fail("Not authenticated"));
        
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(ApiResponse<ProgressLogDto>.Fail("Validation failed", errors));
        }
        
        try
        {
            var log = await _progressLogService.CreateLogAsync(userId.Value, request);
            return CreatedAtAction(nameof(GetLog), new { id = log.Id }, 
                ApiResponse<ProgressLogDto>.Ok(log, "Progress logged successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<ProgressLogDto>.Fail(ex.Message));
        }
    }
    
    /// <summary>
    /// Delete progress log
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteLog(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<bool>.Fail("Not authenticated"));
        
        var result = await _progressLogService.DeleteLogAsync(id, userId.Value);
        
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Log not found"));
        
        return Ok(ApiResponse<bool>.Ok(true, "Log deleted successfully"));
    }
    
    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetStats()
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<DashboardStatsDto>.Fail("Not authenticated"));
        
        var stats = await _progressLogService.GetDashboardStatsAsync(userId.Value);
        return Ok(ApiResponse<DashboardStatsDto>.Ok(stats));
    }
    
    /// <summary>
    /// Get weekly activity data
    /// </summary>
    [HttpGet("weekly")]
    public async Task<ActionResult<ApiResponse<List<WeeklyActivityDto>>>> GetWeeklyActivity([FromQuery] int weeks = 12)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<List<WeeklyActivityDto>>.Fail("Not authenticated"));
        
        var activity = await _progressLogService.GetWeeklyActivityAsync(userId.Value, weeks);
        return Ok(ApiResponse<List<WeeklyActivityDto>>.Ok(activity));
    }
}
