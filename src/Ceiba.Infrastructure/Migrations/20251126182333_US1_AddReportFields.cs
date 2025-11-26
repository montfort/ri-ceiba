using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ceiba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class US1_AddReportFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_REPORTE_INCIDENCIA_CUADRANTE_cuadrante_id",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropForeignKey(
                name: "FK_REPORTE_INCIDENCIA_SECTOR_sector_id",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropForeignKey(
                name: "FK_REPORTE_INCIDENCIA_ZONA_zona_id",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.RenameIndex(
                name: "IX_REPORTE_INCIDENCIA_zona_id",
                table: "REPORTE_INCIDENCIA",
                newName: "idx_reporte_zona");

            migrationBuilder.AddColumn<string>(
                name: "acciones_realizadas",
                table: "REPORTE_INCIDENCIA",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "campos_adicionales",
                table: "REPORTE_INCIDENCIA",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "datetime_hechos",
                table: "REPORTE_INCIDENCIA",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "delito",
                table: "REPORTE_INCIDENCIA",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "discapacidad",
                table: "REPORTE_INCIDENCIA",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "edad",
                table: "REPORTE_INCIDENCIA",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "hechos_reportados",
                table: "REPORTE_INCIDENCIA",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "lgbtttiq_plus",
                table: "REPORTE_INCIDENCIA",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "migrante",
                table: "REPORTE_INCIDENCIA",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "observaciones",
                table: "REPORTE_INCIDENCIA",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "schema_version",
                table: "REPORTE_INCIDENCIA",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "1.0");

            migrationBuilder.AddColumn<string>(
                name: "sexo",
                table: "REPORTE_INCIDENCIA",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "situacion_calle",
                table: "REPORTE_INCIDENCIA",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<short>(
                name: "tipo_de_accion",
                table: "REPORTE_INCIDENCIA",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<string>(
                name: "tipo_de_atencion",
                table: "REPORTE_INCIDENCIA",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<short>(
                name: "traslados",
                table: "REPORTE_INCIDENCIA",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<int>(
                name: "turno_ceiba",
                table: "REPORTE_INCIDENCIA",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "REPORTE_INCIDENCIA",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_reporte_composite_revisor",
                table: "REPORTE_INCIDENCIA",
                columns: new[] { "created_at", "estado", "delito" },
                descending: new[] { true, false, false });

            migrationBuilder.CreateIndex(
                name: "idx_reporte_composite_search",
                table: "REPORTE_INCIDENCIA",
                columns: new[] { "estado", "zona_id", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_reporte_delito",
                table: "REPORTE_INCIDENCIA",
                column: "delito");

            migrationBuilder.CreateIndex(
                name: "idx_reporte_estado",
                table: "REPORTE_INCIDENCIA",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "idx_reporte_fecha",
                table: "REPORTE_INCIDENCIA",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_reporte_usuario",
                table: "REPORTE_INCIDENCIA",
                column: "usuario_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_REPORTE_EDAD",
                table: "REPORTE_INCIDENCIA",
                sql: "edad > 0 AND edad < 150");

            migrationBuilder.AddCheckConstraint(
                name: "CK_REPORTE_ESTADO",
                table: "REPORTE_INCIDENCIA",
                sql: "estado IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_REPORTE_TIPO_ACCION",
                table: "REPORTE_INCIDENCIA",
                sql: "tipo_de_accion IN (1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_REPORTE_TRASLADOS",
                table: "REPORTE_INCIDENCIA",
                sql: "traslados IN (0, 1, 2)");

            migrationBuilder.AddForeignKey(
                name: "FK_REPORTE_CUADRANTE",
                table: "REPORTE_INCIDENCIA",
                column: "cuadrante_id",
                principalTable: "CUADRANTE",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_REPORTE_SECTOR",
                table: "REPORTE_INCIDENCIA",
                column: "sector_id",
                principalTable: "SECTOR",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_REPORTE_ZONA",
                table: "REPORTE_INCIDENCIA",
                column: "zona_id",
                principalTable: "ZONA",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_REPORTE_CUADRANTE",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropForeignKey(
                name: "FK_REPORTE_SECTOR",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropForeignKey(
                name: "FK_REPORTE_ZONA",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropIndex(
                name: "idx_reporte_composite_revisor",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropIndex(
                name: "idx_reporte_composite_search",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropIndex(
                name: "idx_reporte_delito",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropIndex(
                name: "idx_reporte_estado",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropIndex(
                name: "idx_reporte_fecha",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropIndex(
                name: "idx_reporte_usuario",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropCheckConstraint(
                name: "CK_REPORTE_EDAD",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropCheckConstraint(
                name: "CK_REPORTE_ESTADO",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropCheckConstraint(
                name: "CK_REPORTE_TIPO_ACCION",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropCheckConstraint(
                name: "CK_REPORTE_TRASLADOS",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "acciones_realizadas",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "campos_adicionales",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "datetime_hechos",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "delito",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "discapacidad",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "edad",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "hechos_reportados",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "lgbtttiq_plus",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "migrante",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "observaciones",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "schema_version",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "sexo",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "situacion_calle",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "tipo_de_accion",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "tipo_de_atencion",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "traslados",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "turno_ceiba",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.RenameIndex(
                name: "idx_reporte_zona",
                table: "REPORTE_INCIDENCIA",
                newName: "IX_REPORTE_INCIDENCIA_zona_id");

            migrationBuilder.AddForeignKey(
                name: "FK_REPORTE_INCIDENCIA_CUADRANTE_cuadrante_id",
                table: "REPORTE_INCIDENCIA",
                column: "cuadrante_id",
                principalTable: "CUADRANTE",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_REPORTE_INCIDENCIA_SECTOR_sector_id",
                table: "REPORTE_INCIDENCIA",
                column: "sector_id",
                principalTable: "SECTOR",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_REPORTE_INCIDENCIA_ZONA_zona_id",
                table: "REPORTE_INCIDENCIA",
                column: "zona_id",
                principalTable: "ZONA",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
