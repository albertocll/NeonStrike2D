# Público objetivo, anuncios y compras

Respuestas para los apartados de **Contenido de la aplicación** de la Play Console que no son ni
la clasificación ni la seguridad de los datos.

## Público objetivo y contenido

| Pregunta | Respuesta |
|---|---|
| Franjas de edad del público objetivo | **13-15, 16-17 y 18 o más**. No marcar ninguna franja de menores de 13 |
| ¿La aplicación podría atraer de forma no intencionada a niños? | **No**: estilo neón de ciencia ficción, sin personajes infantiles, con cuentas y juego online |
| ¿Forma parte del programa Familias? | **No** |

Por qué estas franjas:

- La política de privacidad y los términos fijan **14 años** para crear cuenta, o menos con
  permiso de padres o tutores. Marcar 13-15 es coherente con eso.
- Si se marca cualquier franja de menores de 13, Google aplica la política de Familias:
  revisión adicional, requisitos de anuncios y de datos más estrictos, y la cuenta con email
  dejaría de valer tal como está.
- Es una recomendación: si Alberto prefiere un público más adulto, basta con marcar solo
  16-17 y 18 o más. En ese caso conviene subir también la edad mínima de la política y de los
  términos a 16.

## Anuncios

| Pregunta | Respuesta |
|---|---|
| ¿Tu aplicación contiene anuncios? | **No, mi aplicación no contiene anuncios** |

No hay ningún SDK de anuncios en `Packages/manifest.json` (tampoco Unity Ads) y los servicios
de Unity están apagados.

## Compras y precio

| Apartado | Respuesta |
|---|---|
| Precio de la aplicación | **Gratuita** (en Google Play, una app gratuita no puede pasar después a ser de pago) |
| Compras en la aplicación | **No** hay: no se usa Google Play Billing ni Unity IAP |
| Pagos fuera de Google Play | **No** |

Lo dicen también la descripción larga de `docs/ficha-google-play.md` («Sin anuncios y sin compras
dentro de la aplicación») y los términos de uso. Si algún día se añaden compras o anuncios,
hay que cambiar estos tres sitios y los formularios de clasificación y de seguridad de los datos.

## Otros apartados que pide la consola

| Apartado | Respuesta |
|---|---|
| Aplicación de noticias | No |
| Aplicación de rastreo de contactos o estado de salud (COVID-19) | No |
| Aplicación gubernamental | No |
| Funciones financieras | No |
| Salud | No |
| Acceso a la aplicación | Todo se puede probar como invitado; si Google lo pide, darle una cuenta de prueba |
