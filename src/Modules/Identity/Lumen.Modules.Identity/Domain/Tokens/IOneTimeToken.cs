using Lumen.SharedKernel.Persistence;

namespace Lumen.Modules.Identity.Domain.Tokens;

internal interface IOneTimeToken : ISoftDeletable
{
    Guid Id { get; }
    Guid UserId { get; }
    string TokenHash { get; }
    DateTime CreatedAt { get; }
    DateTime ExpiresAt { get; }
    DateTime? UsedAt { get; }

    bool IsExpired();
    bool IsUsed();
    bool IsValid();
    void MarkAsUsed();
    void SoftDelete();
}
