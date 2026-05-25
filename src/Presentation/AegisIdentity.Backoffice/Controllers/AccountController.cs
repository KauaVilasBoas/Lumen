using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AegisIdentity.Backoffice.Services;
using AegisIdentity.SharedKernel.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Backoffice.Controllers;

public sealed class AccountController : Controller
{
    private const string AccessTokenClaimType = "access_token";

    private readonly AuthApiClient _authApiClient;

    public AccountController(AuthApiClient authApiClient) => _authApiClient = authApiClient;

    public sealed record LoginFormModel(string Identifier = "", string Password = "");

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginFormModel form,
        string? returnUrl,
        CancellationToken ct)
    {
        ViewData["ReturnUrl"] = returnUrl;

        var result = await _authApiClient.LoginAsync(form.Identifier, form.Password, ct);

        switch (result)
        {
            case AuthApiClient.LoginResult.Success success:
                var principal = BuildPrincipal(success.Response.AccessToken);
                var props = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(success.Response.ExpiresIn),
                };
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    props);

                return LocalRedirect(returnUrl ?? "/");

            case AuthApiClient.LoginResult.InvalidCredentials:
                ModelState.AddModelError(string.Empty, "Identificador ou senha inválidos.");
                break;

            case AuthApiClient.LoginResult.EmailNotConfirmed:
                ModelState.AddModelError(string.Empty, "Confirme seu endereço de email antes de acessar o backoffice.");
                break;

            case AuthApiClient.LoginResult.AccountLocked:
                ModelState.AddModelError(string.Empty, "Conta temporariamente bloqueada por tentativas excessivas. Tente novamente mais tarde.");
                break;

            default:
                ModelState.AddModelError(string.Empty, "Erro ao comunicar com a Api. Tente novamente.");
                break;
        }

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private static ClaimsPrincipal BuildPrincipal(string rawJwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(rawJwt);

        var claims = token.Claims.ToList();

        var subClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        if (subClaim is not null && claims.All(c => c.Type != ClaimTypes.NameIdentifier))
            claims.Add(new Claim(ClaimTypes.NameIdentifier, subClaim.Value));

        var emailClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        if (emailClaim is not null && claims.All(c => c.Type != ClaimTypes.Email))
            claims.Add(new Claim(ClaimTypes.Email, emailClaim.Value));

        var usernameClaim = claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Username);
        if (usernameClaim is not null && claims.All(c => c.Type != ClaimTypes.Name))
            claims.Add(new Claim(ClaimTypes.Name, usernameClaim.Value));

        claims.Add(new Claim(AccessTokenClaimType, rawJwt));

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identity);
    }
}
