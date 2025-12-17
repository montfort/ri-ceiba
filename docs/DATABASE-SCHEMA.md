# Esquema de Base de Datos - Ceiba

> **Versión**: 1.0
> **Fecha**: 2025-12-16
> **Motor**: PostgreSQL 17+

Este documento describe el esquema completo de la base de datos del sistema Ceiba.

---

## Resumen de Tablas

| Tabla | Descripción | Tipo |
|-------|-------------|------|
| `USUARIO` | Usuarios del sistema (ASP.NET Identity) | Identity |
| `ROL` | Roles del sistema (CREADOR, REVISOR, ADMIN) | Identity |
| `USUARIO_ROL` | Relación usuario-rol | Identity |
| `ZONA` | Zonas geográficas | Catálogo |
| `REGION` | Regiones dentro de zonas | Catálogo |
| `SECTOR` | Sectores dentro de regiones | Catálogo |
| `CUADRANTE` | Cuadrantes dentro de sectores | Catálogo |
| `CATALOGO_SUGERENCIA` | Sugerencias para campos de texto | Catálogo |
| `REPORTE_INCIDENCIA` | Reportes de incidencias (Tipo A) | Dominio |
| `AUDITORIA` | Registros de auditoría | Sistema |
| `MODELO_REPORTE` | Plantillas de reportes automatizados | Configuración |
| `REPORTE_AUTOMATIZADO` | Reportes generados automáticamente | Dominio |
| `CONFIGURACION_IA` | Configuración de IA | Configuración |
| `configuracion_email` | Configuración de email | Configuración |
| `CONFIGURACION_REPORTES_AUTOMATIZADOS` | Config. de reportes automáticos | Configuración |

---

## Tablas de Identity (ASP.NET Core Identity)

### USUARIO
Almacena los usuarios del sistema.

```sql
CREATE TABLE "USUARIO" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserName" varchar(256),
    "NormalizedUserName" varchar(256) UNIQUE,
    "Email" varchar(256),
    "NormalizedEmail" varchar(256),
    "EmailConfirmed" boolean NOT NULL DEFAULT false,
    "PasswordHash" text,
    "SecurityStamp" text,
    "ConcurrencyStamp" text,
    "PhoneNumber" text,
    "PhoneNumberConfirmed" boolean NOT NULL DEFAULT false,
    "TwoFactorEnabled" boolean NOT NULL DEFAULT false,
    "LockoutEnd" timestamptz,
    "LockoutEnabled" boolean NOT NULL DEFAULT false,
    "AccessFailedCount" integer NOT NULL DEFAULT 0
);

CREATE INDEX "EmailIndex" ON "USUARIO" ("NormalizedEmail");
CREATE UNIQUE INDEX "UserNameIndex" ON "USUARIO" ("NormalizedUserName");
```

### ROL
Roles del sistema: CREADOR, REVISOR, ADMIN.

```sql
CREATE TABLE "ROL" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" varchar(256),
    "NormalizedName" varchar(256) UNIQUE,
    "ConcurrencyStamp" text
);

CREATE UNIQUE INDEX "RoleNameIndex" ON "ROL" ("NormalizedName");
```

### USUARIO_ROL
Relación muchos-a-muchos entre usuarios y roles.

```sql
CREATE TABLE "USUARIO_ROL" (
    "UserId" uuid NOT NULL REFERENCES "USUARIO"("Id") ON DELETE CASCADE,
    "RoleId" uuid NOT NULL REFERENCES "ROL"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("UserId", "RoleId")
);
```

### Tablas adicionales de Identity

```sql
-- Claims de usuario
CREATE TABLE "USUARIO_CLAIM" (
    "Id" serial PRIMARY KEY,
    "UserId" uuid NOT NULL REFERENCES "USUARIO"("Id") ON DELETE CASCADE,
    "ClaimType" text,
    "ClaimValue" text
);

-- Claims de rol
CREATE TABLE "ROL_CLAIM" (
    "Id" serial PRIMARY KEY,
    "RoleId" uuid NOT NULL REFERENCES "ROL"("Id") ON DELETE CASCADE,
    "ClaimType" text,
    "ClaimValue" text
);

-- Logins externos
CREATE TABLE "USUARIO_LOGIN" (
    "LoginProvider" text NOT NULL,
    "ProviderKey" text NOT NULL,
    "ProviderDisplayName" text,
    "UserId" uuid NOT NULL REFERENCES "USUARIO"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("LoginProvider", "ProviderKey")
);

-- Tokens de usuario
CREATE TABLE "USUARIO_TOKEN" (
    "UserId" uuid NOT NULL REFERENCES "USUARIO"("Id") ON DELETE CASCADE,
    "LoginProvider" text NOT NULL,
    "Name" text NOT NULL,
    "Value" text,
    PRIMARY KEY ("UserId", "LoginProvider", "Name")
);
```

