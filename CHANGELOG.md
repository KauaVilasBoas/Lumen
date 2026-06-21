# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed (branding — new AegisIdentity logos)
- **Brand assets**: added the official logo set under `AegisIdentity.Backoffice/wwwroot/img/brand/` — `aegis-mark.png` (icon), `aegis-lockup.png` (full horizontal lockup with tagline), `aegis-lockup-compact.png` (compact wordmark) and `aegis-app-icon.png` (app icon); cropped from the source artwork to remove transparent padding.
- **Login screen**: replaced the placeholder `_AegisMark` SVG + text brand with the full `aegis-lockup.png` image in `Views/Account/Login.cshtml`.
- **Console sidebar**: replaced the placeholder mark + text with the `aegis-lockup-compact.png` image in `Views/Shared/_Layout.cshtml`; the brand now links to Home.
- **Favicons**: generated `favicon.ico`, `favicon-32.png` and `apple-touch-icon.png` from the new app icon; wired `<link rel="icon">`/`apple-touch-icon` into the Backoffice layout (previously had none) and replaced the API `wwwroot/favicon.ico`.
- **Cleanup**: removed the now-unused `_AegisMark.cshtml` partial and the orphaned `.login-brand-name`, `.login-brand-sub`, `.sidebar-brand-name` CSS rules; added `.login-logo` and `.sidebar-logo` rules.

### Changed (REFACTOR-03 — Onda 6: constants cleanup)
- **EmailLinkPaths**: added `EmailLinkPaths.ConfirmEmail` and `ResetPassword` constants; replaced 3 hardcoded `/api/auth/confirm-email` and 1 `/api/auth/reset-password` URL path literals in `RegisterUserCommandHandler`, `ResendConfirmationEmailCommandHandler`, `UpdateUserCommandHandler` and `ForgotPasswordCommandHandler`.
- **ValidationLimits**: added `UsernameAllowedCharsPattern` (`^[a-zA-Z0-9_-]+$`) and `UserRestoreWindowDays` (30) constants; replaced duplicate regex literals in `RegisterUserCommandHandler` and `UpdateUserCommandHandler` validators.
- **SystemActorNames**: added `SystemActorNames.SystemActor = "system"` constant; replaced `"system"` literal in `SetProfilePermissionsCommandHandler` fallback actor.
- **BackofficeErrorMessages**: new class with PT-BR messages for login failures and profile CRUD fallbacks; replaced 4 hardcoded strings in `AccountController`, 4 in `ProfilesController` and 2 in `UserProfilesController`.
- **AuditMessageTemplates**: new class with parameterized audit message templates; replaced all inline interpolated strings in `UserLoggedInAuditHandler`, `UserLockedOutAuditHandler`, `ProfilePermissionsSetAuditHandler`, `UserProfileAssignedAuditHandler`, `UserProfileRemovedAuditHandler`, `UserPermissionsChangedAuditHandler` and `CleanupJobExecutedAuditHandler`.
- **ProfileDetailViewModel / UserProfilesViewModel**: added two strongly-typed ViewModels to replace `ViewData["AllPermissions"]`, `ViewData["UserId"]` and `ViewData["AvailableProfiles"]` type-unsafe entries in `ProfilesController.Details` and `UserProfilesController.Index`; updated `Views/Profiles/Details.cshtml` and `Views/UserProfiles/Index.cshtml` accordingly.
- **PermissionDisplayHelper**: new static helper in `AegisIdentity.Backoffice.Helpers` with `HttpMethod` and `MethodCssColor`; removed duplicate inline Razor functions from `Views/Permissions/Index.cshtml` and `Views/Profiles/Details.cshtml`.
- **BackofficeDisplayLabels / BackofficeCssTokens**: extracted all lifecycle step labels, date placeholders and CSS color tokens from `UserViewModelBuilder` into dedicated constants classes; also replaced `profile.Name == "Administrator"` comparison with `SystemProfiles.AdministratorId` and literal state strings with `UserStates` constants.
- **DiagnosticsDefaults / RedisInfoKeys / HangfireStorageKeys**: new constants in SharedKernel; removed `DashboardSeriesDays` private const from `DiagnosticsController`; replaced `"stats"`, `"keyspace_hits"`, `"keyspace_misses"` and `"NextExecution"` literals.
- **NetworkDefaults.UnknownIpAddress**: replaced `"unknown"` fallback literal in `ApiBaseController.GetClientIpAddress`.
- **AuthErrorMessages**: added `CannotDeleteBootstrapUser`, `CannotDeleteLastAdministrator`, `UserNotDeleted` and `UserRestoreWindowExpired` (now a `{0}`-days template replacing the hardcoded `"30 dias"` string).
- **DevDefaults**: internal class in `AegisIdentity.Api.Controllers.Dev` with dev-only test email constants; replaced 5 hardcoded strings in `DevController`.

