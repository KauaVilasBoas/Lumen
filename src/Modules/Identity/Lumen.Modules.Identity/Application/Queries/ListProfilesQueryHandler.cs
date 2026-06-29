using Lumen.Modules.Identity.Domain.Authorization;
using MediatR;

namespace Lumen.Modules.Identity.Application.Queries;

internal sealed class ListProfilesQueryHandler
    : IRequestHandler<ListProfilesQueryHandler.Query, IReadOnlyList<ListProfilesQueryHandler.Result>>
{
    public sealed record Query : IRequest<IReadOnlyList<Result>>;

    public sealed record Result(
        Guid Id,
        string Name,
        string Description,
        bool IsSystem);

    private readonly IProfileRepository _profileRepository;

    public ListProfilesQueryHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<IReadOnlyList<Result>> Handle(Query query, CancellationToken ct)
    {
        var profiles = await _profileRepository.ListAllAsync(ct);

        return profiles
            .Select(p => new Result(p.Id, p.Name, p.Description, p.IsSystem))
            .ToList();
    }
}
