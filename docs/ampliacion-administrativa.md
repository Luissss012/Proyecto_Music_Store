# Ampliación administrativa

Implementada por solicitud del usuario después de cerrar el Sprint 5. Es una entrega adicional anterior al Sprint 6. Aceptada por el usuario el 18 de octubre de 2026 tras confirmar que todas las pruebas funcionan; no sustituye el alcance del carrito, checkout o integración final.

## Uso

Iniciar sesión y abrir Administración. El propietario ve Álbumes, Artistas, Géneros y Usuarios y permisos. Los administradores normales solo ven las tres áreas del catálogo.

- Los artistas y géneros se crean y editan desde sus listas. El formulario del álbum ofrece accesos en otra pestaña; recargarlo después de guardar el nuevo artista o género, antes de completar el álbum.
- No se permite eliminar un artista o género que tenga álbumes. Editarlo conserva sus relaciones; para eliminarlo se deben reasignar sus álbumes primero.
- Las cuentas se registran como usuarios normales. Solo el propietario puede buscar otra cuenta y asignarle o retirarle administración, confirmando con su propia contraseña. La contraseña no se guarda en el historial ni se devuelve en los formularios.
- La cuenta afectada debe volver a iniciar sesión después de un cambio de rol. El retiro invalida también una sesión que ya estuviera abierta.
- No se permite cambiar el propio rol ni modificar el rol del propietario desde la web. El rol Owner solo se aprovisiona mediante la herramienta local; conserva también Administrator para operar el catálogo. Los administradores normales no pueden acceder a Usuarios, al historial ni a sus POST, aunque conozcan la URL. Se conserva al menos un administrador y las operaciones concurrentes se serializan mediante un bloqueo transaccional de SQL Server, con comprobación actual del rol del operador dentro de la transacción.
- Usuarios incluye historial de los últimos 100 cambios web de permisos, con administrador, destinatario, acción y hora UTC. La herramienta local sigue disponible para el primer administrador o recuperación; sus promociones no aparecen en ese historial web.

## Protecciones

Todas las rutas administrativas requieren Administrator en el servidor. Las de usuarios, historial y permisos exigen además Owner. La misma autorización se comprueba otra vez dentro de la transacción de cambio de rol. Los cambios solo se aceptan mediante POST con token antifalsificación. Se utilizan modelos de formulario con campos permitidos explícitos, validación de longitudes, identificadores y relaciones. Razor codifica la salida y SQL/EF parametrizan las consultas.

Los nombres se recortan y normalizan en el servidor. Los índices únicos de SQL Server evitan duplicados incluso ante solicitudes simultáneas, siguiendo la intercalación de la base. Los nombres admiten hasta 160 caracteres y las descripciones hasta 1000.

Las claves foráneas de álbumes ya no tienen borrado en cascada hacia artistas o géneros. Las columnas RowVersion en Album, Artist y Genre rechazan ediciones o eliminaciones basadas en versiones antiguas con respuesta 409. La comparación y escritura se ejecutan en una sola sentencia SQL parametrizada; una versión antigua devuelve cero filas modificadas, sin lanzar DbUpdateConcurrencyException. De ese modo el conflicto esperado no depende de excepciones que puedan detener el depurador de Visual Studio. Los identificadores inexistentes o inválidos se rechazan; los errores conocidos muestran mensajes sin detalles internos.

Los álbumes validan que existan artista y género, que el título no sea solo espacios y que la portada use HTTP/HTTPS o una ruta local. Se conservan las validaciones de precio existentes. La creación no admite un AlbumId enviado por el cliente y la edición comprueba que el identificador de la ruta coincida con el formulario.

La confirmación de permisos admite cinco intentos incorrectos antes del bloqueo de 15 minutos de Identity. El cambio de rol y su historial se guardan en la misma transacción; el sello de seguridad evita reutilizar un formulario de permisos anterior.

Estas comprobaciones son evidencia de los casos ejecutados, no una garantía de ausencia de vulnerabilidades. El proyecto sigue siendo un entorno local de desarrollo; un despliegue debe exigir HTTPS y configurar acceso a SQL con los permisos necesarios.

## Base de datos y respaldo

Antes de cambiar el esquema se crearon y verificaron mediante CHECKSUM y RESTORE VERIFYONLY dos respaldos COPY_ONLY en la carpeta Backup de SQLEXPRESS:

- MusicStore_before_admin_extension_20261017.bak.
- MusicStoreAccounts_before_admin_extension_20261017.bak.

No se hizo una restauración de ensayo. Los nombres de los respaldos se conservaron tal como fueron creados.

Se aplicó la migración EF6 `202610180555028_AdminCatalogIntegrity`, que agrega índices únicos, longitudes, campos obligatorios, RowVersion y claves foráneas sin cascada. La migración comprueba los datos existentes y aborta si precisaran truncamiento, sustitución o combinación.

En MusicStoreAccounts se ejecutó `tools/Prepare-AdminAudit.sql`, que crea el historial de forma idempotente sin alterar cuentas o roles.

En otra copia del proyecto, después de restaurar paquetes y compilar, aplicar la migración con la configuración MusicStore.Migrations.Configuration y ejecutar Prepare-AdminAudit.sql sobre la base configurada en AccountsConnection, después de preparar respaldos. No arrancar con el nuevo modelo sobre un catálogo sin migrar. El esquema actual y el código deben mantenerse juntos.