## [0.3.0] - 2026-06-15

### Added (REFACTOR-01 — BaseController hierarchy)
- `ApiBaseController` (abstract, `ControllerBase`) added to `AegisIdentity.Api.Controllers`:
  - `RequireCurrentUserId(out Guid userId)` — parses `ClaimTypes.NameIdentifier` as a `Guid`; returns `Unauthorized()` result when the claim is absent or not a valid `Guid`, `null` on success. Used as an early-return guard in action methods that require an authenticated user id.
  - `TryGetCurrentUserId(out Guid userId)` — pure boolean variant for callsites that need to branch without returning directly.
  - `GetClientIpAddress()` — `HttpContext.Connection.RemoteIpAddress?.ToString()` with `"unknown"` fallback; eliminates the inline null-coalescing literal.
  - `GetActorIdentifier()` — `User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty`; used where the actor is passed to audit or domain commands.
  - Promotes `[ApiController]` and `[Produces("application/json")]` to the base class — removed from all 10 concrete API controllers.
- `BackofficeBaseController` (abstract, `Controller`) added to `AegisIdentity.Backoffice.Controllers`:
  - `TryGetCurrentUserId(out Guid userId)` — same semantics as the API variant; used by `AuthorizationGraphController.CallerHasPermissionAsync`.
- All 10 API controllers now extend `ApiBaseController`; `AccountController` excluded from Backoffice hierarchy (raw JWT parsing for cookie sign-in has no shared claims pattern).
- Inline `System.Security.Claims` usages removed from controllers that no longer reference `ClaimTypes` directly.

### Added (Architecture blindagem — refactor/architecture-blindagem)
- **IsBootstrap no domínio User**: adicionada propriedade `IsBootstrap` (`private set`, imutável) ao agregado `User` e factory method `User.CreateBootstrap` para criação explícita do usuário-semente. Migration `20260614220101_AddUserIsBootstrapColumn` adiciona a coluna (`bit NOT NULL DEFAULT 0`) e marca o usuário admin seed como bootstrap via `UpdateData`. `ListUsersQueryHandler` e `GetUserDetailQueryHandler` passam a retornar o valor real em vez de `false` hardcoded. 5 novos testes unitários em `UserTests`.
- **N+1 eliminado em GetAuthorizationGraphQueryHandler**: `BuildProfileNodesAsync` e `BuildUserNodesAsync` substituem os loops N+1 por chamadas batch (`GetActivePermissionProfilesByProfileIdsAsync` e `ListByUserIdsAsync`). Reduz de `O(P + U)` round-trips adicionais para 2 queries fixas. Novos métodos batch adicionados a `IProfileRepository`, `IUserProfileRepository` e implementados em `ProfileRepository`, `UserProfileRepository` com `AsNoTracking`. Testes atualizados para mocks batch; novo cenário multi-entidade adicionado.
- **Projeto AegisIdentity.ArchitectureTests**: 16 testes de arquitetura automatizados com NetArchTest.Rules (1.3.2) que falham ao build quando regras de Clean Architecture são violadas. Cobre: isolamento do Domain, isolamento do SharedKernel, independência das camadas Application de Infrastructure/Presentation, separação CQRS (Command↔Query), e proibição de Controllers referenciarem tipos de Domain diretamente. Documentado em CLAUDE.md na seção "Constraints de arquitetura (testes automatizados)".

