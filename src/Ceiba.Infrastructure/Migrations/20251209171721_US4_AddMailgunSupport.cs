using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ceiba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class US4_AddMailgunSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mailgun_api_key",
                table: "configuracion_email",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mailgun_domain",
                table: "configuracion_email",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mailgun_region",
                table: "configuracion_email",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mailgun_api_key",
                table: "configuracion_email");

            migrationBuilder.DropColumn(
                name: "mailgun_domain",
                table: "configuracion_email");

            migrationBuilder.DropColumn(
                name: "mailgun_region",
                table: "configuracion_email");
        }
    }
}