## Pruebas realizadas

- Compilación de C# y vistas Razor correcta con MSBuild de Visual Studio Community 18.
- 95 comprobaciones de `tools/Test-AdminExtension.ps1` pasaron con cuentas y catálogo temporales: permisos GET/POST, campos extra al registrarse, antifalsificación, validación, payload HTML, duplicados, CRUD, búsqueda con texto parecido a SQL, relaciones, versiones antiguas, promoción, revocación, sesión invalidada e historial.
- Dos solicitudes simultáneas de creación del mismo artista guardaron exactamente uno. Dos envíos simultáneos del propietario para retirar el mismo permiso lo aplicaron una sola vez y conservaron al propietario. Se comprobó además el bloqueo de administradores normales en GET/POST de usuarios y la protección del propietario en el servicio.
- Las 15 pruebas de regresión de `tools/Test-Sprint5.ps1` también pasaron.
- Revisión visual del panel, lista y formulario de artistas y menú móvil a 390 px. Se corrigió el desbordamiento con correos largos y se verificó que el menú desplegado no desborda horizontalmente.
- Los álbumes originales se compararon antes y después de las pruebas y se conservaron exactamente. Estado final: tres álbumes, dos artistas, dos géneros y las tres migraciones aplicadas.
- Se eliminaron todas las cuentas y registros temporales, incluida la cuenta de revisión visual. Los registros de historial creados por las cuentas de prueba también se limpiaron; las cuentas originales no fueron modificadas.

## Pruebas manuales en orden

En Visual Studio recargar todo si detecta cambios, ejecutar F5 e iniciar sesión con el propietario. Si conservas una página abierta desde antes, recárgala; los formularios anteriores ya no incluyen la versión requerida.

| Paso | Acción | Resultado esperado |
|---|---|---|
| 1 | Abrir Administración | Propietario: cuatro áreas; administrador normal: tres áreas del catálogo |
| 2 | Crear artista y género de prueba | Aparecen en sus listas y en el formulario de álbum al recargar |
| 3 | Enviar nombre vacío, espacios, duplicado con distinto uso de mayúsculas o excesivamente largo | No se guarda y muestra un mensaje |
| 4 | Editar nombre de artista y descripción/nombre de género | Cambios visibles |
| 5 | Crear un álbum de prueba usando ambos | Se guarda con las relaciones correctas |
| 6 | Intentar eliminar su artista o género, incluso abriendo la URL directamente | Operación bloqueada y álbum intacto |
| 7 | Abrir el mismo registro en dos pestañas, guardar un cambio en la primera y otro diferente en la segunda | La segunda rechaza la versión antigua y pide recargar |
| 8 | Abrir Usuarios, buscar otra cuenta normal y asignarle administración | Solo el propietario puede hacerlo; solicita su contraseña, registra el cambio y la otra cuenta administra el catálogo tras entrar nuevamente |
| 9 | Desde el propietario, retirar el rol a esa cuenta mientras está conectada en otro navegador | Pierde la sesión administrativa; puede entrar de nuevo como usuario normal |
| 10 | Revisar tu propia fila y el historial | No hay acción para cambiar tu propio rol; aparecen los cambios web de permisos |
| 11 | Con usuario normal, abrir /Admin, /Artists, /Genres y /Users directamente | Acceso denegado |
| 12 | Eliminar solo el álbum de prueba; después su artista y género | Permite eliminarlos y conserva los datos originales |

No retirar permisos a cuentas ajenas a las pruebas ni borrar álbumes originales. Para probar dos cuentas al mismo tiempo, usar navegadores o perfiles separados.

Reportar cada paso como OK o FALLÓ con URL y mensaje, sin contraseñas. Después de la aceptación manual se hará el commit de esta entrega. El siguiente sprint continúa siendo el 6, carrito y AJAX.

## Corrección tras las pruebas manuales

El usuario reportó un cierre al probar dos pestañas y cuestionó la autoridad equivalente entre administradores. No se confirmó un fallo del proceso mediante los registros disponibles; la detención del depurador al detectar una excepción era una posible causa. Se sustituyó el conflicto esperado por una escritura condicional sin excepción y se verificó que conserva el primer cambio y que el servidor sigue respondiendo. El usuario confirmó posteriormente que todas las pruebas funcionan, incluido este caso, el 18 de octubre de 2026.

El usuario identificó expresamente la cuenta propietaria. Se preparó y verificó el respaldo MusicStoreAccounts_before_owner_20261018.bak antes de asignar Owner, sin modificar su contraseña. El propietario debe volver a iniciar sesión. No se transfiere ni se retira Owner desde la interfaz. Para preparar el primer propietario en otro equipo se usa la herramienta local AccountsAdmin.ps1 con Action owner y el correo de una cuenta ya registrada; se rechaza asignar un segundo propietario distinto. Los propietarios temporales de las pruebas se aprovisionaron solo para verificar las defensas y fueron eliminados.

## Aceptación de la entrega

El usuario confirmó en este chat que todas las pruebas funcionan después de corregir la edición desde dos pestañas y separar propietario de administrador. Se registra su aceptación manual, además de las 95 comprobaciones automáticas y la compilación de aplicación y vistas ya verificadas. No se adjuntaron capturas individuales de las pruebas manuales.
