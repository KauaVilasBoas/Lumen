using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumen.Modules.Identity.Migrations.EfMigrations
{
    public partial class SeedIdentityBootstrapData : Migration
    {
        // ─── Well-known GUIDs (match dbo.* seeds for FK-free cross-schema coexistence) ─
        private static readonly Guid AdminUserId          = new("10000000-0000-0000-0000-000000000001");
        private static readonly Guid AdministratorProfileId = new("20000000-0000-0000-0000-000000000001");
        private static readonly Guid UserProfileId          = new("20000000-0000-0000-0000-000000000002");
        private static readonly Guid AdminUserProfileId     = new("30000000-0000-0000-0000-000000000001");

        // ─── Group GUIDs ────────────────────────────────────────────────────
        private static readonly Guid GroupAuthorizationId  = new("40000000-0000-0000-0000-000000000001");
        private static readonly Guid GroupUsersId          = new("40000000-0000-0000-0000-000000000010");
        private static readonly Guid GroupAuditId          = new("40000000-0000-0000-0000-000000000020");
        private static readonly Guid GroupDiagnosticsId    = new("40000000-0000-0000-0000-000000000030");
        private static readonly Guid GroupProfilesId       = new("40000000-0000-0000-0000-000000000040");
        private static readonly Guid GroupPermissionsId    = new("40000000-0000-0000-0000-000000000050");
        private static readonly Guid GroupUserProfilesId   = new("40000000-0000-0000-0000-000000000060");

        // ─── Permission GUIDs ───────────────────────────────────────────────
        private static readonly Guid PermAuthGraphView       = new("40000000-0000-0000-0000-000000000002");
        private static readonly Guid PermUsersList           = new("40000000-0000-0000-0000-000000000011");
        private static readonly Guid PermUsersGet            = new("40000000-0000-0000-0000-000000000012");
        private static readonly Guid PermUsersUpdate         = new("40000000-0000-0000-0000-000000000013");
        private static readonly Guid PermUsersDelete         = new("40000000-0000-0000-0000-000000000025");
        private static readonly Guid PermUsersRestore        = new("40000000-0000-0000-0000-000000000026");
        private static readonly Guid PermAuditRead           = new("40000000-0000-0000-0000-000000000021");
        private static readonly Guid PermDiagCacheStats      = new("40000000-0000-0000-0000-000000000031");
        private static readonly Guid PermDiagJobStats        = new("40000000-0000-0000-0000-000000000032");
        private static readonly Guid PermProfilesList        = new("40000000-0000-0000-0000-000000000041");
        private static readonly Guid PermProfilesGet         = new("40000000-0000-0000-0000-000000000042");
        private static readonly Guid PermProfilesCreate      = new("40000000-0000-0000-0000-000000000043");
        private static readonly Guid PermProfilesUpdate      = new("40000000-0000-0000-0000-000000000044");
        private static readonly Guid PermProfilesDelete      = new("40000000-0000-0000-0000-000000000045");
        private static readonly Guid PermProfilesSetPerms    = new("40000000-0000-0000-0000-000000000046");
        private static readonly Guid PermPermissionsList     = new("40000000-0000-0000-0000-000000000051");
        private static readonly Guid PermUserProfilesList    = new("40000000-0000-0000-0000-000000000061");
        private static readonly Guid PermUserProfilesAssign  = new("40000000-0000-0000-0000-000000000062");
        private static readonly Guid PermUserProfilesRemove  = new("40000000-0000-0000-0000-000000000063");

        // ─── PermissionProfile assignment GUIDs ─────────────────────────────
        private static readonly Guid PpAdminAuthGraphView      = new("50000000-0000-0000-0000-000000000001");
        private static readonly Guid PpAdminUsersList          = new("50000000-0000-0000-0000-000000000002");
        private static readonly Guid PpAdminUsersGet           = new("50000000-0000-0000-0000-000000000003");
        private static readonly Guid PpAdminUsersUpdate        = new("50000000-0000-0000-0000-000000000004");
        private static readonly Guid PpAdminUsersDelete        = new("50000000-0000-0000-0000-000000000005");
        private static readonly Guid PpAdminUsersRestore       = new("50000000-0000-0000-0000-000000000006");
        private static readonly Guid PpAdminAuditRead          = new("50000000-0000-0000-0000-000000000007");
        private static readonly Guid PpAdminDiagCache          = new("50000000-0000-0000-0000-000000000008");
        private static readonly Guid PpAdminDiagJob            = new("50000000-0000-0000-0000-000000000009");
        private static readonly Guid PpAdminProfilesList       = new("50000000-0000-0000-0000-000000000010");
        private static readonly Guid PpAdminProfilesGet        = new("50000000-0000-0000-0000-000000000011");
        private static readonly Guid PpAdminProfilesCreate     = new("50000000-0000-0000-0000-000000000012");
        private static readonly Guid PpAdminProfilesUpdate     = new("50000000-0000-0000-0000-000000000013");
        private static readonly Guid PpAdminProfilesDelete     = new("50000000-0000-0000-0000-000000000014");
        private static readonly Guid PpAdminProfilesSetPerms   = new("50000000-0000-0000-0000-000000000015");
        private static readonly Guid PpAdminPermsList          = new("50000000-0000-0000-0000-000000000016");
        private static readonly Guid PpAdminUserProfilesList   = new("50000000-0000-0000-0000-000000000017");
        private static readonly Guid PpAdminUserProfilesAssign = new("50000000-0000-0000-0000-000000000018");
        private static readonly Guid PpAdminUserProfilesRemove = new("50000000-0000-0000-0000-000000000019");

        private static readonly DateTime SeedDate = new(2026, 6, 28, 0, 0, 0, DateTimeKind.Utc);

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SeedGroups(migrationBuilder);
            SeedPermissions(migrationBuilder);
            SeedProfiles(migrationBuilder);
            SeedAdminUser(migrationBuilder);
            SeedAdminUserProfile(migrationBuilder);
            SeedAdminPermissionProfiles(migrationBuilder);
        }

        private static void SeedGroups(MigrationBuilder migrationBuilder)
        {
            var groups = new[]
            {
                (GroupAuthorizationId, "Authorization", "Authorization graph and access control"),
                (GroupUsersId,         "Users",         "User management operations"),
                (GroupAuditId,         "Audit",         "Audit log access"),
                (GroupDiagnosticsId,   "Diagnostics",   "System diagnostics and monitoring"),
                (GroupProfilesId,      "Profiles",      "Profile management"),
                (GroupPermissionsId,   "Permissions",   "Permission listing"),
                (GroupUserProfilesId,  "UserProfiles",  "User-to-profile assignment management"),
            };

            foreach (var (id, name, desc) in groups)
            {
                migrationBuilder.InsertData(
                    schema: "identity",
                    table: "GroupPermissions",
                    columns: new[] { "Id", "Name", "Description", "IsDeleted", "DeletedAt" },
                    values: new object[] { id, name, desc, false, null! });
            }
        }

        private static void SeedPermissions(MigrationBuilder migrationBuilder)
        {
            var permissions = new[]
            {
                (PermAuthGraphView,    GroupAuthorizationId, "AuthorizationGraph", "View",          "AuthorizationGraph — View"),
                (PermUsersList,        GroupUsersId,         "Users",              "List",          "Users — List"),
                (PermUsersGet,         GroupUsersId,         "Users",              "Get",           "Users — Get"),
                (PermUsersUpdate,      GroupUsersId,         "Users",              "Update",        "Users — Update"),
                (PermUsersDelete,      GroupUsersId,         "Users",              "Delete",        "Users — Delete"),
                (PermUsersRestore,     GroupUsersId,         "Users",              "Restore",       "Users — Restore"),
                (PermAuditRead,        GroupAuditId,         "Audit",              "Read",          "Audit — Read"),
                (PermDiagCacheStats,   GroupDiagnosticsId,   "Diagnostics",        "GetCacheStats", "Diagnostics — GetCacheStats"),
                (PermDiagJobStats,     GroupDiagnosticsId,   "Diagnostics",        "GetJobStats",   "Diagnostics — GetJobStats"),
                (PermProfilesList,     GroupProfilesId,      "Profiles",           "List",          "Profiles — List"),
                (PermProfilesGet,      GroupProfilesId,      "Profiles",           "Get",           "Profiles — Get"),
                (PermProfilesCreate,   GroupProfilesId,      "Profiles",           "Create",        "Profiles — Create"),
                (PermProfilesUpdate,   GroupProfilesId,      "Profiles",           "Update",        "Profiles — Update"),
                (PermProfilesDelete,   GroupProfilesId,      "Profiles",           "Delete",        "Profiles — Delete"),
                (PermProfilesSetPerms, GroupProfilesId,      "Profiles",           "SetPermissions","Profiles — SetPermissions"),
                (PermPermissionsList,  GroupPermissionsId,   "Permissions",        "List",          "Permissions — List"),
                (PermUserProfilesList,   GroupUserProfilesId, "UserProfiles",      "List",          "UserProfiles — List"),
                (PermUserProfilesAssign, GroupUserProfilesId, "UserProfiles",      "Assign",        "UserProfiles — Assign"),
                (PermUserProfilesRemove, GroupUserProfilesId, "UserProfiles",      "Remove",        "UserProfiles — Remove"),
            };

            foreach (var (id, groupId, controller, action, displayName) in permissions)
            {
                migrationBuilder.InsertData(
                    schema: "identity",
                    table: "Permissions",
                    columns: new[] { "Id", "Code", "Controller", "Action", "DisplayName", "GroupPermissionId", "IsOrphan", "OrphanedAt", "IsDeleted", "DeletedAt" },
                    values: new object[] { id, $"{controller}.{action}", controller, action, displayName, groupId, false, null!, false, null! });
            }
        }

        private static void SeedProfiles(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "identity",
                table: "Profiles",
                columns: new[] { "Id", "Name", "Description", "IsSystem", "IsDeleted", "DeletedAt" },
                values: new object[] { AdministratorProfileId, "Administrator", "System profile with full access to all permissions.", true, false, null! });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "Profiles",
                columns: new[] { "Id", "Name", "Description", "IsSystem", "IsDeleted", "DeletedAt" },
                values: new object[] { UserProfileId, "User", "Base system profile for regular users. Permissions are granted explicitly.", true, false, null! });
        }

        private static void SeedAdminUser(MigrationBuilder migrationBuilder)
        {
            // BCrypt hash of "Admin@123!" at cost 12 — placeholder. Must be changed on first boot.
            const string bootstrapPasswordHash = "$2a$12$bootstrap.placeholder.hash.that.must.be.changed.before.use";

            migrationBuilder.InsertData(
                schema: "identity",
                table: "Users",
                columns: new[] { "Id", "Email", "Username", "PasswordHash", "IsBootstrap", "IsActive", "EmailConfirmedAt", "LastLoginAt", "FailedLoginAttempts", "LockedUntil", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt" },
                values: new object[] { AdminUserId, "admin@lumen.local", "admin", bootstrapPasswordHash, true, true, SeedDate, null!, 0, null!, SeedDate, SeedDate, false, null! });
        }

        private static void SeedAdminUserProfile(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "identity",
                table: "UserProfiles",
                columns: new[] { "Id", "UserId", "ProfileId", "IsDeleted", "DeletedAt" },
                values: new object[] { AdminUserProfileId, AdminUserId, AdministratorProfileId, false, null! });
        }

        private static void SeedAdminPermissionProfiles(MigrationBuilder migrationBuilder)
        {
            var assignments = new[]
            {
                (PpAdminAuthGraphView,      PermAuthGraphView),
                (PpAdminUsersList,          PermUsersList),
                (PpAdminUsersGet,           PermUsersGet),
                (PpAdminUsersUpdate,        PermUsersUpdate),
                (PpAdminUsersDelete,        PermUsersDelete),
                (PpAdminUsersRestore,       PermUsersRestore),
                (PpAdminAuditRead,          PermAuditRead),
                (PpAdminDiagCache,          PermDiagCacheStats),
                (PpAdminDiagJob,            PermDiagJobStats),
                (PpAdminProfilesList,       PermProfilesList),
                (PpAdminProfilesGet,        PermProfilesGet),
                (PpAdminProfilesCreate,     PermProfilesCreate),
                (PpAdminProfilesUpdate,     PermProfilesUpdate),
                (PpAdminProfilesDelete,     PermProfilesDelete),
                (PpAdminProfilesSetPerms,   PermProfilesSetPerms),
                (PpAdminPermsList,          PermPermissionsList),
                (PpAdminUserProfilesList,   PermUserProfilesList),
                (PpAdminUserProfilesAssign, PermUserProfilesAssign),
                (PpAdminUserProfilesRemove, PermUserProfilesRemove),
            };

            foreach (var (id, permId) in assignments)
            {
                migrationBuilder.InsertData(
                    schema: "identity",
                    table: "PermissionProfiles",
                    columns: new[] { "Id", "PermissionId", "ProfileId", "IsDeleted", "DeletedAt" },
                    values: new object[] { id, permId, AdministratorProfileId, false, null! });
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [identity].[PermissionProfiles] WHERE [ProfileId] = '20000000-0000-0000-0000-000000000001';");
            migrationBuilder.Sql("DELETE FROM [identity].[UserProfiles] WHERE [Id] = '30000000-0000-0000-0000-000000000001';");
            migrationBuilder.Sql("DELETE FROM [identity].[Users] WHERE [Id] = '10000000-0000-0000-0000-000000000001';");
            migrationBuilder.Sql("DELETE FROM [identity].[Profiles] WHERE [Id] IN ('20000000-0000-0000-0000-000000000001','20000000-0000-0000-0000-000000000002');");
            migrationBuilder.Sql("DELETE FROM [identity].[Permissions];");
            migrationBuilder.Sql("DELETE FROM [identity].[GroupPermissions];");
        }
    }
}