### Changed (Architecture alignment — refactor/architecture-alignment)
- **Permission constants**: Added `PermissionCodes.Profiles.*`, `PermissionCodes.Permissions.*`, `PermissionCodes.UserProfiles.*` and matching `PermissionGroups.*` to `SharedKernel/Constants/Permissions.cs`. Replaced all literal strings in `PermissionsController`, `ProfilesController` and `UserProfilesController` with the new constants.
- **FluentValidation for queries**: Added `AbstractValidator<Query>` (same file, per CQRS convention) to `ListUsersQueryHandler` and `GetRecentAuditFeedQueryHandler`. Added `PageMinValue`, `PageSizeMinValue/MaxValue`, `AuditTakeMinValue/MaxValue` to `ValidationLimits`. Removed all inline `BadRequest`/`ValidationProblemDetails` guards from `UsersController.List` and `AuditController.Read`. Added FluentValidation package reference to `ReadModels.csproj` and registered validator scanning for the ReadModels assembly in `Program.cs`.
- **N+1 elimination in read models**: Added batch methods `GetProfilesByUserIdsAsync`, `GetPermissionCountsByUserIdsAsync` and `GetPermissionCountsByProfileIdsAsync` to `IProfileRepository` and `ProfileRepository` (single EF Core query each with `AsNoTracking`). `ListUsersQueryHandler` reduced from `1 + 2N` queries to 3 queries total. `GetUserDetailQueryHandler.BuildProfileSummariesAsync` loop replaced with a single batch call.
- **ViewComponent + ViewModel in Backoffice**: Introduced `HomeDashboardViewModel` replacing `ViewBag` in `HomeController`. Created `UserListViewComponent` and `UserDetailViewComponent` with typed PartialViews in `Views/Shared/Components/`. Extracted `UsersPageViewModel`, `UserListItemViewModel`, `UserDetailViewModel`, `ProfileMembershipViewModel`, `LifecycleStepViewModel` and `UserViewModelBuilder` — presentation logic (`AvatarPalette`, `ProfileAccentColor`, `MapDetail`, `BuildLifecycle`, `FormatDate`) moved out of the controller.
- **Legacy comments removed**: Removed XML doc blocks and inline comments from `DevController`, `IProfileRepository`, `IUserRepository`, `UserRepository` and `ProfileRepository` per self-documenting code convention.

### Added (AUTH-19 — GET /api/auth/confirm-email + POST /api/auth/resend-confirmation)
- `GET /api/auth/confirm-email?token=...` with `[AllowAnonymous]`; looks up `EmailConfirmationToken` by SHA-256 hash, validates `IsValid()` (not expired, not used), sets `user.IsActive = true` and `user.EmailConfirmedAt = now`, marks token used; returns `200 OK` or `401` via `UnauthorizedException` handled globally.
- `POST /api/auth/resend-confirmation` with `[AllowAnonymous]`; always returns `200 OK` regardless of email existence (anti-enumeration); for pending users, soft-deletes previous tokens via `InvalidateByUserIdAsync`, generates a new 24h `EmailConfirmationToken`, and sends the `EmailConfirmation` template email; no-ops silently for unknown emails and already-active accounts.
- `IEmailConfirmationTokenRepository.InvalidateByUserIdAsync` added to domain port and implemented in `EmailConfirmationTokenRepository` (soft-deletes all active tokens for the user in one `SaveChangesAsync`).
- `AuthErrorMessages.TokenRequired`, `AuthErrorMessages.InvalidOrExpiredToken` added to `SharedKernel`.
- `EmailSubjects.PasswordChanged` added to `SharedKernel`.
- 10 unit tests (8 handler, 2 validator) and 10 integration endpoint tests.

### Added (AUTH-18 — POST /api/auth/reset-password)
- `POST /api/auth/reset-password` with `[AllowAnonymous]`; accepts `{ token, newPassword }`; validates `PasswordResetToken` by SHA-256 hash and `IsValid()`, rejects with `401` on invalid/expired/used token; applies `IPasswordValidator` (complexity + HIBP) on the new password (`400` on failure); marks token used, updates `PasswordHash` via `IPasswordHasher` (BCrypt work factor 12), revokes all active `RefreshToken`s via `IRefreshTokenRepository`, sends `PasswordChanged` notification email (fail-open on SMTP error); returns `204` on success.
- `AuthErrorMessages.NewPasswordRequired` added to `SharedKernel`.
- 13 unit tests (10 handler, 3 validator) and 7 integration endpoint tests.

