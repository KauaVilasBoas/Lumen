namespace Lumen.Modules.Identity.Domain.Configuration;

internal interface IAppSettings
{
    string BaseUrl { get; }

    int LockoutThreshold { get; }

    TimeSpan LockoutDuration { get; }

    int RefreshTokenExpirationDays { get; }
}
