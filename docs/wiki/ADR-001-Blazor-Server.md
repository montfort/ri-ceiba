# ADR-001: Uso de Blazor Server

**Estado:** Aceptado
**Fecha:** 2025-01-15
**Autores:** Equipo de Desarrollo

## Contexto

Necesitamos elegir una tecnología para la interfaz de usuario del sistema Ceiba que:
- Sea compatible con ASP.NET Core
- Permita desarrollo rápido
- Funcione bien en dispositivos móviles
- No requiera JavaScript complejo
- Sea mantenible a largo plazo

## Opciones Consideradas

### Opción 1: Blazor Server

- Renderizado en el servidor
- Conexión SignalR en tiempo real
- C# para lógica de UI

### Opción 2: Blazor WebAssembly

- Ejecución en el navegador
- Descarga inicial más grande
- Funcionamiento offline posible

### Opción 3: Razor Pages + JavaScript

- Arquitectura tradicional MVC
- JavaScript para interactividad
- Múltiples lenguajes a mantener

### Opción 4: SPA (React/Vue/Angular) + API

- Frontend separado
- API REST backend
- Equipos separados posibles

## Decisión

**Elegimos Blazor Server** por las siguientes razones:

1. **Consistencia de lenguaje**: Todo el código en C#
2. **Desarrollo rápido**: Componentes reutilizables sin JavaScript
3. **Seguridad**: Lógica sensible permanece en el servidor
4. **Compatibilidad**: Funciona en navegadores sin WebAssembly
5. **Debugging**: Mismo entorno de debugging que el backend

## Consecuencias

### Positivas

- Desarrollo más rápido con un solo lenguaje
- Acceso directo a servicios backend sin API adicional
- Actualizaciones instantáneas sin despliegue de frontend
- Menor complejidad en la arquitectura

### Negativas

- Requiere conexión constante (SignalR)
- Mayor carga en el servidor
- Latencia en cada interacción
- No funciona offline

### Mitigaciones

| Riesgo | Mitigación |
|--------|------------|
| Pérdida de conexión | Implementar reconexión automática |
| Carga del servidor | Escalado horizontal con load balancer |
| Latencia | Optimizar consultas y minimizar round-trips |

## Alternativas Futuras

Si las necesidades cambian:
- Migrar componentes críticos a Blazor WebAssembly
- Implementar API REST para integraciones externas
- Considerar PWA para funcionalidad offline

## Referencias

- [Documentación oficial de Blazor](https://docs.microsoft.com/aspnet/core/blazor/)
- [Blazor Server vs WebAssembly](https://docs.microsoft.com/aspnet/core/blazor/hosting-models)
