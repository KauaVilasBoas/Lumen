# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added (API-AUTHZ-01)
- `AuthorizationGraph.View` permission introduced in the `Authorization` group via the existing
  `PermissionDiscoveryHostedService` auto-discovery mechanism.
- `GET /api/authorization-graph` endpoint added to `AuthorizationGraphController` in the API,
  protected by `[RequirePermission]` + `[Authorize(Policy = PermissionCodes.AuthorizationGraph.View)]`
  + `[PermissionGroup(PermissionGroups.Authorization)]`; auto-registered in the permission catalogue
  on startup (discovery produces code `"AuthorizationGraph.View"`, group `"Authorization"`).
- `AuthorizationGraphHub` (SignalR) added at `/hubs/authorization-graph`, protected by
  `[Authorize(Policy = PermissionCodes.AuthorizationGraph.View)]`; `OnConnectedAsync` re-validates
  the permission via `IUserPermissionService` and calls `Context.Abort()` to reject connections
  where the principal lacks the permission (defence-in-depth beyond the JWT policy gate).
- `PermissionCodes.AuthorizationGraph.View` and `PermissionGroups.Authorization` constants added to
  `AegisIdentity.SharedKernel.Constants.Permissions` — referenced by both API and Backoffice to
  avoid hard-coded strings and enable find-usages refactoring.
- Backoffice `AuthorizationGraphController.Index` now injects `IUserPermissionService` and returns
  `Forbid()` for authenticated users who lack `AuthorizationGraph.View`, blocking direct URL access
  even when the menu entry is suppressed.
- Backoffice `_Layout.cshtml` sidebar entry for Authorization Graph now carries
  `asp-require-permission-controller="AuthorizationGraph"` and `asp-require-permission-action="View"`,
  causing the `RequirePermissionTagHelper` to suppress the nav item for users without the permission.
- `AddSignalR()` registered in the API service collection; `MapHub<AuthorizationGraphHub>` added to
  the middleware pipeline.
- 3 unit tests added in `AuthorizationGraphPermissionDiscoveryTests` covering: correct code
  (`AuthorizationGraph.View`), correct group (`Authorization`), and correct controller/action names.
- 3 integration tests added in `AuthorizationGraphEndpointTests` covering 401 (anonymous), 403
  (authenticated without permission), and 200 (authenticated with permission) for the API endpoint.
- 2 integration tests added in `AuthorizationGraphHubTests` covering Hub connection rejection
  (without permission) and successful connection (with permission); `IntegrationFixture.BuildJwtForUser`
  exposed as a public method to support Hub connection tests with a custom bearer token.
- `Microsoft.AspNetCore.SignalR.Client` (v8.0.15) added to `AegisIdentity.IntegrationTests` and
  `Directory.Packages.props` to enable Hub connection testing in the integration test suite.

### Added (API-USERS-02)
- `GET /api/users/{id:guid}` endpoint — returns full user detail including profiles and lifecycle fields:
  - `GetUserDetailQueryHandler` added to `AegisIdentity.ReadModels.Queries`; accepts a user GUID
    and returns a shaped result with `id`, `username`, `email`, `state`, `isBootstrap`, `createdAt`,
    `emailConfirmedAt`, `lastLoginAt`, `lockoutEndAt`, `profiles[]` (with `profileId`, `name`,
    `isSystem`, `permissionCount`), and `resolvedPermissionCount`.
  - State derivation reuses the same precedence rule as `ListUsersQueryHandler`:
    `IsDeleted → deleted`, `LockedUntil > now → locked`, `EmailConfirmedAt == null → pending`,
    otherwise `active`.
  - Soft-deleted users are accessible by id (`IgnoreQueryFilters`) — the `state` field communicates
    the deleted status to the caller.
  - Per-profile `permissionCount` resolved via `GetActivePermissionProfilesByProfileIdAsync`;
    `resolvedPermissionCount` resolved via `GetPermissionCodesByUserIdAsync` (distinct codes).
  - Returns `404` (`NotFoundException`) when no user matches the requested id (including non-existent
    soft-deleted ids that were never seeded).
  - `GetDetail` action added to `UsersController` — protected by `[RequirePermission]` +
    `[Authorize(Policy = PermissionCodes.Users.Get)]` + `[PermissionGroup(PermissionGroups.Users)]`.
  - `PermissionCodes.Users.List` and `PermissionCodes.Users.Get` constants added to
    `AegisIdentity.SharedKernel.Constants.Permissions`; `PermissionGroups.Users` added alongside.
    `UsersController` migrated from hard-coded `"Users.List"` and `"Users"` strings to these constants.
  - `FindByIdIgnoringFiltersAsync` added to `IUserRepository` and implemented in `UserRepository`
    to support the soft-delete bypass needed by the detail endpoint.
  - 14 unit tests added in `GetUserDetailQueryHandlerTests` covering 404, state derivation (4 cases),
    deleted-user visibility, scalar field mapping, nullable fields, profile list, isSystem flag,
    permission count per profile, resolved permission count, and repository access pattern.
  - 5 integration tests added in `UserDetailEndpointTests` covering 401/403 enforcement, 404 on
    unknown id, 200 response shape, active state derivation, and soft-deleted user visibility.

### Added (API-USERS-01)
- `GET /api/users` endpoint — paginates and filters users with full state derivation:
  - `ListUsersQueryHandler` added to `AegisIdentity.ReadModels.Queries`; accepts `search`,
    `state` (active/locked/pending/deleted/all), `page`, and `pageSize`.
  - `state` derived from domain fields: `IsDeleted → deleted`, `LockedUntil > now → locked`,
    `EmailConfirmedAt == null → pending`, otherwise `active`.
  - `UsersController` added to `AegisIdentity.Api.Controllers` — protected by
    `[RequirePermission]` + `[Authorize(Policy = "Users.List")]` + `[PermissionGroup("Users")]`;
    auto-registered in the permission discovery catalogue.
  - `IUserRepository.ListAsync` added with search, soft-delete bypass (`IgnoreQueryFilters`),
    and server-side pagination (SKIP/TAKE). Implemented in `UserRepository`.
  - `resolvedPermissionCount` and `profileCount` resolved in batch per page (parallel
    `Task.WhenAll`) to avoid N+1; avoids a full permission-table scan per user.
  - Input validation: `page ≥ 1`, `1 ≤ pageSize ≤ 100`, and recognised state values enforced in
    the controller, returning `400` with `ValidationProblemDetails` on violation.
  - 17 unit tests added in `ListUsersQueryHandlerTests` covering state derivation, state
    filtering, profile/permission count, paging metadata, and scalar field mapping.
  - 10 integration tests added in `UsersEndpointTests` covering 401/403 enforcement, response
    shape, input validation (page, pageSize, state), and soft-delete filter semantics.

### Changed (INFRA-07)
- `AegisIdentity.Backoffice` startup configuration aligned with the Api host:
  - `BackofficeApiOptions` (`Api:BaseUrl`) introduced as a typed options class bound from the
    `Api` section. Validation (`[Required]`, `[Url]`, `ValidateOnStart`) replaces the two
    manual `?? throw` guards that existed in `Program.cs`.
  - `HttpClient` factories for `AuthApiClient` and `AdminApiClient` now resolve `BaseAddress`
    from `IOptions<BackofficeApiOptions>` instead of reading configuration directly.
  - `appsettings.json` sanitised: empty string defaults replaced with `"REPLACE_ME"` sentinel
    (consistent with `AegisIdentity.Api/appsettings.json`); inline `_comment` fields added for
    `Api`, `SqlServer`, and `Redis` sections, explicitly noting that Redis is a required
    dependency of the Backoffice for the permission cache.
  - `appsettings.Development.json` updated with guidance on setting the three required secrets
    (`SqlServer:ConnectionString`, `Redis:ConnectionString`, `Api:BaseUrl`) via `dotnet
    user-secrets`.
