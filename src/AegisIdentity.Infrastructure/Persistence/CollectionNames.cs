namespace AegisIdentity.Infrastructure.Persistence;

/// <summary>
/// Central registry of MongoDB collection names.
/// Using constants avoids magic strings scattered across repositories and index initializers.
/// </summary>
public static class CollectionNames
{
    public const string Users = "users";
}
