using AegisIdentity.Domain.Users;
using MediatR;

namespace AegisIdentity.ReadModels.Queries;

public sealed class GetCurrentUserQueryHandler
    : IRequestHandler<GetCurrentUserQueryHandler.Query, GetCurrentUserQueryHandler.Result?>
{
    public sealed record Query(string UserId) : IRequest<Result?>;

    public sealed record Result(
        string Id,
        string Email,
        string Username,
        IReadOnlyList<string> Roles,
        DateTime CreatedAt,
        DateTime? LastLoginAt,
        DateTime? EmailConfirmedAt);

    private readonly IUserRepository _userRepository;

    public GetCurrentUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result?> Handle(Query query, CancellationToken ct)
    {
        var user = await _userRepository.FindByIdAsync(query.UserId, ct);

        if (user is null)
            return null;

        return new Result(
            Id: user.Id,
            Email: user.Email,
            Username: user.Username,
            Roles: user.Roles.AsReadOnly(),
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt,
            EmailConfirmedAt: user.EmailConfirmedAt);
    }
}
