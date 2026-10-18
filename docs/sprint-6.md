# Sprint 6 Carrito y AJAX

Implementado, verificado automáticamente y aceptado por el usuario el 18 de octubre de 2026. El usuario autorizó guardar el commit después de completar sus pruebas manuales.

## Alcance y referencias

La planificación ubica Cart, Order, OrderDetail, ViewModels, ShoppingCartController y AJAX en el Sprint 6; sus criterios son AC-08 (agregar, consultar, modificar/eliminar y calcular total) y AC-09 (actualizaciones sin recarga completa). La unidad 8 del tutorial sirve como referencia funcional. Las ubicaciones de ambos archivos están registradas en plan-y-mejoras.md.

El Sprint 7 implementará checkout, órdenes, asociación del carrito con cuentas y protección de órdenes. El Sprint 8 incluye el resumen global del carrito y la integración final. En esta entrega hay un enlace al carrito en la navegación; todavía no hay checkout ni un contador global.

## Comportamiento

- Visitantes pueden agregar desde el detalle del álbum sin registrarse. Se muestran título, artista y precio para decidir antes de agregar.
- Agregar otra vez el mismo álbum aumenta su cantidad en una sola línea.
- Cada línea admite cantidades enteras de 1 a 99; un carrito puede tener hasta 50 álbumes diferentes.
- El carrito muestra precio unitario, cantidad, subtotal, unidades y total. Los precios provienen del catálogo en cada consulta; no se reserva un precio para compra hasta implementar el checkout.
- Actualizar cantidad, eliminar una línea y vaciar funcionan mediante AJAX. Sin JavaScript, los formularios realizan POST y redirección a la vista actualizada.
- Vaciar pide confirmación en el navegador cuando JavaScript está activo y valida la versión completa en el servidor. Eliminar quita la línea completa; para reducir unidades se usa Actualizar.
- El carrito vacío muestra un mensaje y permite seguir comprando.

## Identidad y seguridad

El servidor crea un identificador aleatorio de 32 bytes y lo conserva en ASP.NET Session. No acepta CartId, precios, totales o entidades enviados por el cliente. Las operaciones sobre líneas siempre filtran por la identidad de sesión y por el identificador del registro. Una línea ajena y una inexistente producen el mismo rechazo.

Todos los cambios requieren POST y token antifalsificación. Las cantidades e identificadores se validan en el servidor. Las cantidades y las relaciones también tienen restricciones en SQL Server. El precio se calcula con decimal, sin redondeos binarios de JavaScript.

Los cambios de un mismo carrito se serializan con un bloqueo transaccional de SQL Server. Existe un índice único CartId/AlbumId. La modificación o eliminación exige RowVersion, y vaciar exige una revisión de la lista completa. Los formularios antiguos reciben 409 con el estado actual; no se sobrescriben cambios de otra pestaña ni se usan excepciones para rechazos esperados.

Las respuestas del carrito no se almacenan en caché. Razor codifica los datos de los álbumes; el cliente solo inserta el fragmento del carrito producido por el servidor y no inserta páginas genéricas de error ni scripts recibidos. Ante una respuesta de red incierta, pide recargar para confirmar el estado antes de repetir la operación.

El carrito es temporal y pertenece a la sesión del navegador. Iniciar sesión en el mismo navegador conserva sus productos, pero no se ha implementado la asociación persistente con la cuenta. Cerrar sesión retira la referencia del carrito para que la siguiente cuenta no lo herede. Una sesión distinta o el reinicio/expiración de la sesión no recuperan ese carrito. La limpieza periódica de filas abandonadas no está automatizada en esta entrega.

En el Sprint 7 se debe asociar la cuenta mediante el identificador estable de Identity; no sustituir el identificador aleatorio por un correo ni aceptar propietarios de carrito desde el cliente.

## Base de datos

Se crearon y verificaron respaldos COPY_ONLY con CHECKSUM y RESTORE VERIFYONLY antes de migrar:

- MusicStore_before_sprint6_20261018_01.bak.
- MusicStoreAccounts_before_sprint6_20261018_01.bak.

Están en la carpeta Backup de la instancia SQLEXPRESS. No se realizó restauración de ensayo.

La migración `202610180642380_Sprint6ShoppingCart` agrega Carts, Orders y OrderDetails, sin borrar ni sustituir tablas anteriores. Incluye cantidades entre 1 y 99, identidad de carrito de longitud fija, precios de detalle positivos y totales de orden no negativos. Las relaciones no borran datos en cascada. Un álbum que está en un carrito no puede eliminarse; la administración muestra un rechazo controlado.