- `Program.cs` block comments reorganised: each registration call now has a docblock explaining
  its purpose and what options it consumes, matching the explanatory style used in the Api host.
- `README.md`: new "Backoffice required configuration" subsection documents the three required
  variables (`Api:BaseUrl`, `SqlServer:ConnectionString`, `Redis:ConnectionString`) and
  explicitly flags Redis as a required Backoffice dependency. `dotnet user-secrets` examples
  expanded to include Backoffice commands.

### Performance (FIX-05)
- `GetCurrentUserQueryHandler` (endpoint `/me`) no longer calls `IProfileRepository.ListAllAsync`
  (full table scan) followed by an in-memory join. It now calls the new
  `IProfileRepository.GetProfilesByUserIdAsync(userId)`, which pushes the JOIN and WHERE filter
  to the database and returns only the profiles assigned to the requesting user.
- `ListUserProfilesQueryHandler` no longer calls `ListAllAsync`. It continues to use
  `IUserProfileRepository.ListByUserIdAsync` (already filtered) and now resolves the matching
  profiles with the new `IProfileRepository.GetByIdsAsync(ids)`, which issues a single
  `WHERE Id IN (...)` query instead of materialising the entire Profiles table.
- `IProfileRepository` extended with two new server-side query methods:
  - `GetProfilesByUserIdAsync(Guid userId)` — UserProfiles ⋈ Profiles JOIN translated to SQL,
    soft-delete filter applied by EF Core global query filter.
  - `GetByIdsAsync(IReadOnlyList<Guid> ids)` — `WHERE Id IN (...)` batch fetch, no table scan.
- `GetCurrentUserQueryHandler` constructor simplified: the unused `IUserProfileRepository`
  dependency was removed (the new repository method encapsulates the join).
- Unit tests updated: `GetCurrentUserQueryHandlerTests` rewritten against the new two-dependency
  constructor; new test `Handle_DoesNotCallListAllAsync_UsesSingleFilteredQuery` asserts
  `ListAllAsync` is never invoked.
- Unit tests added: `ListUserProfilesQueryHandlerTests` — 6 cases covering empty assignments
  (short-circuit before second DB call), single/multiple assignments, system profile flag,
  orphan assignment exclusion, and `ListAllAsync` never-called assertion.

### Fixed (FIX-04)
- `UserPermissionCache.InvalidateAsync` is now **fail-closed**: when the Redis `RemoveAsync`
  call raises an exception (network error, timeout, Redis unavailable), the exception is
  re-thrown after logging at `Error` level instead of being swallowed.
- Previously the catch block only logged a `Warning` and returned normally, leaving the caller
  unaware that the revocation never reached the cache — a stale entry could keep a revoked
  permission alive for up to the 5-minute TTL (fail-open security regression).
- `GetAsync` and `SetAsync` retain their existing fail-open behaviour: cache-read misses and
  warm-write failures are tolerable degradations, not security issues.
- `IUserPermissionCache.InvalidateAsync` XMLDoc updated to document the fail-closed contract
  and the `Exception` rethrow semantics explicitly.
- Unit tests added (`UserPermissionCacheTests`): `InvalidateAsync` propagates exception on
  Redis failure; `InvalidateAsync` does not throw on success; `InvalidateAsync` removes the
  entry from cache; `GetAsync` returns null on Redis failure (regression guard); `GetAsync`
  deserializes a stored entry correctly; `SetAsync` does not throw on Redis failure (regression guard).
- Unit tests added (`UserPermissionsChangedHandlerTests`): handler propagates cache exception
  to the caller (fail-closed); handler completes normally when invalidation succeeds.

### Fixed (FIX-03)
- `SetProfilePermissionsCommandHandler.Validator` now includes an explicit `NotNull` rule on
  `PermissionIds`, rejecting `null` values with HTTP 400 (`ValidationProblemDetails`) before
  the handler executes.
- Previously, a `null` `permissionIds` body field bypassed the `RuleForEach` rule (FluentValidation
  silently skips iteration on `null` collections), causing a `NullReferenceException` in the
  `foreach` loop that propagated as HTTP 500.
- The fix is purely in the `Validator` class; no changes to the handler logic or repository layer
  were required.
- An empty list (`[]`) remains a valid value and performs a full permission wipe — this behaviour
  is intentional and unaffected by this fix.
- Unit tests added: `Validator_WhenPermissionIdsIsNull_FailsWithRequiredMessage`,
  `Validator_WhenPermissionIdsIsEmpty_PassesListRule`,
  `Validator_WhenPermissionIdsContainsEmptyGuid_FailsItemRule`,
  `Validator_WhenProfileIdIsEmpty_FailsRequiredMessage`,
  `Validator_WhenCommandIsFullyValid_HasNoErrors` — all using `FluentValidation.TestHelper`
  consistent with the project's validator test style.

### Fixed (FIX-02)
- `DeleteProfileCommandHandler` now performs the full cascade soft-delete — `PermissionProfile`
  records, `UserProfile` records, and the `Profile` itself — inside a single database transaction
  via `IProfileRepository.DeleteWithCascadeAsync`.
- Children are soft-deleted in FK order (PermissionProfiles → UserProfiles → Profile) within one
  `SaveChangesAsync` call, preventing partial state if any step fails.
- If the transaction fails at any point, EF Core rolls back automatically; no soft-deleted
  children are left with a live parent record.
- Cache invalidation (`UserPermissionsChanged` events) is only published **after** the
  transaction commits successfully, so a DB failure does not evict cache entries for a write
  that never persisted.
- The `IsSystem` guard introduced in FIX-01 continues to be evaluated before any mutation or
  transaction begins, ensuring it cannot be bypassed.
- New method `IProfileRepository.DeleteWithCascadeAsync` added to the domain interface and
  implemented in `ProfileRepository` using an explicit EF Core `BeginTransactionAsync` scope.
- Unit tests expanded: cascade called with all soft-deleted entities; empty-association path;
  rollback scenario confirms no cache event fires on DB failure; system profile guard confirms
  `DeleteWithCascadeAsync` is never reached.
- Integration tests added: `DeleteWithCascade_SoftDeletesProfileAndAllAssociationsAtomically`
  and `DeleteWithCascade_WithNoAssociations_SoftDeletesOnlyProfile` — both verify the
  persisted state against the real SQL Server container.

### Fixed (FIX-01)
- `DeleteProfileCommandHandler` now raises `ForbiddenException` (HTTP 403) when attempting to
  delete a system profile (`IsSystem = true`), instead of propagating the domain-level
  `InvalidOperationException` as an unhandled 500.
- `UpdateProfileCommandHandler` now raises `ForbiddenException` when attempting to rename a
  system profile; updating only the description (keeping the same name) is still allowed.
- `SetProfilePermissionsCommandHandler` now raises `ForbiddenException` when attempting to
  overwrite permissions on a system profile. Administrator permissions are managed exclusively
  by the startup reconciliation service.
