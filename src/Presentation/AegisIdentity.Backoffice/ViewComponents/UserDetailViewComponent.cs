using System.Security.Claims;
using AegisIdentity.SharedKernel.Constants;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Backoffice.ViewComponents;

/// <summary>
/// Renders a summary card of the currently authenticated user.
/// </summary>
/// <remarks>
/// The ViewModel is populated from cookie claims rather than a Query to the
/// ReadModels layer — claims already carry the user's id, email and username
/// since they were extracted from the Api JWT at login time.
///
/// When <c>AegisIdentity.ReadModels</c> gains a real <c>GetUserByIdQueryHandler</c>,
/// this component can be updated to call <c>IMediator.Send(new Query(userId))</c>
/// and return richer profile data without changing the View or its callers.
///
/// The <see cref="ViewModel"/> record is a nested type so consumers can reference it
/// as <c>UserDetailViewComponent.ViewModel</c> — matching the project-wide convention
/// established for CommandHandlers and their nested Command/Result types.
/// </remarks>
public sealed class UserDetailViewComponent : ViewComponent
{
    // ── Nested ViewModel ──────────────────────────────────────────────────────

    /// <summary>
    /// Data contract for the user detail card view.
    /// Access from Razor: <c>@model UserDetailViewComponent.ViewModel</c>
    /// </summary>
    public sealed record ViewModel(
        string UserId,
        string Email,
        string Username,
        IReadOnlyList<string> Roles);

    // ── Invoke ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the ViewModel from <see cref="UserClaimsPrincipal"/> claims and
    /// returns the <c>Default.cshtml</c> partial component view.
    /// </summary>
    public IViewComponentResult Invoke()
    {
        var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var email = UserClaimsPrincipal.FindFirstValue(ClaimTypes.Email) ?? "unknown";
        var username = UserClaimsPrincipal.FindFirstValue(ClaimTypes.Name)
                       ?? UserClaimsPrincipal.FindFirstValue(JwtClaimTypes.Username)
                       ?? "unknown";
        var roles = UserClaimsPrincipal
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList()
            .AsReadOnly();

        var vm = new ViewModel(userId, email, username, roles);
        return View(vm);
    }
}
