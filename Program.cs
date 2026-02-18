using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using CSE325FinalProject.Data;
using CSE325FinalProject.Services;
using CSE325FinalProject.Components;
using CSE325FinalProject.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor Server with Interactive Server Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure MySQL Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 29)))
);

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<IProgressLogService, ProgressLogService>();
builder.Services.AddScoped<IAiPlanService, AiPlanService>();
builder.Services.AddScoped<IImageService, ImageService>();

// HTTP Client for AI service
builder.Services.AddHttpClient<IAiPlanService, AiPlanService>();

// Blazor Authentication
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SkillTrackerAuth";
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
            ? CookieSecurePolicy.None 
            : CookieSecurePolicy.Always;
    })
    .AddGitHub(options =>
    {
        options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"] ?? "PLACEHOLDER_CLIENT_ID";
        options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"] ?? "PLACEHOLDER_CLIENT_SECRET";
        options.CallbackPath = "/signin-github";
        options.Scope.Add("user:email");
        // Save tokens to allow retrieving them later if needed
        options.SaveTokens = true;
    });

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

// Configure Forwarded Headers for Render/Proxies
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection(); // Only redirect to HTTPS in production

// Initialize Database (Add missing columns)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}

app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
// Custom middleware to authenticate from refreshToken cookie
app.UseSessionAuth();
app.UseAuthorization();

// API routes (keeping for compatibility)
app.MapControllers();

// Blazor routes
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

