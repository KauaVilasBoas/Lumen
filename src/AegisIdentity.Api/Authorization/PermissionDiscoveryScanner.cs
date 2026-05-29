using AegisIdentity.Domain.Authorization;
using AegisIdentity.SharedKernel.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace AegisIdentity.Api.Authorization;

public sealed class PermissionDiscoveryScanner
{
    private readonly IActionDescriptorCollectionProvider _actionDescriptorProvider;

    public PermissionDiscoveryScanner(IActionDescriptorCollectionProvider actionDescriptorProvider)
    {
        _actionDescriptorProvider = actionDescriptorProvider;
    }

    public IReadOnlyList<DiscoveredPermission> Scan()
    {
        var results = new List<DiscoveredPermission>();

        foreach (var descriptor in _actionDescriptorProvider.ActionDescriptors.Items)
        {
            if (descriptor is not ControllerActionDescriptor controllerDescriptor)
                continue;

            var actionAttribute = controllerDescriptor.MethodInfo
                .GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: true)
                .OfType<RequirePermissionAttribute>()
                .FirstOrDefault();

            var controllerAttribute = controllerDescriptor.ControllerTypeInfo
                .GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: true)
                .OfType<RequirePermissionAttribute>()
                .FirstOrDefault();

            if (actionAttribute is null && controllerAttribute is null)
                continue;

            var normalizedController = ControllerNameNormalizer.Normalize(
                controllerDescriptor.ControllerTypeInfo.Name);

            var normalizedAction = controllerDescriptor.MethodInfo.Name;

            string code;
            if (actionAttribute?.Code is not null)
                code = actionAttribute.Code;
            else if (controllerAttribute?.Code is not null)
                code = controllerAttribute.Code;
            else
                code = Permission.BuildCode(normalizedController, normalizedAction);

            var groupAttribute = controllerDescriptor.ControllerTypeInfo
                .GetCustomAttributes(typeof(PermissionGroupAttribute), inherit: true)
                .OfType<PermissionGroupAttribute>()
                .FirstOrDefault();

            var groupName = groupAttribute?.Name ?? normalizedController;

            var displayName = $"{normalizedController} — {normalizedAction}";

            results.Add(new DiscoveredPermission(
                normalizedController,
                normalizedAction,
                code,
                displayName,
                groupName));
        }

        return results;
    }
}