Order utiliza UserId, no el correo, para la futura pertenencia de órdenes. La información de dirección y contacto está limitada en longitud. No hay rutas para crear o consultar órdenes en este sprint y sus tablas permanecen vacías.

En otra copia, restaurar paquetes, compilar y aplicar la migración con la configuración MusicStore.Migrations.Configuration después de respaldar la base. Mantener el modelo y el esquema actualizados juntos.

## Verificación realizada

- Compilación de C# y vistas Razor correcta.
- 45 comprobaciones de tools/Test-Sprint6.ps1 correctas: acceso anónimo, no caché, antifalsificación, verbos HTTP, identificadores manipulados, aislamiento entre carritos, precios del servidor, cantidades inválidas, decimales, versiones antiguas, límites, restricciones SQL, formularios sin JavaScript, operaciones simultáneas, login y logout.
- Se verificaron cantidades de 99 y rechazo de la unidad 100, 50 álbumes distintos y rechazo del número 51, y dos agregados simultáneos sin perder incrementos.
- Las 15 pruebas de tools/Test-Sprint5.ps1 y las 95 de tools/Test-AdminExtension.ps1 pasaron con el nuevo esquema.
- En navegador se agregó Rock Album, se actualizó de una a tres unidades y el total pasó de 9.99 a 29.97 mediante AJAX. Se verificó que solo cambió la región del carrito, sin errores de consola; eliminar mostró el carrito vacío. Se limpió ese carrito de revisión.
- Un género inexistente responde 404 en vez de producir una excepción.
- Las pruebas compararon el catálogo original antes y después y lo conservaron exactamente. Se eliminaron los álbumes, cuentas y líneas de carrito temporales.

Estas comprobaciones cubren los casos ejecutados y no sustituyen la aceptación manual del usuario.

## Pruebas manuales en orden

Recargar todo en Visual Studio si avisa de cambios y ejecutar con F5. Usar el protocolo y puerto que abra Visual Studio. Cerrar sesión para empezar como visitante; no reutilizar formularios de antes de la actualización.

| Paso | Acción | Resultado esperado |
|---|---|---|
| 1 | Abrir Carrito sin iniciar sesión | Carrito vacío disponible |
| 2 | Tienda → género → álbum → Agregar al carrito | Una unidad con precio y total correctos |
| 3 | Volver al mismo álbum y agregarlo otra vez | Una línea con dos unidades |
| 4 | Cambiar cantidad a 3 y pulsar Actualizar | Subtotal y total cambian sin recargar toda la página |
| 5 | Agregar otro álbum | El total coincide con la suma de ambos subtotales |
| 6 | Probar cantidades 0, negativas, decimales o mayores de 99 | El navegador o el servidor rechaza; no cambia el carrito |
| 7 | Abrir el carrito en dos pestañas antes de guardar. Actualizar una y después la otra con un valor diferente | La segunda avisa del conflicto, conserva el primer cambio y muestra la lista actual |
| 8 | Abrir otro navegador o perfil privado independiente y entrar al carrito | No ve los productos del primero |
| 9 | Eliminar un producto | Desaparece esa línea; total y unidades se actualizan mediante AJAX |
| 10 | Vaciar el carrito y confirmar | Muestra vacío y permite seguir comprando |
| 11 | Agregar un producto anónimo e iniciar sesión en el mismo navegador | Los productos siguen en esa sesión |
| 12 | Cerrar sesión y volver al carrito | La siguiente cuenta no hereda el carrito anterior |

Como prueba adicional, desactivar JavaScript temporalmente en el navegador: agregar, actualizar y eliminar deben seguir funcionando con recarga normal. No hace falta crear 51 álbumes para la prueba manual; ese límite ya se verificó con datos temporales.

Reportar los pasos como OK o FALLÓ y adjuntar el mensaje o la URL si hay un fallo, sin contraseñas. El usuario confirmó resultados satisfactorios y autorizó el commit del Sprint 6.

## Cierre y observación para el Sprint 7

AC-08 y AC-09 aceptados según el reporte manual del usuario en este chat. Se conserva la evidencia previa de 45 pruebas del carrito, 15 de autenticación y 95 de administración. El usuario señaló que el carrito no se recupera al cerrar y volver a iniciar sesión; se aclaró y aceptó que la persistencia por cuenta, recuperación y combinación del carrito anónimo se implementarán en el Sprint 7. Ese escenario se incorporará a sus pruebas de aceptación.
