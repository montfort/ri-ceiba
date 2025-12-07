# Ceiba - Sistema de Gestión de Incidencias

Ceiba es una aplicación web moderna construida sobre .NET para la gestión y seguimiento de incidencias. El sistema está diseñado siguiendo los principios de la Arquitectura Limpia (Clean Architecture), asegurando una clara separación de responsabilidades, alta mantenibilidad y escalabilidad.

## ✨ Características Principales

*   **Gestión de Incidencias:** Creación, visualización, actualización y seguimiento de reportes de incidencias.
*   **Autenticación y Autorización:** Sistema de usuarios robusto basado en ASP.NET Core Identity con roles (Administrador, Revisor, Creador).
*   **Exportación de Datos:** Funcionalidad para exportar reportes a formatos como PDF y JSON.
*   **Auditoría y Logging:** Registro detallado de acciones críticas y errores para monitoreo y seguridad.

## 🏗️ Arquitectura

El proyecto sigue una estructura de capas bien definida, inspirada en la Arquitectura Limpia, para desacoplar la lógica de negocio de los detalles de implementación.

*   **`Ceiba.Core`**: Contiene las entidades del dominio, interfaces y la lógica de negocio más fundamental. No depende de ninguna otra capa.
*   **`Ceiba.Application`**: Orquesta los casos de uso de la aplicación. Contiene los servicios de aplicación que utilizan las interfaces definidas en `Core`.
*   **`Ceiba.Infrastructure`**: Implementa las interfaces definidas en `Core` y `Application`. Se encarga del acceso a datos (usando Entity Framework Core con PostgreSQL), la gestión de identidad y la interacción con servicios externos.
*   **`Ceiba.Web`**: La capa de presentación, construida con Blazor Server. Es el punto de entrada para los usuarios y se comunica con la capa de aplicación.
*   **`tests/`**: Contiene una suite completa de pruebas unitarias, de integración y de la capa web para garantizar la calidad y estabilidad del código.

## 🛠️ Tecnologías Utilizadas

*   **Backend:** .NET, ASP.NET Core
*   **Frontend:** Blazor Server
*   **Base de Datos:** PostgreSQL
*   **ORM:** Entity Framework Core
*   **Autenticación:** ASP.NET Core Identity
*   **Logging:** Serilog
*   **Pruebas:** xUnit

## 🚀 Cómo Empezar

### Prerrequisitos

*   [.NET SDK](https://dotnet.microsoft.com/download)
*   [PostgreSQL](https://www.postgresql.org/download/)

### Pasos de Instalación

1.  **Clonar el repositorio:**
    ```sh
    git clone <URL-del-repositorio>
    cd ri-ceiba
    ```

2.  **Configurar la conexión a la base de datos:**
    *   Abre el archivo `src/Ceiba.Web/appsettings.Development.json`.
    *   Modifica el `ConnectionString` "DefaultConnection" para apuntar a tu instancia de PostgreSQL. Asegúrate de que el usuario y la contraseña sean correctos.

3.  **Aplicar las migraciones de la base de datos:**
    Desde la raíz del proyecto, ejecuta el siguiente comando para crear las tablas en la base de datos:
    ```sh
    dotnet ef database update --project src/Ceiba.Infrastructure
    ```

4.  **Ejecutar la aplicación:**
    ```sh
    dotnet run --project src/Ceiba.Web
    ```
    La aplicación estará disponible en `https://localhost:7241` (o el puerto que se indique en la consola).

## ✅ Ejecutar Pruebas

Para ejecutar toda la suite de pruebas y verificar la integridad del sistema, utiliza el siguiente comando desde la raíz del proyecto:

```
dotnet test
```

---
*Este README fue generado automáticamente basado en la estructura del proyecto.*