- Unit tests added: `DeleteProfileCommandHandlerTests`, `UpdateProfileCommandHandlerTests`,
  `SetProfilePermissionsCommandHandlerTests` — all three covering system-profile guard paths
  and confirming non-system profiles remain fully editable.

### Added (DOC-01)
- `docs/authz.md` — full reference for the relational authorization model: domain entities,
  permission code convention, data initialization (migrations vs startup reconciliation),
  endpoint protection guide, Redis cache architecture, soft-delete rules, `/me` contract,
  Backoffice helpers, data model diagram, and ADR links.
- `README.md` updated: SQL Server + Redis replace MongoDB as the stack; Authorization section
  added (permission flow, endpoint decoration, Razor helpers, cache summary); Engineering
  decisions, Stack, Solution layout, Getting Started, Configuration, API surface, Roadmap and
  Known Limitations sections updated to reflect the completed authz epic.

### Added (AUTH-16)
- `GET /api/me` now returns `profiles` as a list of objects `{ id, name }` (one entry per
  active profile assignment) instead of the removed `roles` field.
- `GetCurrentUserQueryHandler.Result` gains `Profiles: IReadOnlyList<ProfileSummary>`.
  `ProfileSummary` is a nested record `{ Guid Id, string Name }`.
- Soft-delete respected: only `UserProfile` and `Profile` records that survive the global
  query filter are included; users with no active profiles receive `"profiles": []`.
- Unit tests updated: 0, 1, N profiles; orphan assignment (profile not in repository list)
  excluded; scalar fields mapping preserved.

### Breaking change (AUTH-16)
- `GET /api/me` response no longer contains a `roles` field. Consumers must migrate to
  `profiles[].name` for role-like display and to the permission enforcement layer for
  access control.

### Added (AUTH-15)
- CRUD endpoints for `Profile`: list, create, update, soft-delete (system profiles blocked).
- `GET /api/permissions` — lists discovered permissions grouped by `GroupPermission`;
  exposes `IsOrphan` flag for visibility of stale permissions.
- Endpoint to set permissions on a profile (set of `PermissionProfile` join records);
  "remove" is soft-delete of the join row, never physical deletion.
- Endpoint to assign/remove a `Profile` from a `User` via `UserProfile`; removal is
  soft-delete of the join row.
- All endpoints protected by `[RequirePermission]` (e.g. `Profiles.Manage`,
  `UserProfiles.Assign`), automatically granted to the Administrator profile at startup.
- Cache invalidation via `UserPermissionsChanged` event on every write: profile edits
  invalidate all users holding that profile; assignment changes invalidate the affected user.
- Backoffice UI: Profiles index/create/edit/details/delete views; UserProfiles index/assign
  views — all guarded by the `RequirePermissionTagHelper`.

### Added (AUTH-14)
- `HasPermissionAsync(IHtmlHelper, controller, action)` — Razor HTML helper for conditional
  rendering based on the current user's permissions.
- `RequirePermissionTagHelper` — suppresses an HTML element entirely when the user lacks the
  permission (`asp-require-permission-controller` / `asp-require-permission-action`).
- Both helpers resolve permissions via `IUserPermissionService` (Redis-cached, DB fallback)
  and use the same `ControllerNameNormalizer` + `Permission.BuildCode` convention as the API.

### Added (AUTH-13)
- Fallback authorization policy on the Backoffice: all endpoints require authentication by
  default; `[AllowAnonymous]` opts out.
- `Roles` column physically removed from the `Users` table via EF Core migration. The
  domain model no longer carries a `Roles` property.

### Added (AUTH-12)
- EF Core data migration `SeedDefaultProfiles`: inserts `Profile` "Administrator" and "User"
  (`IsSystem = true`), and the `UserProfile` binding the bootstrap admin to Administrator.
- Startup service `AdministratorPermissionReconciler`: additive-only diff — grants the
  Administrator profile every `Permission` not yet associated, without touching other
  profiles or removing existing associations.
- Startup order guaranteed: `Database.Migrate()` → discovery → reconciliation.

### Added (AUTH-11)
- `IUserPermissionCache` (Domain) with Redis implementation in `UserPermissionCache`
  (DataAccess). Safety-net TTL: 5 minutes.
- `IUserPermissionService` — resolution layer: Redis → DB fallback → repopulate cache.
  Redis unavailability never propagates as an authorization failure.
- `UserPermissionsChanged` domain event + `InvalidateUserPermissionsEventHandler` for
  event-driven cache invalidation.

### Added (AUTH-09)
- `PermissionDiscoveryService`: scans all registered `IActionDescriptor`s at startup,
  upserts `Permission` records derived from `[RequirePermission]`-decorated actions, and
  marks removed actions as `IsOrphan = true` (soft-delete rule: never physically deleted).
- Dynamic `IAuthorizationPolicyProvider` resolves permission requirements at request time
  without pre-registering named policies.

### Added (AUTH-08)
- Domain entities: `Permission`, `GroupPermission`, `Profile`, `PermissionProfile`,
  `UserProfile` — all implementing `ISoftDeletable`.
- Repository interfaces: `IPermissionRepository`, `IProfileRepository`,
  `IUserProfileRepository`.
- EF Core configuration and SQL Server migrations for all five entities with filtered unique
  indexes and global query filters.

### Changed (INFRA-01..06)
- **Persistence migrated from MongoDB to SQL Server + EF Core 8** (see
  [ADR-0001](docs/adr/0001-mongodb-to-relational-efcore.md)):
  - `AegisIdentity.DataAccess`: `MongoDbContext` + Mongo repositories replaced by
    `AegisIdentityDbContext` (EF Core) + SQL Server repositories.
  - `AegisIdentity.Migrations` / `AegisIdentity.Migrations.Cli`: Mongo migration runner
    removed; EF Core migrations take over schema + data management.
  - `AegisIdentity.Jobs`: `Hangfire.Mongo` replaced by `Hangfire.SqlServer`.
  - `User.Id`: `string` ObjectId hex replaced by `Guid`.
  - `docker-compose.yml`: `mongo` service replaced by `sqlserver` (SQL Server 2022) and
    `redis` (Redis 7).
  - EF Core global query filters + filtered unique indexes implement the soft-delete strategy.
  - Bootstrap admin user inserted via EF Core data migration (see
    [ADR-0002](docs/adr/0002-admin-bootstrap-credential.md)).
  - Redis distributed cache added for permission resolution (INFRA-06 / AUTH-11).

### Added (AUTH-02)
- **`POST /api/auth/login`** endpoint (`src/AegisIdentity.Api/Endpoints/Auth/LoginEndpoint.cs`):
  - Accepts `{ identifier, password }` where `identifier` is an email address or a username.
    Discriminated by the presence of `@`: if the identifier contains `@`, the lookup is by
    normalised email; otherwise by username.
  - Returns **200 OK** with `{ accessToken, refreshToken, expiresIn }` on success.
  - Returns **400 Bad Request** (ValidationProblem) when `identifier` or `password` is blank.
  - Returns **401 Unauthorized** for invalid credentials (user not found or wrong password).
    The same response is returned for both cases to prevent user enumeration.
  - Returns **403 Forbidden** when the account exists but has not confirmed its email.
  - Returns **423 Locked** when the account is temporarily locked due to repeated failures.
