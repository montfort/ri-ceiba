using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Ceiba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class US4_AddEmailConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configuracion_email",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proveedor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    habilitado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    smtp_host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    smtp_port = table.Column<int>(type: "integer", nullable: true),
                    smtp_username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    smtp_password = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    smtp_use_ssl = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sendgrid_api_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    from_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    from_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    last_tested_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    last_test_success = table.Column<bool>(type: "boolean", nullable: true),
                    last_test_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracion_email", x => x.id);
                    table.ForeignKey(
                        name: "FK_configuracion_email_USUARIO_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_configuracion_email_created_at",
                table: "configuracion_email",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_configuracion_email_habilitado",
                table: "configuracion_email",
                column: "habilitado");

            migrationBuilder.CreateIndex(
                name: "ix_configuracion_email_proveedor",
                table: "configuracion_email",
                column: "proveedor");

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_email_usuario_id",
                table: "configuracion_email",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuracion_email");
        }
    }
}
