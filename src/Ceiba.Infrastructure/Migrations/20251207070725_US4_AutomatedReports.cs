using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Ceiba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class US4_AutomatedReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MODELO_REPORTE",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    contenido_markdown = table.Column<string>(type: "text", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    es_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MODELO_REPORTE", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "REPORTE_AUTOMATIZADO",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fecha_inicio = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    contenido_markdown = table.Column<string>(type: "text", nullable: false),
                    contenido_word_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estadisticas = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    enviado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    fecha_envio = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    error_mensaje = table.Column<string>(type: "text", nullable: true),
                    modelo_reporte_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REPORTE_AUTOMATIZADO", x => x.id);
                    table.ForeignKey(
                        name: "modelo_reporte_id_FK",
                        column: x => x.modelo_reporte_id,
                        principalTable: "MODELO_REPORTE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_modelo_activo",
                table: "MODELO_REPORTE",
                column: "activo");

            migrationBuilder.CreateIndex(
                name: "idx_modelo_default",
                table: "MODELO_REPORTE",
                column: "es_default");

            migrationBuilder.CreateIndex(
                name: "idx_modelo_nombre_unique",
                table: "MODELO_REPORTE",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_reporte_auto_created",
                table: "REPORTE_AUTOMATIZADO",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_reporte_auto_enviado",
                table: "REPORTE_AUTOMATIZADO",
                column: "enviado");

            migrationBuilder.CreateIndex(
                name: "idx_reporte_auto_fecha_inicio",
                table: "REPORTE_AUTOMATIZADO",
                column: "fecha_inicio");

            migrationBuilder.CreateIndex(
                name: "IX_REPORTE_AUTOMATIZADO_modelo_reporte_id",
                table: "REPORTE_AUTOMATIZADO",
                column: "modelo_reporte_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "REPORTE_AUTOMATIZADO");

            migrationBuilder.DropTable(
                name: "MODELO_REPORTE");
        }
    }
}