- **`ILoginUserUseCase`** / **`LoginUserUseCase`** (`src/AegisIdentity.Application/Auth/Login/`):
  - Resolves the user by email or username depending on the identifier format.
  - Checks lockout **before** password verification — prevents BCrypt work on locked accounts.
  - Calls `IPasswordHasher.Verify` for constant-time comparison.
  - On wrong password: increments `FailedLoginAttempts` via `User.RecordFailedLogin` and
    persists the change. Account is locked when the threshold is reached.
  - On success: resets failed attempts (`User.Unlock`) if any, updates `LastLoginAt`, persists
    via `IUserRepository.UpdateAsync`, issues a JWT access token, generates an opaque refresh
    token value, hashes it (SHA-256 hex), persists the `RefreshToken` aggregate via
    `IRefreshTokenRepository.InsertAsync`.
  - `ExpiresIn` in the response is derived from `IJwtService.AccessTokenExpiresIn` — no
    hardcoded constant.
- **`IJwtService`** interface in Application (`src/AegisIdentity.Application/Security/IJwtService.cs`):
  - `GenerateAccessToken(User)` → signed JWT.
  - `GenerateRefreshTokenValue()` → URL-safe Base64 (32 random bytes).
  - `AccessTokenExpiresIn` → token lifetime in seconds.
- **`JwtService`** in Infrastructure (`src/AegisIdentity.Infrastructure/Security/JwtService.cs`):
  - HS256-signed JWT with claims: `sub` (user ID), `email`, `username`, `jti`, `role[]`.
  - Expiry driven by `Jwt:ExpirationMinutes` configuration.
  - Registered as **singleton** in `SecurityServiceExtensions`.
- **`LoginRequest`** / **`LoginResponse`** / **`LoginResult`** DTOs in Application.
- **`LoginRequestValidator`** (FluentValidation): `identifier` and `password` required (not empty).
- **`IAppSettings`** extended with `LockoutThreshold`, `LockoutDuration`, `RefreshTokenExpirationDays`.
- **`AppOptions`** extended with `LockoutThreshold` (default 5), `LockoutDurationMinutes` (default 15),
  `RefreshTokenExpirationDays` (default 7) — all validated with `[Range]` on startup.
- **`AppSettingsAdapter`** updated to expose the three new properties.
- **Unit tests** (30 new scenarios):
  - `LoginRequestValidatorTests` (7): identifier required, password required, full valid request.
  - `LoginUserUseCaseTests` (13): email not found, username not found, email vs username routing,
    wrong password, wrong password increments attempts, account locked skips verify,
    account locked returns expiry, email not confirmed, happy path returns tokens,
    happy path inserts refresh token, happy path updates `LastLoginAt`,
    previous failures reset on success.
  - `JwtServiceTests` (10): valid JWT structure, signature verifies, `sub` claim, `email` claim,
    `username` claim, role claims, expiry matches config, refresh value non-empty, values differ
    each call, URL-safe charset.

### Added (AUTH-01)
- **`POST /api/auth/register`** endpoint (`src/AegisIdentity.Api/Endpoints/Auth/RegisterEndpoint.cs`):
  - Returns **201 Created** with `{ id, email, username }` on success.
  - Returns **400 Bad Request** (ValidationProblem) on FluentValidation failure or weak password.
  - Returns **409 Conflict** with a field-specific message on duplicate email or username.
  - Registered for all environments (not dev-only).
- **`IPasswordHasher`** / **`BCryptPasswordHasher`**:
  - `IPasswordHasher` interface in Application (`src/AegisIdentity.Application/Security/IPasswordHasher.cs`): `Hash(plainText)` + `Verify(plainText, hash)`.
  - `BCryptPasswordHasher` in Infrastructure (`src/AegisIdentity.Infrastructure/Security/BCryptPasswordHasher.cs`): BCrypt work factor **12**. Registered as singleton in DI.
- **`IRegisterUserUseCase`** / **`RegisterUserUseCase`** (`src/AegisIdentity.Application/Auth/Register/`):
  - Validates password strength via `IPasswordValidator` before hashing — weak passwords are rejected without touching the database.
  - Hashes the password with `IPasswordHasher` (BCrypt cost 12).
  - Inserts the user via `IUserRepository` (user is created with `IsActive = false`).
  - Translates `DuplicateEmailException` / `DuplicateUsernameException` to typed `RegisterResult` variants — no MongoDB dependency in the use case.
  - Generates a **32-byte cryptographically random token**, Base64Url-encoded for use in email links.
  - Persists only the **SHA-256 hex hash** of the token in `email_confirmation_tokens` via `IEmailConfirmationTokenRepository`. Token is valid for **24 hours**.
  - Dispatches a confirmation email via `IEmailService` using the `EmailConfirmation` template with `UserName` and `ConfirmationUrl` placeholders. The email step is **fail-open**: SMTP failures are logged and swallowed — the 201 is still returned.
  - Confirmation URL: `{App:BaseUrl}/api/auth/confirm-email?token={rawToken}` (endpoint not yet implemented — AUTH-10).
- **`RegisterRequest`** / **`RegisterResponse`** / **`RegisterResult`** DTOs in Application.
- **`RegisterRequestValidator`** (FluentValidation): email format, username 3–32 chars alphanumeric/underscore/hyphen, password required.
- **`DuplicateEmailException`** / **`DuplicateUsernameException`** in Domain — thrown by `UserRepository.InsertAsync` when MongoDB returns error code 11000. Index name (`ix_email_unique` / `ix_username_unique`) used to distinguish the two cases.
- **`IAppSettings`** interface in Application (`src/AegisIdentity.Application/Configuration/`) + `AppSettingsAdapter` in Infrastructure: bridges `AppOptions` (Infrastructure) to use cases without coupling them to `IOptions<T>`.
- **`AppOptions`** (`App:BaseUrl`) registered in `InfrastructureOptionsExtensions` with `ValidateOnStart`. Added to all `appsettings*.json` files.
- **`IEmailTemplateRenderer`** interface in Application + `EmailTemplateRendererAdapter` in Infrastructure: decouples use cases from the concrete `EmailTemplateRenderer` Infrastructure type.
- **`AuthServiceExtensions.AddAuthUseCases()`** registers `IValidator<RegisterRequest>` and `IRegisterUserUseCase` as scoped services.
- **`Microsoft.Extensions.Logging.Abstractions`** added to `AegisIdentity.Application.csproj` (logging without ASP.NET Core dependency).
- **Unit tests** (29 new scenarios):
  - `RegisterRequestValidatorTests` (13): email required/format/length, username required/min/max/pattern, password required, full valid request.
  - `RegisterUserUseCaseTests` (13): weak password returns `WeakPassword`, duplicate email/username returns correct variant, success returns user data, password is hashed, user starts inactive, token is inserted, confirmation email is sent, confirmation URL contains BaseUrl, validator receives email+username context.
  - `BCryptPasswordHasherTests` (8): non-empty output, salt randomisation, BCrypt format, correct verify, wrong-password verify, empty-input guards.

### Added (EMAIL-01)
- **`IEmailService`** in Domain (`src/AegisIdentity.Domain/Notifications/IEmailService.cs`):
  - Single-method abstraction: `Task SendAsync(EmailMessage, CancellationToken)`.
  - Lives in Domain so future Application handlers (AUTH-08 forgot-password,
    AUTH-09 reset, AUTH-10 confirm-email, USER-05 password-change) can depend on it
    without taking an Infrastructure reference.
