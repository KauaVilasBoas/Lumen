using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumen.Authorization.Migrations.PostgreSQL.EfMigrations
{
    /// <inheritdoc />
    public partial class AddScopeIdToUserProfilePostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop the old unique index on (UserId, ProfileId).
            migrationBuilder.DropIndex(
                name: "ix_lumen_user_profile_active_unique",
                schema: "Lumen",
                table: "UserProfile");

            // 2. Add the nullable scope_id column (PostgreSQL snake_case convention).
            //    NULL means "global assignment" — existing rows are implicitly global.
            migrationBuilder.AddColumn<Guid>(
                name: "ScopeId",
                schema: "Lumen",
                table: "UserProfile",
                type: "uuid",
                nullable: true);

            // 3. Recreate the unique filtered index covering (UserId, ProfileId, ScopeId)
            //    for non-deleted rows. PostgreSQL handles NULL in unique indexes by treating
            //    each NULL as distinct, which is the correct behavior for optional scope.
            migrationBuilder.CreateIndex(
                name: "ix_lumen_user_profile_active_unique",
                schema: "Lumen",
                table: "UserProfile",
                columns: new[] { "UserId", "ProfileId", "ScopeId" },
                unique: true,
                filter: "is_deleted = false");
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
                filter: "is_deleted = false");
        }
    }
}
