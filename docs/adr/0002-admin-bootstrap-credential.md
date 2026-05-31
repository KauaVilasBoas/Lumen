# ADR 0002 — Admin Bootstrap Credential

**Status:** Accepted
**Date:** 2026-05-29
**Related:** INFRA-04, AUTH-12

## Context

The relational migration introduced in INFRA-04 replaces the Mongo migration stack.
A deterministic admin user must exist from the first migration so AUTH-12 (UserProfile seed)
can reference it by a stable Guid.

## Decision

The initial admin is inserted via EF Core migration `SeedInitialAdminUser`.

| Field            | Value                                    |
|------------------|------------------------------------------|
| `Id`             | `10000000-0000-0000-0000-000000000001`   |
| `Email`          | `admin@aegisidentity.local`              |
| `Username`       | `admin`                                  |
| `Password`       | not committed — shared out-of-band by the maintainer |
| `Roles`          | `user,admin`                             |
| `EmailConfirmedAt` | pre-confirmed (bootstrap user)         |

Only the **BCrypt hash** (work factor 12) of the bootstrap password is stored, inside the
migration `SeedInitialAdminUser` (`AdminPasswordHash` constant). The plain-text password is
**not** present anywhere in the repository; it is communicated to the maintainer out-of-band.

## WARNING — Rotate Before Production

**This credential is a bootstrap default and MUST be rotated before the service is
exposed publicly or deployed to any non-development environment.**

Steps to rotate:
1. Log in as `admin@aegisidentity.local` with the bootstrap password (provided out-of-band).
2. Use the change-password endpoint (AUTH-08) to set a strong, environment-specific password.
3. Optionally update the email to a real operations address.

No "force password change on first login" flow exists by design — it is out of scope
for INFRA-04. A future AUTH card may add that enforcement.

## Consequences

- The `AdminUserId` constant (`10000000-0000-0000-0000-000000000001`) is exposed as
  `internal static readonly` on `SeedInitialAdminUser` for reference by AUTH-12.
- The migration `Down()` deletes the admin row cleanly via `DeleteData`.
- The plain-text bootstrap password is never committed; only the BCrypt hash lives in the
  migration. Recovering the password from the hash is infeasible.
