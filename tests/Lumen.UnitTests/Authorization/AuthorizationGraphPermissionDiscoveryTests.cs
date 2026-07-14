using Lumen.Authorization.AspNetCore;
using Lumen.SharedKernel.Constants;
using FluentAssertions;

namespace Lumen.UnitTests.Authorization;

public sealed class AuthorizationGraphPermissionDiscoveryTests
{
    [Fact]
    public void PermissionCodes_AuthorizationGraph_View_FollowsControllerActionConvention()
    {
        var expected = $"{ControllerNameNormalizer.Normalize("AuthorizationGraphController")}.View";

        PermissionCodes.AuthorizationGraph.View.Should().Be(expected);
    }
}
