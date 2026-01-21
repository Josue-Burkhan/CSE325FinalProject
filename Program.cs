using Microsoft.EntityFrameworkCore;
using CSE325FinalProject.Data;
using CSE325FinalProject.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure MySQL Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// Configure Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".SkillTracker.Session";
});

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<IProgressLogService, ProgressLogService>();
builder.Services.AddScoped<IAiPlanService, AiPlanService>();

// HTTP Client for AI service
builder.Services.AddHttpClient<IAiPlanService, AiPlanService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Session middleware must be before authorization
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

// API routes
app.MapControllers();

// MVC routes  
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