---

## Tablas de Catálogos Geográficos

### ZONA
Nivel superior de la jerarquía geográfica.

```sql
CREATE TABLE "ZONA" (
    id serial PRIMARY KEY,
    nombre varchar(100) NOT NULL,
    activo boolean NOT NULL DEFAULT true,
    usuario_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX idx_zona_nombre_unique ON "ZONA" (nombre);
CREATE INDEX idx_zona_activo ON "ZONA" (activo);
```

### REGION
Regiones dentro de cada zona.

```sql
CREATE TABLE "REGION" (
    id serial PRIMARY KEY,
    nombre varchar(100) NOT NULL,
    zona_id integer NOT NULL REFERENCES "ZONA"(id) ON DELETE RESTRICT,
    activo boolean NOT NULL DEFAULT true,
    usuario_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT NOW(),

    CONSTRAINT zona_id_FK FOREIGN KEY (zona_id) REFERENCES "ZONA"(id)
);

CREATE INDEX idx_region_zona ON "REGION" (zona_id);
CREATE INDEX idx_region_activo ON "REGION" (activo);
CREATE UNIQUE INDEX idx_region_zona_nombre_unique ON "REGION" (zona_id, nombre);
```

### SECTOR
Sectores dentro de cada región.

```sql
CREATE TABLE "SECTOR" (
    id serial PRIMARY KEY,
    nombre varchar(100) NOT NULL,
    region_id integer NOT NULL,
    activo boolean NOT NULL DEFAULT true,
    usuario_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT NOW(),

    CONSTRAINT sector_region_id_FK FOREIGN KEY (region_id) REFERENCES "REGION"(id) ON DELETE RESTRICT
);

CREATE INDEX idx_sector_region ON "SECTOR" (region_id);
CREATE INDEX idx_sector_activo ON "SECTOR" (activo);
CREATE UNIQUE INDEX idx_sector_region_nombre_unique ON "SECTOR" (region_id, nombre);
```

### CUADRANTE
Cuadrantes dentro de cada sector.

```sql
CREATE TABLE "CUADRANTE" (
    id serial PRIMARY KEY,
    nombre varchar(100) NOT NULL,
    sector_id integer NOT NULL,
    activo boolean NOT NULL DEFAULT true,
    usuario_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT NOW(),

    CONSTRAINT sector_id_FK FOREIGN KEY (sector_id) REFERENCES "SECTOR"(id) ON DELETE RESTRICT
);

CREATE INDEX idx_cuadrante_activo ON "CUADRANTE" (activo);
CREATE UNIQUE INDEX idx_cuadrante_sector_nombre_unique ON "CUADRANTE" (sector_id, nombre);
```

---

## Tabla de Sugerencias

### CATALOGO_SUGERENCIA
Valores sugeridos para campos de texto libre.

```sql
CREATE TABLE "CATALOGO_SUGERENCIA" (
    id serial PRIMARY KEY,
    campo varchar(50) NOT NULL,        -- sexo, delito, tipo_de_atencion, turno_ceiba, traslados
    valor varchar(200) NOT NULL,
    orden integer NOT NULL DEFAULT 0,
    activo boolean NOT NULL DEFAULT true,
    usuario_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX idx_sugerencia_campo_valor_unique ON "CATALOGO_SUGERENCIA" (campo, valor);
CREATE INDEX idx_sugerencia_campo_activo ON "CATALOGO_SUGERENCIA" (campo, activo);
```

