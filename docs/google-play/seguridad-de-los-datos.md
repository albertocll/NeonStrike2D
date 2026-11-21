# Formulario de seguridad de los datos (Google Play Console)

Respuestas para **Contenido de la aplicación → Seguridad de los datos**, sacadas de lo que guarda
el backend (`Backend/NeonStrike2D.Backend.Api/Models`) y de lo que hace el cliente. Tienen que
coincidir con la [política de privacidad](../politica-privacidad.md) publicada en
https://neonstrike-game.netlify.app/politica-privacidad.html.

Revisado el 18/11/2026 sobre master. Si se añaden anuncios, analítica, inicio de sesión con
Google, chat u otro servicio, hay que repasar este formulario y la política antes de publicar.

## Analítica del motor de Unity: desactivada

`submitAnalytics` está a `0` en `ProjectSettings/ProjectSettings.asset`, así que el motor no envía
a Unity estadísticas del dispositivo y no hay que declararlas. El resto de servicios de Unity
también están apagados (`UnityConnectSettings.asset`: todo `m_Enabled: 0`), igual que el informe
de fallos de Unity (`enableCrashReportAPI: 0`).

Comprobar antes de cada AAB: el proyecto usa Unity Personal, y si en algún momento el editor
vuelve a poner `submitAnalytics: 1`, hay que desactivarlo otra vez o declarar *Información y
rendimiento de la aplicación → Diagnósticos* (recogida y compartida con Unity, finalidad
*Analítica*) y nombrarlo en la política de privacidad.

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
| ID de dispositivo o de publicidad | No se lee ningún identificador del dispositivo |
| Dirección IP | El backend no la guarda. Railway y Netlify pueden tenerla en sus registros por seguridad, como cualquier servidor, y eso ya lo explica la política |
| Información de fallos y diagnóstico | Desactivados (`enableCrashReportAPI: 0` y `submitAnalytics: 0`) |

## 4. Otros apartados de Contenido de la aplicación

- **Política de privacidad**: https://neonstrike-game.netlify.app/politica-privacidad.html
- **Acceso a la aplicación**: todo el juego se puede probar como invitado. Si Google pide una
  cuenta de prueba para el modo con cuenta y el cooperativo, crear una y darla en este apartado.
- Público objetivo, anuncios y compras: ver
  [publico-objetivo-anuncios-y-compras.md](publico-objetivo-anuncios-y-compras.md).
- Clasificación de contenido: ver [clasificacion-de-contenido.md](clasificacion-de-contenido.md).