- **`EmailMessage`** record (`To`, `Subject`, `HtmlBody`, `TextBody`) — Domain DTO
  consumed by `IEmailService`.
- **`MailKitEmailService`** in Infrastructure (`src/AegisIdentity.Infrastructure/Notifications/MailKitEmailService.cs`):
  - Builds a `multipart/alternative` MIME message via `BodyBuilder` when both bodies
    are supplied; falls back to single-part `TextPart` when only one is.
  - Implements the EMAIL-01 retry contract: **2 attempts max** with a 500 ms back-off
    between them. Configured via `MaxAttempts` / `RetryDelay` constants.
  - **Fail-open**: transport failures are caught and logged at `Warning` level —
    they never propagate to the caller. An outage of the SMTP provider therefore
    cannot bring down user registration / password reset flows. The single exception
    is `OperationCanceledException` originating from a caller-supplied
    `CancellationToken`, which is intentionally re-thrown so deadline races behave correctly.
- **`ISmtpTransport`** + **`MailKitSmtpTransport`** (`src/AegisIdentity.Infrastructure/Notifications/`):
  - Thin port over `MailKit.Net.Smtp.SmtpClient` introduced as a unit-test seam
    (sending real SMTP from a unit test was rejected — would couple every test run to Mailpit).
  - 10 s `Timeout` on `ConnectAsync` per the EMAIL-01 risk mitigation. Honours
    `SecureSocketOptions.StartTlsWhenAvailable` when `Smtp:UseStartTls=true` —
    one config value works for both Mailpit (no TLS) and real providers (TLS).
- **`EmailTemplateRenderer`** (`src/AegisIdentity.Infrastructure/Notifications/EmailTemplateRenderer.cs`):
  - Loads templates as **embedded resources** and substitutes `{{Placeholder}}` tokens.
  - Template content is cached in a static `ConcurrentDictionary` after the first read —
    the embedded payload never changes between calls.
  - **Razor rejected for the MVP**: it would either drag ASP.NET into Infrastructure
    (via `Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation`) or push template
    rendering into the API layer, both for three short transactional emails. Placeholder
    substitution covers the EMAIL-01 acceptance criteria with zero extra dependencies.
    Razor or RazorLight remains an option if email layouts grow complex later.
- **Templates** under `src/AegisIdentity.Infrastructure/Templates/Email/` (embedded resources):
  - `EmailConfirmation.{html,txt}` — placeholders `UserName`, `ConfirmationUrl`.
  - `PasswordReset.{html,txt}` — placeholders `UserName`, `ResetUrl`.
  - `PasswordChanged.{html,txt}` — placeholders `UserName`, `ChangedAt`.
- **`AddNotifications()`** DI extension
  (`src/AegisIdentity.Infrastructure/Notifications/NotificationsServiceExtensions.cs`):
  - Registers `EmailTemplateRenderer` as **singleton** (stateless, caches templates),
    `ISmtpTransport` and `IEmailService` as **scoped**. Wired into `Program.cs`.
- **`/dev/email-test` refactored** to go through `IEmailService`:
  - Endpoint now renders the `EmailConfirmation` template and dispatches via the service,
    exercising the full pipeline end-to-end. The previous inline MailKit usage was removed,
    and the `MailKit` `PackageReference` was dropped from the `Api` csproj (Infrastructure owns it).
- **Unit tests** (15 new scenarios):
  - `EmailTemplateRendererTests` (7): placeholder substitution across all three templates,
    null-value defensiveness, unknown-key inertness, unsubstituted-token visibility,
    and null-dictionary rejection.
  - `MailKitEmailServiceTests` (8): first-attempt success, retry-then-success, both-attempts-fail
    fail-open, timeout fail-open, caller cancellation propagation, multipart MIME shape,
    text-only message shape, and null-message rejection.
  - `FakeSmtpTransport` queue-based fake replaces real SMTP I/O — tests run offline in <2 s.
  - `InternalsVisibleTo("AegisIdentity.UnitTests")` added to Infrastructure so tests can
    reach `MailKitEmailService.MaxAttempts` and related test hooks.

### Added (SEC-04)
- **`IPasswordValidator`** (`src/AegisIdentity.Application/Security/IPasswordValidator.cs`):
  - Application-layer contract: `Task<PasswordValidationResult> ValidatePasswordAsync(PasswordValidationContext, CancellationToken)`.
  - Consumed by future AUTH-01 (registration), USER-05 (password change) and AUTH-09 (password reset) handlers.
- **`PasswordValidationContext`** record (`Password`, `Email`, `Username`) — input.
- **`PasswordValidationResult`** record (`IsValid`, `Errors`) — output. Aggregates every failed rule's PT-BR message so callers can surface all violations at once.
- **`PasswordValidator`** (`src/AegisIdentity.Application/Security/PasswordValidator.cs`):
  - Implements both `IPasswordValidator` and FluentValidation's
    `AbstractValidator<PasswordValidationContext>`. Callers can pick either contract.
  - Rules (PT-BR error messages, exactly as specified by the card):
    - Minimum **12 characters** — `"A senha deve ter no mínimo 12 caracteres."`
    - At least one uppercase letter — `"A senha deve conter pelo menos uma letra maiúscula."`
    - At least one lowercase letter — `"A senha deve conter pelo menos uma letra minúscula."`
    - At least one digit — `"A senha deve conter pelo menos um dígito."`
    - At least one special character from ``!@#$%^&*()-_=+[]{};:'",.<>/?\|`~`` — `"A senha deve conter pelo menos um caractere especial."`
    - Password must not match email or username (case-insensitive) — `"A senha não pode ser igual ao seu email/username."`
    - Password must not appear in the HIBP database — `"Esta senha aparece em vazamentos públicos conhecidos. Escolha outra."`
  - HIBP check is gated behind a `When(...)` clause that only runs once every
    structural rule passes — a clearly weak password never burns an external HTTP call.
- **`AddApplicationSecurity()`** DI extension (`src/AegisIdentity.Application/Security/SecurityServiceExtensions.cs`):
  - Registers `IPasswordValidator` → `PasswordValidator` as **scoped**. Wired into `Program.cs`.
- **`Microsoft.Extensions.DependencyInjection.Abstractions` 8.0.2** added to the
  central package versions and referenced by the Application project so it can
  expose `IServiceCollection`-based extension methods without taking a dependency
  on the full DI container or on ASP.NET Core.
- **Unit tests** (`tests/AegisIdentity.UnitTests/Application/Security/PasswordValidatorTests.cs`) — 48 scenarios:
  - Length boundary at 11 vs 12 characters.
  - Missing uppercase / lowercase / digit / special character.
  - Password equal to email and username with case-insensitive theories.
  - HIBP hit (mocked `IPwnedPasswordsClient` returning `true`) — fails the rule.
  - HIBP miss — passes.
  - Error-message accumulation when multiple structural rules fail at once.
  - Assertion that the HIBP client is **never called** when structural rules fail.
  - 32-case theory covering every character in the allowed special-character set.

### Added (SEC-05)
- **`IPwnedPasswordsClient`** in Domain (`src/AegisIdentity.Domain/Security/IPwnedPasswordsClient.cs`):
  - Single-method abstraction: `Task<bool> IsPwnedAsync(string password, CancellationToken)`.
  - Lives in Domain (Dependency Inversion) so `PasswordValidator` in Application
    can depend on it without taking an Infrastructure reference.
