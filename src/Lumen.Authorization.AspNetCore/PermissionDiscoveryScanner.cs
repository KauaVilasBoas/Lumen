using Lumen.Authorization.Application.Permissions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Lumen.Authorization.AspNetCore;

public sealed class PermissionDiscoveryScanner
{
    private readonly IActionDescriptorCollectionProvider _actionDescriptorProvider;

    public PermissionDiscoveryScanner(IActionDescriptorCollectionProvider actionDescriptorProvider)
    {
        _actionDescriptorProvider = actionDescriptorProvider;
    }

    public IReadOnlyList<DiscoveredPermissionEntry> Scan()
    {
        var results = new List<DiscoveredPermissionEntry>();

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
                code = $"{normalizedController}.{normalizedAction}";

            var groupAttribute = controllerDescriptor.ControllerTypeInfo
                .GetCustomAttributes(typeof(PermissionGroupAttribute), inherit: true)
                .OfType<PermissionGroupAttribute>()
                .FirstOrDefault();

            var groupName = groupAttribute?.Name ?? normalizedController;

            results.Add(new DiscoveredPermissionEntry(
                Controller: normalizedController,
                Action: normalizedAction,
                Code: code,
                GroupName: groupName));
        }

        return results;
    }
}
