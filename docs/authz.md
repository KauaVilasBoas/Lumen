# Authorization Model

Lumen uses a permission-based authorization model introduced across AUTH-08 through
AUTH-16. This document describes the full model, enforcement mechanics, data lifecycle, and
operational guides.

---

## Domain model

The authorization schema introduces five entities on top of the existing `User`:

```
User ─────────────────┐
                       │ (N:N via UserProfile)
                       ▼
                    Profile ◄──────────────── PermissionProfile ──► Permission
                                                                         │
                                                                         ▼
                                                                  GroupPermission
```

### Entities

| Entity | Table | Purpose |
|---|---|---|
| `Permission` | `Permissions` | One discovered endpoint action. Code = `Controller.Action`. |
| `GroupPermission` | `GroupPermissions` | Optional logical grouping of permissions (e.g. "Users", "Profiles"). |
| `Profile` | `Profiles` | Named role-like container (e.g. "Administrator", "User"). |
| `PermissionProfile` | `PermissionProfiles` | Join between `Permission` and `Profile` — grants the permission to anyone holding the profile. |
| `UserProfile` | `UserProfiles` | Join between `User` and `Profile` — assigns the profile to a user. |

Every entity implements `ISoftDeletable`:

```csharp
public bool IsDeleted { get; private set; }
public DateTime? DeletedAt { get; private set; }
```

