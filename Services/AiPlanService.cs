using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using CSE325FinalProject.Data;
using CSE325FinalProject.Models;
using CSE325FinalProject.Models.DTOs;

namespace CSE325FinalProject.Services;

public interface IAiPlanService
{
    Task<AiPlanResponse> GeneratePlanAsync(int userId, AiPlanRequest request);
    Task<AiGeneratedGoal?> RefineGoalAsync(string instruction, AiGeneratedGoal currentGoal);
    Task<Skill> ApplyPlanAsync(int userId, AiGeneratedSkillPlan plan, DateTime? targetDate = null);
}

public class AiPlanService : IAiPlanService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiPlanService> _logger;
    
    public AiPlanService(
        ApplicationDbContext context, 
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<AiPlanService> logger)
    {
        _context = context;
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<AiPlanResponse> GeneratePlanAsync(int userId, AiPlanRequest request)
    {
        // Simply get the key and trim whitespace
        var apiKey = _configuration["GeminiAI:ApiKey"]?.Trim();
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Gemini AI API key is missing");
            return new AiPlanResponse 
            { 
                Success = false, 
                ErrorMessage = "AI service not configured. Please check your API key." 
            };
        }
        
        try
        {
            var prompt = BuildPrompt(request);
            
            if (request.Clarifications.Count < 2 && NeedsClarification(request))
            {
                var clarificationQuestion = await GetClarificationQuestionAsync(request, apiKey);
                if (!string.IsNullOrEmpty(clarificationQuestion))
                {
                    await SaveAiPlanAsync(userId, request, null, "pending");
                    return new AiPlanResponse
                    {
                        Success = true,
                        NeedsClarification = true,
                        ClarificationQuestion = clarificationQuestion
                    };
                }
            }
            
            var plan = await GenerateFullPlanAsync(request, apiKey);
            
            if (plan != null)
            {
                await SaveAiPlanAsync(userId, request, plan, "completed");
                return new AiPlanResponse
                {
                    Success = true,
                    NeedsClarification = false,
                    Plan = plan
                };
            }
            
            return new AiPlanResponse
            {
                Success = false,
                ErrorMessage = "Failed to generate plan. Please try again."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI plan");
            return new AiPlanResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while generating your plan. Please try again."
            };
        }
    }

    public async Task<AiGeneratedGoal?> RefineGoalAsync(string instruction, AiGeneratedGoal currentGoal)
    {
         var apiKey = _configuration["GeminiAI:ApiKey"]?.Trim();
         
         if (string.IsNullOrEmpty(apiKey)) return null;

         var currentGoalJson = JsonSerializer.Serialize(currentGoal);
         var prompt = $@"You are an expert learning coach. The user wants to modify a specific goal in their learning plan.

Current Goal JSON:
{currentGoalJson}

User Instruction: ""{instruction}""

Task: Update the goal title, description, and milestones based on the user's instruction. Keep the structure consistent.
Respond with ONLY the updated Goal JSON object.";

         var response = await CallGeminiApiAsync(prompt, apiKey);
         if (string.IsNullOrEmpty(response)) return null;

         try
         {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                return JsonSerializer.Deserialize<AiGeneratedGoal>(jsonStr, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
         }
         catch (Exception ex)
         {
             _logger.LogError(ex, "Failed to parse refined goal JSON");
         }
         return null;
    }
    
    public async Task<Skill> ApplyPlanAsync(int userId, AiGeneratedSkillPlan plan, DateTime? targetDate = null)
    {
        // Find or create category
        int? categoryId = null;
        if (!string.IsNullOrEmpty(plan.Category))
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == plan.Category.ToLower());
            categoryId = category?.Id;
        }
        
        // Create skill
        var skill = new Skill
        {
            UserId = userId,
            CategoryId = categoryId,
            Name = plan.SkillName,
            Description = plan.Description,
            BigGoal = plan.BigGoal,
            Status = "in_progress",
            StartedAt = DateTime.UtcNow,
            TargetDate = targetDate, // Save the target date
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();
        
        // Create goals from plan
        int goalOrder = 0;
        foreach (var aiGoal in plan.Goals)
        {
            var goal = new Goal
            {
                SkillId = skill.Id,
                UserId = userId,
                Title = aiGoal.Title,
                Description = aiGoal.Description,
                Status = "pending",
                IsAiGenerated = true,
                SortOrder = goalOrder++,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _context.Goals.Add(goal);
            await _context.SaveChangesAsync();
            
            // Create milestones
            int milestoneOrder = 0;
            foreach (var milestoneTitle in aiGoal.Milestones)
            {
                var milestone = new Milestone
                {
                    GoalId = goal.Id,
                    UserId = userId,
                    Title = milestoneTitle,
                    IsAiGenerated = true,
                    SortOrder = milestoneOrder++,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Milestones.Add(milestone);
            }
            await _context.SaveChangesAsync();
        }
        
        return skill;
    }
    

    
    private bool NeedsClarification(AiPlanRequest request)
    {
        // Ask for clarification if goal is vague or missing key details
        var goal = request.GoalDescription.ToLower();
        
        // Short goals likely need clarification
        if (goal.Split(' ').Length < 5) return true;
        
        // Vague terms that need clarification
        var vagueTerms = new[] { "learn", "get better at", "improve", "master", "understand" };
        if (vagueTerms.Any(v => goal.Contains(v)) && request.Clarifications.Count == 0)
            return true;
        
        return false;
    }
    
    private async Task<string?> GetClarificationQuestionAsync(AiPlanRequest request, string apiKey)
    {
        var clarificationPrompt = $@"You are helping create a personalized learning plan. The user wants to: ""{request.GoalDescription}""

Target date: {request.TargetDate?.ToString("MMMM d, yyyy") ?? "Not specified"}
Time commitment: {request.HoursPerPeriod} hours {request.Frequency}
Preferred time: {request.PreferredTimeSlot ?? "Any time"}

This seems a bit vague. Ask ONE specific clarifying question to better understand their goal. 
The question should help determine:
- Their current skill level
- Specific sub-topics they're interested in  
- How they plan to apply this skill
- Any specific projects or outcomes they want

Respond with ONLY the question, nothing else. Keep it conversational and friendly.";

        var response = await CallGeminiApiAsync(clarificationPrompt, apiKey);
        return response?.Trim();
    }
    
    private async Task<AiGeneratedSkillPlan?> GenerateFullPlanAsync(AiPlanRequest request, string apiKey)
    {
        var prompt = BuildFullPlanPrompt(request);
        var response = await CallGeminiApiAsync(prompt, apiKey);
        
        if (string.IsNullOrEmpty(response)) return null;
        
        try
        {
            // Parse JSON response
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var plan = JsonSerializer.Deserialize<AiGeneratedSkillPlan>(jsonStr, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return plan;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI response as JSON: {Response}", response);
        }
        
        return null;
    }
    
    private string BuildPrompt(AiPlanRequest request)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Goal: {request.GoalDescription}");
        
        if (request.TargetDate.HasValue)
            sb.AppendLine($"Target date: {request.TargetDate.Value:MMMM d, yyyy}");
        
        if (!string.IsNullOrEmpty(request.Frequency))
            sb.AppendLine($"Frequency: {request.Frequency}");
        
        if (request.HoursPerPeriod.HasValue)
            sb.AppendLine($"Hours per period: {request.HoursPerPeriod}");
        
        foreach (var clarification in request.Clarifications)
        {
            sb.AppendLine($"Q: {clarification.Question}");
            sb.AppendLine($"A: {clarification.Answer}");
        }
        
        return sb.ToString();
    }
    
    private string BuildFullPlanPrompt(AiPlanRequest request)
    {
        var context = BuildPrompt(request);
        
        return $@"You are an expert learning coach. Create a structured learning plan based on the following:

{context}

Generate a JSON response with this exact structure:
{{
  ""skillName"": ""A clear, concise name for this skill"",
  ""description"": ""A 1-2 sentence description of the skill"",
  ""bigGoal"": ""The main objective they want to achieve"",
  ""category"": ""One of: Programming, Languages, Music, Art & Design, Business, Health & Fitness, Personal Development, Academic, or Other"",
  ""goals"": [
    {{
      ""title"": ""Week 1: Goal title"",
      ""description"": ""Brief description of this goal"",
      ""weekNumber"": 1,
      ""milestones"": [
        ""Specific actionable milestone 1"",
        ""Specific actionable milestone 2"",
        ""Specific actionable milestone 3""
      ]
    }}
  ]
}}

Create 4-8 weekly goals with 3-5 milestones each. Make milestones specific and actionable.
Respond with ONLY the JSON, no additional text.";
    }
    
    private async Task<string?> CallGeminiApiAsync(string prompt, string apiKey)
    {
        var model = _configuration["GeminiAI:Model"] ?? "gemini-pro";
        var baseUrl = _configuration["GeminiAI:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1";
        var url = $"{baseUrl}/models/{model}:generateContent?key={apiKey}";
        
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 16384
            }
        };
        
        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();
                return result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
        }
        
        return null;
    }
    
    private async Task SaveAiPlanAsync(int userId, AiPlanRequest request, AiGeneratedSkillPlan? plan, string status)
    {
        var aiPlan = new AiGeneratedPlan
        {
            UserId = userId,
            GoalDescription = request.GoalDescription,
            TargetDate = request.TargetDate,
            DedicationFrequency = request.Frequency,
            DedicationHours = request.HoursPerPeriod,
            PreferredTimeSlot = request.PreferredTimeSlot,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        if (request.Clarifications.Count > 0)
        {
            aiPlan.Clarification1Question = request.Clarifications[0].Question;
            aiPlan.Clarification1Answer = request.Clarifications[0].Answer;
        }
        if (request.Clarifications.Count > 1)
        {
            aiPlan.Clarification2Question = request.Clarifications[1].Question;
            aiPlan.Clarification2Answer = request.Clarifications[1].Answer;
        }
        
        if (plan != null)
        {
            aiPlan.AiResponseJson = JsonSerializer.Serialize(plan);
        }
        
        _context.AiGeneratedPlans.Add(aiPlan);
        await _context.SaveChangesAsync();
    }
}

// Gemini API response models
public class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; set; }
}

public class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }
}

public class GeminiContent
{
    [JsonPropertyName("parts")]
    public List<GeminiPart>? Parts { get; set; }
}

public class GeminiPart
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
