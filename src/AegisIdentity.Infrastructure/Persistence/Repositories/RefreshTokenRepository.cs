using AegisIdentity.Domain.Tokens;
using MongoDB.Driver;

namespace AegisIdentity.Infrastructure.Persistence.Repositories;

/// <summary>
/// MongoDB implementation of <see cref="IRefreshTokenRepository"/>.
/// </summary>
public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IMongoCollection<RefreshToken> _collection;

    public RefreshTokenRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<RefreshToken>(CollectionNames.RefreshTokens);
    }

    /// <inheritdoc/>
    public async Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        var filter = Builders<RefreshToken>.Filter.Eq(t => t.TokenHash, tokenHash);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RefreshToken>> FindByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var filter = Builders<RefreshToken>.Filter.Eq(t => t.UserId, userId);
        var results = await _collection.Find(filter).ToListAsync(ct);
        return results.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task InsertAsync(RefreshToken token, CancellationToken ct = default)
    {
        await _collection.InsertOneAsync(token, options: null, ct);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        var filter = Builders<RefreshToken>.Filter.Eq(t => t.Id, token.Id);
        await _collection.ReplaceOneAsync(filter, token, cancellationToken: ct);
    }
}
