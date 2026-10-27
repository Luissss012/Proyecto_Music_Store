# Sprint 7 Carrito por cuenta y órdenes

Implementado el 27 de octubre de 2026. Compilación, pruebas automáticas y revisión visual correctas. Validaciones finales aceptadas por el usuario el 27 de octubre de 2026, con autorización para guardar el commit.

## Alcance

Se siguieron la planificación, criterios AC-10 a AC-13, y la unidad 9 del tutorial: conservar el carrito al registrarse/iniciar sesión, checkout autenticado, datos válidos, creación de Order y OrderDetail, confirmación y bloqueo de órdenes ajenas. Las referencias se encuentran en plan-y-mejoras.md.

La persistencia tras cerrar y volver a iniciar sesión, solicitada durante el cierre del Sprint 6, queda implementada. La identidad de la cuenta se obtiene de ASP.NET Identity; no se utiliza el correo como propietario ni se aceptan propietarios desde el formulario.

## Carrito guardado

- UserCarts vincula el identificador estable de la cuenta con un identificador aleatorio de carrito, único por cuenta.
- El login combina el carrito anónimo actual con el guardado. Los álbumes coincidentes suman cantidades sin duplicar líneas; los diferentes se agregan.
- El registro conserva los productos de la sesión y la ruta de regreso al checkout. Después del login se asocian a la nueva cuenta, manteniendo el flujo existente de registro seguido de login.
- Las sesiones independientes de una misma cuenta recuperan y comparten el carrito guardado. Se conservan los controles RowVersion para rechazar escrituras antiguas.
- Cerrar sesión solo retira las referencias de esa sesión; no borra los productos guardados. Otra cuenta o un visitante no heredan ese carrito. La expiración de autenticación también separa el carrito anterior del acceso anónimo.
- Se impide cambiar a otra cuenta mediante el formulario de login sin cerrar sesión primero.
- Si combinar superaría 99 unidades de un álbum o 50 álbumes diferentes, el login muestra un error y no modifica ninguno de los carritos. Se deben ajustar los productos actuales antes de reintentar; se puede usar otra ventana privada para ajustar el carrito guardado. No se recortan cantidades ni se descartan productos silenciosamente.

La combinación se realiza en una transacción con bloqueos de aplicación por cuenta y por carrito. Se toma un orden estable de bloqueo cuando hay dos carritos. Se rechaza como origen un carrito ya vinculado a otra cuenta. No hay transferencia de datos desde parámetros de propietario o identificador de carrito enviados por el cliente.

Los carritos anónimos de sesiones ya expiradas antes de este sprint no pueden atribuirse retroactivamente a una cuenta. Se conserva su contenido en la base, sin asignarlo por suposiciones.

## Checkout y confirmación

- Continuar compra dirige a Checkout/AddressAndPayment y exige autenticación. El carrito vacío regresa a la vista del carrito.
- La demostración usa el código FREE, como el tutorial. No solicita tarjetas ni procesa cobros reales. El total registrado representa el catálogo de demostración.
- El formulario valida nombre, apellido, dirección, ciudad, país, código postal, teléfono y correo. Los errores se muestran en pantalla y no generan órdenes ni vacían el carrito.
- Se muestran los productos, cantidades, precios y total antes de confirmar. El precio, total, usuario, fecha y líneas de orden se calculan en el servidor.
- Un ticket protegido con MachineKey vincula usuario, carrito, revisión, identificador aleatorio y expiración de 20 minutos. No se aceptan tickets ajenos, manipulados o expirados. Si cambian productos o precios, se exige revisar el resumen actualizado y confirmar de nuevo.
- Una transacción guarda orden y detalles y vacía el carrito. Un fallo anterior al commit revierte toda la operación. Se bloquea el carrito durante el checkout y se mantienen estables los precios leídos hasta guardar.
- El identificador de confirmación es único en SQL Server. Reenviar el mismo formulario devuelve la orden original; dos formularios distintos no pueden comprar dos veces el mismo estado del carrito. Reenviar una compra ya terminada tampoco borra productos agregados después.
- Las líneas guardan título, artista, cantidad y precio de la compra. Cambiar el catálogo posteriormente no altera la evidencia histórica.
- Mis pedidos lista solo las órdenes de la cuenta actual; Complete filtra por usuario e identificador. Una orden ajena y una inexistente reciben 404. Los administradores no tienen una excepción que les permita leer órdenes ajenas.

Se conservan POST y tokens antifalsificación, consultas parametrizadas, salida Razor codificada y respuestas sin caché. Los errores de compra no exponen detalles internos ni contraseñas. Los datos de contacto solo aparecen en el detalle de la cuenta propietaria.

## Base de datos y respaldo

Se crearon respaldos COPY_ONLY con CHECKSUM y se verificaron mediante RESTORE VERIFYONLY antes de cambiar el esquema:

- MusicStore_before_sprint7_20261027_01.bak.
- MusicStoreAccounts_before_sprint7_20261027_01.bak.

Se encuentran en la carpeta Backup de SQLEXPRESS. Esta verificación no equivale a una restauración de ensayo.

Se aplicó la migración 202610272226288_Sprint7CheckoutAndSavedCarts: tabla UserCarts, índice por usuario para órdenes, token único de checkout y textos históricos de cada detalle. La migración rellena valores históricos de órdenes existentes antes de exigir columnas no nulas, sin borrar las órdenes anteriores. La relación con Identity es lógica porque las cuentas están en otra base de datos.

En otra copia del proyecto se deben restaurar paquetes, compilar y aplicar esta migración con MusicStore.Migrations.Configuration después de preparar respaldos. El esquema y el modelo deben permanecer juntos.

## Verificación

- C# y todas las vistas Razor compilan.
- tools/Test-Sprint7.ps1: 60 comprobaciones correctas. Incluyen persistencia, registro/login, separación entre cuentas, combinación, límites sin pérdida, sesión nueva o autenticación expirada, validaciones, antifalsificación, HTML manipulado, tickets ajenos, precios cambiados, reenvíos, dos envíos simultáneos, dos formularios distintos y protección de órdenes incluso frente a un administrador.
- Una falla SQL temporal y exclusiva de la cuenta de prueba comprobó el rollback completo de la orden y la conservación del carrito. El disparador de prueba se eliminó al terminar.
- tools/Test-Sprint5.ps1: 15 pruebas correctas; tools/Test-Sprint6.ps1: 45; tools/Test-AdminExtension.ps1: 95. Sus limpiezas se adaptaron para eliminar también los vínculos de carrito de las cuentas temporales.
- Total: 215 comprobaciones automáticas correctas.
- En navegador se recuperó el carrito de una cuenta temporal, se verificaron mensajes de formulario vacío, se completó la orden FREE y se consultaron confirmación e historial sin errores de JavaScript.
- Se compararon y conservaron catálogo y órdenes anteriores. Se limpiaron usuarios, carritos, órdenes, detalles, álbumes y disparadores temporales, incluida la cuenta visual. Quedan tres álbumes originales y ninguna orden de prueba.

## Validaciones finales del usuario

Recargar todo en Visual Studio si avisa de cambios y ejecutar F5. Usar dos cuentas de prueba en navegadores/perfiles separados. Las compras son de demostración y usan FREE; no introducir tarjetas.

| Paso | Validación | Resultado esperado |
|---|---|---|
| 1 | Agregar álbumes sin sesión y continuar compra | Solicita login; el carrito se conserva al entrar o tras registro y login |
| 2 | Cerrar sesión y volver a entrar con la misma cuenta | Recupera productos y cantidades |
| 3 | Cerrar sesión, agregar productos como visitante y volver a entrar en la cuenta con carrito guardado | Combina ambos y suma los álbumes repetidos sin duplicar líneas |
| 4 | Entrar con otra cuenta o navegador independiente | No ve el carrito ni las órdenes de la primera cuenta |
| 5 | Enviar checkout vacío, correo/teléfono inválidos o código distinto de FREE | Muestra errores; no crea orden ni pierde productos |
| 6 | Completar datos válidos y confirmar con FREE | Muestra número de orden y detalles correctos; vacía solo el carrito comprado |
| 7 | Actualizar la confirmación o reenviar el formulario anterior | No crea otra orden; conserva el mismo número |
| 8 | Dejar el checkout abierto, cambiar una cantidad en otra pestaña y después confirmar el formulario antiguo | Pide revisar los cambios; no crea una orden con datos antiguos |
| 9 | Abrir Mis pedidos después de cerrar y volver a iniciar sesión | La orden sigue disponible con sus cantidades, precio y total históricos |
| 10 | Con la segunda cuenta, abrir la URL de confirmación de la primera | Rechaza el acceso, también si esa segunda cuenta es administradora |
| 11 | Intentar continuar compra con carrito vacío | Regresa al carrito y no crea órdenes |

La prueba adicional de límites puede hacerse dejando 99 unidades guardadas y agregando una del mismo álbum como visitante: el login debe explicar el conflicto y conservar ambos carritos hasta resolverlo. El límite de 50 diferentes ya fue comprobado con álbumes temporales.

El usuario confirmó que todas las validaciones funcionan y autorizó el commit. La publicación en GitHub requiere una instrucción posterior.

## Aceptación manual

El usuario confirmó en este chat que todo funcionó completamente y solicitó guardar el Sprint 7 antes de realizar correcciones adicionales de presentación en la confirmación del pedido. Esas correcciones se mantendrán separadas de esta entrega aceptada.
