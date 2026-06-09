using System.Net;
using System.Text.Json;
using AegisIdentity.Backoffice.Controllers;
using AegisIdentity.Backoffice.Services;
using AegisIdentity.UnitTests.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace AegisIdentity.UnitTests.Backoffice;

public sealed class HomeControllerTests
{
    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HomeControllerTests()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
    }

    private HomeController BuildController(StubHttpMessageHandler httpHandler)
    {
        var httpClient   = new HttpClient(httpHandler) { BaseAddress = new Uri("http://api.test/") };
        var adminApiClient = new AdminApiClient(httpClient, _httpContextAccessor);

        return new HomeController(adminApiClient)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private static StubHttpMessageHandler BuildHandler(
        int userTotal              = 3,
        int profileCount           = 2,
        int activityCount          = 5,
        double? cacheHitRate       = 0.95,
        bool usersEndpointFails    = false,
        bool activityEndpointFails = false,
        bool cacheEndpointFails    = false,
        bool jobsEndpointFails     = false)
    {
        return new StubHttpMessageHandler((req, _) =>
        {
            var path = req.RequestUri!.PathAndQuery;

            if (path.StartsWith("/api/users"))
            {
                if (usersEndpointFails)
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

                var usersPage = new AdminApiClient.UsersPage([], 1, 1, userTotal);
                return JsonOk(usersPage);
            }

            if (path.StartsWith("/api/profiles"))
            {
                var profiles = Enumerable
                    .Range(0, profileCount)
                    .Select(i => new AdminApiClient.ProfileItem(Guid.NewGuid(), $"P{i}", "", false))
                    .ToList();
                return JsonOk(profiles);
            }

            if (path.StartsWith("/api/permissions"))
            {
                var perms = new List<AdminApiClient.PermissionGroup>
                {
                    new(null, "Default", [
                        new(Guid.NewGuid(), "Users.List", "List users", false),
                        new(Guid.NewGuid(), "Orphan.Action", "Orphan", true)
                    ])
                };
                return JsonOk(perms);
            }

            if (path.StartsWith("/api/audit/recent"))
            {
                if (activityEndpointFails)
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

                var entries = Enumerable
                    .Range(0, activityCount)
                    .Select(i => new AdminApiClient.AuditEntry(
                        "auth.login", "actor", "target", $"Event {i}", DateTime.UtcNow))
                    .ToList();
                return JsonOk(entries);
            }

            if (path.StartsWith("/api/diagnostics/cache-stats"))
            {
                if (cacheEndpointFails)
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

                var stats = new { hitRate = cacheHitRate };
                return JsonOk(stats);
            }

            if (path.StartsWith("/api/diagnostics/job-stats"))
            {
                if (jobsEndpointFails)
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

                var jobStats = new AdminApiClient.JobStats(
                    DailySeries: [1L, 2L, 3L],
                    NextRunUtc: DateTime.UtcNow.AddHours(6));
                return JsonOk(jobStats);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });
    }

    private static Task<HttpResponseMessage> JsonOk<T>(T payload)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload, CamelCase),
                System.Text.Encoding.UTF8,
                "application/json")
        });

    [Fact]
    public async Task Index_AllSourcesSucceed_PopulatesAllViewBagEntries()
    {
        var sut = BuildController(BuildHandler(userTotal: 7, profileCount: 3));

        var result = await sut.Index(CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        var view = (ViewResult)result;

        ((int?)view.ViewData["UserCount"]).Should().Be(7);
        ((int?)view.ViewData["ProfileCount"]).Should().Be(3);
        ((int?)view.ViewData["PermissionCount"]).Should().Be(2);
        ((int?)view.ViewData["OrphanCount"]).Should().Be(1);
        view.ViewData["Activity"].Should().NotBeNull();
        view.ViewData["CacheHitRate"].Should().NotBeNull();
        view.ViewData["JobStats"].Should().NotBeNull();
    }

    [Fact]
    public async Task Index_UserEndpointFails_UserCountAbsentFromViewBag()
    {
        var sut = BuildController(BuildHandler(usersEndpointFails: true));

        var result = await sut.Index(CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        var view = (ViewResult)result;
        ((int?)view.ViewData["UserCount"]).Should().BeNull();
    }

    [Fact]
    public async Task Index_ActivityEndpointFails_ActivityAbsentFromViewBag()
    {
        var sut = BuildController(BuildHandler(activityEndpointFails: true));

        var result = await sut.Index(CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        var view = (ViewResult)result;
        view.ViewData["Activity"].Should().BeNull();
    }

    [Fact]
    public async Task Index_CacheEndpointFails_CacheHitRateAbsentFromViewBag()
    {
        var sut = BuildController(BuildHandler(cacheEndpointFails: true));

        var result = await sut.Index(CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        var view = (ViewResult)result;
        view.ViewData["CacheHitRate"].Should().BeNull();
    }

    [Fact]
    public async Task Index_JobsEndpointFails_JobStatsAbsentFromViewBag()
    {
        var sut = BuildController(BuildHandler(jobsEndpointFails: true));

        var result = await sut.Index(CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        var view = (ViewResult)result;
        view.ViewData["JobStats"].Should().BeNull();
    }

    [Fact]
    public async Task Index_CacheHitRateIsNull_CacheHitRateAbsentFromViewBag()
    {
        var sut = BuildController(BuildHandler(cacheHitRate: null));

        var result = await sut.Index(CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        var view = (ViewResult)result;
        view.ViewData["CacheHitRate"].Should().BeNull();
    }

    [Fact]
    public async Task Index_AllSourcesFail_StillReturnsView()
    {
        var handler = StubHttpMessageHandler.Throwing(new HttpRequestException("Network unreachable"));
        var sut     = BuildController(handler);

        var result = await sut.Index(CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Index_ActivityHasEntries_ActivityPopulatedCorrectly()
    {
        var sut = BuildController(BuildHandler(activityCount: 5));

        var result = await sut.Index(CancellationToken.None);

        var view     = (ViewResult)result;
        var activity = view.ViewData["Activity"] as IReadOnlyList<AdminApiClient.AuditEntry>;

        activity.Should().NotBeNull();
        activity!.Count.Should().Be(5);
    }
}