**Campos soportados**:
- `sexo`: Hombre, Mujer, No binario, Prefiere no decir
- `delito`: Violencia familiar, Abuso sexual, Acoso sexual, etc.
- `tipo_de_atencion`: Llamada telefónica, Mensaje de texto, Radio, Primer respondiente
- `turno_ceiba`: Balderas 1-3, Nonoalco 1-3
- `traslados`: Sí, No, No aplica

---

## Tabla Principal de Reportes

### REPORTE_INCIDENCIA
Reportes de incidencias tipo A.

```sql
CREATE TABLE "REPORTE_INCIDENCIA" (
    id serial PRIMARY KEY,

    -- Metadatos
    tipo_reporte varchar(10) NOT NULL DEFAULT 'A',
    schema_version varchar(10) NOT NULL DEFAULT '1.0',
    estado smallint NOT NULL DEFAULT 0,  -- 0=Borrador, 1=Entregado
    usuario_id uuid NOT NULL,

    -- Datos del hecho
    datetime_hechos timestamptz NOT NULL,

    -- Datos de la víctima
    sexo varchar(50) NOT NULL,
    edad integer NOT NULL CHECK (edad > 0 AND edad < 150),
    lgbtttiq_plus boolean NOT NULL DEFAULT false,
    situacion_calle boolean NOT NULL DEFAULT false,
    migrante boolean NOT NULL DEFAULT false,
    discapacidad boolean NOT NULL DEFAULT false,

    -- Tipo de delito
    delito varchar(100) NOT NULL,

    -- Ubicación geográfica
    zona_id integer NOT NULL REFERENCES "ZONA"(id),
    region_id integer NOT NULL REFERENCES "REGION"(id),
    sector_id integer NOT NULL REFERENCES "SECTOR"(id),
    cuadrante_id integer NOT NULL REFERENCES "CUADRANTE"(id),

    -- Detalles operativos
    turno_ceiba varchar(100) NOT NULL,
    tipo_de_atencion varchar(100) NOT NULL,
    tipo_de_accion varchar(500) NOT NULL,

    -- Narrativas
    hechos_reportados text NOT NULL,
    acciones_realizadas text NOT NULL,
    traslados varchar(100) NOT NULL,
    observaciones text,

    -- Extensibilidad
    campos_adicionales jsonb,

    -- Timestamps
    created_at timestamptz NOT NULL DEFAULT NOW(),
    updated_at timestamptz,

    -- Constraints
    CONSTRAINT CK_REPORTE_EDAD CHECK (edad > 0 AND edad < 150),
    CONSTRAINT CK_REPORTE_ESTADO CHECK (estado IN (0, 1)),
    CONSTRAINT FK_REPORTE_ZONA FOREIGN KEY (zona_id) REFERENCES "ZONA"(id),
    CONSTRAINT FK_REPORTE_REGION FOREIGN KEY (region_id) REFERENCES "REGION"(id),
    CONSTRAINT FK_REPORTE_SECTOR FOREIGN KEY (sector_id) REFERENCES "SECTOR"(id),
    CONSTRAINT FK_REPORTE_CUADRANTE FOREIGN KEY (cuadrante_id) REFERENCES "CUADRANTE"(id)
);

-- Índices para búsquedas frecuentes
CREATE INDEX idx_reporte_fecha ON "REPORTE_INCIDENCIA" (created_at DESC);
CREATE INDEX idx_reporte_usuario ON "REPORTE_INCIDENCIA" (usuario_id);
CREATE INDEX idx_reporte_estado ON "REPORTE_INCIDENCIA" (estado);
CREATE INDEX idx_reporte_zona ON "REPORTE_INCIDENCIA" (zona_id);
CREATE INDEX idx_reporte_region ON "REPORTE_INCIDENCIA" (region_id);
CREATE INDEX idx_reporte_delito ON "REPORTE_INCIDENCIA" (delito);
CREATE INDEX idx_reporte_datetime_hechos ON "REPORTE_INCIDENCIA" (datetime_hechos);

-- Índices compuestos para queries comunes
CREATE INDEX idx_reporte_usuario_estado ON "REPORTE_INCIDENCIA" (usuario_id, estado, created_at DESC);
CREATE INDEX idx_reporte_sector_fecha ON "REPORTE_INCIDENCIA" (sector_id, created_at DESC);
CREATE INDEX idx_reporte_cuadrante_fecha ON "REPORTE_INCIDENCIA" (cuadrante_id, created_at DESC);
CREATE INDEX idx_reporte_composite_search ON "REPORTE_INCIDENCIA" (estado, zona_id, created_at DESC);
CREATE INDEX idx_reporte_composite_revisor ON "REPORTE_INCIDENCIA" (created_at DESC, estado, delito);
```

