using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ceiba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTurnoCeibaAndTrasladosToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_REPORTE_TRASLADOS",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.AlterColumn<string>(
                name: "turno_ceiba",
                table: "REPORTE_INCIDENCIA",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "traslados",
                table: "REPORTE_INCIDENCIA",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "turno_ceiba",
                table: "REPORTE_INCIDENCIA",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<short>(
                name: "traslados",
                table: "REPORTE_INCIDENCIA",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddCheckConstraint(
                name: "CK_REPORTE_TRASLADOS",
                table: "REPORTE_INCIDENCIA",
                sql: "traslados IN (0, 1, 2)");
        }
    }
}
