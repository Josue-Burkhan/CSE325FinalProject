using Microsoft.AspNetCore.Mvc;
using CSE325FinalProject.Models.DTOs;
using CSE325FinalProject.Services;

namespace CSE325FinalProject.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
    private readonly ISkillService _skillService;
    
    public SkillsController(ISkillService skillService)
    {
        _skillService = skillService;
    }
    
    private int? GetUserId() => HttpContext.Session.GetInt32("UserId");
    
    /// <summary>
    /// <summary>
    /// Retrieves a list of skills for the authenticated user, optionally filtered
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SkillDto>>>> GetSkills(
        [FromQuery] string? visibility = null,
        [FromQuery] string? status = null)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<List<SkillDto>>.Fail("Not authenticated"));
        
        var skills = await _skillService.GetUserSkillsAsync(userId.Value, visibility, status);
        return Ok(ApiResponse<List<SkillDto>>.Ok(skills));
    }
    
    /// <summary>
    /// <summary>
    /// Retrieves detailed information for a specific skill by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<SkillDto>>> GetSkill(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<SkillDto>.Fail("Not authenticated"));
        
        var skill = await _skillService.GetSkillByIdAsync(id, userId.Value);
        
        if (skill == null)
            return NotFound(ApiResponse<SkillDto>.Fail("Skill not found"));
        
        return Ok(ApiResponse<SkillDto>.Ok(skill));
    }
    
    /// <summary>
    /// <summary>
    /// Retrieves a public skill by its unique slug
    /// </summary>
    [HttpGet("public/{slug}")]
    public async Task<ActionResult<ApiResponse<SkillDto>>> GetPublicSkill(string slug)
    {
        var skill = await _skillService.GetSkillBySlugAsync(slug);
        
        if (skill == null)
            return NotFound(ApiResponse<SkillDto>.Fail("Skill not found"));
        
        return Ok(ApiResponse<SkillDto>.Ok(skill));
    }
    
    /// <summary>
    /// <summary>
    /// Creates a new skill in the user's portfolio
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SkillDto>>> CreateSkill([FromBody] CreateSkillRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<SkillDto>.Fail("Not authenticated"));
        
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(ApiResponse<SkillDto>.Fail("Validation failed", errors));
        }
        
        var skill = await _skillService.CreateSkillAsync(userId.Value, request);
        return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, ApiResponse<SkillDto>.Ok(skill, "Skill created successfully"));
    }
    
    /// <summary>
    /// <summary>
    /// Updates an existing skill's properties
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<SkillDto>>> UpdateSkill(int id, [FromBody] UpdateSkillRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<SkillDto>.Fail("Not authenticated"));
        
        var skill = await _skillService.UpdateSkillAsync(id, userId.Value, request);
        
        if (skill == null)
            return NotFound(ApiResponse<SkillDto>.Fail("Skill not found"));
        
        return Ok(ApiResponse<SkillDto>.Ok(skill, "Skill updated successfully"));
    }
    
    /// <summary>
    /// <summary>
    /// Deletes a skill and its associated data
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSkill(int id)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<bool>.Fail("Not authenticated"));
        
        var result = await _skillService.DeleteSkillAsync(id, userId.Value);
        
        if (!result)
            return NotFound(ApiResponse<bool>.Fail("Skill not found"));
        
        return Ok(ApiResponse<bool>.Ok(true, "Skill deleted successfully"));
    }
    
    /// <summary>
    /// <summary>
    /// Retrieves all available skill categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories()
    {
        var categories = await _skillService.GetCategoriesAsync();
        return Ok(ApiResponse<List<CategoryDto>>.Ok(categories));
    }
}
