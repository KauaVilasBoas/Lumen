using AegisIdentity.Domain.Authorization;
using AegisIdentity.Domain.Users;
using AegisIdentity.ReadModels.Users;
using AegisIdentity.SharedKernel.Constants;
using FluentValidation;
using MediatR;

namespace AegisIdentity.ReadModels.Queries;

public sealed class ListUsersQueryHandler
    : IRequestHandler<ListUsersQueryHandler.Query, ListUsersQueryHandler.PagedResult>
{
    public sealed record Query(
        string? Search,
        string? State,
        int Page,
        int PageSize) : IRequest<PagedResult>;

    public sealed record UserResult(
        Guid Id,
        string Username,
        string Email,
        string State,
        bool IsBootstrap,
        DateTime CreatedAt,
        DateTime? LastLoginAt,
        DateTime? EmailConfirmedAt,
        DateTime? LockoutEndAt,
        int ProfileCount,
        int ResolvedPermissionCount);

    public sealed record PagedResult(
        IReadOnlyList<UserResult> Items,
        int Page,
        int PageSize,
        int Total);

    public sealed class Validator : AbstractValidator<Query>
    {
        private static readonly HashSet<string> ValidStateValues =
            ["active", "locked", "pending", "deleted", "all", ""];

        public Validator()
        {
            RuleFor(q => q.Page)
                .GreaterThanOrEqualTo(ValidationLimits.PageMinValue)
                .OverridePropertyName("page");

            RuleFor(q => q.PageSize)
                .InclusiveBetween(ValidationLimits.PageSizeMinValue, ValidationLimits.PageSizeMaxValue)
                .OverridePropertyName("pageSize");

            RuleFor(q => q.State)
                .Must(s => s is null || ValidStateValues.Contains(s.ToLowerInvariant()))
                .OverridePropertyName("state")
                .WithMessage(q => $"Invalid state value '{q.State}'. Allowed values: active, locked, pending, deleted, all.");
        }
    }

    private enum UserStateFilter { Active, Locked, Pending, Deleted, All }

    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;

    public ListUsersQueryHandler(
        IUserRepository userRepository,
        IProfileRepository profileRepository)
    {
        _userRepository = userRepository;
        _profileRepository = profileRepository;
    }

    public async Task<PagedResult> Handle(Query query, CancellationToken ct)
    {
        var stateFilter = ParseStateFilter(query.State);

        var includeDeleted = stateFilter is UserStateFilter.Deleted or UserStateFilter.All;

        var (users, total) = await _userRepository.ListAsync(
            query.Search,
            includeDeleted,
            query.Page,
            query.PageSize,
            ct);

        var now = DateTime.UtcNow;
        var filtered = stateFilter switch
        {
            UserStateFilter.Active  => users.Where(u => UserStateResolver.Resolve(u, now) == UserStates.Active).ToList(),
            UserStateFilter.Locked  => users.Where(u => UserStateResolver.Resolve(u, now) == UserStates.Locked).ToList(),
            UserStateFilter.Pending => users.Where(u => UserStateResolver.Resolve(u, now) == UserStates.Pending).ToList(),
            UserStateFilter.Deleted => users.Where(u => UserStateResolver.Resolve(u, now) == UserStates.Deleted).ToList(),
            _                       => (IReadOnlyList<User>)users,
        };

        if (filtered.Count == 0)
            return new PagedResult([], query.Page, query.PageSize, 0);

        var userIds = filtered.Select(u => u.Id).ToList();
        var profilesByUser = await _profileRepository.GetProfilesByUserIdsAsync(userIds, ct);
        var permissionCountByUser = await _profileRepository.GetPermissionCountsByUserIdsAsync(userIds, ct);

        var items = filtered
            .Select(u => new UserResult(
                Id: u.Id,
                Username: u.Username,
                Email: u.Email,
                State: UserStateResolver.Resolve(u, now),
                IsBootstrap: u.IsBootstrap,
                CreatedAt: u.CreatedAt,
                LastLoginAt: u.LastLoginAt,
                EmailConfirmedAt: u.EmailConfirmedAt,
                LockoutEndAt: u.LockedUntil,
                ProfileCount: profilesByUser.TryGetValue(u.Id, out var profiles) ? profiles.Count : 0,
                ResolvedPermissionCount: permissionCountByUser.GetValueOrDefault(u.Id, 0)))
            .ToList();

        return new PagedResult(items, query.Page, query.PageSize, total);
    }

    private static UserStateFilter ParseStateFilter(string? state)
        => state?.ToLowerInvariant() switch
        {
            null or "" or "all" => UserStateFilter.All,
            "active"            => UserStateFilter.Active,
            "locked"            => UserStateFilter.Locked,
            "pending"           => UserStateFilter.Pending,
            "deleted"           => UserStateFilter.Deleted,
            _                   => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported user state filter."),
        };
}
