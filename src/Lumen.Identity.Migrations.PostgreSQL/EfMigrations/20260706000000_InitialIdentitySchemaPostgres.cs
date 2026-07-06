using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumen.Identity.Migrations.PostgreSQL.EfMigrations
{
    /// <inheritdoc />
    public partial class InitialIdentitySchemaPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "identity");

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    id                   = table.Column<Guid>(type: "uuid", nullable: false),
                    email                = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    username             = table.Column<string>(type: "character varying(64)",  maxLength: 64,  nullable: false),
                    password_hash        = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_bootstrap         = table.Column<bool>(type: "boolean", nullable: false),
                    is_active            = table.Column<bool>(type: "boolean", nullable: false),
                    email_confirmed_at   = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_at        = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
                    locked_until         = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at           = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at           = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted           = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at           = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => table.PrimaryKey("pk_users", x => x.id));

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "identity",
                columns: table => new
                {
                    id                    = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id               = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash            = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_by_ip         = table.Column<string>(type: "character varying(64)",  maxLength: 64,  nullable: false),
                    replaced_by_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at            = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at            = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at            = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted            = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at            = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                schema: "identity",
                columns: table => new
                {
                    id         = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id    = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at    = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_reset_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_password_reset_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "email_confirmation_tokens",
                schema: "identity",
                columns: table => new
                {
                    id         = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id    = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at    = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_confirmation_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_confirmation_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Indexes
            migrationBuilder.CreateIndex(
                name: "ix_identity_users_email_unique",
                table: "users",
                column: "email",
                schema: "identity",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_identity_users_username_unique",
                table: "users",
                column: "username",
                schema: "identity",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_identity_users_locked_until",
                table: "users",
                column: "locked_until",
                schema: "identity",
                filter: "locked_until IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_identity_refresh_tokens_hash",
                table: "refresh_tokens",
                column: "token_hash",
                schema: "identity",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_identity_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id",
                schema: "identity");

            migrationBuilder.CreateIndex(
                name: "ix_identity_email_confirmation_tokens_hash",
                table: "email_confirmation_tokens",
                column: "token_hash",
                schema: "identity",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_identity_email_confirmation_tokens_user_id",
                table: "email_confirmation_tokens",
                column: "user_id",
                schema: "identity");

            migrationBuilder.CreateIndex(
                name: "ix_identity_password_reset_tokens_hash",
                table: "password_reset_tokens",
                column: "token_hash",
                schema: "identity",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_identity_password_reset_tokens_user_id",
                table: "password_reset_tokens",
                column: "user_id",
                schema: "identity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "email_confirmation_tokens", schema: "identity");
            migrationBuilder.DropTable(name: "password_reset_tokens",     schema: "identity");
            migrationBuilder.DropTable(name: "refresh_tokens",            schema: "identity");
            migrationBuilder.DropTable(name: "users",                     schema: "identity");
        }
    }
}
