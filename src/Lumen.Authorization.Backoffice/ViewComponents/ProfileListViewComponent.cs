using Lumen.Authorization.Backoffice.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Authorization.Backoffice.ViewComponents;

public sealed class ProfileListViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(ProfileListViewModel model) =>
        View(model);
}
