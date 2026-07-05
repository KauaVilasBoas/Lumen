using Lumen.Authorization;
using Lumen.Backoffice.Configuration;
using Lumen.Backoffice.Middleware;
using Lumen.Backoffice.Services;
using Lumen.Infrastructure.Configuration;
using Lumen.Jobs.Configuration;
using Lumen.Jobs.Dashboard;
using Lumen.Modularity;
using Lumen.Modules.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ── MVC ──────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// ── Infrastructure options (SqlServer for Hangfire) ───────────────────────────
builder.Services.AddSqlServerOptions(builder.Configuration);

// ── Backoffice-specific options ───────────────────────────────────────────────
builder.Services
    .AddOptions<BackofficeApiOptions>()
    .Bind(builder.Configuration.GetSection(BackofficeApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ── Lumen authorization core (IUserPermissionService, Redis-backed cache) ────
builder.Services.AddLumenAuthorization(builder.Configuration);

// ── Identity module (replaces authz no-op sources with Identity-backed ones) ─
builder.Services.AddModules(builder.Configuration, typeof(IdentityModule).Assembly);

// ── Hangfire dashboard (storage only — no job server) ────────────────────────
builder.Services.AddAegisHangfire(builder.Configuration);
builder.Services.AddAegisDashboard(builder.Configuration);

// ── Authentication ────────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Lumen.Backoffice.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ── HttpClient — Api ──────────────────────────────────────────────────────────
builder.Services.AddHttpClient<AuthApiClient>((serviceProvider, client) =>
{
    var apiOptions = serviceProvider.GetRequiredService<IOptions<BackofficeApiOptions>>().Value;
    client.BaseAddress = new Uri(apiOptions.BaseUrl);
});

builder.Services.AddHttpClient<AdminApiClient>((serviceProvider, client) =>
{
    var apiOptions = serviceProvider.GetRequiredService<IOptions<BackofficeApiOptions>>().Value;
    client.BaseAddress = new Uri(apiOptions.BaseUrl);
});

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseWebSockets();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuthorizationGraphProxyMiddleware>();

// ── Hangfire Dashboard ────────────────────────────────────────────────────────
app.UseAegisDashboard();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