### Added (USER-05 — POST /api/me/change-password)
- `POST /api/me/change-password` in `MeController`; protected by FallbackPolicy (authenticated, no dedicated permission); `userId` extracted from JWT `sub` claim.
- `ChangePasswordCommandHandler`: resolves user by id (`NotFoundException` on missing); verifies `currentPassword` via `IPasswordHasher.Verify` (`400` on mismatch); rejects `newPassword` equal to current (`400`); applies `IPasswordValidator` (`400` on failure); updates `PasswordHash`, revokes all active `RefreshToken`s (logout from all devices), sends `PasswordChanged` email (fail-open); returns `204`.
- `AuthErrorMessages.CurrentPasswordRequired`, `AuthErrorMessages.CurrentPasswordIncorrect`, `AuthErrorMessages.NewPasswordSameAsCurrent` added to `SharedKernel`.
- 12 unit tests (9 handler, 3 validator) and 6 integration endpoint tests.

### Added (AUTH-17 — POST /api/auth/forgot-password)
- `POST /api/auth/forgot-password` in `AuthController` with `[AllowAnonymous]`; always returns `200 OK` regardless of whether the email exists (prevents user enumeration).
- `ForgotPasswordCommandHandler` (CQRS/MediatR): looks up user by normalised email, generates a cryptographically-random raw token (32 bytes, Base64Url), persists `PasswordResetToken` with SHA-256 hash and 30-minute expiry, then dispatches the password-reset email.
- Timing-attack mitigation: when the email is not found, a dummy token derivation + 50 ms delay runs to equalise the response time with the happy path.
- Reuses the existing `PasswordResetToken` aggregate, `IPasswordResetTokenRepository`, `EmailTemplateNames.PasswordReset`, `EmailPlaceholderKeys.ResetUrl`, `EmailSubjects.PasswordReset`, and `TokenSizes.RawTokenBytes` — no new constants needed.
- 13 new tests (9 unit handler, 4 unit validator) and 6 integration endpoint tests.

### Added (USER-03 — PUT /api/users/{id} update endpoint)
- `PUT /api/users/{id}` in `UsersController` protected by `[RequirePermission]` + `Users.Update` policy.
- `UpdateUserCommandHandler` (CQRS/MediatR): updates `Username` and/or `Email`; no-ops when nothing changes.
- Domain methods `User.ChangeEmail` and `User.ChangeUsername` encapsulate mutation rules; `ChangeEmail` sets `IsActive = false` and normalises the address.
- Email change triggers a new `EmailConfirmationToken` (SHA-256, 24 h lifetime) and sends the confirmation email via the existing template pipeline.
- Duplicate detection done in-band before `UpdateAsync` and re-caught from `DuplicateEmailException` / `DuplicateUsernameException` raised by the repository — both surface as `409 Conflict`.
- Audit trail written to `AuditEntries` (`user.updated` kind) with actor, target user id and changed fields list.
- `PermissionCodes.Users.Update` constant added to `SharedKernel`.
- `AuditEventKinds.UserUpdated` constant added to `SharedKernel`.
- `AuthErrorMessages.UserNotFound` constant added to `SharedKernel`.
- EF data migration `20260611000000_SeedUsersUpdatePermission` seeds the `Users.Update` permission row with deterministic id `40000000-0000-0000-0000-000000000024`.
- 29 new tests (14 unit handler, 10 unit validator, 5 integration endpoint).

### Added (EMAIL-02 — production SMTP fail-fast validation)
- `SmtpProductionOptionsValidator` registered only in `Production` and executed on startup:
  the boot fails when `Smtp__Host`, `Smtp__User`, `Smtp__Pass` or `Smtp__From` is missing or
  still the committed `REPLACE_ME` placeholder, or when `Smtp__Host` points at a loopback
  address. Failure messages name the offending env var without echoing its value.
- `ConfigurationPlaceholders` constants (`REPLACE_ME`, loopback host aliases) in SharedKernel.
- `docs/configuration.md`: SMTP provider options (Brevo/SendGrid free tier, self-hosted Postal,
  Mailpit for dev), `Smtp__Pass` marked as secret, fail-fast behavior documented; README links
  the provider-agnostic SMTP setup.

### Changed (EMAIL-02)
- `SmtpOptions.UseStartTls` now defaults to `true` (secure by default); dev relays without TLS
  (Mailpit) opt out explicitly in `appsettings.Development.json`.
- The inline loopback-SMTP check in `Program.cs` moved into the options validator, so all SMTP
  misconfiguration is reported in one pass by `ValidateOnStart`.

### Changed (README — recruiter-focused improvements)
- Opening section rewritten problem-first (who is the user, what can they do, who changed it);
  the portfolio/process framing moved into the Engineering workflow section.
- Engineering decisions table: added the rationale for using database-backed Profiles instead of
  JWT role claims (immediate revocation) and the justification for CQRS over a single store.
