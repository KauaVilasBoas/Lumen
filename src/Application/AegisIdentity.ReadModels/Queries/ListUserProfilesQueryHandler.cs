using AegisIdentity.Domain.Authorization;
using MediatR;

namespace AegisIdentity.ReadModels.Queries;

public sealed class ListUserProfilesQueryHandler
    : IRequestHandler<ListUserProfilesQueryHandler.Query, IReadOnlyList<ListUserProfilesQueryHandler.Result>>
{
    public sealed record Query(Guid UserId) : IRequest<IReadOnlyList<Result>>;

    public sealed record Result(
        Guid AssignmentId,
        Guid ProfileId,
        string ProfileName,
        bool IsSystem);

    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IProfileRepository _profileRepository;

    public ListUserProfilesQueryHandler(
        IUserProfileRepository userProfileRepository,
        IProfileRepository profileRepository)
    {
        _userProfileRepository = userProfileRepository;
        _profileRepository = profileRepository;
    }

    public async Task<IReadOnlyList<Result>> Handle(Query query, CancellationToken ct)
    {
        var userProfiles = await _userProfileRepository.ListByUserIdAsync(query.UserId, ct);

        var profileIds = userProfiles.Select(up => up.ProfileId).Distinct().ToList();

        var allProfiles = await _profileRepository.ListAllAsync(ct);
        var profileById = allProfiles.ToDictionary(p => p.Id);

        return userProfiles
            .Where(up => profileById.ContainsKey(up.ProfileId))
            .Select(up => new Result(
                up.Id,
                up.ProfileId,
                profileById[up.ProfileId].Name,
                profileById[up.ProfileId].IsSystem))
            .ToList();
    }
}