- **`PwnedPasswordsClient`** in Infrastructure (`src/AegisIdentity.Infrastructure/Security/PwnedPasswordsClient.cs`):
  - Queries the public HaveIBeenPwned Pwned Passwords API at
    `https://api.pwnedpasswords.com/range/{prefix}`.
  - **k-anonymity**: sends only the first 5 hex characters of `SHA1(password)`;
    the full hash and the password itself never leave the process.
  - Sends `Add-Padding: true` to defeat traffic-size correlation, and a configurable
    `User-Agent` from `Hibp:UserAgent` (HIBP rejects empty user-agents with 403).
  - **Fail-open**: timeouts (`TaskCanceledException` with inner `TimeoutException`),
    HTTP error status (`EnsureSuccessStatusCode`) and `HttpRequestException` are
    caught and logged at `Warning` level; the method returns `false` so an outage
    of an external dependency cannot block registration.
  - **In-memory cache** via `IMemoryCache` keyed on the range prefix, TTL **1 hour** —
    chosen to match the HIBP card requirement: keeps dev loops fast and stays well
    under the 1.5M req/day soft limit. Test inputs that share a prefix hit the API
    only once.
- **`AddSecurity()`** DI extension (`src/AegisIdentity.Infrastructure/Security/SecurityServiceExtensions.cs`):
  - Registers `IPwnedPasswordsClient` → `PwnedPasswordsClient` via
    `AddHttpClient<TClient, TImplementation>` with a typed `HttpClient`:
    `BaseAddress` from `Hibp:ApiBaseUrl`, `Timeout = 2s`, the required headers
    pre-configured. Also calls `AddMemoryCache()`.
  - Wired into `Program.cs`.
- **`Microsoft.Extensions.Caching.Memory` 8.0.1** added to the central package versions
  and referenced by the Infrastructure project.
- **Unit tests** (`tests/AegisIdentity.UnitTests/Infrastructure/Security/PwnedPasswordsClientTests.cs`) — 9 scenarios:
  - Suffix-present with positive count → `true`.
  - Suffix-absent → `false`.
  - Suffix-present with count `0` (HIBP padding entries) → `false`.
  - Server error (`500`) → fail-open `false`, no throw.
  - `HttpRequestException` → fail-open `false`, no throw.
  - HTTP-client timeout (`TaskCanceledException(TimeoutException)`) → fail-open
    `false`, no throw.
  - Two calls sharing a prefix → API is hit only once (cache contract).
  - Empty password → `ArgumentException`.
  - Outbound request URL shape matches `https://api.pwnedpasswords.com/range/{prefix}`.
  - Backed by a hand-rolled `StubHttpMessageHandler` so the suite never touches the network.
- **Integration test** (`tests/AegisIdentity.IntegrationTests/Security/PwnedPasswordsClientIntegrationTests.cs`)
  marked `[Trait("Category","ExternalApi")]` — calls the real HIBP API for smoke checks
  (`"password"` → `true`, fresh random Guid → `false`). Excluded from default test runs
  to keep CI deterministic; run explicitly with
  `dotnet test --filter "Category=ExternalApi"`.

### Added (DATA-03)
- **`RefreshToken` aggregate** (`src/AegisIdentity.Domain/Tokens/RefreshToken.cs`):
  - Properties: `Id`, `UserId`, `TokenHash` (SHA-256 — never plaintext), `CreatedByIp`,
    `ReplacedByTokenHash`, `CreatedAt`, `ExpiresAt`, `RevokedAt`.
  - Static factory `RefreshToken.Create(userId, tokenHash, expiresAt, createdByIp)` —
    guards all required fields; rejects `expiresAt` in the past.
  - Behaviour: `IsExpired()`, `IsRevoked()`, `IsActive()`, `Revoke(replacedByTokenHash?)`.
  - `ReplacedByTokenHash` enables auditable rotation chains (token A → B → C).
  - Zero external dependencies — pure domain model.
- **`PasswordResetToken` aggregate** (`src/AegisIdentity.Domain/Tokens/PasswordResetToken.cs`):
  - Properties: `Id`, `UserId`, `TokenHash`, `CreatedAt`, `ExpiresAt`, `UsedAt`.
  - Factory and behaviour: `Create(userId, tokenHash, expiresAt)`, `IsExpired()`, `IsUsed()`,
    `IsValid()`, `MarkAsUsed()`. Single-use enforced by `UsedAt` presence check.
- **`EmailConfirmationToken` aggregate** (`src/AegisIdentity.Domain/Tokens/EmailConfirmationToken.cs`):
  - Same structure and behaviour as `PasswordResetToken`, scoped to email confirmation flow.
- **Repository interfaces** in Domain (Dependency Inversion):
  - `IRefreshTokenRepository`: `FindByTokenHashAsync`, `FindByUserIdAsync`, `InsertAsync`, `UpdateAsync`.
  - `IPasswordResetTokenRepository`: `FindByTokenHashAsync`, `InsertAsync`, `UpdateAsync`.
  - `IEmailConfirmationTokenRepository`: `FindByTokenHashAsync`, `InsertAsync`, `UpdateAsync`.
- **BSON class maps** in Infrastructure (same pattern as `UserClassMap`):
  - `RefreshTokenClassMap`, `PasswordResetTokenClassMap`, `EmailConfirmationTokenClassMap`.
  - Each registered once via double-checked lock; called from `MongoDbContext.RegisterClassMapsOnce()`.
- **`CollectionNames` updated** — added constants:
  - `RefreshTokens = "refresh_tokens"`, `PasswordResetTokens = "password_reset_tokens"`,
    `EmailConfirmationTokens = "email_confirmation_tokens"`.
- **`MongoIndexInitializer` extended** with indexes for all three token collections:
  - `refresh_tokens`: `ix_tokenHash_unique` (unique), `ix_userId` (non-unique), `ix_expiresAt_ttl` (TTL).
  - `password_reset_tokens`: `ix_tokenHash_unique` (unique), `ix_expiresAt_ttl` (TTL).
  - `email_confirmation_tokens`: `ix_tokenHash_unique` (unique), `ix_expiresAt_ttl` (TTL).
  - TTL indexes use `ExpireAfter = TimeSpan.Zero` — MongoDB auto-deletes expired documents.
    Application code still validates `ExpiresAt` in code (TTL cleanup has ~60 s delay).
- **Repository implementations**: `RefreshTokenRepository`, `PasswordResetTokenRepository`,
  `EmailConfirmationTokenRepository` — all follow `UserRepository` pattern.
- **`MongoDbServiceExtensions` updated** — registers the three new `IRepository` interfaces as **scoped**.
- **Unit tests** (36 scenarios across 3 files):
  - Factory guard clauses (blank args, past `ExpiresAt`).
  - `IsExpired`, `IsRevoked`/`IsUsed`, `IsActive`/`IsValid` state transitions.
  - `Revoke` with and without `replacedByTokenHash`; `MarkAsUsed` timestamp.
- **Integration tests** (Testcontainers `mongo:7`, 4–5 scenarios per entity):
  - Insert → generate ObjectId; duplicate hash → `MongoWriteException`.
  - `FindByTokenHashAsync` happy path and not-found.
  - `UpdateAsync` persists revocation/usage state.
  - Index existence verification (unique hash, TTL, userId for refresh tokens).

