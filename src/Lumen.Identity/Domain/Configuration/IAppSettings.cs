namespace Lumen.Identity.Domain.Configuration;

public interface IAppSettings
{
    string BaseUrl { get; }

    int LockoutThreshold { get; }

    TimeSpan LockoutDuration { get; }

    int RefreshTokenExpirationDays { get; }
}
