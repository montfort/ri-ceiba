# Proyecto "Ceiba - Reportes de Incidencias"

## Descripción del proyecto

"Ceiba - Reportes de Incidencias" será una aplicación web para la gestión de Reportes de Incidencias para la Unidad Especializada en Género de la Secretaría de Seguridad Ciudadana (SSC) de la Ciudad de México.

## Objetivos

Proporcionar un sistema informático que gestione reportes de incidencias generadas durante la prestación de servicios de seguridad. Los agentes de policía podrán ingresar a la plataforma para llenar un formulario de incidencia, el cual permanecerá editable hasta que se entregue por medio de la propia plataforma. Entonces, ya entregado, el reporte pasa a un estado de no editable y, aunque puede ser consultado por el agente que lo hizo, en un historial de reportes, ya no lo podrá editar. Existen también supervisores, que tienen otro tipo de cuenta de usuario en la plataforma, que reciben los reportes de incidencias. Estos supervisores pueden ver un listado de reportes ordenados por ciertos campos, pueden realizar búsquedas por campos y también construir reportes que serán impresos o exportados a archivos PDF. Por último, existe un usuario administrador técnico, que realiza acciones como alta y baja de usuarios agentes y usuarios supervisores, puede ver y buscar registros de auditoría, como cuándo y desde dónde ingresaron usuarios y qué hicieron en la plataforma.

En cuanto a los reportes de incidencias, los supervisores pueden editar los reportes que les llegan, por ejemplo, para agregar información sobre las acciones que se tomaron en respuesta a la incidencia reportada. Debes considerar que los reportes de incidencias pueden ser muchos, por lo que la aplicación debe ser capaz de manejar grandes volúmenes de datos y permitir búsquedas eficientes. También es importante que la aplicación sea segura, para proteger la información sensible contenida en los reportes de incidencias.

Considera que los reportes pueden ser de diversos tipos, por lo que vamos a agregar en el futuro más formularios para diferentes tipos de reportes. Por lo tanto, la aplicación debe ser flexible y permitir la adición de nuevos formularios sin necesidad de reestructurar toda la base de datos, actualmente te estoy indicando un modelo de datos para Reporte de Incidencia, vamos a llamar a ese modelo "Tipo A", recuerda que el usuario deberá poder elegir qué tipo de reporte está creando y que en el futuro habrá más tipos de reportes con sus respectivos formularios de creación y edición que correspondan, aunque actualmente su elección esté limitada a "Tipo A".

También considera que hay campos en los modelos de tipos de Reportes de Incidencias, que se refieren a códigos a los que les corresponde un conjunto de opciones con texto descriptivo, por ejemplo, los campos `zona`, `sector` y `cuadrante` tienen un código numérico que no debe ser expuesto a la vista del usuario, sino su representación textual, por ejemplo, Zona 1 podría ser "Centro" al ojo del usuario (más delante te proporcionaré los códigos y sus representaciones textuales), también considera que tanto `zona` como `sector` y `cuadrante` son dependientes de manera escalonada: Para cada Zona hay una lista específica de Sectores que les corresponden, y cada Sector específico, tiene su propia lista de Cuadrantes, todas estas listas deben ser fácilmente configurables por el administrador técnico desde la interface administrativa, te voy a proporcionar más adelante los modelos de datos y listas de dependencias y contenidos. Nota que otros campos son textuales, pero se necesita que, además de entrada manual para el usuario, se puedan seleccionar de una lista predefinida de sugerencias, como los campos `sexo`, `delito` y `tipoDeAtencion`, estas listas de sugerencias y los campos a los que están dirigidos deben ser configurables por el administrador técnico desde la interface administrativa.

Toda configuración de campos debe ser auditada y registrada en la base de datos. La configuración de campos, debe ser compartida a entre todos los tipos de Formulario que posean campos configurables y con el mismo nombre de campo, me explico, digamos que tenemos un formulario tipo A y un formulario tipo B, tienen ambos campos `zona`, `sector`, `cuadrante`, `sexo`, `delito` o `tipoDeAtencion`, entonces cada tipo de formulario deberá obtener la configuración de los campos desde la misma fuente.

