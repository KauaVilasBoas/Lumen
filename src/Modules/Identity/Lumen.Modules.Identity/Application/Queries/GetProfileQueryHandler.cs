using Lumen.Modules.Identity.Domain.Authorization;
using MediatR;

namespace Lumen.Modules.Identity.Application.Queries;

public sealed record GetProfileQuery(Guid Id) : IRequest<GetProfileResult?>;

public sealed record GetProfileResult(
    Guid Id,
    string Name,
    string Description,
    bool IsSystem,
    IReadOnlyList<Guid> PermissionIds);

internal sealed class GetProfileQueryHandler
    : IRequestHandler<GetProfileQuery, GetProfileResult?>
{
    private readonly IProfileRepository _profileRepository;

    public GetProfileQueryHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<GetProfileResult?> Handle(GetProfileQuery query, CancellationToken ct)
    {
        var profile = await _profileRepository.FindByIdAsync(query.Id, ct);

        if (profile is null)
            return null;

        var permissionProfiles = await _profileRepository
            .GetPermissionProfilesByProfileIdAsync(query.Id, ct);

        var permissionIds = permissionProfiles.Select(pp => pp.PermissionId).ToList();

        return new GetProfileResult(
            profile.Id,
            profile.Name,
            profile.Description,
            profile.IsSystem,
            permissionIds);
    }
}
