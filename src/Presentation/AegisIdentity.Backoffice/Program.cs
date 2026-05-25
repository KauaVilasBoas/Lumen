using AegisIdentity.Backoffice.Services;
using AegisIdentity.Infrastructure.Configuration;
using AegisIdentity.Jobs.Configuration;
using AegisIdentity.Jobs.Dashboard;
using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ── MVC ──────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Hangfire dashboard (storage only — no job server) ────────────────────────
// The Backoffice reads from the same Hangfire Mongo database as the Api so
// the dashboard reflects live job state.  AddAegisHangfireServer is NOT called
// here; only the Api runs jobs to avoid competing consumers.
builder.Services.AddInfrastructureOptions(builder.Configuration);
builder.Services.AddAegisHangfire(builder.Configuration);

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

builder.Services.AddAuthorization();

// ── HttpClient — Api ──────────────────────────────────────────────────────────
builder.Services.AddHttpClient<AuthApiClient>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl is required in appsettings.json.");

    client.BaseAddress = new Uri(baseUrl);
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

// Order matters: Authentication must precede Authorization.
app.UseAuthentication();
app.UseAuthorization();

// ── Hangfire Dashboard ────────────────────────────────────────────────────────
// Protected by HangfireDashboardAuthorizationFilter (requires authenticated
// Backoffice session cookie).  Unauthenticated requests receive 401/403 which
// the cookie auth middleware converts to a redirect to /Account/Login.
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter()],
    // Dashboard is read-only from the Backoffice perspective — job execution
    // is handled exclusively by the Api's Hangfire server.
    IsReadOnlyFunc = _ => false,
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