---

## Tabla de Auditoría

### AUDITORIA
Registro de todas las acciones del sistema.

```sql
CREATE TABLE "AUDITORIA" (
    id bigserial PRIMARY KEY,
    codigo varchar(50) NOT NULL,         -- LOGIN_SUCCESS, REPORT_CREATE, etc.
    usuario_id uuid,
    tabla_relacionada varchar(50),
    id_relacionado integer,
    detalles jsonb,
    ip varchar(45),
    created_at timestamptz NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_auditoria_codigo ON "AUDITORIA" (codigo);
CREATE INDEX idx_auditoria_usuario ON "AUDITORIA" (usuario_id);
CREATE INDEX idx_auditoria_fecha ON "AUDITORIA" (created_at);
CREATE INDEX idx_auditoria_ip ON "AUDITORIA" (ip);
CREATE INDEX idx_auditoria_entidad ON "AUDITORIA" (tabla_relacionada, id_relacionado);
CREATE INDEX idx_auditoria_codigo_fecha ON "AUDITORIA" (codigo, created_at DESC);
CREATE INDEX idx_auditoria_fecha_usuario ON "AUDITORIA" (created_at DESC, usuario_id);
```

---

## Tablas de Reportes Automatizados

### MODELO_REPORTE
Plantillas para reportes automatizados.

```sql
CREATE TABLE "MODELO_REPORTE" (
    id serial PRIMARY KEY,
    nombre varchar(100) NOT NULL UNIQUE,
    descripcion varchar(500),
    contenido_markdown text NOT NULL,
    es_default boolean NOT NULL DEFAULT false,
    activo boolean NOT NULL DEFAULT true,
    usuario_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT NOW(),
    updated_at timestamptz
);

CREATE UNIQUE INDEX idx_modelo_nombre_unique ON "MODELO_REPORTE" (nombre);
CREATE INDEX idx_modelo_activo ON "MODELO_REPORTE" (activo);
CREATE INDEX idx_modelo_default ON "MODELO_REPORTE" (es_default);
```

### REPORTE_AUTOMATIZADO
Reportes generados automáticamente.

```sql
CREATE TABLE "REPORTE_AUTOMATIZADO" (
    id serial PRIMARY KEY,
    fecha_inicio timestamptz NOT NULL,
    fecha_fin timestamptz NOT NULL,
    contenido_markdown text NOT NULL,
    contenido_word_path varchar(500),
    estadisticas jsonb NOT NULL DEFAULT '{}',
    enviado boolean NOT NULL DEFAULT false,
    fecha_envio timestamptz,
    error_mensaje text,
    modelo_reporte_id integer REFERENCES "MODELO_REPORTE"(id) ON DELETE SET NULL,
    created_at timestamptz NOT NULL DEFAULT NOW(),

    CONSTRAINT modelo_reporte_id_FK FOREIGN KEY (modelo_reporte_id)
        REFERENCES "MODELO_REPORTE"(id) ON DELETE SET NULL
);

CREATE INDEX idx_reporte_auto_created ON "REPORTE_AUTOMATIZADO" (created_at DESC);
CREATE INDEX idx_reporte_auto_enviado ON "REPORTE_AUTOMATIZADO" (enviado);
CREATE INDEX idx_reporte_auto_fecha_inicio ON "REPORTE_AUTOMATIZADO" (fecha_inicio);
```

---

## Tablas de Configuración

### CONFIGURACION_IA
Configuración del proveedor de IA.

