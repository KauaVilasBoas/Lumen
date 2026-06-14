using AegisIdentity.Backoffice.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Backoffice.ViewComponents;

public sealed class UserListViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(IReadOnlyList<UserListItemViewModel> users, Guid? selectedId)
        => View(new UserListComponentViewModel(users, selectedId));
}

public sealed record UserListComponentViewModel(
    IReadOnlyList<UserListItemViewModel> Users,
    Guid? SelectedId);