Nothing is ever physically deleted. Soft-delete is enforced at the repository level via EF
Core global query filters (see [Soft-Delete](#soft-delete)).

### Permission code convention

The canonical permission code is derived from the controller and action names:

```
{Controller}.{Action}
```

`ControllerNameNormalizer.Normalize` strips the `Controller` suffix before building the code.
Examples:

| Controller class | Action | Code |
|---|---|---|
| `UsersController` | `Index` | `Users.Index` |
| `ProfilesController` | `Delete` | `Profiles.Delete` |
| `UserProfilesController` | `Assign` | `UserProfiles.Assign` |

`Permission.BuildCode(controller, action)` is the single construction point — both the API
enforcement layer and the Backoffice helpers use it.

---

## Data initialization

### Initial data via EF Core migration

The admin user, the two system profiles and the initial user-profile binding are all inserted
by **EF Core data migrations** — there is no runtime seed and no seed CLI command.

| Migration | What it inserts |
|---|---|
| `SeedInitialAdminUser` | `User` with `Id = 10000000-0000-0000-0000-000000000001` (see [ADR-0002](adr/0002-admin-bootstrap-credential.md)) |
| `SeedDefaultProfiles` | `Profile` "Administrator" (`IsSystem = true`) and "User" (`IsSystem = true`) |
| `SeedAdminUserProfile` | `UserProfile` binding the admin user to the Administrator profile |

System profiles (`IsSystem = true`) cannot be soft-deleted.

### Administrator permission reconciliation at startup

Because permissions are discovered from live endpoints at boot time (see [Discovery](#permission-discovery)),
they cannot all be pre-seeded in a static migration.

On every startup, after migrations and after discovery, an **additive-only reconciliation**
runs for the Administrator profile:

1. Load all currently known `Permission` records from the database.
2. Load all `PermissionProfile` associations for the Administrator profile.
3. For each permission that does not yet have an association: insert a new `PermissionProfile`.
4. Never remove existing associations, never touch other profiles.

This is **not a seed** — it derives its input from the live discovery result, not from
hardcoded business data. The limit is explicit:

- Migration = static business data (profiles + initial user binding).
- Startup reconciliation = derived sync from discovery (permissions assigned to Administrator).

---

## Permission discovery

On startup, `PermissionDiscoveryService` inspects all registered `IActionDescriptor`s and
extracts `[RequirePermission]`-decorated actions. The convention:

1. Each action that carries `[RequirePermission]` (with or without an explicit code) is
   registered as a `Permission`.
2. If the action already exists in the database (`Code` match), its metadata is updated.
3. If a previously-discovered action is no longer reachable (controller removed or renamed),
   it is marked `IsOrphan = true` — **never deleted** (soft-delete only rule applies to
   orphans as well).

The startup order is:

```
Database.Migrate()
  → PermissionDiscoveryService (scan + upsert)
    → AdministratorPermissionReconciler (additive grant to Administrator)
```

---

## Protecting an endpoint

### API controllers

Decorate the controller class or individual action with `[RequirePermission]`:

```csharp
[RequirePermission]
public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { ... }
```

An explicit code override is supported but rarely needed (the convention derives it
automatically from the controller and action name):

```csharp
[RequirePermission("Profiles.Delete")]
public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { ... }
```

The dynamic policy provider resolves the requirement at runtime:
- `401 Unauthorized` — request is not authenticated.
- `403 Forbidden` — request is authenticated but the user lacks the required permission.

### Fallback policy

All API endpoints in the Backoffice are covered by a fallback authorization policy that
**requires authentication** by default. To allow anonymous access, use `[AllowAnonymous]`:

```csharp
[AllowAnonymous]
public IActionResult Login() { ... }
```

### Registering a new endpoint (step-by-step)

1. Decorate the new action with `[RequirePermission]`.
2. Run the application. Discovery upserts the new `Permission` into the database and the
   startup reconciler grants it to the Administrator profile.
3. Log in as Administrator. The new permission is immediately enforced.
4. Use the Profiles UI (AUTH-15) to assign the permission to other profiles as needed.

---

## Permission cache (Redis)

### Architecture

User permissions are cached in Redis under the key:

```
user:permissions:{userId}
```

Cache read/write is abstracted by `IUserPermissionCache` (Domain) with the Redis
implementation in `UserPermissionCache` (DataAccess). The resolution layer is
`IUserPermissionService` which:

1. Checks the Redis cache.
2. On a cache miss, queries the database via `IProfileRepository.GetPermissionCodesByUserIdAsync`.
3. Writes the result back to Redis with a safety-net TTL (`IUserPermissionCache.DefaultTtl = 5 min`).

### Fallback on Redis unavailability

When Redis is unreachable, `GetAsync` returns `null` (cache miss). The resolution layer falls
back to the database transparently — **authorization never fails open or closed due to a Redis
outage**. When Redis recovers, the next request repopulates the cache automatically.

### Invalidation

Invalidation is **event-driven**, not TTL-driven (TTL is a safety net only).

Whenever a profile assignment or permission association changes, the relevant command handler
publishes `UserPermissionsChanged { UserId }` for every affected user. The `InvalidateUserPermissionsEventHandler` calls `IUserPermissionCache.InvalidateAsync(userId)`.

Scenarios that trigger invalidation:

| Change | Affected users |
|---|---|
| Profile assigned to user | That user |
| Profile removed from user | That user |
| Permission added to profile | All users holding that profile |
| Permission removed from profile | All users holding that profile |
| Profile soft-deleted | All users that held that profile |

---

## Soft-delete

### Global query filter

Every entity that implements `ISoftDeletable` is configured with an EF Core global query filter:

```csharp
modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
modelBuilder.Entity<Permission>().HasQueryFilter(p => !p.IsDeleted);
modelBuilder.Entity<Profile>().HasQueryFilter(p => !p.IsDeleted);
modelBuilder.Entity<UserProfile>().HasQueryFilter(up => !up.IsDeleted);
modelBuilder.Entity<PermissionProfile>().HasQueryFilter(pp => !pp.IsDeleted);
```

Deleted records are invisible to all normal queries. To query deleted records:

```csharp
dbContext.Users.IgnoreQueryFilters().Where(u => u.IsDeleted);
```

### Filtered unique index

`Email` and `Username` have a filtered unique index that ignores deleted records, allowing
the same email to be re-registered after a soft-delete:

```sql
CREATE UNIQUE INDEX UX_Users_Email    ON Users (Email)    WHERE IsDeleted = 0;
CREATE UNIQUE INDEX UX_Users_Username ON Users (Username) WHERE IsDeleted = 0;
```

### Cascading soft-deletes

When a `Profile` is soft-deleted:
1. All `UserProfile` rows for that profile are soft-deleted.
2. All `PermissionProfile` rows for that profile are soft-deleted.
3. `UserPermissionsChanged` is published for all affected users (cache invalidation).

When a `User` is soft-deleted:
1. All `UserProfile` rows for that user are soft-deleted.

Orphan `Permission` records (actions that no longer exist in the running application) are
marked `IsOrphan = true` — never soft-deleted — so their history and existing profile
assignments are preserved.

---

## `/me` endpoint contract

`GET /api/me` returns the authenticated user's profile memberships in the response body:

```json
{
  "id": "f0e1d2c3-b4a5-9687-0102-030405060708",
  "email": "user@example.com",
  "username": "user",
  "createdAt": "2026-01-01T00:00:00Z",
  "lastLoginAt": "2026-05-31T12:00:00Z",
  "emailConfirmedAt": "2026-01-02T08:00:00Z",
  "profiles": [
    { "id": "a1b2c3d4-...", "name": "Administrator" },
    { "id": "e5f6a7b8-...", "name": "User" }
  ]
}
```

- `profiles` is never null — it is an empty array `[]` when the user has no profile
  assignments.
- Only active profiles (non-soft-deleted `Profile` and non-soft-deleted `UserProfile` join)
  appear in the list.
- `roles` is not present in this response. The role-based model was removed in AUTH-13.
- Permissions are not exposed on `/me` — they are resolved at enforcement time via the
  permission cache.

---

## Backoffice permission helpers

The Backoffice Razor application provides two mechanisms to conditionally render UI based on
the current user's permissions.

### `HasPermissionAsync` HTML helper

Use in Razor views for conditional blocks:

```cshtml
@if (await Html.HasPermissionAsync("Profiles", "Delete"))
{
    <a asp-action="Delete" asp-route-id="@item.Id" class="btn btn-danger btn-sm">Delete</a>
}
```

The method calls `IUserPermissionService.HasPermissionAsync`, which goes through the Redis
cache before hitting the database.

### `RequirePermissionTagHelper`

Use on any HTML element to suppress it when the user lacks the permission. The element and
all its children are removed from the output entirely:

```cshtml
<div asp-require-permission-controller="Profiles"
     asp-require-permission-action="Create">
    <a asp-action="Create" class="btn btn-primary">New Profile</a>
</div>
```

The attributes `asp-require-permission-controller` and `asp-require-permission-action` are
removed from the final HTML output. The permission code is built using the same
`ControllerNameNormalizer` + `Permission.BuildCode` convention as the API enforcement layer.

Anonymous users always see the element suppressed for both helpers.

---

## Data model diagram

```
Users
├── Id                  (Guid PK)
├── Email               (nvarchar, filtered unique index WHERE IsDeleted = 0)
├── Username            (nvarchar, filtered unique index WHERE IsDeleted = 0)
├── PasswordHash        (nvarchar)
├── IsActive            (bit)
├── EmailConfirmedAt    (datetime2, nullable)
├── LastLoginAt         (datetime2, nullable)
├── FailedLoginAttempts (int)
├── LockedUntil         (datetime2, nullable)
├── CreatedAt           (datetime2)
├── UpdatedAt           (datetime2)
├── IsDeleted           (bit)
└── DeletedAt           (datetime2, nullable)

GroupPermissions
├── Id          (Guid PK)
├── Name        (nvarchar, unique)
├── IsDeleted   (bit)
└── DeletedAt   (datetime2, nullable)

Permissions
├── Id                (Guid PK)
├── Code              (nvarchar, unique)  ← "Controller.Action"
├── Controller        (nvarchar)
├── Action            (nvarchar)
├── DisplayName       (nvarchar)
├── GroupPermissionId (Guid FK → GroupPermissions, nullable)
├── IsOrphan          (bit)
├── OrphanedAt        (datetime2, nullable)
├── IsDeleted         (bit)
└── DeletedAt         (datetime2, nullable)

Profiles
├── Id          (Guid PK)
├── Name        (nvarchar, filtered unique index WHERE IsDeleted = 0)
├── Description (nvarchar)
├── IsSystem    (bit)
├── IsDeleted   (bit)
└── DeletedAt   (datetime2, nullable)

PermissionProfiles              ← join: Permission ↔ Profile
├── Id            (Guid PK)
├── PermissionId  (Guid FK → Permissions)
├── ProfileId     (Guid FK → Profiles)
├── IsDeleted     (bit)
└── DeletedAt     (datetime2, nullable)

UserProfiles                    ← join: User ↔ Profile
├── Id          (Guid PK)
├── UserId      (Guid FK → Users)
├── ProfileId   (Guid FK → Profiles)
├── IsDeleted   (bit)
└── DeletedAt   (datetime2, nullable)
```

---

## ADR references

- **[ADR-0001](adr/0001-mongodb-to-relational-efcore.md)** — Migration from MongoDB to SQL
  Server with EF Core; decision to use Railway as deployment target; Redis for distributed
  permission cache; Vercel ruled out for .NET runtime hosting.
- **[ADR-0002](adr/0002-admin-bootstrap-credential.md)** — Admin bootstrap credential
  inserted via EF Core data migration; plain-text password never committed; rotate before
  any non-development deployment.