- Author/contact footer added (LinkedIn, GitHub, email, timezone overlap).

## [0.2.1] - 2026-06-10

### Fixed (concurrent DbContext access — 500 on Users endpoints)
- `ListUsersQueryHandler` resolved profile and permission counts with `Task.WhenAll` over
  repositories that share the request-scoped `DbContext`; EF Core forbids concurrent operations
  on one context, so any page with more than one user returned `500`. Both batch helpers now
  iterate sequentially (≤ page size queries per request).
- `GetUserDetailQueryHandler.BuildProfileSummariesAsync` had the same latent pattern, triggered
  for any user with two or more profiles; also made sequential.

### Fixed (integration tests — first CI run on a clean database)
- `AuthorizationSeeder` added to the integration test infrastructure; it creates the `Users` row
  (with a deterministic id) before inserting `UserProfiles`, fixing the
  `FK_UserProfiles_Users_UserId` violation that broke 31 tests on a clean SQL Server container.
  The seven copy-pasted `SeedUserWithPermissionAsync` helpers were replaced by the shared seeder.
- `PermissionEnforcementTests.AuthenticatedUser_WithoutRequiredPermission_Returns403` now sends an
  authenticated request via `CreateProbeClientWithUser`; it previously used an anonymous client,
  which can only ever produce `401`.
- `ProfileManagementTests` seeds the referenced user before creating `UserProfile` join rows in the
  five tests that used orphan `Guid.NewGuid()` user ids.
- `DefaultProfilesTests.AdministratorPermissionReconciliation_IsAdditive` no longer calls
  FluentAssertions `Contain` with an empty expected collection (which throws `ArgumentException`)
  and now asserts directly that the extra permission was granted after reconciliation.

## [0.2.0] - 2026-06-09

### Added (OPS-01)
- GitHub Actions CI pipeline (`.github/workflows/ci.yml`): build with `TreatWarningsAsErrors`,
  unit tests and Testcontainers integration tests on every push and pull request to `main`.

### Changed (DOC-02)
- README rewritten as a portfolio-grade document: dynamic release badge, updated architecture
  diagram (SignalR hub, audit, refresh/logout), consolidated engineering decisions, screenshots
  and demo sections, and an engineering workflow section.
- Operational reference moved out of the README into `docs/configuration.md` (environment
  variables, user secrets, logging, correlation IDs) and `docs/security.md` (password policy,
  HIBP k-anonymity, login semantics).

### Fixed (authorization mismatches — convention vs. policy code)
- `UsersController`: action `GetDetail` renamed to `Get` so the convention `Controller.Action`
  produces `"Users.Get"`, matching `PermissionCodes.Users.Get` used by the `[Authorize]` policy.
  The route `[HttpGet("{id:guid}")]` is unchanged; only the C# method name was corrected.
- `AuditController`: action `Recent` renamed to `Read` so the convention produces `"Audit.Read"`,
  matching `PermissionCodes.Audit.Read`. The route `[HttpGet("recent")]` is unchanged.
- `DiagnosticsController`: `GetCacheStats` and `GetJobStats` were both referencing the removed
  `PermissionCodes.Diagnostics.Read` (`"Diagnostics.Read"`), which never matched their
  convention-generated codes. The shared constant was replaced with two per-endpoint constants:
  `PermissionCodes.Diagnostics.GetCacheStats` (`"Diagnostics.GetCacheStats"`) and
  `PermissionCodes.Diagnostics.GetJobStats` (`"Diagnostics.GetJobStats"`). Each action now
  declares the matching `[Authorize(Policy = ...)]`.
- Migration `20260609010000_SeedConventionPermissionFixes` seeds all four permission rows
  (`Users.Get`, `Audit.Read`, `Diagnostics.GetCacheStats`, `Diagnostics.GetJobStats`) and
  their respective groups idempotently before the discovery service runs. Existing rows
  created by previous discovery boots (e.g. `Users.GetDetail`, `Audit.Recent`,
  `Diagnostics.Read`) are not deleted; they are marked as orphan by the sync service.
- 4 regression unit tests added in `ConventionPermissionRegressionTests` to assert that the
  scanner produces the exact convention code for each corrected action.

