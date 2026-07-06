using Lumen.Authorization.Domain;
using Lumen.Identity.Domain.Users;
using MediatR;

namespace Lumen.Identity.Application.Users.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<GetCurrentUserResult?>;

public sealed record GetCurrentUserProfileSummary(Guid Id, string Name);

public sealed record GetCurrentUserResult(
    string Id,
    string Email,
    string Username,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    DateTime? EmailConfirmedAt,
    IReadOnlyList<GetCurrentUserProfileSummary> Profiles);

internal sealed class GetCurrentUserQueryHandler
    : IRequestHandler<GetCurrentUserQuery, GetCurrentUserResult?>
{
    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;

    public GetCurrentUserQueryHandler(
        IUserRepository userRepository,
        IProfileRepository profileRepository)
    {
        _userRepository = userRepository;
        _profileRepository = profileRepository;
    }

    public async Task<GetCurrentUserResult?> Handle(GetCurrentUserQuery query, CancellationToken ct)
    {
        var user = await _userRepository.FindByIdAsync(query.UserId, ct);

        if (user is null)
            return null;

        var profiles = await _profileRepository.GetProfilesByUserIdAsync(query.UserId, ct);

        var profileSummaries = profiles
            .Select(p => new GetCurrentUserProfileSummary(p.Id, p.Name))
            .ToList();

        return new GetCurrentUserResult(
            Id: user.Id.ToString(),
            Email: user.Email,
            Username: user.Username,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt,
            EmailConfirmedAt: user.EmailConfirmedAt,
            Profiles: profileSummaries);
    }
}
