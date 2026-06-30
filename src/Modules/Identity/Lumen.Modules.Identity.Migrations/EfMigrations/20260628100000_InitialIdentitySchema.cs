using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumen.Modules.Identity.Migrations.EfMigrations
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
                    Id                    = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId                = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash             = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedByIp           = table.Column<string>(type: "nvarchar(64)",  maxLength: 64,  nullable: false),
                    ReplacedByTokenHash   = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt             = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt             = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt             = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted             = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt             = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                constraints: table => table.PrimaryKey("PK_PasswordResetTokens", x => x.Id));

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
                constraints: table => table.PrimaryKey("PK_EmailConfirmationTokens", x => x.Id));

            migrationBuilder.CreateTable(
                name: "GroupPermissions",
                schema: "identity",
                columns: table => new
                {
                    Id          = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name        = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    IsDeleted   = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt   = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_GroupPermissions", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "identity",
                columns: table => new
                {
                    Id                = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code              = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Controller        = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Action            = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayName       = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    GroupPermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsOrphan          = table.Column<bool>(type: "bit", nullable: false),
                    OrphanedAt        = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted         = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt         = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_GroupPermissions_GroupPermissionId",
                        column: x => x.GroupPermissionId,
                        principalSchema: "identity",
                        principalTable: "GroupPermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                schema: "identity",
                columns: table => new
                {
                    Id          = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name        = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    IsSystem    = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted   = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt   = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Profiles", x => x.Id));

            migrationBuilder.CreateTable(
                name: "PermissionProfiles",
                schema: "identity",
                columns: table => new
                {
                    Id           = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId    = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted    = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt    = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionProfiles_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "identity",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermissionProfiles_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "identity",
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                schema: "identity",
                columns: table => new
                {
                    Id        = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId    = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "identity",
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // ─── Indexes ─────────────────────────────────────────────────────
            migrationBuilder.CreateIndex("ix_identity_users_email_unique",    "Users", "Email",    schema: "identity", unique: true, filter: "[IsDeleted] = 0");
            migrationBuilder.CreateIndex("ix_identity_users_username_unique",  "Users", "Username", schema: "identity", unique: true, filter: "[IsDeleted] = 0");
            migrationBuilder.CreateIndex("ix_identity_users_locked_until",     "Users", "LockedUntil", schema: "identity", filter: "[LockedUntil] IS NOT NULL");
            migrationBuilder.CreateIndex("ix_identity_refresh_tokens_hash",     "RefreshTokens", "TokenHash", schema: "identity", unique: true);
            migrationBuilder.CreateIndex("ix_identity_refresh_tokens_user_id",  "RefreshTokens", "UserId",    schema: "identity");
            migrationBuilder.CreateIndex("ix_identity_email_confirmation_tokens_hash",    "EmailConfirmationTokens", "TokenHash", schema: "identity", unique: true);
            migrationBuilder.CreateIndex("ix_identity_email_confirmation_tokens_user_id", "EmailConfirmationTokens", "UserId",    schema: "identity");
            migrationBuilder.CreateIndex("ix_identity_password_reset_tokens_hash",    "PasswordResetTokens", "TokenHash", schema: "identity", unique: true);
            migrationBuilder.CreateIndex("ix_identity_password_reset_tokens_user_id", "PasswordResetTokens", "UserId",    schema: "identity");
            migrationBuilder.CreateIndex("ix_identity_group_permissions_name_unique", "GroupPermissions", "Name", schema: "identity", unique: true, filter: "[IsDeleted] = 0");
            migrationBuilder.CreateIndex("ix_identity_permissions_code_unique",       "Permissions",      "Code", schema: "identity", unique: true, filter: "[IsDeleted] = 0");
            migrationBuilder.CreateIndex("ix_identity_profiles_name_unique",          "Profiles",         "Name", schema: "identity", unique: true, filter: "[IsDeleted] = 0");
            migrationBuilder.CreateIndex("ix_identity_permission_profiles_active_unique", "PermissionProfiles", new[] { "PermissionId", "ProfileId" }, schema: "identity", unique: true, filter: "[IsDeleted] = 0");
            migrationBuilder.CreateIndex("ix_identity_permission_profiles_permission_id", "PermissionProfiles", "PermissionId", schema: "identity");
            migrationBuilder.CreateIndex("ix_identity_user_profiles_active_unique", "UserProfiles", new[] { "UserId", "ProfileId" }, schema: "identity", unique: true, filter: "[IsDeleted] = 0");
            migrationBuilder.CreateIndex("ix_identity_user_profiles_user_id",        "UserProfiles", "UserId", schema: "identity");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UserProfiles",             schema: "identity");
            migrationBuilder.DropTable(name: "PermissionProfiles",       schema: "identity");
            migrationBuilder.DropTable(name: "Profiles",                 schema: "identity");
            migrationBuilder.DropTable(name: "Permissions",              schema: "identity");
            migrationBuilder.DropTable(name: "GroupPermissions",         schema: "identity");
            migrationBuilder.DropTable(name: "EmailConfirmationTokens",  schema: "identity");
            migrationBuilder.DropTable(name: "PasswordResetTokens",      schema: "identity");
            migrationBuilder.DropTable(name: "RefreshTokens",            schema: "identity");
            migrationBuilder.DropTable(name: "Users",                    schema: "identity");
        }
    }
}
