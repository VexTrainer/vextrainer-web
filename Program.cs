using Microsoft.AspNetCore.Authentication.Cookies;
using VexTrainer.Data.Services;
using VexTrainerWeb.Services;
using Microsoft.AspNetCore.DataProtection;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ===== Configuration =====
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string not configured");

// ===== Kestrel: suppress Server header =====
// IIS outbound rule + requestFiltering in web.config handle the IIS layer.
// This suppresses the Kestrel-added Server header at the ASP.NET Core layer.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

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
//builder.Services.AddScoped<ConfirmationTokenService>();

// Session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===== Rate limiting =====
// Global: 150 page requests per minute per IP.
// Bots hammering the site are throttled; legitimate users are unaffected.
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit              = 150,
                Window                   = TimeSpan.FromMinutes(1),
                QueueProcessingOrder     = QueueProcessingOrder.OldestFirst,
                QueueLimit               = 0
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ===== Build =====
var app = builder.Build();

// ===== Middleware Pipeline =====

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var feature = context.Features
                .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();

            if (feature?.Error is not null)
            {
                app.Logger.LogError(feature.Error, "Unhandled exception");

                // Fire-and-forget: email must not delay or mask the error page
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ErrorNotification.SendAsync(
                            feature.Error, context, app.Configuration, "VexTrainer Web");
                    }
                    catch (Exception emailEx)
                    {
                        app.Logger.LogError(emailEx, "Error notification email failed");
                    }
                });
            }

            context.Response.Redirect("/Error");
        });
    });
    app.UseHsts();
}

// ===== Security headers middleware =====
// Belt-and-suspenders with web.config headers — covers edge cases where
// the IIS layer is bypassed (e.g. direct Kestrel access in staging).
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var h = context.Response.Headers;
        h.Remove("Server");
        h.Remove("X-Powered-By");
        h.Remove("X-AspNet-Version");
        h["X-Content-Type-Options"] = "nosniff";
        h["X-Frame-Options"]        = "SAMEORIGIN";
        h["X-XSS-Protection"]       = "1; mode=block";
        h["Referrer-Policy"]        = "strict-origin-when-cross-origin";
        return Task.CompletedTask;
    });
    await next();
});

app.UseHttpsRedirection();
// app.UseStaticFiles();
var contentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".md"] = "text/markdown";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider,
    OnPrepareResponse = ctx =>
    {
        var path    = ctx.File.Name.ToLowerInvariant();
        var headers = ctx.Context.Response.Headers;

        if (path.EndsWith(".css") || path.EndsWith(".js") ||
            path.EndsWith(".woff2") || path.EndsWith(".woff"))
        {
            // 1 year + immutable: safe because _Layout.cshtml uses
            // asp-append-version="true" which fingerprints filenames
            // with a content hash — cache is auto-busted on every change.
            headers.CacheControl = "public, max-age=31536000, immutable";
        }
        else if (path.EndsWith(".png") || path.EndsWith(".jpg") ||
                 path.EndsWith(".jpeg") || path.EndsWith(".svg") ||
                 path.EndsWith(".ico") || path.EndsWith(".webp"))
        {
            // 30 days for images — change rarely, acceptable slight staleness.
            headers.CacheControl = "public, max-age=2592000";
        }
        else
        {
            // Everything else (markdown content files etc.) — no cache.
            headers.CacheControl = "no-store";
        }
    }
});

app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Default route
app.MapGet("/", () => Results.Redirect("/Index"));

app.Logger.LogInformation("VexTrainer Web starting...");
app.Run();
