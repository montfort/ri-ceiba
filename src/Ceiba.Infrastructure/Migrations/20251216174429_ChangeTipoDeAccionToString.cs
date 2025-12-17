using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ceiba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTipoDeAccionToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_REPORTE_TIPO_ACCION",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.AlterColumn<string>(
                name: "tipo_de_accion",
                table: "REPORTE_INCIDENCIA",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<short>(
                name: "tipo_de_accion",
                table: "REPORTE_INCIDENCIA",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddCheckConstraint(
                name: "CK_REPORTE_TIPO_ACCION",
                table: "REPORTE_INCIDENCIA",
                sql: "tipo_de_accion IN (1, 2, 3)");
        }
    }
}
