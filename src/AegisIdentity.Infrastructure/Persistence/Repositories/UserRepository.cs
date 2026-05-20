using AegisIdentity.Domain.Users;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AegisIdentity.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
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
        await _collection.InsertOneAsync(user, options: null, ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
        await _collection.ReplaceOneAsync(filter, user, cancellationToken: ct);
    }
}
