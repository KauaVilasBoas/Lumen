using Lumen.Authorization.Backoffice.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Authorization.Backoffice.ViewComponents;

public sealed class PermissionCatalogueViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(PermissionCatalogueViewModel model) =>
        View(model);
}
