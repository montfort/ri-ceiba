using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Ceiba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class US4_AddAutomatedReportConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CONFIGURACION_REPORTES_AUTOMATIZADOS",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    habilitado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    hora_generacion = table.Column<TimeSpan>(type: "interval", nullable: false, defaultValue: new TimeSpan(0, 6, 0, 0, 0)),
                    destinatarios = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    ruta_salida = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: "./generated-reports"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONFIGURACION_REPORTES_AUTOMATIZADOS", x => x.id);
                    table.ForeignKey(
                        name: "FK_CONFIGURACION_REPORTES_AUTOMATIZADOS_USUARIO_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_config_reportes_habilitado",
                table: "CONFIGURACION_REPORTES_AUTOMATIZADOS",
                column: "habilitado");

            migrationBuilder.CreateIndex(
                name: "IX_CONFIGURACION_REPORTES_AUTOMATIZADOS_usuario_id",
                table: "CONFIGURACION_REPORTES_AUTOMATIZADOS",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CONFIGURACION_REPORTES_AUTOMATIZADOS");
        }
    }
}