Recapitulando, el flujo de trabajo será el siguiente: Un agente de policía inicia sesión en la aplicación, crea un nuevo Reporte de Incidencia (Tipo A), llena el formulario con los datos correspondientes, y lo entrega. El reporte pasa a un estado no editable. Un supervisor inicia sesión, ve el listado de reportes entregados, busca uno específico, lo visualiza y lo edita para hacer correcciones, también puede exportar los reportes a formato PDF o JSON. Finalmente, un administrador técnico puede gestionar usuarios y ver registros de auditoría.

Queremos agregar algo más: A una hora programada del día, se debe generar un reporte automático que contenga un resumen de las incidencias reportadas en el último día, por ejemplo, si el reporte debe generarse y enviarse a las 6:00am del día de hoy, entonces se refiere a los reportes generados entre las 6:00am del día de ayer y las 6:00am del día de hoy. Este reporte debe ser enviado por correo electrónico a una lista de destinatarios configurables por el administrador técnico. Estos reportes automatizados también deben ser guardados en la base de datos, y el usuario REVISOR puede ver una lista de ellos (la lista se puede ordenar por fecha), también podrá seleccionar uno, visualizarlo y editarlo. Los informes deberán crearse y editarse en formato Markdown, pero deberán convertirse a Word para su envío por correo electrónico (considera para el envío por correo electrónico el uso de una API externa). Este resumen diario debe incluir estadísticas como el número total de reportes, tipos de delitos más comunes, zonas con más incidencias, entre otros datos relevantes, así como un resumen en texto de los campos descriptivos de las incidencias. El reporte debe ser generado en formato Word. El resumen deber ser generado por un sistema de inteligencia artificial que procese los datos de los reportes y genere un texto coherente y relevante, se debe considerar el uso de una API externa para este propósito, y que se proporcionaría un modelo de reporte (quizás en formato markdown) como ejemplo para que la IA entienda qué es lo que se quiere en el reporte. El modelo de reporte debe ser editable por el usuario REVISOR. Los modelos de reporte deben ser almacenados en la base de datos, y el usuario REVISOR puede ver una lista de ellos (la lista se puede ordenar por fecha), también podrá seleccionar uno, visualizarlo y editarlo.

Cada operación en la aplicación debe ser registrada para efectos de auditoría, capturando los datos indispensables para identificar qué usuario hizo qué y cuándo. Esto significa que cada acción en el sistema tiene un código numérico (o alfanumérico si se considera más óptimo) y una representación o significado textual, que será usada para los registros de auditoría.

Toda actividad automatizada como la creación de reportes y envíos por correo electrónico, debe ser registrada en los registros de auditoría, indicando qué proceso automatizado realizó la acción y cuándo.

Finalmente, la aplicación debe ser fácil de usar, con una interfaz intuitiva que permita a los usuarios navegar y utilizar las funcionalidades sin dificultad.

## Propuesta inicial de Requisitos

- Existen tres tipos de usuario, que corresponden a los tres roles para efecto de role-based access control (RBAC), que serían: CREADOR, REVISOR, ADMIN.
- El usuario CREADOR podrá:
  - Crear Reportes de Incidencias
  - Mientras el usuario no haya entregado el Reporte de Incidencia, podrá editarlo.
  - Una vez que el usuario entregue el Reporte de Incidencia, este pasará a un estado no editable.
  - Listar sus propios Reportes de Incidencias.
  - Buscar en sus propios Reportes de Incidencias.
  - Desde la lista de sus propios Reportes de Incidencias, seleccionar uno y visualizarlo.
  - Usuario creador suspendido no podrá iniciar sesión ni crear ni editar ni listar ni buscar ni visualizar Reportes de Incidencias.
- El usuario REVISOR podrá:
  - Listar todos los Reportes de Incidencias.
  - Buscar en todos los Reportes de Incidencias.
  - Desde una lista, seleccionar un Reporte de Incidencia y visualizarlo o editarlo.
  - Este usuario no podrá crear Reportes de Incidencias.
  - Exportar Reportes de Incidencias a formatos PDF y JSON.
  - Usuario revisor suspendido no podrá iniciar sesión ni listar ni buscar ni visualizar ni editar Reportes de Incidencias.
