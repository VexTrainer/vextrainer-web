using Microsoft.AspNetCore.Authentication.Cookies;
using VexTrainer.Data.Services;
using VexTrainerWeb.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// ===== Configuration =====
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string not configured");

// ===== Service Registration =====

// Add Razor Pages
builder.Services.AddRazorPages();

// Persist DataProtection keys so antiforgery tokens survive app restarts
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "session-data", "DataProtection-Keys")))
    .SetApplicationName("VexTrainerWeb");
// Add HttpContextAccessor (for header/footer)
builder.Services.AddHttpContextAccessor();

// Register API client — used by web pages to call the VexTrainer API
// instead of touching the DB directly, keeping token/email logic centralised.
builder.Services.AddHttpClient<ApiClient>(client => {
  var apiUrl = builder.Configuration["Site:ApiUrl"] ?? "https://api.vextrainer.com";
  client.BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/");
  client.Timeout = TimeSpan.FromSeconds(30);
});

// Add Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/SignIn";
        options.LogoutPath = "/Auth/SignOut";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30); // Remember me for 30 days
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

// Register shared services from VexTrainer.Data (Scoped - per request)
builder.Services.AddScoped(sp => new AuthService(
    connectionString,
    sp.GetRequiredService<PasswordService>(),
    new WebTokenService() // Simple token service for web
));
builder.Services.AddScoped(sp => new LessonService(connectionString));
builder.Services.AddScoped(sp => new QuizService(connectionString));
builder.Services.AddScoped(sp => new AdminService(connectionString));

// Register singletons
builder.Services.AddSingleton<PasswordService>();

// Register web-specific services
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<ConfirmationTokenService>();

// Session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===== Build Application =====
var app = builder.Build();

// ===== Middleware Pipeline =====

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Default route
app.MapGet("/", () => Results.Redirect("/Index"));

app.Logger.LogInformation("VexTrainer Web starting...");
app.Run();
