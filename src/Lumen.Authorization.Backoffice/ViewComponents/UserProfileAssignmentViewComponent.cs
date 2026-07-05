using Lumen.Authorization.Backoffice.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Authorization.Backoffice.ViewComponents;

public sealed class UserProfileAssignmentViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(UserProfileAssignmentViewModel model) =>
        View(model);
}
