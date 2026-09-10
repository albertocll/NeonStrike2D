# Reorganización de carpetas y nomenclatura — historial completo

Este documento cubre **dos rondas**:
1. La primera pasada, acotada, de los issues **#132/#133** (rama
   `chore/reorganize-and-rename`, ya mergeada en `master` vía PR #144).
2. La **ejecución completa** del `RENAME_PLAN.md` autorizada por Alberto
   (rama `chore/global-rename-execution`), que aplica la convención
   **PascalCase sin separadores** a todo `Assets/Prefab/`, `Assets/Art/`,
   `Assets/Script/` y los `CharacterData`.

Todos los cambios se hicieron con las herramientas de Unity MCP
(`move_asset` / `rename_asset`), que preservan el GUID del asset — ninguna
referencia en escenas, prefabs o el Inspector se ha roto. Cada movimiento se
verificó con `get_component_properties`/`get_serialized_fields` (no se
asumió que "compilar" bastaba) y, en los puntos de mayor riesgo, con pruebas
funcionales reales en Play Mode.

---

## Ronda 2 (esta sesión) — Ejecución completa de `RENAME_PLAN.md`

### 0. `Player.prefab` y los "Variant" (decisión de Alberto)

| Antes | Después |
|---|---|
| `Assets/Prefab/Player.prefab` | `Assets/Prefab/Characters/PlayerBase.prefab` |
| `Assets/Prefab/Cyrus Variant.prefab` | `Assets/Prefab/Characters/CyrusVariant.prefab` |
| `Assets/Prefab/Nyx Variant.prefab` | `Assets/Prefab/Characters/NyxVariant.prefab` |
| `Assets/Prefab/Atlas Variant.prefab` | `Assets/Prefab/Characters/AtlasVariant.prefab` |

Decisión de Alberto: `PlayerBase` (rig compartido, sin crear un variant
propio para Violet — sigue usando la base directamente, igual que antes).
Verificado en Play Mode: Violet (`PlayerBase(Clone)`) y Cyrus
(`CyrusVariant(Clone)`) spawnean correctamente tras el cambio.

### 1. Scripts — único mismatch archivo/clase

| Antes | Después | Clase |
|---|---|---|
| `Assets/Script/UI/Menu/ButtonBack.cs` | `Assets/Script/UI/Menu/BackButton.cs` | `BackButton` (sin cambios, ya coincidía con el archivo nuevo) |

### 2. Prefabs — reorganizados por responsabilidad (16 movidos)

| Carpeta nueva | Prefabs |
|---|---|
| `Assets/Prefab/Characters/` | `PlayerBase`, `CyrusVariant`, `NyxVariant`, `AtlasVariant` |
| `Assets/Prefab/UI/` | `CharacterSlot`, `StatAttack` (antes `Stat_Attack`), `FriendRow`, `RankingRow` (antes `RankingRowPrefab ` con espacio sobrante) |
| `Assets/Prefab/Projectiles/` | `BulletPlayer` (antes `Bullet_Player`), `BulletEnemy` (antes `Bullet_Enemy`) |
| `Assets/Prefab/Enemies/` | `EnemyStriker` (antes `Enemy_Striker`), `EnemyWarden` (antes `Enemy_Warden`) |
| `Assets/Prefab/Collectibles/` | `CollectibleChip`, `CollectibleCrystal`, `CollectibleStar`, `CollectibleHealth` (antes `Collectible_*`) |
| `Assets/Prefab/PowerUps/` | Sin cambios — ya cumplían la convención |

`Assets/Prefab/` en la raíz solo contiene carpetas ahora, ningún prefab
suelto.

**Incidencia:** al crear la carpeta `Characters`, Unity la creó mal como
`CharacterS` — corregido con un renombrado en dos pasos (mismo truco que
`DashButton`, ver más abajo).

**Verificación:** `CollectibleSpawner`, `EnemySpawner`, `WaveManager`,
`WeaponController` (en `PlayerBase`), `WardenAI` (en `EnemyWarden`) — todos
sus campos de prefabs serializados siguen resolviendo correctamente
(comprobado campo por campo, no solo "compila").

