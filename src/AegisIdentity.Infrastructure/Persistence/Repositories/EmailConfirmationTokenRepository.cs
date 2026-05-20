using AegisIdentity.Domain.Tokens;
using MongoDB.Driver;

namespace AegisIdentity.Infrastructure.Persistence.Repositories;

/// <summary>
/// MongoDB implementation of <see cref="IEmailConfirmationTokenRepository"/>.
/// </summary>
public sealed class EmailConfirmationTokenRepository : IEmailConfirmationTokenRepository
{
    private readonly IMongoCollection<EmailConfirmationToken> _collection;

    public EmailConfirmationTokenRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<EmailConfirmationToken>(CollectionNames.EmailConfirmationTokens);
    }

    /// <inheritdoc/>
    public async Task<EmailConfirmationToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        var filter = Builders<EmailConfirmationToken>.Filter.Eq(t => t.TokenHash, tokenHash);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc/>
    public async Task InsertAsync(EmailConfirmationToken token, CancellationToken ct = default)
    {
        await _collection.InsertOneAsync(token, options: null, ct);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(EmailConfirmationToken token, CancellationToken ct = default)
    {
        var filter = Builders<EmailConfirmationToken>.Filter.Eq(t => t.Id, token.Id);
        await _collection.ReplaceOneAsync(filter, token, cancellationToken: ct);
    }
}
