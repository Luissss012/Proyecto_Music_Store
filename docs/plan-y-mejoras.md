# Referencias y alcance del proyecto

La planificación establece los sprints y criterios de aceptación. El tutorial es la referencia funcional; su tecnología MVC 3 y Membership se adapta a MVC 5, .NET Framework 4.8, EF6 e Identity.

## Documentos de referencia del usuario

- Planificación: `C:\Users\User\Downloads\Planificacion_Proyecto_MVC_Music_Store(1)(1).docx`.
- Tutorial: `C:\Users\User\Downloads\Proyecto_MVC_Tutorial_Arquitectura_Sistemas_V1(1).pdf`.

Consultar estos documentos antes de cada sprint. Si ya no están disponibles, solicitar su nueva ubicación; no reconstruir requisitos a partir de suposiciones.

## Estado de sprints

- Sprints 1 a 4: implementaciones anteriores registradas en Git.
- Sprint 5: cuentas, roles y StoreManager protegido; comprobaciones automáticas correctas y pruebas manuales aceptadas por el usuario.
- Sprint 6 pendiente: carrito anónimo, cantidades, total y AJAX (AC-08 y AC-09).
- Sprint 7 pendiente: migración del carrito, checkout y órdenes protegidas (AC-10 a AC-13).
- Sprint 8 pendiente: resumen del carrito, menú de géneros, más vendidos, responsive, errores, documentación y presentación (AC-14 a AC-18).

## Comprobación de alcance administrativo

La planificación especifica CRUD de Album en el Sprint 4 y registro/login/logout, roles y protección de StoreManager en el Sprint 5. No especifica CRUD de Artist o Genre ni una interfaz de asignación de administradores en los sprints 6 a 8.

El tutorial utiliza listas de artistas y géneros existentes para crear álbumes (hojas 64 y 65 del PDF; páginas impresas 63 y 64). La unidad 7 configura el rol y su usuario mediante una herramienta externa de configuración ASP.NET (hojas 83 a 86 del PDF), antes de proteger el controlador. No exige una pantalla de gestión de usuarios dentro de la tienda.

La versión original del Sprint 5 promovía administradores con tools/AccountsAdmin.ps1 y solo permitía elegir artistas/géneros existentes. La ampliación administrativa solicitada posteriormente ya agrega gestión web de estas funciones; ver docs/ampliacion-administrativa.md.

## Ampliación administrativa solicitada por el usuario

El usuario autorizó implementar estas mejoras antes del Sprint 6. Están implementadas en una entrega separada del Sprint 5, con comprobaciones automáticas correctas y aceptación manual confirmada por el usuario el 18 de octubre de 2026. La ampliación queda documentada para su revisión por el responsable académico.

1. Gestión de usuarios accesible solo al propietario Owner (que también conserva Administrator): buscar cuentas existentes y asignar o retirar el rol administrativo. Los administradores normales solo gestionan el catálogo y no pueden modificar al propietario. Conservar al menos un administrador y evitar que alguien elimine su propio acceso accidentalmente. Mantener la herramienta local para aprovisionar al primero.
2. Gestión de artistas: crear y editar desde administración; evitar nombres vacíos o duplicados y proteger relaciones con álbumes al eliminar.
3. Gestión de géneros: crear y editar desde administración; validar nombres y proteger relaciones al eliminar.
4. Accesos a estas operaciones desde administración y el formulario de álbumes, con pruebas de autorización, validación e integridad.

Ningún visitante podrá elegir Administrator al registrarse. No mezclar estas mejoras con el commit del Sprint 5 ya validado.
