using AegisIdentity.Domain.Authorization;
using MediatR;

namespace AegisIdentity.ReadModels.Queries;

public sealed class GetProfileQueryHandler
    : IRequestHandler<GetProfileQueryHandler.Query, GetProfileQueryHandler.Result?>
{
    public sealed record Query(Guid Id) : IRequest<Result?>;

    public sealed record Result(
        Guid Id,
        string Name,
        string Description,
        bool IsSystem,
        IReadOnlyList<Guid> PermissionIds);

    private readonly IProfileRepository _profileRepository;

    public GetProfileQueryHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<Result?> Handle(Query query, CancellationToken ct)
    {
        var profile = await _profileRepository.FindByIdAsync(query.Id, ct);

        if (profile is null)
            return null;

        var permissionProfiles = await _profileRepository
            .GetPermissionProfilesByProfileIdAsync(query.Id, ct);

        var permissionIds = permissionProfiles.Select(pp => pp.PermissionId).ToList();

        return new Result(
            profile.Id,
            profile.Name,
            profile.Description,
            profile.IsSystem,
            permissionIds);
    }
}
