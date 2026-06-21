using AegisIdentity.Backoffice.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Backoffice.ViewComponents;

public sealed class UserDetailViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(UserDetailViewModel? detail)
        => View(detail);
}