### Fixed (boot crash — AuthorizationGraph.View permission)
- `AuthorizationGraphController`: action renamed from `Get` to `View` so the convention
  `Controller.Action` produces `"AuthorizationGraph.View"`, matching `PermissionCodes.AuthorizationGraph.View`
  used by the Authorize policy, the hub, and the Backoffice proxy. The bare `[RequirePermission]`
  attribute (no argument) ensures the discovery scanner operates on the renamed action via convention,
  eliminating the code-divergence that caused a unique-index violation on `ix_permissions_code_unique`
  at the second boot.
- Migration `20260609000000_SeedAuthorizationGraphPermission` seeds the `GroupPermissions` row
  for `"Authorization"` and the `Permissions` row for `"AuthorizationGraph.View"` with deterministic
  GUIDs before the `PermissionDiscoveryHostedService` runs. On subsequent boots the discovery scanner
  finds the pre-existing row by code and performs an UPDATE instead of INSERT — no collision, no crash.
  The previous erroneous `"AuthorizationGraph.Get"` row (if present) is marked as orphan by the sync
  service without being deleted.

### Fixed (BACKOFFICE-QA-01)
- `_Layout.cshtml`: hardcoded `admin@aegisidentity.local` replaced with `User.FindFirstValue(ClaimTypes.Email)`
  sourced from the authenticated cookie principal; email div is conditionally rendered and absent when the claim
  is not present, ensuring no fake data is ever displayed.
- `_Layout.cshtml`: sidebar health widget removed hardcoded status values ("healthy", "1 queued") that were
  presented as real service state. Dots now use `--faint` colour and no status text is emitted until a real
  `/api/diagnostics/health` integration is added in a future card.

### Added (API-SIGNALR-02)
- `GraphLivePushHandler` added to `AegisIdentity.Api.Hubs`; implements `INotificationHandler<UserPermissionsChanged>`
  and pushes a `GraphSnapshot` delta to all connected clients of the affected user via
  `IHubContext<AuthorizationGraphHub, IAuthorizationGraphHubClient>.Clients.User(userId)`.
- Handler is independent of `UserPermissionsChangedHandler` (cache invalidation) and
  `UserPermissionsChangedAuditHandler`; MediatR fans out to all three on each notification.
- Delta payload recomputes the user's active profile memberships and resolved permission IDs
  using `IUserProfileRepository` and `IProfileRepository`, keeping the push lean (single-user
  sub-graph, not the full snapshot).
- User not found at push time is treated as a no-op with a warning log; push errors propagate
  to let the MediatR pipeline decide retry semantics.
- `RegisterServicesFromAssemblyContaining<GraphLivePushHandler>()` added to the MediatR
  registration in `Program.cs` so the Api assembly is scanned alongside CommandHandlers,
  QueryHandlers, and EventHandlers.
- 4 unit tests added in `GraphLivePushHandlerTests` covering: push to correct user group,
  user-not-found no-op, empty-profiles snapshot, and multi-profile snapshot composition.

### Added (API-SIGNALR-01)
- `AddSignalR()` registered in `Program.cs`; `MapHub<AuthorizationGraphHub>` wired to
  `HubRoutes.AuthorizationGraph` (`/hubs/authorization-graph`).
- `AuthorizationGraphHub` typed as `Hub<IAuthorizationGraphHubClient>`, enforcing the
  server→client contract at compile time.
- `IAuthorizationGraphHubClient` interface declares the two server-push methods:
  `GraphUpdated(GraphSnapshot delta)` and `UserPermissionsInvalidated(Guid userId)`.
- `HubRoutes` and `HubMethods.AuthorizationGraph` constants added to `AegisIdentity.SharedKernel.Constants`
  to eliminate route/method name literals across hub, Program.cs, and tests.
- `AuthorizationGraphHub` requires the `AuthorizationGraph.View` permission via
  `[Authorize(Policy = PermissionCodes.AuthorizationGraph.View)]` and a secondary runtime
  check in `OnConnectedAsync` that calls `IUserPermissionService.HasPermissionAsync` and
  aborts the connection if the permission is absent.
- `JwtBearer.OnMessageReceived` configured in `SecurityServiceExtensions` to extract the
  bearer token from the `access_token` query parameter on requests targeting the hub path —
  secondary fallback for SignalR negotiation flows; primary path remains the `Authorization`
  header injected server-side by the Backoffice reverse-proxy.
- Backplane Redis intentionally omitted; single-instance deployment does not require it.
  Re-evaluate `AddStackExchangeRedis()` on the SignalR builder if horizontal scale-out is added.
