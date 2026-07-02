using Lumen.Authorization.Application.Queries;

namespace Lumen.Authorization.Backoffice.ViewModels;

public sealed record ProfileListViewModel(IReadOnlyList<ListProfilesResult> Profiles);
