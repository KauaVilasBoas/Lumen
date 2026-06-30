using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Lumen.IntegrationTests.Infrastructure;
using Lumen.Modules.Identity.Domain.Security;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.Modules.Identity.Persistence.Repositories;
using Lumen.SharedKernel.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.IntegrationTests.CrossModule;

/// <summary>
/// Validates the cross-module flow: a successful login triggers <c>UserLoggedInEvent</c>
/// via the in-process event bus, which the Audit module handles by writing to
/// <c>audit.AuditEntries</c>.
/// </summary>
[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class LoginAuditCrossModuleTests
{
    private readonly IntegrationFixture _fixture;

    public LoginAuditCrossModuleTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_SuccessfulAuthentication_CreatesAuditEntry()
    {
        var username = $"audit-user-{Guid.NewGuid():N}";
        var email = $"{username}@test.local";
        const string plainPassword = "Password123!";

        await SeedActiveUserAsync(email, username, plainPassword);

        var client = _fixture.CreateAnonymousClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Identifier = username,
            Password = plainPassword,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, "login must succeed before checking audit");

        await using var auditDb = _fixture.CreateAuditDbContext();
        var entry = await auditDb.AuditEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Kind == AuditEventKinds.AuthLogin && e.Actor == username);

        entry.Should().NotBeNull(
            "UserLoggedInEvent must be published by Identity and handled by Audit to create an audit.login entry");
        entry!.Kind.Should().Be(AuditEventKinds.AuthLogin);
        entry.Actor.Should().Be(username);
    }

    private async Task SeedActiveUserAsync(string email, string username, string plainPassword)
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var passwordHash = hasher.Hash(plainPassword);

        await using var db = _fixture.CreateIdentityDbContext();
        var repository = new UserRepository(db);
        var user = User.Create(email, username, passwordHash);

        await repository.InsertAsync(user);

        user.ConfirmEmail();
        await repository.UpdateAsync(user);
    }
}