```sql
CREATE TABLE "CONFIGURACION_IA" (
    "Id" serial PRIMARY KEY,
    "Proveedor" varchar(50) NOT NULL,     -- OpenAI, AzureOpenAI, Local
    "Modelo" varchar(100) NOT NULL,
    "ApiKey" varchar(500),
    "Endpoint" varchar(500),
    "AzureEndpoint" varchar(500),
    "AzureApiVersion" varchar(50),
    "LocalEndpoint" varchar(500),
    "Temperature" double precision NOT NULL DEFAULT 0.7,
    "MaxTokens" integer NOT NULL DEFAULT 1000,
    "MaxReportesParaNarrativa" integer NOT NULL,
    "Activo" boolean NOT NULL DEFAULT true,
    "ModificadoPorId" uuid,
    "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamptz
);

CREATE INDEX "IX_CONFIGURACION_IA_ACTIVO" ON "CONFIGURACION_IA" ("Activo");
```

### configuracion_email
Configuración del servicio de email.

```sql
CREATE TABLE configuracion_email (
    id serial PRIMARY KEY,
    proveedor varchar(50) NOT NULL,       -- SMTP, SendGrid, Mailgun
    habilitado boolean NOT NULL DEFAULT false,
    from_email varchar(255) NOT NULL,
    from_name varchar(255) NOT NULL,

    -- SMTP
    smtp_host varchar(255),
    smtp_port integer,
    smtp_username varchar(255),
    smtp_password varchar(500),
    smtp_use_ssl boolean NOT NULL DEFAULT true,

    -- SendGrid
    sendgrid_api_key varchar(500),

    -- Mailgun
    mailgun_api_key varchar(500),
    mailgun_domain varchar(255),
    mailgun_region varchar(10),

    -- Pruebas
    last_tested_at timestamptz,
    last_test_success boolean,
    last_test_error varchar(1000),

    -- Metadatos
    usuario_id uuid NOT NULL,
    created_at timestamptz NOT NULL,
    updated_at timestamptz
);

CREATE INDEX ix_configuracion_email_proveedor ON configuracion_email (proveedor);
CREATE INDEX ix_configuracion_email_habilitado ON configuracion_email (habilitado);
CREATE INDEX ix_configuracion_email_created_at ON configuracion_email (created_at);
```

### CONFIGURACION_REPORTES_AUTOMATIZADOS
Configuración de generación automática.

```sql
CREATE TABLE "CONFIGURACION_REPORTES_AUTOMATIZADOS" (
    id serial PRIMARY KEY,
    habilitado boolean NOT NULL DEFAULT false,
    hora_generacion interval NOT NULL DEFAULT '06:00:00',
    destinatarios varchar(2000) NOT NULL DEFAULT '',
    ruta_salida varchar(500) NOT NULL DEFAULT './generated-reports',
    usuario_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT NOW(),
    updated_at timestamptz
);

CREATE INDEX idx_config_reportes_habilitado ON "CONFIGURACION_REPORTES_AUTOMATIZADOS" (habilitado);
```

---

## Jerarquía Geográfica

```
ZONA (1)
  └── REGION (N)
        └── SECTOR (N)
              └── CUADRANTE (N)
                    └── REPORTE_INCIDENCIA (N)
```

---

## Diagrama de Relaciones (Simplificado)

```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌───────────┐
│   ZONA   │────<│  REGION  │────<│  SECTOR  │────<│ CUADRANTE │
└──────────┘     └──────────┘     └──────────┘     └───────────┘
                                                          │
                                                          │
                                                          ▼
┌──────────┐     ┌──────────────┐     ┌───────────────────────┐
│ USUARIO  │────<│ USUARIO_ROL  │────<│         ROL           │
└──────────┘     └──────────────┘     └───────────────────────┘
      │
      │
      ▼
┌─────────────────────┐
│ REPORTE_INCIDENCIA  │
└─────────────────────┘
      │
      ▼
┌─────────────────────┐
│     AUDITORIA       │
└─────────────────────┘
```

---

## Notas de Implementación

1. **Timestamps**: Todas las tablas usan `timestamptz` (timestamp with time zone) para UTC compliance.
2. **Soft Delete**: Las tablas de catálogos usan campo `activo` en lugar de eliminación física.
3. **Auditoría**: Todas las acciones críticas se registran en la tabla `AUDITORIA`.
4. **Identity**: Se usa ASP.NET Core Identity con tablas renombradas en español.
5. **Extensibilidad**: `REPORTE_INCIDENCIA` tiene campo `campos_adicionales` (JSONB) para futuras extensiones.

---

*Documento generado: 2025-12-16*
