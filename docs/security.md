# Security reference

## Password policy

Every password accepted by the system (registration, change, reset) is validated by
`IPasswordValidator` (implementation in `src/Lumen.Infrastructure/Security/`). The rules:

- Minimum **12 characters**.
- At least **one uppercase letter**, **one lowercase letter**, **one digit** and **one special character** from ``!@#$%^&*()-_=+[]{};:'",.<>/?\|`~``.
- Must not equal the user's email or username (case-insensitive).
- Must not appear in the **HaveIBeenPwned Pwned Passwords** dataset.

Error messages are returned one rule per line — the user sees everything they need to fix in a
single response.

## HaveIBeenPwned integration (k-anonymity)

The HIBP check uses the **k-anonymity** model: `PwnedPasswordsClient` sends only the first
5 hex characters of `SHA1(password)` to `https://api.pwnedpasswords.com/range/{prefix}`
(with `Add-Padding: true`). The full password — and even its full hash — never leaves the
process. Results are cached in memory for **1 hour** per prefix.

The HIBP client is **fail-open**: timeouts or upstream errors do not block registration — they
emit a structured `Warning` and the password is accepted. This is a deliberate trade-off: an
external dependency outage must never deny access to our own system. The residual risk is
tracked in `SEC-05`.

## Login responses

| Code | Meaning |
|---|---|
| `200` | Login succeeded — returns `accessToken`, `refreshToken` and `expiresIn` |
| `400` | Validation failed — `identifier` or `password` is blank |
| `401` | Invalid credentials — user does not exist or password is wrong (deliberately opaque to prevent enumeration) |
| `403` | Email is not confirmed |
| `423` | Account locked after repeated failures — wait and retry |

## Authorization fail-safe behaviour

User permissions are cached in Redis under `user:permissions:{userId}` with event-driven
invalidation (`UserPermissionsChanged`). When Redis is unavailable the enforcement layer falls
back to the database — **authorization never fails open**.

See [authz.md](authz.md) for the complete authorization model.