- El usuario ADMIN podrá:
  - Listar todos los usuarios
  - Crear nuevo usuario.
  - Eliminar usuario.
  - Suspender usuario.
  - Asignar roles y permisos a usuarios.
  - Ver registros de auditoría.
  - Buscar en registros de auditoría.
  - Este usuario no podrá crear Reportes de Incidencias, ni editarlos, ni listarlos, ni buscarlos.
  - Usuario admin suspendido no podrá iniciar sesión ni listar ni crear ni eliminar ni suspender ni asignar roles ni ver ni buscar registros de auditoría.
- El sistema deberá manejar los siguientes estados para los Reportes de Incidencias:
  - Borrador: El reporte está siendo creado o editado por el usuario CREADOR.
  - Entregado: El reporte ha sido entregado por el usuario CREADOR y ya no puede ser editado por él.
- El sistema deberá manejar los siguientes códigos para los campos dependientes:
  - Zona: Códigos del 1 al 9.
  - Sector: Códigos del 1 al 20, dependientes de la Zona seleccionada.
  - Cuadrante: Códigos del 1 al 10, dependientes del Sector seleccionado.
- El sistema deberá manejar listas de sugerencias para los siguientes campos:
  - Sexo: Masculino, Femenino, Otro.
  - Delito: Violencia contra la mujer, Violencia familiar, Abuso sexual, Acoso, Otros.
  - Tipo de Atención: Llamada Nextel/Mensaje WhatsApp, Atención psicológica, Atención médica, Asesoría legal, Otros.
- El sistema deberá permitir que el usuario ADMIN configure los códigos y sus representaciones textuales para los campos dependientes `zona`, `sector` y `cuadrante`.
- Todas las operaciones en el sistema deberán ser registradas para efecto de auditoría, capturando los datos indispensables para identificar qué usuario hizo qué y cuándo. Esto significa que cada acción en el sistema tiene un código numérico (o alfanumérico si se considera más óptimo) y una representación o significado textual, que será usada para los registros de auditoría.

## Modelos de datos

A continuación están los modelos de datos que vamos a emplear en esta aplicación. Puedes crear otros modelos auxiliares si los consideras necesarios. Nota que necesitamos campos para registro de fecha y hora de creación de la entidad de datos, el tipo de datos actualmente está como "timestamp", por favor, ajusta el tipo de datos apropiado para el gestor de base de datos, Firebase Firestore, que vamos a emplear.

### Reportes de Incidencias

En la siguiente tabla puedes encontrar el modelo de datos para el Reporte de Incidencia "":

| Campo | Tipo de Dato | Descripción | Requerido |
| :--- | :--- | :--- | :---: |
| `id` | `Integer` | Identificador único del reporte de incidencia. | Sí |
| `estado` | `Integer` | Código del Estado del reporte ("0 = borrador", "1 = entregado"). | Sí |
| `usuarioId` | `Integer` | Identificador del usuario que creó el reporte. | Sí |
| `datetime` | `timestamp` | Fecha y hora de creación del reporte. | Sí |
| `datetimeHechos` | `timestamp` | Fecha y hora de los hechos reportados. | Sí |
| `sexo` | `String` | Sexo o género del solicitante (Sugerencias: masculino, femenino, otro) | Sí |
| `edad` | `Integer` | Edad del solicitante | Sí |
| `lgbtttiqPlus` | `Boolean` | Pertenencia a comunidad LGBTTTIQ+ | No |
| `situacionCalle` | `Boolean` | Se encuentra en situación de calle | No |
| `migrante` | `Boolean` | Es un migrante | No |
| `discapacidad` | `Boolean` | Tiene una discapacidad | No |
| `delito` | `String` | Delito involucrado (Sugerencias: Violencia contra la mujer, Violencia familiar) | Sí |
| `zona` | `Integer` | Código de Zona | Sí |
| `sector` | `Integer` | Código de Sector | Sí |
| `cuadrante` | `Integer` | Código de Cuadrante | Sí |
| `turnoCeiba` | `Integer` | Identificador de Turno Ceiba | Sí |
| `tipoDeAtencion` | `String` | Tipo de Atención (Sugerencias: Llamada Nextel/Mensaje WhatsApp, Atención psicológica) | Sí |
| `tipoDeAccion` | `Integer` | Código de Tipo de Acción ("1=ATOS", "2=Capacitación", "3=Prevención") | Sí |
| `hechosReportados` | `String` | Descripción de los hechos reportados | Sí |
| `accionesRealizadas` | `String` | Descripción de las acciones realizadas | Sí |
| `traslados` | `Integer` | Código de Traslados ("0=Sin Traslado", "1=Con Traslado", "2=No Aplica")  | Sí |
| `observaciones` | `String` | Observaciones adicionales | No |

