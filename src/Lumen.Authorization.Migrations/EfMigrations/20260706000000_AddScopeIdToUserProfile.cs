using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumen.Authorization.Migrations.EfMigrations
{
    /// <inheritdoc />
    public partial class AddScopeIdToUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop the old unique index on (UserId, ProfileId) — it will be replaced by
            //    the new (UserId, ProfileId, ScopeId) index that covers both global and scoped
            //    assignments.
            migrationBuilder.DropIndex(
                name: "ix_lumen_user_profile_active_unique",
                schema: "Lumen",
                table: "UserProfile");

            // 2. Add the nullable ScopeId column.
            //    NULL means "global assignment" — existing rows are implicitly global.
            migrationBuilder.AddColumn<Guid>(
                name: "ScopeId",
                schema: "Lumen",
                table: "UserProfile",
                type: "uniqueidentifier",
                nullable: true);

            // 3. Recreate the unique filtered index to cover the three-part key
            //    (UserId, ProfileId, ScopeId) for non-deleted rows.
            //    NULL values in ScopeId are treated as distinct from each other by SQL Server
            //    filtered indexes, but a filtered WHERE clause on IsDeleted = 0 ensures
            //    soft-deleted rows are excluded from uniqueness enforcement.
            migrationBuilder.CreateIndex(
                name: "ix_lumen_user_profile_active_unique",
                schema: "Lumen",
                table: "UserProfile",
                columns: new[] { "UserId", "ProfileId", "ScopeId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_lumen_user_profile_active_unique",
                schema: "Lumen",
                table: "UserProfile");

            migrationBuilder.DropColumn(
                name: "ScopeId",
                schema: "Lumen",
                table: "UserProfile");

            migrationBuilder.CreateIndex(
                name: "ix_lumen_user_profile_active_unique",
                schema: "Lumen",
                table: "UserProfile",
                columns: new[] { "UserId", "ProfileId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
