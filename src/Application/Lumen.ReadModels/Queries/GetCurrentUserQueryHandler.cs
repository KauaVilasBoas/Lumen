using AegisIdentity.Domain.Authorization;
using AegisIdentity.Domain.Users;
using MediatR;

namespace AegisIdentity.ReadModels.Queries;

public sealed class GetCurrentUserQueryHandler
    : IRequestHandler<GetCurrentUserQueryHandler.Query, GetCurrentUserQueryHandler.Result?>
{
    public sealed record Query(Guid UserId) : IRequest<Result?>;

    public sealed record ProfileSummary(Guid Id, string Name);

    public sealed record Result(
        string Id,
        string Email,
        string Username,
        DateTime CreatedAt,
        DateTime? LastLoginAt,
        DateTime? EmailConfirmedAt,
        IReadOnlyList<ProfileSummary> Profiles);

    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;

    public GetCurrentUserQueryHandler(
        IUserRepository userRepository,
        IProfileRepository profileRepository)
    {
        _userRepository = userRepository;
        _profileRepository = profileRepository;
    }

    public async Task<Result?> Handle(Query query, CancellationToken ct)
    {
        var user = await _userRepository.FindByIdAsync(query.UserId, ct);

        if (user is null)
            return null;

        var profiles = await _profileRepository.GetProfilesByUserIdAsync(query.UserId, ct);

        var profileSummaries = profiles
            .Select(p => new ProfileSummary(p.Id, p.Name))
            .ToList();

        return new Result(
            Id: user.Id.ToString(),
            Email: user.Email,
            Username: user.Username,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt,
            EmailConfirmedAt: user.EmailConfirmedAt,
            Profiles: profileSummaries);
    }
}