### Added (DATA-02)
- **`User` aggregate** (`src/AegisIdentity.Domain/Users/User.cs`):
  - Domain entity with all required properties: `Id` (string/ObjectId), `Email` (normalised),
    `Username`, `PasswordHash`, `Roles` (default `["user"]`), `IsActive` (default `false`),
    `EmailConfirmedAt`, `LastLoginAt`, `FailedLoginAttempts`, `LockedUntil`,
    `CreatedAt`, `UpdatedAt`.
  - Static factory `User.Create(email, username, passwordHash)` — enforces normalisation
    and non-null invariants at creation time.
  - `NormalizeEmail` static helper: lowercase + trim, must be applied before persist/compare.
  - Brute-force protection behaviour: `RecordFailedLogin(threshold, duration)` and `Unlock()`.
  - `IsLockedOut()` predicate for login gate checks.
  - Zero external dependencies — pure domain model.
- **`IUserRepository`** (`src/AegisIdentity.Domain/Users/IUserRepository.cs`):
  - Domain-layer interface (Dependency Inversion): `FindByEmailAsync`, `FindByIdAsync`,
    `FindByUsernameAsync`, `InsertAsync`, `UpdateAsync` — all with `CancellationToken`.
- **`UserClassMap`** (`src/AegisIdentity.Infrastructure/Persistence/Mappings/UserClassMap.cs`):
  - `BsonClassMap.RegisterClassMap<User>` with explicit element names matching camelCase
    convention. `Id` mapped as `ObjectId` on the wire via `StringObjectIdGenerator` +
    `StringSerializer(BsonType.ObjectId)`, keeping the domain model free of MongoDB types.
  - Registered once via double-checked lock, called from `MongoDbContext` constructor.
- **`CollectionNames`** (`src/AegisIdentity.Infrastructure/Persistence/CollectionNames.cs`):
  - Central constant registry for MongoDB collection names (`users`). Eliminates magic strings.
- **`MongoIndexInitializer`** (`src/AegisIdentity.Infrastructure/Persistence/Indexes/MongoIndexInitializer.cs`):
  - `IHostedService` that creates all required indexes on startup before the app accepts requests.
  - Idempotent: named `CreateIndexModel` calls are no-ops when the index already exists.
  - Indexes: `ix_email_unique` (unique), `ix_username_unique` (unique),
    `ix_lockedUntil_sparse` (sparse — keeps index small since most users are not locked).
- **`UserRepository`** (`src/AegisIdentity.Infrastructure/Persistence/Repositories/UserRepository.cs`):
  - MongoDB implementation of `IUserRepository`. Guards against invalid ObjectId strings in
    `FindByIdAsync` (returns `null` instead of throwing).
- **`MongoDbContext` updated** to call `UserClassMap.RegisterOnce()` alongside conventions.
- **`MongoDbServiceExtensions` updated**:
  - Registers `IUserRepository` → `UserRepository` as **scoped**.
  - Registers `MongoIndexInitializer` as a **hosted service**.
- **Unit tests** (`tests/AegisIdentity.UnitTests/Domain/Users/UserTests.cs`) — 21 scenarios:
  - Factory validation (blank args, email normalisation, default state, timestamp).
  - `NormalizeEmail` theory (3 cases).
  - Lockout behaviour: below threshold, at threshold, `Unlock`, `IsLockedOut` edge cases.
  - Role independence (separate `List<string>` per instance).
- **Integration tests** (`tests/AegisIdentity.IntegrationTests/Persistence/UserRepositoryIntegrationTests.cs`):
  - Full CRUD scenarios via Testcontainers `mongo:7` container.
  - Index existence verification for all three indexes.
  - Duplicate-email rejection after unique index creation.

### Added (DATA-01)
- **`MongoDbContext`** (`src/AegisIdentity.Infrastructure/Persistence/MongoDbContext.cs`):
  - Singleton wrapper over `IMongoDatabase` exposing `GetCollection<T>(name)` and a
    `Database` property for raw-command access.
  - Owns one-time `ConventionPack` registration via a double-checked lock:
    `CamelCaseElementNameConvention`, `IgnoreExtraElementsConvention(true)`,
    `EnumRepresentationConvention(BsonType.String)`.
- **`MongoDbServiceExtensions`** (`src/AegisIdentity.Infrastructure/Persistence/MongoDbServiceExtensions.cs`):
  - `AddMongoDb(IServiceCollection, IConfiguration)` extension method.
  - Registers `IMongoClient` as **singleton** (driver manages its own connection pool),
    `IMongoDatabase` as **scoped** (cheap factory from the singleton client, aligned with
    unit-of-work boundaries), and `MongoDbContext` as **singleton**.
- **`MongoDbHealthCheck`** (`src/AegisIdentity.Infrastructure/HealthChecks/MongoDbHealthCheck.cs`):
  - Implements `IHealthCheck`; issues `{ ping: 1 }` against the configured database.
  - Returns `HealthCheckResult.Healthy` on success, `Unhealthy` with the caught exception
    on failure.
- **Health check endpoint** `GET /health/db` registered in `Program.cs`:
  - Returns `200 OK` with a JSON body when MongoDB is reachable, `503 Service Unavailable`
    when the ping fails.
  - Requests to `/health` are already downgraded to `Verbose` in `UseSerilogRequestLogging`,
    so health probes do not pollute dashboards.
- **Integration tests** (`tests/AegisIdentity.IntegrationTests/Persistence/MongoDbContextIntegrationTests.cs`):
  - Four scenarios using `Testcontainers.MongoDb` (ephemeral `mongo:7` container):
    `GetCollection` handle validation, insert-and-read roundtrip, health check Healthy
    path, health check Unhealthy path.
  - Requires Docker Desktop with the daemon accessible to the test process
    (TCP on `localhost:2375` or membership in the `docker-users` group).

### Added (SETUP-05)
- **`docker-compose.yml`** at project root defining two services for the local development stack:
  - `mailpit` (`axllent/mailpit:latest`): catches all outbound emails from the app and exposes
    them in a web UI. SMTP on `localhost:1025`; UI on http://localhost:8025.
    Messages are **not persisted** — intentional design decision to keep dev inboxes clean
    between restarts. Persistence can be re-enabled by uncommenting the volume in the compose file.
  - `mongo` (`mongo:7`): local MongoDB instance on `localhost:27017` with a named volume
    `mongo-data` for data persistence across restarts. Includes a `healthcheck` using
    `mongosh --eval 'db.runCommand({ ping: 1 })'` so dependent services know when Mongo is ready.
- **`.mailpit-data/` added to `.gitignore`** to prevent accidental commit of Mailpit data
  if the optional persistence volume is ever re-enabled.
- **Production hardening** in `Program.cs`: the app throws `InvalidOperationException` at startup
  if `Smtp:Host` resolves to a loopback address (`localhost`, `127.0.0.1`, `::1`) when
  `ASPNETCORE_ENVIRONMENT=Production`. Prevents silent email loss from a misconfigured deploy.
- **Dev-only email smoke test endpoint** (`GET /dev/email-test?to=<address>`):
  - Registered at `src/AegisIdentity.Api/Endpoints/Dev/EmailTestEndpoint.cs`.
  - Available **only** when `ASPNETCORE_ENVIRONMENT=Development` — never in Staging or Production.
  - Sends a plain-text message through the configured SMTP relay (Mailpit in dev) and returns
    `200 { "ok": true, "to": "...", "viewer": "http://localhost:8025" }` on success,
    `500` with error detail on failure.
  - Uses `SmtpOptions` injected from DI — no hard-coded SMTP settings.
  - Inline Minimal API style; no domain coupling.

