using FluentAssertions;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lumen.Authorization.Tests.MigrationBuilders;

public sealed class SqlServerMigrationBuilderExtensionsTests
{
    private static CapturingMigrationBuilder Build() => new();

    [Fact]
    public void SeedLumenPermissionGroup_GeneratesSqlTargetingPermissionGroupTable()
    {
        var mb = Build();
        mb.SeedLumenPermissionGroup("Estoque", "Gestão de estoque");

        mb.LastSql.Should().Contain("[Lumen].[PermissionGroup]",
            because: "SQL Server seeds must target the singular table name [Lumen].[PermissionGroup]");
        mb.LastSql.Should().Contain("[Description]",
            because: "the column is Description, not DisplayName");
    }

    [Fact]
    public void SeedLumenPermission_GeneratesSqlTargetingPermissionTable()
    {
        var mb = Build();
        mb.SeedLumenPermission("Estoque.Baixa", "Registrar baixa", "Estoque");

        mb.LastSql.Should().Contain("[Lumen].[Permission]",
            because: "SQL Server seeds must target the singular table name [Lumen].[Permission]");
        mb.LastSql.Should().NotContain("[Lumen].[Permissions]",
            because: "the plural table name Permissions does not exist");
        mb.LastSql.Should().Contain("[DisplayName]",
            because: "the permission column is DisplayName");
        mb.LastSql.Should().NotContain("[Controller]",
            because: "Controller column was removed in 3.0");
        mb.LastSql.Should().NotContain("[Action]",
            because: "Action column was removed in 3.0");
        mb.LastSql.Should().NotContain("[IsOrphan]",
            because: "IsOrphan column was removed in 3.0");
    }

    [Fact]
    public void SeedLumenPermission_WithoutGroup_InsertsNullForGroupPermissionId()
    {
        var mb = Build();
        mb.SeedLumenPermission("Auth.Login", "Autenticar");

        mb.LastSql.Should().Contain("NULL",
            because: "when no group is specified, GroupPermissionId must be NULL");
    }

    [Fact]
    public void SeedLumenPermissionGroup_WhenNameIsEmpty_Throws()
    {
        var mb = Build();
        var act = () => mb.SeedLumenPermissionGroup("", "desc");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SeedLumenPermission_WhenCodeIsEmpty_Throws()
    {
        var mb = Build();
        var act = () => mb.SeedLumenPermission("", "display");
        act.Should().Throw<ArgumentException>();
    }
}

public sealed class PostgreSqlMigrationBuilderExtensionsTests
{
    private static CapturingMigrationBuilder Build() => new();

    [Fact]
    public void SeedLumenPermissionGroup_GeneratesSqlTargetingPermissionGroupTable()
    {
        var mb = Build();
        Lumen.Authorization.Migrations.PostgreSQL.MigrationBuilderExtensions.SeedLumenPermissionGroup(
            mb, "Estoque", "Gestão de estoque");

        mb.LastSql.Should().Contain("\"Lumen\".\"PermissionGroup\"",
            because: "PostgreSQL seeds must target the singular table name \"Lumen\".\"PermissionGroup\"");
        mb.LastSql.Should().Contain("\"Description\"",
            because: "the column is Description, not DisplayName");
    }

    [Fact]
    public void SeedLumenPermission_GeneratesSqlTargetingPermissionTable()
    {
        var mb = Build();
        Lumen.Authorization.Migrations.PostgreSQL.MigrationBuilderExtensions.SeedLumenPermission(
            mb, "Estoque.Baixa", "Registrar baixa", "Estoque");

        mb.LastSql.Should().Contain("\"Lumen\".\"Permission\"",
            because: "PostgreSQL seeds must target the singular table name \"Lumen\".\"Permission\"");
        mb.LastSql.Should().NotContain("\"Lumen\".\"Permissions\"",
            because: "the plural table name Permissions does not exist");
        mb.LastSql.Should().Contain("\"DisplayName\"",
            because: "the permission column is DisplayName");
        mb.LastSql.Should().NotContain("\"Controller\"",
            because: "Controller column was removed in 3.0");
        mb.LastSql.Should().NotContain("\"Action\"",
            because: "Action column was removed in 3.0");
        mb.LastSql.Should().NotContain("\"IsOrphan\"",
            because: "IsOrphan column was removed in 3.0");
    }

    [Fact]
    public void SeedLumenPermission_WithoutGroup_InsertsNullForGroupPermissionId()
    {
        var mb = Build();
        Lumen.Authorization.Migrations.PostgreSQL.MigrationBuilderExtensions.SeedLumenPermission(
            mb, "Auth.Login", "Autenticar");

        mb.LastSql.Should().Contain("NULL",
            because: "when no group is specified, GroupPermissionId must be NULL");
    }

    [Fact]
    public void SeedLumenPermissionGroup_WhenNameIsEmpty_Throws()
    {
        var mb = Build();
        var act = () => Lumen.Authorization.Migrations.PostgreSQL.MigrationBuilderExtensions.SeedLumenPermissionGroup(
            mb, "", "desc");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SeedLumenPermission_WhenCodeIsEmpty_Throws()
    {
        var mb = Build();
        var act = () => Lumen.Authorization.Migrations.PostgreSQL.MigrationBuilderExtensions.SeedLumenPermission(
            mb, "", "display");
        act.Should().Throw<ArgumentException>();
    }
}

internal sealed class CapturingMigrationBuilder : MigrationBuilder
{
    private string _lastSql = string.Empty;

    public string LastSql => _lastSql;

    public CapturingMigrationBuilder() : base(activeProvider: "fake") { }

    public void SeedLumenPermissionGroup(string name, string description)
        => Lumen.Authorization.Migrations.MigrationBuilderExtensions.SeedLumenPermissionGroup(this, name, description);

    public void SeedLumenPermission(string code, string displayName, string? groupName = null)
        => Lumen.Authorization.Migrations.MigrationBuilderExtensions.SeedLumenPermission(this, code, displayName, groupName);

    public override Microsoft.EntityFrameworkCore.Migrations.Operations.Builders.OperationBuilder<Microsoft.EntityFrameworkCore.Migrations.Operations.SqlOperation> Sql(
        string sql, bool suppressTransaction = false)
    {
        _lastSql = sql;
        return base.Sql(sql, suppressTransaction);
    }
}
