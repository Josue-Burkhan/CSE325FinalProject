using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using CSE325FinalProject.Data;
using CSE325FinalProject.Services;
using CSE325FinalProject.Components;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor Server with Interactive Server Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure MySQL Database (UNCHANGED from MVC)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// Register Services (100% REUSED from MVC - no changes needed)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<IProgressLogService, ProgressLogService>();
builder.Services.AddScoped<IAiPlanService, AiPlanService>();

// HTTP Client for AI service (UNCHANGED)
builder.Services.AddHttpClient<IAiPlanService, AiPlanService>();

// Blazor Authentication
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationStateProvider, BlazorAuthStateProvider>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

// Keep API controllers for external/mobile app use
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// API routes (keeping for compatibility)
app.MapControllers();

// Blazor routes
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
