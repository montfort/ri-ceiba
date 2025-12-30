using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ceiba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioCustomProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "USUARIO",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_login_at",
                table: "USUARIO",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nombre",
                table: "USUARIO",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Copy email to nombre for existing users (development data only)
            migrationBuilder.Sql(
                """
                UPDATE "USUARIO"
                SET nombre = COALESCE("Email", '')
                WHERE nombre IS NULL OR nombre = ''
                """);

            migrationBuilder.CreateIndex(
                name: "idx_usuario_created_at",
                table: "USUARIO",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_usuario_last_login_at",
                table: "USUARIO",
                column: "last_login_at");

            migrationBuilder.AddForeignKey(
                name: "usuario_reportes_FK",
                table: "REPORTE_INCIDENCIA",
                column: "usuario_id",
                principalTable: "USUARIO",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "usuario_reportes_FK",
                table: "REPORTE_INCIDENCIA");

            migrationBuilder.DropIndex(
                name: "idx_usuario_created_at",
                table: "USUARIO");

            migrationBuilder.DropIndex(
                name: "idx_usuario_last_login_at",
                table: "USUARIO");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "USUARIO");

            migrationBuilder.DropColumn(
                name: "last_login_at",
                table: "USUARIO");

            migrationBuilder.DropColumn(
                name: "nombre",
                table: "USUARIO");
        }
    }
}
