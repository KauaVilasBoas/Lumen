using AegisIdentity.Domain.Tokens;
using MongoDB.Driver;

namespace AegisIdentity.DataAccess.Persistence.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IMongoCollection<RefreshToken> _collection;

    public RefreshTokenRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<RefreshToken>(CollectionNames.RefreshTokens);
    }

    public async Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        var filter = Builders<RefreshToken>.Filter.Eq(t => t.TokenHash, tokenHash);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<RefreshToken>> FindByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var filter = Builders<RefreshToken>.Filter.Eq(t => t.UserId, userId);
        var results = await _collection.Find(filter).ToListAsync(ct);
        return results.AsReadOnly();
    }

    public async Task InsertAsync(RefreshToken token, CancellationToken ct = default)
    {
        await _collection.InsertOneAsync(token, options: null, ct);
    }

    public async Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        var filter = Builders<RefreshToken>.Filter.Eq(t => t.Id, token.Id);
        await _collection.ReplaceOneAsync(filter, token, cancellationToken: ct);
    }

    public async Task<long> DeleteExpiredAsync(DateTime cutoff, CancellationToken ct = default)
    {
        var filter = Builders<RefreshToken>.Filter.Lt(t => t.ExpiresAt, cutoff);
        var result = await _collection.DeleteManyAsync(filter, ct);
        return result.DeletedCount;
    }
}
