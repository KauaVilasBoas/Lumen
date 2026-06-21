using AegisIdentity.Backoffice.Configuration;
using AegisIdentity.Backoffice.Middleware;
using AegisIdentity.Backoffice.Services;
using AegisIdentity.DataAccess.Cache;
using AegisIdentity.DataAccess.Persistence;
using AegisIdentity.Infrastructure.Configuration;
using AegisIdentity.Jobs.Configuration;
using AegisIdentity.Jobs.Dashboard;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ── MVC ──────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// ── Infrastructure options (SqlServer) ───────────────────────────────────────
// The Backoffice only consumes the data-access layer (SQL Server + Redis), so it
// validates SqlServerOptions only — NOT the full AddInfrastructureOptions set,
// which also demands Jwt/Smtp/Hibp/App (API-only concerns the Backoffice never uses).
// RedisOptions is bound separately by AddRedisCache below.
builder.Services.AddSqlServerOptions(builder.Configuration);

// ── Backoffice-specific options ───────────────────────────────────────────────
// Api:BaseUrl — the upstream AegisIdentity API this host proxies calls to.
// Validated on startup so a missing/empty value fails fast before serving traffic.
builder.Services
    .AddOptions<BackofficeApiOptions>()
    .Bind(builder.Configuration.GetSection(BackofficeApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ── Data Access: SQL Server + Redis ──────────────────────────────────────────
// Both extension methods consume the validated options registered above.
// SQL Server: SqlServerOptions → EF Core DbContext + all domain repositories.
// Redis: RedisOptions → IDistributedCache (StackExchange) + IUserPermissionCache.
builder.Services.AddRelationalDataAccess();
builder.Services.AddRedisCache(builder.Configuration);

// ── Hangfire dashboard (storage only — no job server) ────────────────────────
// AddAegisHangfireServer is NOT called here; only the Api runs jobs to avoid
// competing consumers.  AddAegisDashboard binds HangfireDashboardOptions
// (path + Basic Auth credentials) from configuration.
builder.Services.AddAegisHangfire(builder.Configuration);
builder.Services.AddAegisDashboard(builder.Configuration);

// ── Authentication ────────────────────────────────────────────────────────────
// Strategy: Cookie authentication backed by claims extracted from the Api JWT.
//
// Flow:
//   1. User submits credentials on /Account/Login.
//   2. AccountController POSTs them to POST /api/auth/login (via AuthApiClient).
//   3. Api returns a JWT. Backoffice decodes it with JwtSecurityTokenHandler,
//      builds a ClaimsPrincipal from the JWT claims, and signs an ASP.NET cookie.
//   4. The raw JWT is stored as an extra claim ("access_token") so future
//      Backoffice → Api calls can set Authorization: Bearer <token>.
//   5. HttpContext.User is populated from the cookie on every subsequent request.
//
// The Backoffice never validates JWT signatures itself — it trusts the Api as the
// issuer and stores the opaque token only for re-use in upstream calls.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "AegisIdentity.Backoffice.Auth";
        options.Cookie.HttpOnly = true;
        // SameAsRequest: HTTP in dev, HTTPS in prod behind a TLS terminator.
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
// BaseAddress is resolved from the validated BackofficeApiOptions, so a missing
// Api:BaseUrl value causes an InvalidOperationException on startup rather than at
// the first request.
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

// Order matters: Authentication must precede Authorization.
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuthorizationGraphProxyMiddleware>();

// ── Hangfire Dashboard ────────────────────────────────────────────────────────
// Mounts the dashboard at the path defined in Hangfire:Dashboard:Path
// (default: /internal/jobs-admin).  Protected by Basic Auth via
// HangfireDashboardAuthorizationFilter — credentials come from configuration.
app.UseAegisDashboard();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