### Usuarios

En la siguiente tabla puedes encontrar el modelo de datos propuesto para el Usuario:

| Campo | Tipo de Dato | Descripción | Requerido |
| :--- | :--- | :--- | :---: |
| `id` | `Integer` | Identificador único del usuario. | Sí |
| `nombreUsuario` | `String` | Nombre de usuario para el inicio de sesión. | Sí |
| `email` | `String` | Correo electrónico del usuario (debe ser único). | Sí |
| `activo` | `Boolean` | Indica si la cuenta está activa. `true` por defecto. | Sí |
| `datetime` | `timestamp` | Fecha y hora de creación de la cuenta. | Sí |
| `perfil` | `Object` | Objeto que contiene información adicional del perfil. | No |
| `roles` | `Array<String>` | Lista de los roles asignados al usuario. | Sí |

### Registro de Auditoría

En la siguiente tabla puedes encontrar el modelo de datos propuesto para el Registro de Auditoría:

| Campo | Tipo de Dato | Descripción | Requerido |
| :--- | :--- | :--- | :---: |
| `id` | `Integer` | Identificador único del registro de auditoría. | Sí |
| `codigo` | `String` | Identificador del tipo de evento de auditoría. | Sí |
| `idRelacionado` | `integer` | Si existe, ID del registro relacionado. | No |
| `datetime` | `timestamp` | Fecha y hora de creación del registro de auditoría. | Sí |
| `usuarioId` | `Integer` | Identificador del usuario que generó el reporte. | Sí |
| `IP` | `String` | Dirección IP del usuario que generó el reporte. | No |

### Modelos auxiliares

Puedes crear otros modelos de datos auxiliares si los consideras necesarios, por ejemplo, para manejar las listas de sugerencias y las listas de dependencias para los campos `zona`, `sector` y `cuadrante`.

En la siguiente tabla puedes encontrar el modelo de datos propuesto para el Modelo Auxiliar "Zona":

| Campo | Tipo de Dato | Descripción | Requerido |
| :--- | :--- | :--- | :---: |
| `id` | `Integer` | Identificador único de la zona. | Sí |
| `nombre` | `String` | Nombre descriptivo de la zona. | Sí |
| `datetime` | `timestamp` | Fecha y hora de creación del registro. | Sí |
| `usuarioId` | `Integer` | Identificador del usuario que creó el registro. | Sí |
| `activo` | `Boolean` | Indica si la zona está activa. `true` por defecto. | Sí |

En la siguiente tabla puedes encontrar el modelo de datos propuesto para el Modelo Auxiliar "Sector":

| Campo | Tipo de Dato | Descripción | Requerido |
| :--- | :--- | :--- | :---: |
| `id` | `Integer` | Identificador único del sector. | Sí |
| `nombre` | `String` | Nombre descriptivo del sector. | Sí |
| `zonaId` | `Integer` | Identificador de la zona a la que pertenece el sector. | Sí |
| `datetime` | `timestamp` | Fecha y hora de creación del registro. | Sí |
| `usuarioId` | `Integer` | Identificador del usuario que creó el registro. | Sí |
| `activo` | `Boolean` | Indica si el sector está activo. `true` por defecto. | Sí |

En la siguiente tabla puedes encontrar el modelo de datos propuesto para el Modelo Auxiliar "Cuadrante":

| Campo | Tipo de Dato | Descripción | Requerido |
| :--- | :--- | :--- | :---: |
| `id` | `Integer` | Identificador único del cuadrante. | Sí |
| `nombre` | `String` | Nombre descriptivo del cuadrante. | Sí |
| `sectorId` | `Integer` | Identificador del sector al que pertenece el cuadrante. | Sí |
| `datetime` | `timestamp` | Fecha y hora de creación del registro. | Sí |
| `usuarioId` | `Integer` | Identificador del usuario que creó el registro. | Sí |
| `activo` | `Boolean` | Indica si el cuadrante está activo. `true` por defecto. | Sí |

## Requisitos no funcionales

