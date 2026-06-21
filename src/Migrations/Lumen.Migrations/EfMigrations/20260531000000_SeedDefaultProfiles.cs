using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisIdentity.Migrations.EfMigrations
{
    public partial class SeedDefaultProfiles : Migration
    {
        internal static readonly Guid AdministratorProfileId = new("20000000-0000-0000-0000-000000000001");
        internal static readonly Guid UserProfileId          = new("20000000-0000-0000-0000-000000000002");

        private static readonly Guid AdminUserProfileId = new("30000000-0000-0000-0000-000000000001");

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                table: "Profiles",
                columns: new[] { "Id", "Name", "Description", "IsSystem", "IsDeleted", "DeletedAt" },
                values: new object[]
                {
                    AdministratorProfileId,
                    "Administrator",
                    "System profile with full access to all permissions.",
                    true,
                    false,
                    null
                });

            migrationBuilder.InsertData(
                table: "Profiles",
                columns: new[] { "Id", "Name", "Description", "IsSystem", "IsDeleted", "DeletedAt" },
                values: new object[]
                {
                    UserProfileId,
                    "User",
                    "Base system profile for regular users. Permissions are granted explicitly.",
                    true,
                    false,
                    null
                });

            migrationBuilder.InsertData(
                table: "UserProfiles",
                columns: new[] { "Id", "UserId", "ProfileId", "IsDeleted", "DeletedAt" },
                values: new object[]
                {
                    AdminUserProfileId,
                    SeedInitialAdminUser.AdminUserId,
                    AdministratorProfileId,
                    false,
                    null
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: AdminUserProfileId);

            migrationBuilder.DeleteData(
                table: "Profiles",
                keyColumn: "Id",
                keyValue: UserProfileId);

            migrationBuilder.DeleteData(
                table: "Profiles",
                keyColumn: "Id",
                keyValue: AdministratorProfileId);
        }
    }
}
