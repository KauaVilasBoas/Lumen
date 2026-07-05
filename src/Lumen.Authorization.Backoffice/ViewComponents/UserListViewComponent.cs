using Lumen.Authorization.Backoffice.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Authorization.Backoffice.ViewComponents;

public sealed class UserListViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(UserListViewModel model) =>
        View(model);
}
