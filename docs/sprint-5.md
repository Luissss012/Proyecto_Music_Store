# Sprint 5 Autenticación y roles

Implementación aceptada por el usuario: confirmó que todas las pruebas manuales funcionaron correctamente. El cierre incluye las comprobaciones automáticas y la aceptación manual reportada.

## Preservación de datos

- Base inicial: commit 2c9c42b del Sprint 4, sin cambios pendientes.
- Respaldo COPY_ONLY con CHECKSUM: C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\MusicStore_before_sprint5_20261017_01.bak.
- RESTORE VERIFYONLY WITH CHECKSUM terminó correctamente; no se realizó una restauración de ensayo.
- Se desactivó DropCreateDatabaseIfModelChanges. El catálogo mantiene sus tres álbumes y sus dos migraciones anteriores.
- Las cuentas usan la base separada MusicStoreAccounts, en la misma instancia SQLEXPRESS, con autenticación de Windows. Esto evita alterar el esquema del catálogo. Los sprints posteriores deben usar el identificador estable de Identity para relacionar usuarios, carrito y órdenes.
- La base de cuentas se creó con CreateDatabaseIfNotExists. No se recrea una base existente cuando cambia el modelo; futuros cambios requerirán migraciones explícitas.

## Implementación

- ASP.NET Identity 2.2.4 y cookies OWIN 4.2.3.
- Registro con correo único y rol Usuario, login y logout mediante POST.
- Contraseñas procesadas por Identity: mínimo 10 caracteres, mayúscula, minúscula, número y símbolo; máximo 128. No hay contraseñas en el repositorio.
- Bloqueo de 15 minutos después de cinco intentos fallidos.
- Todas las acciones de StoreManager exigen Administrator. Visitantes redirigidos a login y usuarios normales reciben 403.
- Protección antifalsificación en todos los POST de cuentas; solicitudes inválidas reciben 400.
- Retornos tras login limitados a URLs locales.
- Cookie HttpOnly y SameSite Lax, de 30 minutos, sin persistencia. En HTTPS se marca Secure; el despliegue debe exigir HTTPS.
- Validación del sello de seguridad y actualización de roles en cada solicitud autenticada.
- Navegación con cuentas, identidad, cierre de sesión y administración según rol.
- La promoción a administrador se hace mediante herramienta local autorizada; no existe un endpoint público para conceder roles.

## Preparación en otra copia

Restaurar NuGet, compilar la solución y ejecutar desde la raíz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\AccountsAdmin.ps1 -Action setup
```

Requiere acceso de Windows a SQL Server y permiso para crear MusicStoreAccounts. Ya se ejecutó en el equipo actual.

## Validación automática

La aplicación y sus vistas Razor compilan con MSBuild de Visual Studio Community 18. Las 15 comprobaciones de tools/Test-Sprint5.ps1 pasaron contra IIS Express en localhost:54834:

1. Visitante redirigido a login desde administración.
2. Registro inválido rechazado.
3. Registro válido.
4. Correo duplicado rechazado.
5. Contraseña incorrecta rechazada.
6. Login válido y retorno externo rechazado.
7. Usuario normal bloqueado en StoreManager.
8. Usuario normal bloqueado en StoreManager/Create.
9. Logout elimina identidad.
10. Administración protegida después del logout.
11. Administrador accede a StoreManager.
12. Administrador accede al formulario Create.
13. Logout sin token antifalsificación rechazado.
14. Logout rechazado conserva la sesión.
15. Cinco intentos fallidos bloquean temporalmente el login incluso con contraseña correcta.

Se eliminó la cuenta temporal al terminar. Se verificó que el catálogo conserva tres álbumes y las migraciones InitialCreate y Sprint4AlbumValidations. El CRUD con escritura del catálogo queda pendiente de validación manual.

## Pruebas manuales en orden

Abrir MusicStore.slnx en Visual Studio y ejecutar con F5. Usar siempre la URL y el protocolo que abra Visual Studio.

| Paso | Acción | Resultado esperado |
|---|---|---|
| 1 | Sin sesión, abrir catálogo y detalle | Álbumes existentes visibles |
| 2 | Abrir /StoreManager y /StoreManager/Create | Solicita login |
| 3 | Registro inválido y después válido con cuenta normal | Rechaza datos inválidos; cuenta válida lleva al login |
| 4 | Intentar registrar el mismo correo | No permite duplicarlo |
| 5 | Login incorrecto una vez y después correcto | Muestra error y después el correo con Cerrar sesión |
| 6 | Con usuario normal, abrir /StoreManager y /StoreManager/Create | Acceso denegado y sin enlace Administración |
| 7 | Cerrar sesión y abrir /StoreManager | Solicita login |
| 8 | Registrar segunda cuenta y promoverla localmente | Cuenta recibe Administrator |
| 9 | Iniciar sesión con segunda cuenta | Enlace Administración y acceso a StoreManager |
| 10 | Crear álbum de prueba, editar, consultar y eliminar solo ese álbum | CRUD correcto; originales conservados |
| 11 | Cerrar sesión de administrador y abrir /StoreManager | Solicita login |

Para el paso 8, desde una terminal situada en la carpeta del proyecto:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\AccountsAdmin.ps1 -Action promote
```

La herramienta solicita el correo de la segunda cuenta ya registrada, sin pedir contraseña. Después de promover, cerrar sesión y volver a entrar.

Reportar cada paso como OK o FALLÓ. Si falla, indicar paso, URL y mensaje sin contraseñas. AC-06 y AC-07 aceptados según el reporte del usuario en este chat. Las pruebas manuales fueron realizadas por el usuario; no se adjuntaron capturas individuales de cada caso.
