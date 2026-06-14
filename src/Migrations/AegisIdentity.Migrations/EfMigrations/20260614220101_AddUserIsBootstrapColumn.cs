using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisIdentity.Migrations.EfMigrations
{
    /// <inheritdoc />
    public partial class AddUserIsBootstrapColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBootstrap",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: SeedInitialAdminUser.AdminUserId,
                column: "IsBootstrap",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBootstrap",
                table: "Users");
        }
    }
}
