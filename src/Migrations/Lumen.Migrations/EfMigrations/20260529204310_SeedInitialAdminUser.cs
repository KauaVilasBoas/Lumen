using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisIdentity.Migrations.EfMigrations
{
    public partial class SeedInitialAdminUser : Migration
    {
        // Deterministic Guid — referenced by AUTH-12 (UserProfile seed).
        // Do NOT change this value after the migration is applied to any environment.
        internal static readonly Guid AdminUserId = new("10000000-0000-0000-0000-000000000001");

        // Default bootstrap credential (BCrypt, work factor 12) — rotate before production.
        // See docs/adr/0002-admin-bootstrap-credential.md.
        private const string AdminPasswordHash =
            "$2a$12$6SQF6.kItSzz8QssA9f96eIHQAZtPeXcvZ.QvV/WvO7Ko13BfIpYu";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = new DateTime(2026, 5, 29, 0, 0, 0, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[]
                {
                    "Id", "Email", "Username", "PasswordHash", "Roles",
                    "IsActive", "EmailConfirmedAt", "LastLoginAt",
                    "FailedLoginAttempts", "LockedUntil",
                    "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt"
                },
                values: new object[]
                {
                    AdminUserId,
                    "admin@aegisidentity.local",
                    "admin",
                    AdminPasswordHash,
                    "user,admin",
                    true,
                    now,
                    null,
                    0,
                    null,
                    now,
                    now,
                    false,
                    null
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: AdminUserId);
        }
    }
}