- Sigue mejores prácticas de seguridad para aplicaciones web.
- La interface web de la aplicación debe ser accesible y responsiva, es decir, adaptable a las dimensiones y capacidades del dispositivo desde donde se vea.
- El control de usuarios debe seguir la lógica de role-based access control (RBAC), donde cada usuario ve solo lo que le corresponde.
- La base de datos debe ser PostgreSQL versión 18.
- La aplicación web deberá programarse en ASP.NET Core versión 10 y Blazor Server, y deberá seguir la arquitectura de Monolito Modular y Clean Architecture.
- La autenticación y autorización de usuarios deberá implementarse usando ASP.NET Identity.
- Esta web app deberá desplegarse para su ejecución en un servidor local, su sistema operativo es Fedora Linux Server versión 42.
- Prepara lo necesario para la continua entrega y el despliegue automático, considerando que el repositorio remoto está alojado en GitHub y la rama principal es main.
- Considera el uso de contenedores Docker para el despliegue, así como la orquestación con Docker Compose u otra mejor tecnología si lo consideras necesario.
- Considera la documentación específica para las tareas de desarrollo, pruebas y despliegue.
- Considera el uso del framework .NET ASPIRE para facilitar las tareas de desarrollo, pruebas y despliegue.
- La aplicación debe ser desarrollada en español, tanto en la interface de usuario como en la documentación.
- Considera que el equipo de desarrollo es de tamaño pequeño, por lo que las soluciones deben ser simples y prácticas.
- La aplicación debe ser escalable para manejar un crecimiento futuro en la cantidad de usuarios y datos.
- La aplicación debe ser mantenible, con un código limpio y bien documentado para facilitar futuras modificaciones y mejoras.
- La aplicación debe ser eficiente en términos de rendimiento, minimizando los tiempos de carga y optimizando el uso de recursos del sistema.
- La aplicación debe ser confiable, con mecanismos para manejar errores y asegurar la integridad de los datos.
- La aplicación debe cumplir con las normativas y regulaciones aplicables en materia de protección de datos y privacidad.
- La aplicación debe ser compatible con los navegadores web más comunes, incluyendo Chrome, Firefox, Edge y Safari.
- La aplicación debe ser desarrollada siguiendo una metodología ágil, con iteraciones cortas y entregas frecuentes de funcionalidades.
- La aplicación debe incluir pruebas automatizadas para asegurar la calidad del código y la funcionalidad del sistema.
- La aplicación debe incluir documentación técnica y de usuario para facilitar su uso y mantenimiento.
- El código fuente de la aplicación debe ser gestionado en un sistema de control de versiones, preferentemente Git, con un repositorio remoto alojado en GitHub.
- La aplicación debe incluir mecanismos de monitoreo y logging para facilitar la detección y resolución de problemas en producción.
- El código debe seguir las convenciones y estándares de codificación recomendados para ASP.NET Core y Blazor Server.
- El código fuente debe estar estructurado de manera modular, siguiendo los principios de Clean Architecture para facilitar la mantenibilidad y escalabilidad del sistema.
- El código fuente debe incluir comentarios y documentación inline para facilitar la comprensión del código por parte de otros desarrolladores.
- La aplicación debe incluir mecanismos de seguridad para proteger contra ataques comunes en aplicaciones web, como inyección SQL, cross-site scripting (XSS) y cross-site request forgery (CSRF).
- La aplicación debe incluir mecanismos de validación de datos tanto en el cliente como en el servidor para asegurar la integridad y consistencia de los datos ingresados por los usuarios.
- La aplicación debe incluir mecanismos de backup y recuperación de datos para proteger contra la pérdida de información, estos mecanismos pueden ser scripts que hagan copias de seguridad periódicas y restauraciones automáticas en caso de fallo. Toma en cuenta que esta web app se ejecutará en un servidor local con Fedora Linux Server versión 42, y dicho servidor posee dos unidades de almacenamiento, en la principal estará instalado el sistema operativo, y programas como esta webapp y el gestor de base de datos, mientras que en la segunda unidad de almacenamiento se pueden guardar copias de respaldo de la base de datos. Elabora una estrategia de respaldos adecuada.
- La aplicación debe incluir mecanismos de gestión de sesiones para asegurar que los usuarios permanezcan autenticados durante su uso de la aplicación.
- Durante el desarrollo de la aplicación, es decir, durante las fases de implementación, emplea el MCP Context7 para obtener asistencia en la generación de código, resolución de problemas y optimización del desarrollo.
