using AegisIdentity.Domain.Users;
using MediatR;

namespace AegisIdentity.ReadModels.Queries;

public sealed class GetCurrentUserQueryHandler
    : IRequestHandler<GetCurrentUserQueryHandler.Query, GetCurrentUserQueryHandler.Result?>
{
    public sealed record Query(Guid UserId) : IRequest<Result?>;

    public sealed record Result(
        string Id,
        string Email,
        string Username,
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
            Id: user.Id.ToString(),
            Email: user.Email,
            Username: user.Username,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt,
            EmailConfirmedAt: user.EmailConfirmedAt);
    }
}
