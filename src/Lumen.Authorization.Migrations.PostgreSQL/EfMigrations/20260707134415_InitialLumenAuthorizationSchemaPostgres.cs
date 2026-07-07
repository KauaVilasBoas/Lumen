using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumen.Authorization.Migrations.PostgreSQL.EfMigrations
{
    /// <inheritdoc />
    public partial class InitialLumenAuthorizationSchemaPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Lumen");

            migrationBuilder.CreateTable(
                name: "PermissionGroup",
                schema: "Lumen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profile",
                schema: "Lumen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                schema: "Lumen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Controller = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    GroupPermissionId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsOrphan = table.Column<bool>(type: "boolean", nullable: false),
                    OrphanedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permission_PermissionGroup_GroupPermissionId",
                        column: x => x.GroupPermissionId,
                        principalSchema: "Lumen",
                        principalTable: "PermissionGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserProfile",
                schema: "Lumen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfile_Profile_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "Lumen",
                        principalTable: "Profile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PermissionProfile",
                schema: "Lumen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionProfile_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "Lumen",
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermissionProfile_Profile_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "Lumen",
                        principalTable: "Profile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_lumen_permission_code_unique",
                schema: "Lumen",
                table: "Permission",
                column: "Code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_GroupPermissionId",
                schema: "Lumen",
                table: "Permission",
                column: "GroupPermissionId");

            migrationBuilder.CreateIndex(
                name: "ix_lumen_permission_group_name_unique",
                schema: "Lumen",
                table: "PermissionGroup",
                column: "Name",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_lumen_permission_profile_active_unique",
                schema: "Lumen",
                table: "PermissionProfile",
                columns: new[] { "PermissionId", "ProfileId" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_lumen_permission_profile_permission_id",
                schema: "Lumen",
                table: "PermissionProfile",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionProfile_ProfileId",
                schema: "Lumen",
                table: "PermissionProfile",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "ix_lumen_profile_name_unique",
                schema: "Lumen",
                table: "Profile",
                column: "Name",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_lumen_user_profile_active_unique",
                schema: "Lumen",
                table: "UserProfile",
                columns: new[] { "UserId", "ProfileId", "ScopeId" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_lumen_user_profile_user_id",
                schema: "Lumen",
                table: "UserProfile",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_ProfileId",
                schema: "Lumen",
                table: "UserProfile",
                column: "ProfileId");

            // Seed system profiles required by the authorization module on first boot.
            // Using fixed, well-known GUIDs so they can be referenced by downstream seed data.
            migrationBuilder.InsertData(
                schema: "Lumen",
                table: "Profile",
                columns: new[] { "Id", "Name", "Description", "IsSystem", "IsDeleted", "DeletedAt" },
                values: new object[]
                {
                    new Guid("20000000-0000-0000-0000-000000000001"),
                    "Administrator",
                    "System profile with full access to all permissions.",
                    true, false, null
                });

            migrationBuilder.InsertData(
                schema: "Lumen",
                table: "Profile",
                columns: new[] { "Id", "Name", "Description", "IsSystem", "IsDeleted", "DeletedAt" },
                values: new object[]
                {
                    new Guid("20000000-0000-0000-0000-000000000002"),
                    "User",
                    "Base system profile for regular users. Permissions are granted explicitly.",
                    true, false, null
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove seeded system profiles before dropping tables
            migrationBuilder.Sql(
                "DELETE FROM \"Lumen\".\"Profile\" WHERE \"Id\" IN " +
                "('20000000-0000-0000-0000-000000000001','20000000-0000-0000-0000-000000000002');");

            migrationBuilder.DropTable(name: "PermissionProfile", schema: "Lumen");
            migrationBuilder.DropTable(name: "UserProfile",        schema: "Lumen");
            migrationBuilder.DropTable(name: "Permission",         schema: "Lumen");
            migrationBuilder.DropTable(name: "Profile",            schema: "Lumen");
            migrationBuilder.DropTable(name: "PermissionGroup",    schema: "Lumen");
        }
    }
}
