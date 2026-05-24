using AegisIdentity.Domain.Users;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace AegisIdentity.DataAccess.Persistence.Mappings;

public static class UserClassMap
{
    private static readonly object RegisterLock = new();
    private static bool _registered;

    public static void RegisterOnce()
    {
        if (_registered) return;

        lock (RegisterLock)
        {
            if (_registered) return;

            BsonClassMap.RegisterClassMap<User>(map =>
            {
                map.AutoMap();

                map.MapIdMember(u => u.Id)
                   .SetIdGenerator(StringObjectIdGenerator.Instance)
                   .SetSerializer(new StringSerializer(BsonType.ObjectId));

                map.MapMember(u => u.Email).SetElementName("email");
                map.MapMember(u => u.Username).SetElementName("username");
                map.MapMember(u => u.PasswordHash).SetElementName("passwordHash");
                map.MapMember(u => u.Roles).SetElementName("roles");
                map.MapMember(u => u.IsActive).SetElementName("isActive");
                map.MapMember(u => u.EmailConfirmedAt).SetElementName("emailConfirmedAt");
                map.MapMember(u => u.LastLoginAt).SetElementName("lastLoginAt");
                map.MapMember(u => u.FailedLoginAttempts).SetElementName("failedLoginAttempts");
                map.MapMember(u => u.LockedUntil).SetElementName("lockedUntil");
                map.MapMember(u => u.CreatedAt).SetElementName("createdAt");
                map.MapMember(u => u.UpdatedAt).SetElementName("updatedAt");
            });

            _registered = true;
        }
    }
}
