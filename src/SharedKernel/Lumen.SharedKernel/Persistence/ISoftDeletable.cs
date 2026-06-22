namespace Lumen.SharedKernel.Persistence;

/// <summary>
/// Marks an entity as soft-deletable. EF Core applies a global query filter
/// that excludes rows where <see cref="IsDeleted"/> is <c>true</c>.
/// Use <c>IgnoreQueryFilters()</c> on a query to include deleted records.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
}
