using Lumen.Modules.Identity.Domain.Configuration;
using Microsoft.Extensions.Options;

namespace Lumen.Modules.Identity.Infrastructure.Configuration;

internal sealed class AppSettingsAdapter : IAppSettings
{
    private readonly IdentityAppOptions _options;

    public AppSettingsAdapter(IOptions<IdentityAppOptions> options)
    {
        _options = options.Value;
    }

    public string BaseUrl => _options.BaseUrl;

    public int LockoutThreshold => _options.LockoutThreshold;

    public TimeSpan LockoutDuration => TimeSpan.FromMinutes(_options.LockoutDurationMinutes);

    public int RefreshTokenExpirationDays => _options.RefreshTokenExpirationDays;
}
