using Lumen.Modules.Identity.Domain.Authorization;
using MediatR;

namespace Lumen.Modules.Identity.Application.Queries;

public sealed record ListProfilesQuery : IRequest<IReadOnlyList<ListProfilesResult>>;

public sealed record ListProfilesResult(
    Guid Id,
    string Name,
    string Description,
    bool IsSystem);

internal sealed class ListProfilesQueryHandler
    : IRequestHandler<ListProfilesQuery, IReadOnlyList<ListProfilesResult>>
{
    private readonly IProfileRepository _profileRepository;

    public ListProfilesQueryHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<IReadOnlyList<ListProfilesResult>> Handle(ListProfilesQuery query, CancellationToken ct)
    {
        var profiles = await _profileRepository.ListAllAsync(ct);

        return profiles
            .Select(p => new ListProfilesResult(p.Id, p.Name, p.Description, p.IsSystem))
            .ToList();
    }
}