### 3. CharacterData (`Assets/Script/Core/Data/`)

| Antes | Después |
|---|---|
| `Violet_Data.asset` | `VioletData.asset` |
| `Cyrus_Data.asset` | `CyrusData.asset` |
| `Nyx_Data.asset` | `NyxData.asset` |
| `Atlas_Data.asset` | `AtlasData.asset` |

Verificado: el array `characters` de `PlayerSpawner` en `Level1.unity` sigue
con las 4 entradas correctamente asignadas.

### 4. Art/Character/ — sprites de personajes

Quitado el guion bajo en los sprites de disparo/vuelo de los 4 personajes
(`Violet_Shooting.png` → `VioletShooting.png`, etc. — 8 archivos en total).
Los que ya no tenían separador (`Violet.png`, `BulletCyrus.png`,
`BulletNyx.png`, `BulletAtlas.png`) se dejaron tal cual.

### 5. Art/Enemies/ — sprites sueltos

`Striker_*.png` (4) y `Warden_*.png` (7) → `Striker*.png`/`Warden*.png`,
quitando el guion bajo.

### 6. Art/Collectibles/, Art/Canvas/, Art/UI/

| Antes | Después | Nota |
|---|---|---|
| `Collectible_Health.png` | `CollectibleHealth.png` | |
| `Coleccionables_Background.png` | `CollectiblesBackground.png` | traducido a inglés para consistencia |
| `BG_Bar.png` | `BgBar.png` | |
| `PanelOptionsBackgorund.png` | `PanelOptionsBackground.png` | **typo corregido** ("Backgorund"→"Background") |
| `handle-Photoroom (1).png` | `SliderHandle.png` | investigado antes de renombrar: es el "Handle" real de un slider en `MainMenu.unity` (confirmado por GUID + nombre del GameObject que lo usa) |
| `Prueba_de_slider-removebg-preview.png` | `UIRoundedPanel.png` | investigado antes de renombrar: usado como fondo de `Image` genérico en botones/paneles en **5 sitios** (`Level1.unity`, `MainMenu.unity`, `CharacterSelect.unity`, `FriendRow.prefab`) — pese al nombre original, no es específico de un slider, así que se le puso un nombre que refleja su uso real |
| `logo.png` | `Logo.png` | mayúscula inicial |

Ambos archivos "sin identificar" del plan **sí estaban en uso** — no se
borró ninguno, se renombraron según lo que realmente hacen (instrucción de
Alberto: confirmar uso antes de decidir).

### 7. Art/Enviroment/ → Art/Environment/

Carpeta renombrada (typo: faltaba la segunda "n"). Contiene `Ground.jpeg`
(nombre del archivo en sí ya cumplía, solo cambió de carpeta).

### 8. Art/Animations/ — 46 archivos (clips + controllers + override controllers)

Mismo tratamiento — quitar guion bajo — aplicado a los 4 personajes (Violet,
Cyrus, Nyx, Atlas: 6 clips cada uno) y los 2 enemigos (Striker: 4 clips,
Warden: 6 clips), más:
- `Player_Controller.controller` → `PlayerBaseController.controller`
- `Striker_Animator.controller` → `StrikerAnimator.controller`
- `Warden_Animator.controller` → `WardenAnimator.controller`
- `Cyrus_AnimatorOverride.overrideController` → `CyrusAnimatorOverride.overrideController`
- `Nyx_AnimatorOverride.overrideController` → `NyxAnimatorOverride.overrideController`
- `Atlas_AnimatorOverride.overrideController` → `AtlasAnimatorOverride.overrideController`

**Corrección adicional de consistencia:** `Nyx_Shooting_Fly.anim` y
`Atlas_Shooting_Fly.anim` invertían el orden respecto a los demás
personajes (que usan "Fly_Shooting"). Renombrados a `NyxFlyShooting.anim` y
`AtlasFlyShooting.anim` para unificar el orden en los 4 personajes.

