using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Ceiba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracionIA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CONFIGURACION_IA",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Proveedor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ApiKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Modelo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AzureEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AzureApiVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LocalEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MaxTokens = table.Column<int>(type: "integer", nullable: false, defaultValue: 1000),
                    Temperature = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.69999999999999996),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ModificadoPorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONFIGURACION_IA", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CONFIGURACION_IA_ACTIVO",
                table: "CONFIGURACION_IA",
                column: "Activo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CONFIGURACION_IA");
        }
    }
}
