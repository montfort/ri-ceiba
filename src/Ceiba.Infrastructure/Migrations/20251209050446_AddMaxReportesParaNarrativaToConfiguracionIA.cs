using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ceiba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxReportesParaNarrativaToConfiguracionIA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxReportesParaNarrativa",
                table: "CONFIGURACION_IA",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxReportesParaNarrativa",
                table: "CONFIGURACION_IA");
        }
    }
}
