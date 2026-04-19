# 🚀 NeonStrike2D

> Shooter 2D multijugador con estética cyberpunk, desarrollado en Unity URP con backend en ASP.NET Core 9 desplegado en Railway.

---

## 🎮 Descripción

NeonStrike2D es un shooter 2D de naves espaciales con ambientación cyberpunk. Los jugadores eligen entre cuatro personajes únicos y se enfrentan a oleadas de enemigos cada vez más difíciles. El juego cuenta con un sistema de ranking online, autenticación de usuarios y soporte para multijugador en desarrollo.

---

## 👾 Personajes

| Personaje | Descripción |
|-----------|-------------|
| **Violet** | Nave de ataque equilibrada. Personaje por defecto. |
| **Cyrus** | Nave de asalto pesado. Alta resistencia. |
| **Nyx** | Nave sigilosa. Velocidad superior. |
| **Atlas** | Nave de soporte. Máxima potencia de fuego. |

Cada personaje tiene sus propias animaciones (`Idle`, `Fly`, `Hit`, `Die`, `Shooting`, `Fly_Shooting`) y stats individuales configurados mediante ScriptableObjects.

---

## 🛠️ Stack Tecnológico

### Cliente (Unity)
- **Engine:** Unity 2D con URP (Universal Render Pipeline)
- **Lenguaje:** C#
- **Físicas:** Rigidbody2D con Collision Detection Continuous
- **Animaciones:** Animator + Animation Override Controllers por personaje
- **UI:** Canvas + TextMesh Pro
- **Networking:** UnityWebRequest + SignalR Client

### Backend
- **Framework:** ASP.NET Core 9 (Minimal API)
- **Base de datos:** PostgreSQL + Entity Framework Core
- **Autenticación:** JWT Bearer Tokens
- **Tiempo real:** SignalR
- **Despliegue:** Railway

---

## 📁 Estructura del Proyecto

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
│   │   ├── Player/        # PlayerController, PlayerHealth, PlayerHealthUI
│   │   ├── Enemies/       # WardenAI, EnemyController, WaveManager, EnemySpawner
│   │   ├── Network/       # ApiManager, NetworkManager
│   │   ├── Projectiles/   # Bullet
│   │   └── UI/            # GameOverUI, RankingUI, CharacterSelectController
│   └── Prefab/
├── Backend/
│   └── NeonStrike2D.Backend.Api/
│       ├── Data/          # AppDbContext
│       ├── Models/        # User, GameSession, GameResult
│       ├── Dtos/          # Request/Response DTOs
│       ├── Hubs/          # GameHub (SignalR)
│       ├── Program.cs
│       └── Dockerfile
└── README.md
```

---

## ⚙️ Setup Local

### Requisitos
- Unity 6 con módulo URP
- .NET 9 SDK
- PostgreSQL
- Railway CLI (opcional)

### Backend

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

### Cliente (Unity)

1. Abre el proyecto en Unity Hub
2. Selecciona el GameObject `ApiManager` en la escena `MainMenu`
3. Actualiza el campo **Server Url** con tu URL de backend
4. Abre la escena `MainMenu` y pulsa Play

---

## 🚢 Despliegue en Railway

El backend está desplegado en Railway con PostgreSQL como servicio de base de datos.

### Variables de entorno necesarias en Railway

| Variable | Descripción |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | Connection string de PostgreSQL |
| `Jwt__Key` | Clave secreta para JWT |
| `Jwt__Issuer` | Issuer del token JWT |
| `Jwt__Audience` | Audience del token JWT |

### Migraciones en producción

```bash
dotnet ef database update --connection "Host=HOST;Port=PORT;Database=railway;Username=postgres;Password=PASSWORD"
```

---

## 🌐 Endpoints API

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| GET | `/` | Health check | ❌ |
| POST | `/register` | Registro de usuario | ❌ |
| POST | `/login` | Login, devuelve JWT | ❌ |
| GET | `/me` | Info del usuario actual | ✅ |
| GET | `/ranking` | Top 10 jugadores | ❌ |
| POST | `/match/result` | Guarda resultado de partida | ✅ |

---

## 🗺️ Roadmap

### ✅ Completado
-  Sistema de login y registro
-  Flujo MainMenu → CharacterSelect → Level1
-  4 personajes jugables con stats individuales
-  Sistema de oleadas (WaveManager)
-  Ranking online con datos reales
-  Joystick virtual para móvil
-  MusicManager entre escenas
-  Backend desplegado en Railway
-  PostgreSQL en producción con migraciones

### 🔄 En progreso
-  Quitar joystick en paneles de UI
-  Ampliar tamaño del panel de ranking
-  Botón "Recordar cuenta"
-  Traducción del juego (i18n)
-  Controles de música en opciones

### 📋 Pendiente
-  Multijugador en tiempo real (PlayerSync + SignalR)
-  Más tipos de enemigos
-  Sistema de dificultad progresiva
-  Autenticación con Google
-  Chat entre amigos e invitaciones
-  Enlace botón About con web del lore

---

## 👤 Autor

**Alberto Claros López**  
[github.com/albertocll](https://github.com/albertocll)

---

## 📄 Licencia

Proyecto privado. Todos los derechos reservados.
