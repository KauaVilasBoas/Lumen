using AegisIdentity.Application.Configuration;
using Microsoft.Extensions.Options;

namespace AegisIdentity.Infrastructure.Configuration;

// Bridges AppOptions (Infrastructure) to IAppSettings (Application) so that
// use cases can consume configuration values without referencing IOptions<T>.
public sealed class AppSettingsAdapter : IAppSettings
{
    private readonly AppOptions _options;

    public AppSettingsAdapter(IOptions<AppOptions> options)
    {
        _options = options.Value;
    }

    public string BaseUrl => _options.BaseUrl;

    public int LockoutThreshold => _options.LockoutThreshold;

    public TimeSpan LockoutDuration => TimeSpan.FromMinutes(_options.LockoutDurationMinutes);

    public int RefreshTokenExpirationDays => _options.RefreshTokenExpirationDays;
}
