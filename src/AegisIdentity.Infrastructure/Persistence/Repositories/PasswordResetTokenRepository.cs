using AegisIdentity.Domain.Tokens;
using MongoDB.Driver;

namespace AegisIdentity.Infrastructure.Persistence.Repositories;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly IMongoCollection<PasswordResetToken> _collection;

    public PasswordResetTokenRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<PasswordResetToken>(CollectionNames.PasswordResetTokens);
    }

    public async Task<PasswordResetToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        var filter = Builders<PasswordResetToken>.Filter.Eq(t => t.TokenHash, tokenHash);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task InsertAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        await _collection.InsertOneAsync(token, options: null, ct);
    }

    public async Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        var filter = Builders<PasswordResetToken>.Filter.Eq(t => t.Id, token.Id);
        await _collection.ReplaceOneAsync(filter, token, cancellationToken: ct);
    }
}
