using Lumen.Authorization.Application.Permissions;
using Lumen.Authorization.Domain;
using Lumen.Authorization.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lumen.Authorization.AspNetCore;

internal sealed class PermissionCatalogValidationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PermissionDiscoveryScanner _scanner;
    private readonly bool _failFastOnMissingPermission;
    private readonly ILogger<PermissionCatalogValidationService> _logger;

    public PermissionCatalogValidationService(
        IServiceScopeFactory scopeFactory,
        PermissionDiscoveryScanner scanner,
        bool failFastOnMissingPermission,
        ILogger<PermissionCatalogValidationService> logger)
    {
        _scopeFactory = scopeFactory;
        _scanner = scanner;
        _failFastOnMissingPermission = failFastOnMissingPermission;
        _logger = logger;
    }

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        var discovered = _scanner.Scan();

        if (discovered.Count == 0)
        {
            _logger.LogInformation("No actions decorated with [RequirePermission] found.");
            return;
        }

        var discoveredCodes = discovered.Select(d => d.Code).ToHashSet(StringComparer.Ordinal);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var permissionRepository = scope.ServiceProvider.GetRequiredService<IPermissionRepository>();

        var existingPermissions = await permissionRepository.ListAllAsync(cancellationToken);
        var existingCodes = existingPermissions.Select(p => p.Code).ToHashSet(StringComparer.Ordinal);

        var missingCodes = discoveredCodes
            .Where(code => !existingCodes.Contains(code))
            .OrderBy(code => code)
            .ToList();

        if (missingCodes.Count == 0)
        {
            _logger.LogInformation(
                "Permission catalog validation passed. All {Count} permission(s) are seeded.",
                discoveredCodes.Count);
            return;
        }

        var missingList = string.Join(", ", missingCodes);

        if (_failFastOnMissingPermission)
            throw new InvalidOperationException(
                string.Format(AuthorizationErrorMessages.MissingPermissionsInCatalog, missingList));

        _logger.LogWarning(
            "Permission catalog validation: {Count} permission code(s) used in [RequirePermission] have no corresponding row in the database: {Codes}. " +
            "Seed them via MigrationBuilder.SeedLumenPermission() in a consumer migration. " +
            "Set FailFastOnMissingPermission = true to abort startup instead of warning.",
            missingCodes.Count,
            missingList);
    }
}
