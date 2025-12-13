using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Ceiba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AUDITORIA",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    id_relacionado = table.Column<int>(type: "integer", nullable: true),
                    tabla_relacionada = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    detalles = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AUDITORIA", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CATALOGO_SUGERENCIA",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    campo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    valor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CATALOGO_SUGERENCIA", x => x.id);
                });

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
                    MaxReportesParaNarrativa = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ModificadoPorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONFIGURACION_IA", x => x.Id);
                });

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
                name: "ROL",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROL", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "USUARIO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZONA",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZONA", x => x.id);
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

            migrationBuilder.CreateTable(
                name: "ROL_CLAIM",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROL_CLAIM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ROL_CLAIM_ROL_RoleId",
                        column: x => x.RoleId,
                        principalTable: "ROL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "configuracion_email",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proveedor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    habilitado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    smtp_host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    smtp_port = table.Column<int>(type: "integer", nullable: true),
                    smtp_username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    smtp_password = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    smtp_use_ssl = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sendgrid_api_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    mailgun_api_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    mailgun_domain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    mailgun_region = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    from_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    from_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    last_tested_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    last_test_success = table.Column<bool>(type: "boolean", nullable: true),
                    last_test_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracion_email", x => x.id);
                    table.ForeignKey(
                        name: "FK_configuracion_email_USUARIO_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "USUARIO_CLAIM",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO_CLAIM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_USUARIO_CLAIM_USUARIO_UserId",
                        column: x => x.UserId,
                        principalTable: "USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USUARIO_LOGIN",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO_LOGIN", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_USUARIO_LOGIN_USUARIO_UserId",
                        column: x => x.UserId,
                        principalTable: "USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USUARIO_ROL",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO_ROL", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_USUARIO_ROL_ROL_RoleId",
                        column: x => x.RoleId,
                        principalTable: "ROL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_USUARIO_ROL_USUARIO_UserId",
                        column: x => x.UserId,
                        principalTable: "USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USUARIO_TOKEN",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO_TOKEN", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_USUARIO_TOKEN_USUARIO_UserId",
                        column: x => x.UserId,
                        principalTable: "USUARIO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "REGION",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    zona_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REGION", x => x.id);
                    table.ForeignKey(
                        name: "zona_id_FK",
                        column: x => x.zona_id,
                        principalTable: "ZONA",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SECTOR",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    region_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SECTOR", x => x.id);
                    table.ForeignKey(
                        name: "sector_region_id_FK",
                        column: x => x.region_id,
                        principalTable: "REGION",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CUADRANTE",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sector_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CUADRANTE", x => x.id);
                    table.ForeignKey(
                        name: "sector_id_FK",
                        column: x => x.sector_id,
                        principalTable: "SECTOR",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "REPORTE_INCIDENCIA",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipo_reporte = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "A"),
                    estado = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    datetime_hechos = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    sexo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    edad = table.Column<int>(type: "integer", nullable: false),
                    lgbtttiq_plus = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    situacion_calle = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    migrante = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    discapacidad = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    delito = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    zona_id = table.Column<int>(type: "integer", nullable: false),
                    region_id = table.Column<int>(type: "integer", nullable: false),
                    sector_id = table.Column<int>(type: "integer", nullable: false),
                    cuadrante_id = table.Column<int>(type: "integer", nullable: false),
                    turno_ceiba = table.Column<int>(type: "integer", nullable: false),
                    tipo_de_atencion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo_de_accion = table.Column<short>(type: "smallint", nullable: false),
                    hechos_reportados = table.Column<string>(type: "text", nullable: false),
                    acciones_realizadas = table.Column<string>(type: "text", nullable: false),
                    traslados = table.Column<short>(type: "smallint", nullable: false),
                    observaciones = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    campos_adicionales = table.Column<string>(type: "jsonb", nullable: true),
                    schema_version = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "1.0"),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REPORTE_INCIDENCIA", x => x.id);
                    table.CheckConstraint("CK_REPORTE_EDAD", "edad > 0 AND edad < 150");
                    table.CheckConstraint("CK_REPORTE_ESTADO", "estado IN (0, 1)");
                    table.CheckConstraint("CK_REPORTE_TIPO_ACCION", "tipo_de_accion IN (1, 2, 3)");
                    table.CheckConstraint("CK_REPORTE_TRASLADOS", "traslados IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "FK_REPORTE_CUADRANTE",
                        column: x => x.cuadrante_id,
                        principalTable: "CUADRANTE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_REPORTE_REGION",
                        column: x => x.region_id,
                        principalTable: "REGION",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_REPORTE_SECTOR",
                        column: x => x.sector_id,
                        principalTable: "SECTOR",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_REPORTE_ZONA",
                        column: x => x.zona_id,
                        principalTable: "ZONA",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_auditoria_codigo",
                table: "AUDITORIA",
                column: "codigo");

            migrationBuilder.CreateIndex(
                name: "idx_auditoria_codigo_fecha",
                table: "AUDITORIA",
                columns: new[] { "codigo", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_auditoria_entidad",
                table: "AUDITORIA",
                columns: new[] { "tabla_relacionada", "id_relacionado" });

            migrationBuilder.CreateIndex(
                name: "idx_auditoria_fecha",
                table: "AUDITORIA",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_auditoria_fecha_usuario",
                table: "AUDITORIA",
                columns: new[] { "created_at", "usuario_id" },
                descending: new[] { true, false });

            migrationBuilder.CreateIndex(
                name: "idx_auditoria_ip",
                table: "AUDITORIA",
                column: "ip");

            migrationBuilder.CreateIndex(
                name: "idx_auditoria_usuario",
                table: "AUDITORIA",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "idx_sugerencia_campo_activo",
                table: "CATALOGO_SUGERENCIA",
                columns: new[] { "campo", "activo" });

            migrationBuilder.CreateIndex(
                name: "idx_sugerencia_campo_valor_unique",
                table: "CATALOGO_SUGERENCIA",
                columns: new[] { "campo", "valor" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_configuracion_email_created_at",
                table: "configuracion_email",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_configuracion_email_habilitado",
                table: "configuracion_email",
                column: "habilitado");

            migrationBuilder.CreateIndex(
                name: "ix_configuracion_email_proveedor",
                table: "configuracion_email",
                column: "proveedor");

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_email_usuario_id",
                table: "configuracion_email",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_CONFIGURACION_IA_ACTIVO",
                table: "CONFIGURACION_IA",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "idx_config_reportes_habilitado",
                table: "CONFIGURACION_REPORTES_AUTOMATIZADOS",
                column: "habilitado");

            migrationBuilder.CreateIndex(
                name: "IX_CONFIGURACION_REPORTES_AUTOMATIZADOS_usuario_id",
                table: "CONFIGURACION_REPORTES_AUTOMATIZADOS",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "idx_cuadrante_activo",
                table: "CUADRANTE",
                column: "activo");

            migrationBuilder.CreateIndex(
                name: "idx_cuadrante_sector_nombre_unique",
                table: "CUADRANTE",
                columns: new[] { "sector_id", "nombre" },
                unique: true);

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
                name: "idx_region_activo",
                table: "REGION",
                column: "activo");

            migrationBuilder.CreateIndex(
                name: "idx_region_zona",
                table: "REGION",
                column: "zona_id");

            migrationBuilder.CreateIndex(
                name: "idx_region_zona_nombre_unique",
                table: "REGION",
                columns: new[] { "zona_id", "nombre" },
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
                name: "idx_reporte_cuadrante_fecha",
                table: "REPORTE_INCIDENCIA",
                columns: new[] { "cuadrante_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_reporte_datetime_hechos",
                table: "REPORTE_INCIDENCIA",
                column: "datetime_hechos");

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
                name: "idx_reporte_region",
                table: "REPORTE_INCIDENCIA",
                column: "region_id");

            migrationBuilder.CreateIndex(
                name: "idx_reporte_sector_fecha",
                table: "REPORTE_INCIDENCIA",
                columns: new[] { "sector_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_reporte_usuario",
                table: "REPORTE_INCIDENCIA",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "idx_reporte_usuario_estado",
                table: "REPORTE_INCIDENCIA",
                columns: new[] { "usuario_id", "estado", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_reporte_zona",
                table: "REPORTE_INCIDENCIA",
                column: "zona_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "ROL",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ROL_CLAIM_RoleId",
                table: "ROL_CLAIM",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "idx_sector_activo",
                table: "SECTOR",
                column: "activo");

            migrationBuilder.CreateIndex(
                name: "idx_sector_region",
                table: "SECTOR",
                column: "region_id");

            migrationBuilder.CreateIndex(
                name: "idx_sector_region_nombre_unique",
                table: "SECTOR",
                columns: new[] { "region_id", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "USUARIO",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "USUARIO",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USUARIO_CLAIM_UserId",
                table: "USUARIO_CLAIM",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_USUARIO_LOGIN_UserId",
                table: "USUARIO_LOGIN",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_USUARIO_ROL_RoleId",
                table: "USUARIO_ROL",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "idx_zona_activo",
                table: "ZONA",
                column: "activo");

            migrationBuilder.CreateIndex(
                name: "idx_zona_nombre_unique",
                table: "ZONA",
                column: "nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AUDITORIA");

            migrationBuilder.DropTable(
                name: "CATALOGO_SUGERENCIA");

            migrationBuilder.DropTable(
                name: "configuracion_email");

            migrationBuilder.DropTable(
                name: "CONFIGURACION_IA");

            migrationBuilder.DropTable(
                name: "CONFIGURACION_REPORTES_AUTOMATIZADOS");

            migrationBuilder.DropTable(
                name: "REPORTE_AUTOMATIZADO");

            migrationBuilder.DropTable(
                name: "REPORTE_INCIDENCIA");

            migrationBuilder.DropTable(
                name: "ROL_CLAIM");

            migrationBuilder.DropTable(
                name: "USUARIO_CLAIM");

            migrationBuilder.DropTable(
                name: "USUARIO_LOGIN");

            migrationBuilder.DropTable(
                name: "USUARIO_ROL");

            migrationBuilder.DropTable(
                name: "USUARIO_TOKEN");

            migrationBuilder.DropTable(
                name: "MODELO_REPORTE");

            migrationBuilder.DropTable(
                name: "CUADRANTE");

            migrationBuilder.DropTable(
                name: "ROL");

            migrationBuilder.DropTable(
                name: "USUARIO");

            migrationBuilder.DropTable(
                name: "SECTOR");

            migrationBuilder.DropTable(
                name: "REGION");

            migrationBuilder.DropTable(
                name: "ZONA");
        }
    }
}
