using Lumen.Authorization.Backoffice.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Authorization.Backoffice.ViewComponents;

public sealed class ProfileDetailViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(ProfileDetailViewModel model) =>
        View(model);
}