- 4 unit tests added in `AuthorizationGraphHubTests` covering: permission-granted connection,
  permission-denied abort, missing NameIdentifier claim abort, and non-GUID subject abort.
- `AuthorizationGraphHubTests` (integration) updated to reference `HubRoutes.AuthorizationGraph`
  instead of a hardcoded path literal.

### Changed (BACKOFFICE-USERS-01)
- `UsersController` no longer contains any mock data: `DemoUsers()` removed entirely.
  `AdminApiClient` injected via constructor; `Index(Guid? id)` calls `ListUsersAsync`
  (page 1, size 100) then `GetUserAsync` for the selected identity.
- Avatar gradient generated deterministically in the Backoffice from `Guid.GetHashCode()`
  against a fixed 7-colour palette — consistent with the pattern used in `Profiles/Index.cshtml`.
  No color field comes from the API.
- Account lifecycle section built from real timestamps: `createdAt`, `emailConfirmedAt`,
  `lastLoginAt`, `lockoutEndAt`. State variants active/locked/pending/deleted each produce
  the appropriate final lifecycle step.
- Profile accent colours follow the same deterministic `GetHashCode() % palette` logic as
  `Profiles/Index.cshtml`; Administrator always `#8b6dff`, base User profile always `#5b6478`.
- `Views/Users/Index.cshtml` updated: `UsersPageModel.Selected` is now nullable; the detail
  panel renders a graceful empty state when the API is unreachable or returns no users.

### Added (BACKOFFICE-API-CLIENT-01)
- `AdminApiClient` extended with four new methods consuming the recently added API endpoints:
  `ListUsersAsync` (GET /api/users with search/state/page/pageSize), `GetUserAsync` (GET /api/users/{id},
  returns `null` on 404), `GetAuthorizationGraphAsync` (GET /api/authorization-graph), and
  `GetRecentActivityAsync` (GET /api/audit/recent with `take` param).
- Response records added to `AdminApiClient`: `UsersPage`, `UserListItem`, `UserDetail`,
  `ProfileMembership`, `GraphSnapshot`, `UserNode`, `ProfileNode`, `PermissionNode`, `AuditEntry`;
  all aligned 1:1 with the API query handler contracts.

### Added (API-GRAPH-01)
- `GetAuthorizationGraphQueryHandler` added to `AegisIdentity.ReadModels.Queries`; composes users,
  profiles, permissions, and their relations (UserProfile/PermissionProfile) into a `GraphSnapshot`.
  Returns `users[]` (with `id`, `username`, `email`, `state`, `profiles[]`), `profiles{}` (keyed by
  profile id, with `name`, `isSystem`, `permissions[]`), and `permissions{}` (keyed by permission id,
  with `code`, `name`, `group`, `orphan`). No `color` or `method` fields — both are presentation
  concerns handled by the front end. Soft-deleted entities excluded via EF global query filters.
- `GET /api/authorization-graph` wired to `GetAuthorizationGraphQueryHandler` in
  `AuthorizationGraphController`; replaces stub `Ok()` with typed `GraphSnapshot` result.
  Permission enforcement unchanged: `[RequirePermission(PermissionCodes.AuthorizationGraph.View)]` +
  `[Authorize(Policy = PermissionCodes.AuthorizationGraph.View)]`. Explicit permission code passed
  to `[RequirePermission]` so the discovery scanner resolves `AuthorizationGraph.View` independently
  of the action method name.
- 13 unit tests added in `GetAuthorizationGraphQueryHandlerTests` covering empty snapshot, user state
  derivation (active/locked/pending), user-profile and profile-permission wiring, group name
  resolution, orphan flag, and shape contract (no color/method fields).
- 6 integration tests added in `AuthorizationGraphSnapshotTests` covering 401/403/200 enforcement,
  response shape (users/profiles/permissions keys), user array field presence, and absence of
  `color` from the payload.
- `AuthorizationGraphPermissionDiscoveryTests` updated to reference the renamed action `Get`
  (previously `View`); all 3 discovery tests continue passing.

### Added (API-AUDIT-01)
- `AuditEntry` entity added to `AegisIdentity.Domain.Audit` with fields `id`, `kind`, `actor`,
  `target`, `message`, `occurredAt`; created via `AuditEntry.Create(kind, actor, target, message)`.