### Added (SETUP-04)
- **Serilog two-stage initialization** in `Program.cs`: bootstrap logger captures startup
  errors before DI is ready; full logger (from `appsettings`) takes over after
  `WebApplication.CreateBuilder`. Fatal exceptions are caught and flushed before exit.
- **Structured logging configuration** in `appsettings.json`:
  - Minimum levels: `Information` default, `Warning` for `Microsoft.AspNetCore` and `System`,
    `Information` for `Microsoft.AspNetCore.Hosting.Diagnostics`.
  - Console sink with `CompactJsonFormatter` (production-ready JSON).
  - File sink: `logs/aegis-.log`, daily rolling, 7-day retention, `CompactJsonFormatter`.
  - Enrichers: `FromLogContext`, `WithMachineName`, `WithThreadId`.
- **Development log override** in `appsettings.Development.json`:
  - Console only (no file sink), `Debug` minimum level, human-readable `outputTemplate`.
- **`CorrelationIdMiddleware`** (`src/AegisIdentity.Api/Middleware/`):
  - Reads `X-Correlation-Id` request header; generates a 32-char hex Guid when absent.
  - Writes value to response header and pushes it to Serilog `LogContext` so every log
    entry in the request scope carries `CorrelationId`.
  - Registered before `UseSerilogRequestLogging` so the request-completion log includes the field.
- **Health check log filter** in `UseSerilogRequestLogging`: requests to `/health` are
  logged at `Verbose` level (will not appear under default `Information` minimum) to
  avoid dashboard pollution when health-check endpoints are implemented.
- **`SensitiveDataConvention`** (`src/AegisIdentity.Api/Logging/`):
  - Static class documenting which fields (`Password`, `PasswordHash`, `Token`,
    `AccessToken`, `RefreshToken`, `ResetCode`, `Secret`) must never be passed as
    structured log arguments.
  - Enforcement is by convention and code review at this stage. A destructuring policy
    or log-sink filter will be added in the security hardening card when the corresponding
    use cases exist. Premature defensive code was intentionally omitted (YAGNI).
- **New NuGet packages** pinned in `Directory.Packages.props`:
  - `Serilog.Formatting.Compact` 3.0.0
  - `Serilog.Enrichers.Environment` 3.0.1
  - `Serilog.Enrichers.Thread` 4.0.0
- **Unit tests** for `CorrelationIdMiddleware` (4 scenarios):
  - Generates new ID when header is absent.
  - Preserves incoming ID when header is present.
  - Always sets response header.
  - Generated ID matches 32-char hex format (Guid "N").


### Added
- `JwtOptions`, `MongoOptions`, `SmtpOptions`, `HibpOptions` in `Infrastructure/Configuration/`
  with `[Required]`, `[MinLength]`, `[Range]` and `[Url]` data-annotation constraints.
- Startup validation via `AddOptions<T>().Bind(...).ValidateDataAnnotations().ValidateOnStart()`
  registered in `InfrastructureOptionsExtensions`. Any missing required value causes an
  `OptionsValidationException` before the app begins accepting requests.
- Full `appsettings.json` schema with safe `REPLACE_ME` placeholders covering:
  `Mongo`, `Jwt`, `Smtp`, `Hibp`, `Cors` and `Serilog` sections.
- `appsettings.Development.json` with local dev defaults: Mongo on `mongodb://localhost:27017`
  and SMTP on Mailpit (`localhost:1025`).
- `appsettings.example.json` versionable template documenting all keys and their expected
  format with `<set via env or user-secrets>` placeholders.
- `UserSecretsId` already present in `AegisIdentity.Api.csproj` from SETUP-01 bootstrap;
  confirmed operational via `dotnet user-secrets list`.
- README: table of required environment variables, local setup via `dotnet user-secrets`,
  and production configuration via env vars (Fly.io / Docker formats).

### Added
- Central Package Management: all MVP NuGet dependencies pinned in `Directory.Packages.props`.
  - **Persistence:** `MongoDB.Driver` 2.30.0
  - **Auth / Security:** `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.15,
    `System.IdentityModel.Tokens.Jwt` 8.9.0, `BCrypt.Net-Next` 4.0.3
  - **Validation:** `FluentValidation` 11.11.0, `FluentValidation.AspNetCore` 11.3.0
  - **Email:** `MailKit` 4.16.0 (MimeKit is a transitive dependency)
  - **API / Docs:** `Swashbuckle.AspNetCore` 8.1.1
  - **Observability:** `Serilog.AspNetCore` 9.0.0, `Serilog.Sinks.Console` 6.0.0,
    `Serilog.Sinks.File` 6.0.0
  - **HTTP / Integrations:** `Microsoft.Extensions.Http` 8.0.1
  - **Testing:** `xunit` 2.9.3, `xunit.runner.visualstudio` 2.8.2,
    `coverlet.collector` 6.0.4, `Microsoft.NET.Test.Sdk` 17.13.0,
    `FluentAssertions` 7.2.0, `NSubstitute` 5.3.0,
    `Microsoft.AspNetCore.Mvc.Testing` 8.0.15, `Testcontainers.MongoDb` 4.4.0
- `PackageReference` entries wired per project following Clean Architecture boundaries:
  - `Domain`: zero external packages (pure domain model)
  - `Application`: `FluentValidation` only (no ASP.NET Core dependency)
  - `Infrastructure`: MongoDB, BCrypt, MailKit, Serilog, HttpClient, JWT token library
  - `Api`: JwtBearer, FluentValidation.AspNetCore, Swashbuckle, Serilog.AspNetCore
  - `UnitTests`: xunit, FluentAssertions, NSubstitute
  - `IntegrationTests`: everything in UnitTests + Mvc.Testing + Testcontainers.MongoDb

### Decisions
- **Rate Limiting:** Native `Microsoft.AspNetCore.RateLimiting` middleware (ASP.NET Core 7+)
  is included in the `Microsoft.AspNetCore.App` shared framework — no additional NuGet
  package needed for `Sdk="Microsoft.NET.Sdk.Web"` projects. The third-party
  `AspNetCoreRateLimit` package was intentionally omitted. Revisit in SEC-01 if
  native middleware proves insufficient.

### Changed
- Renamed `AegisIdentity.Backoffice` to `AegisIdentity.Api` to align project name
  with Clean Architecture entry-point convention (hosts both Minimal API endpoints
  and Razor Pages backoffice UI).
- Centralized build settings (`Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`,
  `LangVersion`) in `Directory.Build.props` — individual csproj files are now minimal.
- Activated Central Package Management via `Directory.Packages.props` to enforce
  a single version source of truth for all NuGet dependencies.

### Added
- `Directory.Build.props` — solution-wide MSBuild properties.
- `Directory.Packages.props` — Central Package Management.
- `LICENSE` — MIT license.

### Fixed
- `.gitignore` extended with `appsettings.*.local.json` and `secrets.json` patterns.

## [0.1.0] - 2026-05-18

### Added
- Initial Clean Architecture skeleton: `Api`, `Application`, `Domain`, `Infrastructure`,
  `UnitTests`, `IntegrationTests`.
- Razor Pages backoffice entrypoint with Serilog structured logging.
- `.gitignore`, `.editorconfig` and base solution file.
