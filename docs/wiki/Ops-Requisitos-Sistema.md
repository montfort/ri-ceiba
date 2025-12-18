# Requisitos del Sistema

Esta guía describe los requisitos de hardware y software para ejecutar Ceiba.

## Requisitos de Hardware

### Mínimos (Desarrollo/Testing)

| Recurso | Mínimo |
|---------|--------|
| CPU | 2 cores |
| RAM | 4 GB |
| Disco | 20 GB |
| Red | 100 Mbps |

### Recomendados (Producción)

| Recurso | Recomendado |
|---------|-------------|
| CPU | 4+ cores |
| RAM | 8+ GB |
| Disco | 50+ GB SSD |
| Red | 1 Gbps |

### Estimación de Almacenamiento

| Componente | Tamaño Estimado |
|------------|-----------------|
| Aplicación | ~200 MB |
| PostgreSQL base | ~500 MB |
| Por 1,000 reportes | ~10 MB |
| Logs (mensual) | ~1 GB |
| Backups | 2x tamaño DB |

## Requisitos de Software

### Sistema Operativo

| OS | Versión | Soporte |
|----|---------|---------|
| Fedora Linux | 42+ | Recomendado |
| Ubuntu LTS | 22.04+ | Soportado |
| RHEL/CentOS | 9+ | Soportado |
| Windows Server | 2022+ | Soportado |

### Con Docker (Recomendado)

| Software | Versión |
|----------|---------|
| Docker Engine | 24+ |
| Docker Compose | 2.20+ |

### Sin Docker

| Software | Versión |
|----------|---------|
| .NET Runtime | 10.0+ |
| ASP.NET Core Runtime | 10.0+ |
| PostgreSQL | 18+ |

## Requisitos de Red

### Puertos Necesarios

| Puerto | Servicio | Notas |
|--------|----------|-------|
| 80 | HTTP | Redirigir a HTTPS |
| 443 | HTTPS | Producción |
| 5000 | HTTP (dev) | Solo desarrollo |
| 5001 | HTTPS (dev) | Solo desarrollo |
| 5432 | PostgreSQL | Solo interno |

### Conectividad Externa

| Servicio | Propósito | Obligatorio |
|----------|-----------|-------------|
| SMTP | Envío de emails | Si (para reportes auto) |
| OpenAI/Azure | IA | Si (para reportes auto) |
| NTP | Sincronización hora | Recomendado |

## Navegadores Soportados

| Navegador | Versión Mínima |
|-----------|----------------|
| Chrome | 90+ |
| Firefox | 90+ |
| Edge | 90+ |
| Safari | 14+ |

> **Nota:** Internet Explorer no está soportado.

## Requisitos de Seguridad

### Certificados SSL

- Certificado TLS 1.2+ para HTTPS
- Let's Encrypt (gratuito) o certificado comercial
- Renovación automática recomendada

### Firewall

- Puertos 80 y 443 abiertos al público
- Puerto 5432 solo acceso interno
- Sin acceso SSH público (usar VPN o bastion)

## Dependencias del Sistema

### Fedora/RHEL

```bash
# Docker
sudo dnf install docker docker-compose-plugin

# O instalación nativa
sudo dnf install dotnet-runtime-10.0 aspnetcore-runtime-10.0
sudo dnf install postgresql18-server
```

### Ubuntu/Debian

```bash
# Docker
sudo apt install docker.io docker-compose-v2

# O instalación nativa
sudo apt install dotnet-runtime-10.0 aspnetcore-runtime-10.0
sudo apt install postgresql-18
```

## Verificación Pre-Instalación

```bash
# Verificar recursos
free -h
df -h
nproc

# Verificar Docker
docker --version
docker compose version

# Verificar puertos disponibles
ss -tlnp | grep -E ':80|:443|:5432'

# Verificar conectividad externa
curl -I https://api.openai.com
```

## Próximos Pasos

- [[Ops-Instalacion-Docker|Instalación con Docker]]
- [[Ops-Instalacion-Linux|Instalación en Linux]]
- [[Ops-Instalacion-Windows|Instalación en Windows]]