- `IAuditRepository` (domain port) added with `InsertAsync` and `GetRecentAsync(take)`.
- `AuditRepository` (infrastructure adapter) implemented in `AegisIdentity.DataAccess` — `GetRecentAsync`
  queries ordered by `OccurredAt DESC`, bounded by `take`.
- `DbSet<AuditEntry> AuditEntries` added to `AegisIdentityDbContext`; EF configuration
  (`AuditEntryConfiguration`) maps to `AuditEntries` table with indices on `OccurredAt` and `Kind`.
- `IAuditRepository` registered as `AuditRepository` (scoped) in `SqlServerServiceExtensions.AddRelationalDataAccess`.
- EF Core migration `20260608005918_AddAuditEntries` created: `Up` adds `AuditEntries` table +
  two indices; `Down` drops the table.
- Six domain notification records added to `AegisIdentity.Domain.Audit`: `UserLoggedIn`,
  `UserLockedOut`, `ProfilePermissionsSet`, `UserProfileAssigned`, `UserProfileRemoved`,
  `CleanupJobExecuted` — each implements `INotification`.
- Six dedicated audit `INotificationHandler` implementations added to `AegisIdentity.EventHandlers.Audit`
  (one per notification), each inserting an `AuditEntry` with the appropriate `AuditEventKinds` constant.
  MediatR allows multiple handlers for the same notification — audit handlers are fully decoupled from
  the existing cache-invalidation handler.
- `AuditEventKinds` constants class added to `AegisIdentity.SharedKernel.Constants` defining
  `auth.login`, `auth.lockout`, `cache.invalidate`, `profile.permset`, `userprofile.assign`,
  `userprofile.remove`, `job.cleanup`.
- `PermissionCodes.Audit.Read` (`"Audit.Read"`) and `PermissionGroups.Audit` (`"Audit"`) constants
  added to `AegisIdentity.SharedKernel.Constants.Permissions` — auto-discovered by
  `PermissionDiscoveryHostedService` on startup.
- `JobSchedules.CleanupJobName` constant (`"cleanup-expired-refresh-tokens"`) added to
  `AegisIdentity.SharedKernel.Constants.JobSchedules`.
- `LoginUserCommandHandler` now injects `IPublisher` and publishes `UserLoggedIn` on successful
  authentication and `UserLockedOut` when the lockout threshold is reached.
- `SetProfilePermissionsCommandHandler.Command` gains an optional `ActorUsername` field; publishes
  `ProfilePermissionsSet` after applying permission changes. `ProfilesController.SetPermissions`
  passes the current principal's identity as the actor.
- `AssignUserProfileCommandHandler` publishes `UserProfileAssigned` after inserting the assignment.
- `RemoveUserProfileCommandHandler` now resolves user and profile (for their names) and publishes
  `UserProfileRemoved`; constructor gains `IUserRepository` and `IProfileRepository` parameters.
- `CleanupExpiredRefreshTokensJob` injects `IPublisher` and publishes `CleanupJobExecuted` after
  each cleanup run; `MediatR` package reference added to `AegisIdentity.Jobs.csproj`.
- `GetRecentAuditFeedQueryHandler` added to `AegisIdentity.ReadModels.Queries`; accepts `take`
  (clamped to 1–100, default 20) and returns `IReadOnlyList<AuditEntryResult>` ordered desc by
  `OccurredAt`.
- `GET /api/audit/recent?take=N` endpoint added to `AuditController` — protected by
  `[RequirePermission]` + `[Authorize(Policy = PermissionCodes.Audit.Read)]` +
  `[PermissionGroup(PermissionGroups.Audit)]`; returns `400` when `take` is out of [1,100].
- 10 unit tests added in `GetRecentAuditFeedQueryHandlerTests` covering empty result, field
  mapping, take clamping (4 boundary cases), valid take passthrough, and order preservation.
- 7 unit tests added in `AuditEventHandlerTests` covering each audit handler's kind and field
  mapping (one test per notification type).
- 3 unit tests updated (`RemoveUserProfileCommandHandlerTests`) for the new constructor signature;
  2 new test cases added covering user-not-found and profile-not-found paths.
- `LoginUserCommandHandlerTests` and `CleanupExpiredRefreshTokensJobTests` updated to mock
  the newly injected `IPublisher`.
- 4 integration tests added in `AuditRecentEndpointTests` covering 401 (anonymous), 403
  (authenticated without permission), 200 with expected JSON shape, take-limit respected, and
  400 on invalid take.

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