**Verificación funcional en Play Mode** (no solo compilación):
- Violet, Cyrus, Nyx y Atlas spawneados los 4 — cada uno con sprite válido
  (`sprite != null`) y su `AnimatorController`/`AnimatorOverrideController`
  correcto (`PlayerBaseController`, `CyrusAnimatorOverride`,
  `NyxAnimatorOverride`, `AtlasAnimatorOverride`).
- `EnemyStriker` y `EnemyWarden` instanciados — sprite y controller
  (`StrikerAnimator`, `WardenAnimator`) correctos en ambos.

### Incidencias de esta ronda

1. **Carpeta `Characters` creada como `CharacterS`** por un glitch de
   `create_folder` — corregido con un renombrado en dos pasos.
2. **`logo.png` → `Logo.png`**: mismo problema de case-insensitivity de
   Windows/Unity que `Dashbutton.png` en la ronda 1 — resuelto con el mismo
   truco de nombre intermedio (`LogoTmpFix.png`).
3. **`handle-Photoroom (1).png`**: el primer intento de rename fue
   bloqueado por el clasificador de permisos de Claude Code (motivo no
   claro, posiblemente los paréntesis en el nombre); el reintento funcionó
   sin cambios.
4. **Barrido final**: `find Assets -name "* *"` y una búsqueda de `_` en
   nombres de archivo confirman que no queda ningún separador dentro de
   `Assets/Prefab/`, `Assets/Art/`, `Assets/Script/` ni `Assets/Script/Core/Data/`
   (solo queda una fuente de `Assets/Fonts/`, ruido preexistente y ajeno a
   este trabajo, y `Assets/Documents/`, `Assets/TextMesh Pro/` y
   `Assets/_Recovery/`, fuera del alcance pedido).

### Convención actualizada en `CLAUDE.md`

La convención documentada en la ronda 1 (`Categoria_Nombre` con guion bajo)
queda **reemplazada** por la de esta ronda: **PascalCase sin separadores**
en todo — prefabs, sprites, animaciones, controllers y `CharacterData`
(`PlayerViolet`, no `Player_Violet` ni `PlayerViolet_Base`). `CLAUDE.md` se
ha actualizado para reflejar esto (no versionado, sigue en `.gitignore`).

---

## Ronda 1 (sesión anterior) — Issues #132/#133 (ya mergeada)

### #132 — Reorganización inicial de `Assets/Script/`

| Antes | Después | GUID (sin cambios) |
|---|---|---|
| `Assets/Script/Core/Collectible.cs` | `Assets/Script/Collectibles/Collectible.cs` | `097e66f4a1e27a74b8dcb9f7182dd1b0` |
| `Assets/Script/Core/CollectibleSpawner.cs` | `Assets/Script/Collectibles/CollectibleSpawner.cs` | `6f86232f999fdd94aba4f95fc7f8f427` |
| `Assets/Script/UI/Joystick.cs` | `Assets/Script/UI/HUD/Joystick.cs` | `39e6983562d554e4b82c7257f08d9d81` |
| `Assets/Script/UI/MultiplayerUI.cs` | `Assets/Script/UI/Menu/MultiplayerUI.cs` | `14fb0983f5ad37d4a8e4c748f81b06b6` |

### #133 — Primeros 2 renombrados (motivación del issue)

| Antes | Después | GUID (sin cambios) |
|---|---|---|
| `Assets/Art/Collectibles/Collectibe_Health.png` | `Assets/Art/Collectibles/Collectible_Health.png` | `67c98fb4cc8c7914fb98faafd71bc935` |
| `Assets/Art/Canvas/Dashbutton.png` | `Assets/Art/Canvas/DashButton.png` | `09d392c36adeffb48ba6b11d8a615b54` |

(`Collectible_Health.png` se renombró de nuevo en la ronda 2, a
`CollectibleHealth.png`, para quitar el guion bajo restante bajo la nueva
convención global.)

**Nota histórica:** en la ronda 1 documenté aquí una convención de
"`Categoria_Nombre` con guion bajo" como la recomendada. Esa recomendación
queda **obsoleta** — la convención final, autorizada por Alberto y aplicada
en la ronda 2, es PascalCase sin separadores en todo el proyecto.
