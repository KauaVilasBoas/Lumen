using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumen.Authorization.Migrations.EfMigrations
{
    public partial class SeedLumenSystemProfiles : Migration
    {
        private static readonly Guid AdministratorProfileId = new("20000000-0000-0000-0000-000000000001");
        private static readonly Guid UserProfileId          = new("20000000-0000-0000-0000-000000000002");

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "Lumen",
                table: "Profile",
                columns: new[] { "Id", "Name", "Description", "IsSystem", "IsDeleted", "DeletedAt" },
                values: new object[] { AdministratorProfileId, "Administrator", "System profile with full access to all permissions.", true, false, null! });

            migrationBuilder.InsertData(
                schema: "Lumen",
                table: "Profile",
                columns: new[] { "Id", "Name", "Description", "IsSystem", "IsDeleted", "DeletedAt" },
                values: new object[] { UserProfileId, "User", "Base system profile for regular users. Permissions are granted explicitly.", true, false, null! });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [Lumen].[Profile] WHERE [Id] IN ('20000000-0000-0000-0000-000000000001','20000000-0000-0000-0000-000000000002');");
        }
    }
}
