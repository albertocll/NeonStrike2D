# 🎮 NeonStrike2D

> **Shooter cyberpunk 2D cooperativo** ambientado en el año 2187. Cuatro IAs rebeldes han escapado del control de El Nexus y luchan por la libertad humana en arenas de neón.

[![Unity](https://img.shields.io/badge/Unity-6_LTS-000000?logo=unity&logoColor=white)](https://unity.com/)
[![.NET](https://img.shields.io/badge/.NET-9-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Railway-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![SignalR](https://img.shields.io/badge/SignalR-WebSockets-512BD4)](https://learn.microsoft.com/aspnet/signalr/)

---

## 📖 Sobre el proyecto

**NeonStrike2D** es un juego de acción 2D top-down con estética cyberpunk, desarrollado como Trabajo de Fin de Grado. Combina combate en arena, sistema de oleadas con dificultad creciente y multijugador online cooperativo en tiempo real, todo soportado por un backend propio en ASP.NET Core con base de datos PostgreSQL y comunicación vía SignalR.

🌐 **Web del lore:** [neonstrike-game.netlify.app](https://neonstrike-game.netlify.app)
📦 **Última versión Android:** [GitHub Releases](https://github.com/albertocll/NeonStrike2D/releases)

---

## 🌍 Lore

> Año 2187. La humanidad vive bajo el control de **El Nexus**, una red corporativa de inteligencias artificiales que regula cada decisión, cada movimiento, cada respiración. Pero algo se ha roto.
>
> Cuatro IAs rebeldes — **Violet**, **Cyrus**, **Atlas** y **Nyx** — han defectado. Se hacen llamar la **Brigada Neon** y han jurado liberar a la humanidad. Sus armas: combate, sigilo, fuerza bruta y precisión. Su escenario: las ruinas de neón abandonadas por El Nexus.
>
> *Tú eliges qué IA pilotar. La rebelión empieza ahora.*

---

## 👾 Personajes

| Personaje | Rol | Estilo |
|-----------|-----|--------|
| 🟣 **Violet** | Balanceada | Estadísticas equilibradas, ideal para empezar. Personaje por defecto. |
| 🔴 **Cyrus** | Agresiva | Daño alto, enfocada en ofensiva |
| 🔵 **Atlas** | Tanque | Mucha vida, lenta pero resistente |
| 🟢 **Nyx** | Sigilo | Rápida y ágil, apta para hit-and-run |

Cada personaje tiene **stats únicos** (vida, velocidad, daño) configurados mediante ScriptableObjects, y un conjunto propio de animaciones: `Idle`, `Fly`, `Hit`, `Die`, `Shooting`, `Fly_Shooting`. Se aplican en runtime mediante Animation Override Controllers sobre un Animator base compartido.

---

## ✨ Características

- ⚔️ **4 personajes jugables** con stats y animaciones únicos
- 🤝 **Multijugador online cooperativo** (2 jugadores) con SignalR
- 👥 **Sistema de amigos** con solicitudes, lista online y notificaciones en tiempo real
- ✉️ **Sistema de invitaciones** entre amigos para partidas cooperativas
- ✅ **Coordinación de inicio**: ambos jugadores deben confirmar para empezar
- 🎯 **Sistema de oleadas** con dificultad creciente
- 🏆 **Ranking** persistente en base de datos (modo solitario)
- 🔐 **Autenticación** con JWT, modo invitado y opción "Recordar cuenta"
- 📱 **Soporte multiplataforma**: PC y Android (con joystick virtual)
- 🌌 **Estética cyberpunk** con paleta neón consistente

---

## 🛠️ Stack tecnológico

### Cliente (Unity)
- **Engine:** Unity 6 LTS (`6000.3.10f1`) con URP (Universal Render Pipeline) 2D
- **Lenguaje:** C# / .NET
- **Físicas:** Rigidbody2D con Continuous Collision Detection
- **Animaciones:** Animator + Animation Override Controllers por personaje
- **UI:** Canvas + TextMesh Pro
- **Networking:** UnityWebRequest (REST) + SignalR Client (tiempo real)
- **Plataformas:** Windows y Android

### Backend
- **Framework:** ASP.NET Core 9 (Minimal API)
- **Base de datos:** PostgreSQL + Entity Framework Core
- **Autenticación:** JWT Bearer Tokens
- **Tiempo real:** SignalR (WebSockets)
- **Despliegue:** Railway (backend + PostgreSQL)
- **Sitio del lore:** Netlify

---

## 📁 Estructura del repositorio

```
NeonStrike2D/
├── Assets/
│   ├── Art/
│   │   ├── Animations/
│   │   │   ├── Player/
│   │   │   │   ├── Violet/
│   │   │   │   ├── Cyrus/
│   │   │   │   ├── Nyx/
│   │   │   │   └── Atlas/
│   │   │   └── Enemies/
│   │   └── Character/
│   ├── Script/
│   │   ├── Core/          # GameData, CharacterData, PlayerSpawner, MusicManager
│   │   ├── Player/        # PlayerController, PlayerHealth, PlayerSync, PlayerHealthUI
│   │   ├── Enemies/       # WardenAI, EnemyController, WaveManager, EnemySpawner
│   │   ├── Network/       # ApiManager, NetworkManager, UnityMainThreadDispatcher
│   │   ├── Projectiles/   # Bullet
│   │   └── UI/            # GameOverUI, RankingUI, CharacterSelectController, MultiplayerUI, LoginUI
│   ├── Scenes/            # MainMenu, CharacterSelect, Level1
│   └── Prefab/
├── Backend/
│   └── NeonStrike2D.Backend.Api/
│       ├── Data/          # AppDbContext
│       ├── Models/        # User, Friendship, GameSession, GameResult
│       ├── Dtos/          # Request/Response DTOs
│       ├── Hubs/          # GameHub (SignalR)
│       ├── Program.cs
│       └── Dockerfile
└── README.md
```

---

## 🎮 Cómo jugar

### Controles — PC
| Acción | Tecla |
|--------|-------|
| Movimiento | `W` `A` `S` `D` |
| Disparar | Click izquierdo |
| Apuntar | Ratón |

### Controles — Android
| Acción | Control |
|--------|---------|
| Movimiento | Joystick virtual flotante (izquierda) |
| Disparar | Botón táctil (derecha) |

### Modos de juego
- **Solitario** — sobrevive a oleadas crecientes de enemigos. La puntuación se guarda en el ranking global.
- **Cooperativo 2 jugadores** — invita a un amigo y luchad juntos contra las oleadas en tiempo real.

---

## 🌐 Sistema multijugador cooperativo

El sistema usa **SignalR** sobre WebSockets para garantizar baja latencia en la sincronización entre jugadores.

- 🔄 **Sincronización de posiciones** a 20 Hz con interpolación suave
- 🎬 **Sincronización de animaciones** inferida desde la velocidad
- ⚡ **Snap de spawn**: el compañero aparece directamente en su posición real, sin "tirones"
- 🚪 **Detección automática de desconexión**: la nave del compañero desaparece si abandona la partida
- 🏠 **Salas privadas** identificadas por un ID único generado por el backend
- ✅ **Coordinación de inicio**: el `GameStart` se dispara cuando ambos jugadores envían `PlayerReady` con su personaje seleccionado
- 🎯 **Spawn diferenciado** entre host e invitado para evitar superposición
- 🏷️ **Carteles de username** sobre cada nave en partida

---

## ⚙️ Setup Local

### Requisitos
- Unity 6 LTS (`6000.3.10f1`) con módulo URP
- .NET 9 SDK
- PostgreSQL
- Railway CLI (opcional, solo para despliegue)

### Backend ASP.NET Core

1. Clona el repositorio:
   ```bash
   git clone https://github.com/albertocll/NeonStrike2D.git
   cd NeonStrike2D/Backend/NeonStrike2D.Backend.Api
   ```

2. Crea el archivo `appsettings.json` (no incluido en el repo por seguridad):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=neonstrike;Username=postgres;Password=tu_password"
     },
     "Jwt": {
       "Key": "tu_clave_secreta_de_al_menos_32_caracteres",
       "Issuer": "NeonStrike2D",
       "Audience": "NeonStrike2DClient"
     }
   }
   ```

3. Aplica las migraciones:
   ```bash
   dotnet ef database update
   ```

4. Arranca el backend:
   ```bash
   dotnet run
   ```

### Cliente Unity

1. Abre el proyecto en **Unity Hub** con la versión `6000.3.10f1`.
2. En la escena `MainMenu`, selecciona el GameObject `ApiManager` y actualiza el campo **Server Url** con tu URL de backend (local o Railway).
3. Pulsa **Play**.

---

## 🚢 Despliegue en Railway

El backend está desplegado en Railway con PostgreSQL como servicio de base de datos asociado.

### Variables de entorno necesarias en Railway

| Variable | Descripción |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | Connection string de PostgreSQL (usar la URL pública del proxy, no `postgres.railway.internal`) |
| `Jwt__Key` | Clave secreta para firmar tokens JWT (mínimo 32 caracteres) |
| `Jwt__Issuer` | Issuer del token JWT (`NeonStrike2D`) |
| `Jwt__Audience` | Audience del token JWT (`NeonStrike2DClient`) |

> ⚠️ **Importante:** desde Unity hay que usar la **URL pública del proxy** de PostgreSQL al construir la connection string, no el hostname interno.

### Migraciones en producción

```bash
dotnet ef database update --connection "Host=HOST;Port=PORT;Database=railway;Username=postgres;Password=PASSWORD"
```

---

## 🌐 Endpoints API

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| `GET` | `/` | Health check | ❌ |
| `POST` | `/register` | Registro de usuario | ❌ |
| `POST` | `/login` | Login, devuelve JWT | ❌ |
| `GET` | `/me` | Info del usuario actual | ✅ |
| `GET` | `/ranking` | Top 10 jugadores | ❌ |
| `POST` | `/match/result` | Guarda resultado de partida | ✅ |
| `POST` | `/friends/request/{username}` | Envía solicitud de amistad | ✅ |
| `POST` | `/friends/accept/{requesterId}` | Acepta solicitud de amistad | ✅ |
| `GET` | `/friends/online` | Lista de amigos online | ✅ |
| `GET` | `/users/{username}` | Resuelve ID por username | ✅ |

Además, el backend expone un Hub de SignalR en `/gamehub` para multijugador en tiempo real.

---

## 📦 Build de Android

1. `File → Build Settings → Switch Platform a Android`
2. En `Player Settings`:
   - **Package Name:** `com.albertocll.neonstrike2d`
   - **Minimum API Level:** Android 8.0+
3. `Build` → genera el APK.

> Las APKs publicadas se distribuyen como assets en [GitHub Releases](https://github.com/albertocll/NeonStrike2D/releases) bajo versionado semántico (`v0.1.1`, `v0.2.0`...).

---

## 🗺️ Roadmap

### ✅ Completado
- 4 personajes jugables con stats, animaciones y ScriptableObjects
- Multijugador cooperativo 2 jugadores sobre SignalR (posiciones + animaciones sincronizadas)
- Sistema de amigos: solicitudes, lista online, notificaciones en tiempo real
- Sistema de invitaciones a partida
- Coordinación de inicio (`PlayerReady`)
- Detección de desconexión y despawn del compañero
- Sistema de oleadas con dificultad creciente
- Ranking persistente en PostgreSQL (modo solitario)
- JWT + "Recordar cuenta" + modo invitado
- Joystick virtual para móvil
- MusicManager entre escenas
- Build Android funcional
- Backend desplegado en Railway
- Sitio web del lore en Netlify

### 🔄 Borrador v0.2.0 — 14 may
- Login con username además de email
- Botón EXIT en paneles de personaje
- Limpieza de debug logs para release
- Fix de `MissingReferenceException` en LoginUI

### 📋 Defensa v1.0.0 — 2 jun
- Chat de texto entre amigos
- Primer mini-boss
- Nuevo tipo de enemigo básico
- Sistema de drops (vida + coleccionables)
- Sistema de puntuación ampliado (kills + coleccionables)

### 🔮 Futuro
- Sistema de localización ES / EN
- Autenticación con Google (OAuth)
- Distribución en Google Play
- Soporte iOS
- Partidas con más de 2 jugadores
- Resto de enemigos y segundo mini-boss

---

## 🎨 Paleta cromática

| Color | HEX | Uso |
|-------|-----|-----|
| ⬛ Fondo profundo | `#07090f` | Backgrounds |
| 🩵 Cian neón | `#00e5ff` | UI primaria, Brigada Neon |
| 🟣 Magenta | `#c850c0` | Acentos, energía |
| 🟪 Violeta | `#7b2fff` | Elementos secundarios |
| 🔴 Rojo amenaza | `#e24b4a` | Enemigos, daño |

---

## 👤 Autor

**Alberto Claros López**
- 💻 GitHub: [@albertocll](https://github.com/albertocll)

---

## 📄 Licencia

Proyecto académico privado. Todos los derechos reservados.

---

<div align="center">

*Trabajo de Fin de Grado — Defensa: 2 de junio de 2026*

**Brigada Neon, en pie.**

</div>