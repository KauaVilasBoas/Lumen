using Lumen.Authorization.Contracts;

namespace Lumen.Authorization.Backoffice.ViewModels;

public sealed record UserListViewModel(
    IReadOnlyList<UserListItemViewModel> Users,
    bool IsEmpty);

public sealed record UserListItemViewModel(
    Guid Id,
    string Username,
    string Email,
    string State,
    int ProfileCount);
