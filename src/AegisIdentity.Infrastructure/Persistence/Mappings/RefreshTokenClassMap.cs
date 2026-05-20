using AegisIdentity.Domain.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace AegisIdentity.Infrastructure.Persistence.Mappings;

public static class RefreshTokenClassMap
{
    private static readonly object RegisterLock = new();
    private static bool _registered;

    public static void RegisterOnce()
    {
        if (_registered) return;

        lock (RegisterLock)
        {
            if (_registered) return;

            BsonClassMap.RegisterClassMap<RefreshToken>(map =>
            {
                map.AutoMap();

                map.MapIdMember(t => t.Id)
                   .SetIdGenerator(StringObjectIdGenerator.Instance)
                   .SetSerializer(new StringSerializer(BsonType.ObjectId));

                map.MapMember(t => t.UserId).SetElementName("userId");
                map.MapMember(t => t.TokenHash).SetElementName("tokenHash");
                map.MapMember(t => t.CreatedByIp).SetElementName("createdByIp");
                map.MapMember(t => t.ReplacedByTokenHash).SetElementName("replacedByTokenHash");
                map.MapMember(t => t.CreatedAt).SetElementName("createdAt");
                map.MapMember(t => t.ExpiresAt).SetElementName("expiresAt");
                map.MapMember(t => t.RevokedAt).SetElementName("revokedAt");
            });

            _registered = true;
        }
    }
}
