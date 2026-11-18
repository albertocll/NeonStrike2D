# Formulario de seguridad de los datos (Google Play Console)

Respuestas para **Contenido de la aplicación → Seguridad de los datos**, sacadas de lo que guarda
el backend (`Backend/NeonStrike2D.Backend.Api/Models`) y de lo que hace el cliente. Tienen que
coincidir con la [política de privacidad](../politica-privacidad.md) publicada en
https://neonstrike-game.netlify.app/politica-privacidad.html.

Revisado el 18/11/2026 sobre master. Si se añaden anuncios, analítica, inicio de sesión con
Google, chat u otro servicio, hay que repasar este formulario y la política antes de publicar.

## Antes de rellenarlo: decisión pendiente

En `ProjectSettings/ProjectSettings.asset` está activado `submitAnalytics: 1`. Con esa opción, el
motor de Unity envía a Unity estadísticas del dispositivo (modelo, sistema, GPU...). Hay dos
caminos, y lo decide Alberto:

- **Desactivarla** (si la licencia de Unity lo permite) y rellenar el formulario tal cual está
  abajo.
- **Dejarla** y declarar además *Información y rendimiento de la aplicación → Diagnósticos*
  como recogida, compartida con Unity, con finalidad *Analítica*, y nombrarlo en la política.

El resto de servicios de Unity están apagados (`UnityConnectSettings.asset`: todo
`m_Enabled: 0`) y el informe de fallos de Unity también (`enableCrashReportAPI: 0`).

## 1. Recogida de datos y seguridad

| Pregunta | Respuesta |
|---|---|
| ¿Tu aplicación recoge o comparte alguno de los tipos de datos de usuario obligatorios? | **Sí** |
| ¿Todos los datos de usuario que recoge tu aplicación están cifrados en tránsito? | **Sí** (HTTPS y WSS hacia Railway) |
| ¿Qué métodos de creación de cuenta admite tu aplicación? | **Nombre de usuario y contraseña** (con email). No hay inicio de sesión con terceros |
| URL para pedir que se elimine la cuenta | https://neonstrike-game.netlify.app/borrar-cuenta.html |
| ¿Ofreces una forma de pedir que se eliminen algunos datos o todos sin eliminar la cuenta? | **No** (solo se borra la cuenta entera) |

Nota: las respuestas de cifrado y borrado dependen de que la web esté desplegada en Netlify
con `borrar-cuenta.html` antes de enviar el formulario.

## 2. Tipos de datos

Solo se marcan los de esta tabla. Ninguno se **comparte**: Railway y Netlify son proveedores
que tratan los datos en nombre del juego, y Google no lo considera compartir.

| Categoría → tipo | ¿Se recoge? | ¿Se comparte? | ¿Efímero? | ¿Obligatorio u opcional? | Finalidades |
|---|---|---|---|---|---|
| Información personal → **Dirección de correo electrónico** | Sí | No | No | Opcional (se puede jugar como invitado) | Funcionalidad de la aplicación; Gestión de la cuenta |
| Información personal → **ID de usuario** (nombre de usuario e id interno) | Sí | No | No | Opcional | Funcionalidad de la aplicación; Gestión de la cuenta |
| Actividad en la aplicación → **Otras acciones** (resultados de partida, partidas cooperativas y amistades) | Sí | No | No | Opcional | Funcionalidad de la aplicación |

Qué hay detrás de cada fila (para justificarlo si Google pregunta):

- **Email**: `User.Email`. Sirve para iniciar sesión. Nunca se muestra a otros jugadores.
- **ID de usuario**: `User.Username` y `User.Id`, más la fecha de alta (`User.CreatedAt`). El
  nombre de usuario sale en el ranking (los 10 mejores) y en las listas de amigos.
- **Otras acciones**:
  - `GameResult`: puntuación, rondas ganadas y perdidas, si ganó y fecha.
  - `GameSession`: sala, los dos jugadores, inicio, fin, ganador y estado.
  - `Friendship`: quién envía, quién recibe, estado y fecha.

## 3. Lo que no hay que declarar, y por qué

| Dato | Motivo |
|---|---|
| Contraseña | Solo se guarda su hash (`User.PasswordHash`). Google no la cuenta como tipo de dato aparte de la cuenta |
| Estado de conexión (en línea o no) | Solo vive en la memoria del servidor mientras el jugador está conectado; no se guarda |
| Posición de la nave en cooperativo | Se reenvía en el momento al otro jugador y no se guarda |
| Email y token de sesión guardados con "recordar" | Se quedan en el dispositivo; no se envían al desarrollador como dato nuevo |
| Ajustes (idioma, volumen, controles) | Solo en el dispositivo (`PlayerPrefs`) |
| Ubicación, contactos, fotos, audio, archivos, calendario, salud, finanzas, mensajes, historial web, apps instaladas | El juego no accede a nada de esto |
| ID de dispositivo o de publicidad | No se lee ningún identificador del dispositivo (ver la decisión pendiente sobre `submitAnalytics`) |
| Dirección IP | El backend no la guarda. Railway y Netlify pueden tenerla en sus registros por seguridad, como cualquier servidor, y eso ya lo explica la política |
| Información de fallos y diagnóstico | Desactivados, salvo lo dicho sobre `submitAnalytics` |

## 4. Otros apartados de Contenido de la aplicación relacionados

- **Política de privacidad**: https://neonstrike-game.netlify.app/politica-privacidad.html
- **Anuncios**: la aplicación **no** contiene anuncios.
- **Acceso a la aplicación**: todo el juego se puede probar como invitado. Si Google pide una
  cuenta de prueba para el modo con cuenta y el cooperativo, crear una y darla en este apartado.
- **Público objetivo**: mayores de 14 años como mínimo (lo dicen la política y los términos).
  No marcar franjas de menores de 13 para no entrar en el programa Familias.
