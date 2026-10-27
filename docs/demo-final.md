# Guion de demostración final

Duración sugerida: 8 a 10 minutos. Iniciar la aplicación con Ctrl+F5. Usar datos ficticios en la compra y cuentas de prueba ya registradas. No mostrar contraseñas, cadenas con claves ni datos personales en capturas o diapositivas.

1. **Inicio y diseño:** mostrar portada, más vendidos por unidades reales, novedades y menú de géneros. Explicar que la colección de demostración es ficticia y las portadas están incluidas localmente.
2. **Catálogo:** recorrer un género, abrir un álbum, comprobar artista, precio y portada. Mostrar el mismo recorrido con la ventana estrecha o herramientas móviles del navegador.
3. **Carrito:** agregar dos álbumes, cambiar una cantidad y comprobar que subtotal, total y resumen superior se actualizan sin recargar. Eliminar una línea y verificar el contador.
4. **Cuenta:** iniciar sesión con un usuario normal. Mostrar que el carrito se conserva al cerrar y volver a entrar con la misma cuenta y que otra cuenta no lo hereda.
5. **Validación:** escribir `Luis123` como nombre y un correo inválido. Confirmar que se explican los errores y los productos siguen en el carrito. Corregir a `José Luis` y completar datos ficticios válidos.
6. **Checkout:** usar `FREE`, confirmar la orden y abrir Mis pedidos. Refrescar la confirmación para mostrar que no duplica la compra. Explicar que no hay pagos reales.
7. **Privacidad:** con otra cuenta, intentar abrir el número de la orden anterior. Debe devolver 404 sin revelar datos de la otra persona.
8. **Administración:** con administrador, mostrar CRUD de álbumes, artistas y géneros. Mostrar que un administrador normal no puede administrar roles. Solo el propietario puede asignarlos o retirarlos; no cambiar roles durante la presentación salvo necesidad.
9. **Errores y cierre:** abrir `/Store/Details/2147483647` para ver un 404 con regreso a la colección. Mostrar documentación, pruebas y commits como evidencia del trabajo por sprints.

## Validaciones finales del usuario

| Orden | Prueba | Resultado esperado |
| --- | --- | --- |
| 1 | Abrir Inicio y recorrer géneros y álbumes | Diseño consistente; portadas o sustituto local; enlaces correctos |
| 2 | Reducir ventana a tamaño móvil y abrir menú | Navegación accesible; sin desplazamiento horizontal de toda la página; tablas desplazan dentro de su contenedor |
| 3 | Agregar álbum y actualizar cantidad en carrito | Resumen superior, unidades y total coinciden sin recargar |
| 4 | Cerrar y abrir sesión con la misma cuenta | Carrito guardado recuperado; otra cuenta permanece aislada |
| 5 | Intentar checkout con nombre con números y luego corregir | Error explicado sin perder productos; con datos válidos y FREE guarda una sola orden |
| 6 | Refrescar orden y verla en Mis pedidos | No duplica; importes e historial correctos |
| 7 | Abrir orden ajena y álbum inexistente | Respuestas controladas sin datos internos |
| 8 | Entrar como administrador y propietario | Accesos y jerarquía de roles conservados |

El usuario confirmó los resultados y autorizó el commit y la publicación del Sprint 8 el 27 de octubre de 2026. No registrar en el repositorio bases o backups con información de usuarios.
