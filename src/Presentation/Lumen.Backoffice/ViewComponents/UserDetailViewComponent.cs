using Lumen.Backoffice.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Backoffice.ViewComponents;

public sealed class UserDetailViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(UserDetailViewModel? detail)
        => View(detail);
}
