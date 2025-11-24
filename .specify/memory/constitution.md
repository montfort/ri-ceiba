# Ceiba - Reportes de Incidencias Constitution

## Core Principles

### I. Arquitectura Modular Orientada al Dominio
Cada funcionalidad principal de la aplicación se construirá como un módulo autocontenido. Los módulos deben tener responsabilidades claras y bien definidas, comunicándose entre sí exclusivamente a través de contratos de API públicos y estables. Se prohíbe la dependencia directa de componentes internos de otros módulos para garantizar un bajo acoplamiento y facilitar el mantenimiento y las pruebas independientes.

### II. Test-First (No Negociable)
El desarrollo seguirá obligatoriamente la metodología de Desarrollo Guiado por Pruebas (TDD). Antes de escribir cualquier línea de código de implementación, se deberá escribir una prueba que la valide y que, inicialmente, falle. El ciclo Rojo-Verde-Refactorizar es de estricto cumplimiento para todo el código de lógica de negocio, garantizando una alta cobertura y fiabilidad desde el inicio.

### III. Seguridad y Auditabilidad por Diseño
La seguridad no es una característica adicional, sino un pilar del diseño. Todas las funcionalidades deben diseñarse siguiendo el principio de menor privilegio. Se deben aplicar rigurosamente las mejores prácticas de seguridad web (ej. OWASP Top 10). Además, toda acción crítica debe generar un registro de auditoría estructurado que identifique al actor, la acción y la fecha/hora, para garantizar la trazabilidad y la responsabilidad.

### IV. Interfaz Adaptable y Accesible
La interfaz de usuario se desarrollará con un enfoque "mobile-first", asegurando una experiencia de usuario óptima en dispositivos móviles que escala elegantemente a pantallas de escritorio. El desarrollo deberá cumplir con las pautas de accesibilidad web (WCAG) nivel AA como mínimo, para garantizar que la aplicación pueda ser utilizada por la mayor cantidad de personas posible.

### VI. Documentación como Entregable
La documentación no es una tarea posterior, sino una parte integral de cualquier entrega de código. Todo módulo nuevo, API pública o decisión arquitectónica relevante debe estar documentado. El código debe ser autocomentado y claro, pero se requerirá una documentación explícita para los contratos de API, las entidades de datos y las guías de configuración.

## Governance
Esta Constitución es el documento de más alto nivel y prevalece sobre cualquier otra práctica o convención de desarrollo. Las enmiendas a este documento requieren una propuesta documentada, la aprobación unánime del equipo de liderazgo técnico y un plan de migración claro si el cambio afecta al código existente. Todas las revisiones de código deben verificar el cumplimiento de estos principios.

**Version**: 1.0.0 | **Ratified**: 2025-11-17 | **Last Amended**: 2025-11-17