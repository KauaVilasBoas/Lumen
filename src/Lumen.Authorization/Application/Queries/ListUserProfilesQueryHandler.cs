using Lumen.Authorization.Domain;
using MediatR;

namespace Lumen.Authorization.Application.Queries;

public sealed record ListUserProfilesQuery(Guid UserId) : IRequest<IReadOnlyList<ListUserProfilesResult>>;

public sealed record ListUserProfilesResult(
    Guid AssignmentId,
    Guid ProfileId,
    string ProfileName,
    bool IsSystem);

internal sealed class ListUserProfilesQueryHandler
    : IRequestHandler<ListUserProfilesQuery, IReadOnlyList<ListUserProfilesResult>>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IProfileRepository _profileRepository;

    public ListUserProfilesQueryHandler(
        IUserProfileRepository userProfileRepository,
        IProfileRepository profileRepository)
    {
        _userProfileRepository = userProfileRepository;
        _profileRepository = profileRepository;
    }

    public async Task<IReadOnlyList<ListUserProfilesResult>> Handle(ListUserProfilesQuery query, CancellationToken ct)
    {
        var userProfiles = await _userProfileRepository.ListByUserIdAsync(query.UserId, ct);

        if (userProfiles.Count == 0)
            return [];

        var profileIds = userProfiles.Select(up => up.ProfileId).Distinct().ToList();
        var profiles = await _profileRepository.GetByIdsAsync(profileIds, ct);
        var profileById = profiles.ToDictionary(p => p.Id);

        return userProfiles
            .Where(up => profileById.ContainsKey(up.ProfileId))
            .Select(up => new ListUserProfilesResult(
                up.Id,
                up.ProfileId,
                profileById[up.ProfileId].Name,
                profileById[up.ProfileId].IsSystem))
            .ToList();
    }
}
