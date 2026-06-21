using AegisIdentity.Domain.Authorization;
using AegisIdentity.Domain.Users;
using AegisIdentity.ReadModels.Users;
using AegisIdentity.SharedKernel.Exceptions;
using MediatR;

namespace AegisIdentity.ReadModels.Queries;

public sealed class GetUserDetailQueryHandler
    : IRequestHandler<GetUserDetailQueryHandler.Query, GetUserDetailQueryHandler.Result>
{
    public sealed record Query(Guid UserId) : IRequest<Result>;

    public sealed record ProfileSummary(
        Guid ProfileId,
        string Name,
        bool IsSystem,
        int PermissionCount);

    public sealed record Result(
        Guid Id,
        string Username,
        string Email,
        string State,
        bool IsBootstrap,
        DateTime CreatedAt,
        DateTime? EmailConfirmedAt,
        DateTime? LastLoginAt,
        DateTime? LockoutEndAt,
        IReadOnlyList<ProfileSummary> Profiles,
        int ResolvedPermissionCount);

    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;

    public GetUserDetailQueryHandler(
        IUserRepository userRepository,
        IProfileRepository profileRepository)
    {
        _userRepository = userRepository;
        _profileRepository = profileRepository;
    }

    public async Task<Result> Handle(Query query, CancellationToken ct)
    {
        var user = await _userRepository.FindByIdIgnoringFiltersAsync(query.UserId, ct)
            ?? throw new NotFoundException($"User '{query.UserId}' was not found.");

        var profiles = await _profileRepository.GetProfilesByUserIdAsync(query.UserId, ct);
        var permissionCodes = await _profileRepository.GetPermissionCodesByUserIdAsync(query.UserId, ct);

        var profileSummaries = await BuildProfileSummariesAsync(profiles, ct);

        return new Result(
            Id: user.Id,
            Username: user.Username,
            Email: user.Email,
            State: UserStateResolver.Resolve(user, DateTime.UtcNow),
            IsBootstrap: user.IsBootstrap,
            CreatedAt: user.CreatedAt,
            EmailConfirmedAt: user.EmailConfirmedAt,
            LastLoginAt: user.LastLoginAt,
            LockoutEndAt: user.LockedUntil,
            Profiles: profileSummaries,
            ResolvedPermissionCount: permissionCodes.Count);
    }

    private async Task<IReadOnlyList<ProfileSummary>> BuildProfileSummariesAsync(
        IReadOnlyList<Profile> profiles,
        CancellationToken ct)
    {
        if (profiles.Count == 0)
            return [];

        var profileIds = profiles.Select(p => p.Id).ToList();
        var permissionCountsByProfile = await _profileRepository.GetPermissionCountsByProfileIdsAsync(profileIds, ct);

        return profiles
            .Select(p => new ProfileSummary(
                ProfileId: p.Id,
                Name: p.Name,
                IsSystem: p.IsSystem,
                PermissionCount: permissionCountsByProfile.GetValueOrDefault(p.Id, 0)))
            .ToList();
    }
}
