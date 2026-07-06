using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumen.Identity.Migrations.EfMigrations
{
    public partial class InitialIdentitySchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "identity");

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "identity",
                columns: table => new
                {
                    Id                  = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email               = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Username            = table.Column<string>(type: "nvarchar(64)",  maxLength: 64,  nullable: false),
                    PasswordHash        = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsBootstrap         = table.Column<bool>(type: "bit", nullable: false),
                    IsActive            = table.Column<bool>(type: "bit", nullable: false),
                    EmailConfirmedAt    = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginAt         = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockedUntil         = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt           = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt           = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted           = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt           = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Users", x => x.Id));

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "identity",
                columns: table => new
                {
                    Id                  = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId              = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash           = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedByIp         = table.Column<string>(type: "nvarchar(64)",  maxLength: 64,  nullable: false),
                    ReplacedByTokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt           = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt           = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt           = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted           = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt           = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                schema: "identity",
                columns: table => new
                {
                    Id        = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId    = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt    = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailConfirmationTokens",
                schema: "identity",
                columns: table => new
                {
                    Id        = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId    = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt    = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailConfirmationTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailConfirmationTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Indexes
            migrationBuilder.CreateIndex(
                name: "ix_identity_users_email_unique",
                table: "Users",
                column: "Email",
                schema: "identity",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "ix_identity_users_username_unique",
                table: "Users",
                column: "Username",
                schema: "identity",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "ix_identity_users_locked_until",
                table: "Users",
                column: "LockedUntil",
                schema: "identity",
                filter: "[LockedUntil] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_identity_refresh_tokens_hash",
                table: "RefreshTokens",
                column: "TokenHash",
                schema: "identity",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_identity_refresh_tokens_user_id",
                table: "RefreshTokens",
                column: "UserId",
                schema: "identity");

            migrationBuilder.CreateIndex(
                name: "ix_identity_email_confirmation_tokens_hash",
                table: "EmailConfirmationTokens",
                column: "TokenHash",
                schema: "identity",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_identity_email_confirmation_tokens_user_id",
                table: "EmailConfirmationTokens",
                column: "UserId",
                schema: "identity");

            migrationBuilder.CreateIndex(
                name: "ix_identity_password_reset_tokens_hash",
                table: "PasswordResetTokens",
                column: "TokenHash",
                schema: "identity",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_identity_password_reset_tokens_user_id",
                table: "PasswordResetTokens",
                column: "UserId",
                schema: "identity");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "EmailConfirmationTokens",  schema: "identity");
            migrationBuilder.DropTable(name: "PasswordResetTokens",      schema: "identity");
            migrationBuilder.DropTable(name: "RefreshTokens",            schema: "identity");
            migrationBuilder.DropTable(name: "Users",                    schema: "identity");
        }
    }
}
