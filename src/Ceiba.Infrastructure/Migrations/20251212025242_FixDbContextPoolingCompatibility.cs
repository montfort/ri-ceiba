using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ceiba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDbContextPoolingCompatibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_REPORTE_INCIDENCIA_cuadrante_id",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropIndex(
                name: "IX_REPORTE_INCIDENCIA_sector_id",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.CreateIndex(
                name: "idx_reporte_cuadrante_fecha",
                table: "REPORTE_INCIDENCIA",
                columns: new[] { "cuadrante_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_reporte_datetime_hechos",
                table: "REPORTE_INCIDENCIA",
                column: "datetime_hechos");

            migrationBuilder.CreateIndex(
                name: "idx_reporte_sector_fecha",
                table: "REPORTE_INCIDENCIA",
                columns: new[] { "sector_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_reporte_usuario_estado",
                table: "REPORTE_INCIDENCIA",
                columns: new[] { "usuario_id", "estado", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_auditoria_codigo_fecha",
                table: "AUDITORIA",
                columns: new[] { "codigo", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_auditoria_fecha_usuario",
                table: "AUDITORIA",
                columns: new[] { "created_at", "usuario_id" },
                descending: new[] { true, false });

            migrationBuilder.CreateIndex(
                name: "idx_auditoria_ip",
                table: "AUDITORIA",
                column: "ip");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_reporte_cuadrante_fecha",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropIndex(
                name: "idx_reporte_datetime_hechos",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropIndex(
                name: "idx_reporte_sector_fecha",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropIndex(
                name: "idx_reporte_usuario_estado",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropIndex(
                name: "idx_auditoria_codigo_fecha",
                table: "AUDITORIA");

            migrationBuilder.DropIndex(
                name: "idx_auditoria_fecha_usuario",
                table: "AUDITORIA");

            migrationBuilder.DropIndex(
                name: "idx_auditoria_ip",
                table: "AUDITORIA");

            migrationBuilder.CreateIndex(
                name: "IX_REPORTE_INCIDENCIA_cuadrante_id",
                table: "REPORTE_INCIDENCIA",
                column: "cuadrante_id");

            migrationBuilder.CreateIndex(
                name: "IX_REPORTE_INCIDENCIA_sector_id",
                table: "REPORTE_INCIDENCIA",
                column: "sector_id");
        }
    }
}
