using Lumen.Authorization.Domain;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Exceptions;
using MediatR;

namespace Lumen.Modules.Identity.Application.Queries;

public sealed record GetUserDetailQuery(Guid UserId) : IRequest<GetUserDetailResult>;

public sealed record GetUserDetailProfileSummary(
    Guid ProfileId,
    string Name,
    bool IsSystem,
    int PermissionCount);

public sealed record GetUserDetailResult(
    Guid Id,
    string Username,
    string Email,
    string State,
    bool IsBootstrap,
    DateTime CreatedAt,
    DateTime? EmailConfirmedAt,
    DateTime? LastLoginAt,
    DateTime? LockoutEndAt,
    IReadOnlyList<GetUserDetailProfileSummary> Profiles,
    int ResolvedPermissionCount);

internal sealed class GetUserDetailQueryHandler
    : IRequestHandler<GetUserDetailQuery, GetUserDetailResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;

    public GetUserDetailQueryHandler(
        IUserRepository userRepository,
        IProfileRepository profileRepository)
    {
        _userRepository = userRepository;
        _profileRepository = profileRepository;
    }

    public async Task<GetUserDetailResult> Handle(GetUserDetailQuery query, CancellationToken ct)
    {
        var user = await _userRepository.FindByIdIgnoringFiltersAsync(query.UserId, ct)
            ?? throw new NotFoundException($"User '{query.UserId}' was not found.");

        var profiles = await _profileRepository.GetProfilesByUserIdAsync(query.UserId, ct);
        var permissionCodes = await _profileRepository.GetPermissionCodesByUserIdAsync(query.UserId, scopeId: null, ct);

        var profileSummaries = await BuildProfileSummariesAsync(profiles, ct);

        return new GetUserDetailResult(
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

    private async Task<IReadOnlyList<GetUserDetailProfileSummary>> BuildProfileSummariesAsync(
        IReadOnlyList<Profile> profiles,
        CancellationToken ct)
    {
        if (profiles.Count == 0)
            return [];

        var profileIds = profiles.Select(p => p.Id).ToList();
        var permissionCountsByProfile = await _profileRepository.GetPermissionCountsByProfileIdsAsync(profileIds, ct);

        return profiles
            .Select(p => new GetUserDetailProfileSummary(
                ProfileId: p.Id,
                Name: p.Name,
                IsSystem: p.IsSystem,
                PermissionCount: permissionCountsByProfile.GetValueOrDefault(p.Id, 0)))
            .ToList();
    }
}
