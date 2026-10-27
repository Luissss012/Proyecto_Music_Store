# Sprint 8 Navegación diseño y entrega

Implementación del 27 de octubre de 2026. Aceptado por el usuario el 27 de octubre de 2026, con autorización para commit y publicación en GitHub.

## Referencias y alcance

Se consultaron la planificación DOCX y el tutorial PDF indicados en `docs/plan-y-mejoras.md`. La unidad 10 del tutorial (hojas 127 a 135) implementa Cart Summary, Genre Menu, portadas y Top Selling. Se adapta a MVC 5, Identity, EF6 y Bootstrap existentes. La planificación exige AC-14 a AC-18; el ranking usa **SUM(Quantity)** para respetar unidades vendidas, en lugar de contar filas de detalle como el ejemplo antiguo.

| Criterio | Implementación | Evidencia |
| --- | --- | --- |
| AC-14 | Resumen privado del carrito y menú de géneros en el layout mediante acciones hijas; sin caché compartida | Navegación, páginas de cuenta y prueba de AJAX |
| AC-15 | Hasta seis álbumes ordenados por unidades compradas, desempate por AlbumId; novedades separadas | Comparación HTTP contra consulta SQL de cantidades; sin ventas se muestran Recién llegados |
| AC-16 | Diseño editorial crema, carbón y rojo; tarjetas con portadas; menú móvil; tablas desplazables; foco visible y enlace para saltar al contenido | Evidencias en escritorio, móvil a 390 y comprobación de ancho 320 |
| AC-17 | 404 con regreso a tienda, errores generales sin detalles internos, respuestas AJAX del carrito preservadas y validaciones anteriores conservadas | HTTP 404, acciones hijas, regresiones y rollback SQL |
| AC-18 | README, instrucciones Windows y Release, catálogo reproducible, evidencias y guion de demo | Publicación local en `.artifacts/sprint8/site`, pendiente de commit final |

## Diseño y catálogo

Se agregaron 36 álbumes ficticios, 12 artistas ficticios y los géneros que faltaban de una colección de 12 géneros. Son datos de demostración, sin artistas reales ni portadas descargadas. Los SVG son gráficos originales del proyecto; se sirven localmente junto con una portada sustituta. No dependen de APIs, fuentes externas ni licencias de fotografías. Se conservan los álbumes, cuentas, carritos y pedidos previos; los más vendidos reflejan únicamente pedidos existentes, sin crear ventas ficticias.

La importación `tools/Import-DemoCatalog.ps1` lee `tools/demo-catalog.json`, respalda y verifica MusicStore y MusicStoreAccounts, y usa una transacción serializable. Repetirla no duplica título y artista ni modifica filas existentes. Si falla, revierte toda la importación. No modifica el esquema ni restaura respaldos automáticamente.

Respaldos verificados antes de la importación en la carpeta de SQL Server:

- `MusicStore_before_sprint8_20261027_172138_261.bak`
- `MusicStoreAccounts_before_sprint8_20261027_172138_261.bak`

La verificación fue RESTORE VERIFYONLY con CHECKSUM; no se realizó una restauración de ensayo. Los backups no se incluyen en Git ni en el paquete web.

## Protección y comportamiento

- El resumen resuelve el carrito de la sesión/cuenta mediante el servicio existente. No recibe un CartId elegido por el cliente y no se almacena en cachés compartidas.
- El resumen se sincroniza con el fragmento de carrito validado por JavaScript tras actualizar o eliminar. Operaciones sin JavaScript conservan su redirección normal.
- Las acciones de navegación son ChildActionOnly; fallos en sus consultas muestran una alternativa y no revelan excepciones.
- Las portadas admiten rutas locales de Content y HTTPS; Razor codifica títulos, nombres y URLs. Una portada ausente o fallida usa la ilustración local sustituta.
- Los bundles generan URLs con huella de contenido para evitar que una versión anterior de CSS o JavaScript quede en caché tras publicar.
- Fechas de órdenes continúan etiquetadas como UTC; se conserva el contrato anterior. No se agregan pagos reales, audios ni gestión de inventario.

## Validación técnica

Compilación con vistas y publicación Release correctas. Las seis baterías aprobaron 304 comprobaciones: Sprint 5 (15), ampliación administrativa (95), Sprint 6 (45), Sprint 7 (69), validación de checkout (20) y Sprint 8 (60). La copia Release publicada en el puerto temporal 54835 aprobó nuevamente las 60 comprobaciones del Sprint 8 y las 69 del Sprint 7. El servidor temporal de verificación se detuvo al terminar; la aplicación de trabajo permanece en 54834.

Limpieza final verificada en SQL Server: cero cuentas, álbumes y triggers temporales de las pruebas; las tres cuentas previas y los 36 álbumes de demo permanecen. La carpeta `.artifacts/sprint8/` contiene el sitio publicado, `manifest.json` con SHA-256 del binario y `MusicStore-Sprint8-Release.zip`. Se corrigió una comprobación frágil del Sprint 6: acepta `<html lang="es">` mediante `<html\b`, conservando la verificación de redirección sin JavaScript.

Comprobación visual: inicio en escritorio y móvil, navegación móvil expandida, catálogo y login a 320 píxeles. El carrito de prueba pasó de una unidad a dos: resumen superior y fragmento mostraron 2 y 32.98 sin recarga. Después se eliminó ese producto de prueba y se confirmó carrito vacío. No se usaron cuentas personales para las capturas.

Evidencias:

- `evidencias/sprint8-portada.png`: primera pantalla de escritorio.
- `evidencias/sprint8-escritorio.png`: página completa de escritorio.
- `evidencias/sprint8-movil.png`: página completa móvil.

## Versión ejecutable y presentación

La publicación Release es una aplicación ASP.NET para IIS/IIS Express, no un programa de escritorio. Contiene los binarios, vistas y las 37 portadas locales, con debug retirado y errores internos ocultos. Las conexiones usan autenticación integrada; cada equipo requiere permisos y sus bases preparadas. No se empaquetan bases, cuentas ni backups. El esquema actual del catálogo contiene cinco migraciones EF.

Consultar `README.md` para ejecutar y `docs/demo-final.md` para el guion y las ocho validaciones manuales. El usuario confirmó que todo funciona y autorizó el commit y push final el 27 de octubre de 2026.
