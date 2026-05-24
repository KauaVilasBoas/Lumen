using AegisIdentity.Domain.Users;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AegisIdentity.DataAccess.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    // MongoDB duplicate-key error code as defined by the BSON spec.
    private const int MongoDuplicateKeyCode = 11000;

    private readonly IMongoCollection<User> _collection;

    public UserRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<User>(CollectionNames.Users);
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Email, email);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<User?> FindByIdAsync(string id, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(id, out _))
            return null;

        var filter = Builders<User>.Filter.Eq(u => u.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<User?> FindByUsernameAsync(string username, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Username, username);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task InsertAsync(User user, CancellationToken ct = default)
    {
        try
        {
            await _collection.InsertOneAsync(user, options: null, ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Code == MongoDuplicateKeyCode)
        {
            // Translate the storage-level constraint to a domain exception so the
            // Application layer can handle conflicts without a MongoDB dependency.
            if (IsEmailIndex(ex.WriteError.Message))
                throw new DuplicateEmailException(user.Email);

            throw new DuplicateUsernameException(user.Username);
        }
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
        await _collection.ReplaceOneAsync(filter, user, cancellationToken: ct);
    }

    // The error message includes the index name ("ix_email_unique" or "ix_username_unique").
    // Falling back to username conflict when the message doesn't contain the email index
    // name is intentional — it handles both expected cases without needing reflection.
    private static bool IsEmailIndex(string errorMessage)
        => errorMessage.Contains("ix_email_unique", StringComparison.OrdinalIgnoreCase)
        || errorMessage.Contains("\"email\"", StringComparison.OrdinalIgnoreCase);
}
