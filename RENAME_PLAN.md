# Plan de reorganización y renombrado global — SOLO PLANIFICACIÓN

**Nada de lo descrito aquí se ha ejecutado.** Es un inventario completo para
que Alberto lo revise antes de autorizar nada. Convención objetivo:
**PascalCase sin separadores** (`PlayerViolet.prefab`, no `Player_Violet` ni
`player_violet`) para prefabs, sprites, carpetas y assets en general. Los
scripts C# ya siguen mayoritariamente esta convención (ver sección propia).

Todas las referencias listadas se han comprobado con `grep`/Unity MCP sobre
el estado real del repo (rama `chore/reorganize-and-rename`), no se ha
asumido nada.

---

## 0. Caso especial que señaló Alberto: el prefab de "Violet"

**No existe ningún `Violet.prefab` ni `PlayerViolet.prefab`.** La arquitectura
real es:

- `Assets/Prefab/Player.prefab` es el rig **base compartido** por los 4
  personajes (todos los componentes: `PlayerController`, `WeaponController`,
  `PlayerHealth`, `PlayerPowerUps`, etc.).
- `Violet_Data.asset` (el `CharacterData` de Violet) apunta **directamente**
  a `Player.prefab` — Violet no tiene prefab propio, usa el base tal cual.
- Cyrus, Nyx y Atlas sí tienen cada uno su propio **Prefab Variant**, que
  hereda de `Player.prefab` y solo sobreescribe el `AnimatorOverrideController`
  (y probablemente el sprite base):
  - `Cyrus_Data.asset` → `Assets/Prefab/Cyrus Variant.prefab`
  - `Nyx_Data.asset` → `Assets/Prefab/Nyx Variant.prefab`
  - `Atlas_Data.asset` → `Assets/Prefab/Atlas Variant.prefab`

Confirmado inspeccionando el campo `prefab:` de los 4 `.asset` en
`Assets/Script/Core/Data/` y cruzando el GUID contra el `.meta` de cada
prefab.

**Esto no es un error de nomenclatura, es una asimetría de diseño real.**
Antes de renombrar nada aquí, Alberto tiene que decidir uno de estos caminos
(no lo decido yo, es una decisión de producto/arquitectura):

- **Opción A** — Renombrar `Player.prefab` → `PlayerBase.prefab` (dejando
  claro que es el rig compartido, no el de un personaje concreto) y los 3
  variantes a `PlayerCyrus.prefab`, `PlayerNyx.prefab`, `PlayerAtlas.prefab`
  (quitando el espacio y la palabra "Variant"). Violet seguiría sin prefab
  propio, usando `PlayerBase.prefab` directamente — igual que ahora.
- **Opción B** — Igual que A, pero además crear `PlayerViolet.prefab` como
  variant vacío (sin overrides) por simetría con los otros 3, y apuntar
  `Violet_Data.asset` a él en vez de al base.
- **Opción C** — Dejarlo tal cual está (`Player.prefab` + 3 "Variant"),
  solo quitar el espacio de los 3 variantes (`CyrusVariant.prefab`, etc.) y
  no tocar `Player.prefab`.

Bajo cualquier opción, tocar `Player.prefab` es la operación de mayor
riesgo de todo este plan: lo referencian los 4 `CharacterData`, y es la base
de 3 Prefab Variants — un fallo aquí rompe a los 4 personajes a la vez.

---

## 1. Assets/Script/ — ya casi 100% conforme

Los 49 scripts de `Assets/Script/` ya siguen `PascalCase.cs` sin
separadores. Comprobado con `grep` de todas las declaraciones
`public class`/`public static class` del árbol: **en 48 de 49 casos el
nombre de archivo coincide exactamente con el de la clase.** No se propone
ningún cambio para esos 48.

**Única excepción encontrada — no asumida, verificada:**

| Ruta actual | Nombre nuevo propuesto | Tipo | Clase actual | ¿Coincide con archivo? | Clase nueva propuesta | Riesgo |
|---|---|---|---|---|---|---|
| `Assets/Script/UI/Menu/ButtonBack.cs` | `Assets/Script/UI/Menu/BackButton.cs` (o renombrar la clase a `ButtonBack` en vez del archivo) | Script (MonoBehaviour) | `BackButton` | **No** — archivo dice "ButtonBack", clase dice "BackButton" (orden invertido) | `BackButton` (si se renombra el archivo) | **Medio** |

**Referencias a la clase `BackButton`** (si se decide renombrar la CLASE en
vez del archivo, o viceversa, hay que tocar estos 3 sitios):
- `Assets/Script/UI/Menu/ButtonBack.cs` (la propia declaración)
- `Assets/Scenes/MainMenu.unity` (`m_EditorClassIdentifier: Assembly-CSharp::BackButton`)
- `Assets/Scenes/CharacterSelect.unity` (mismo identificador, más un GameObject literalmente llamado `ButtonBack` en la jerarquía — nombre de GameObject, no de clase, no rompe nada si no se toca)

Riesgo **medio** y no **bajo** porque, aunque el compilador avisaría de
cualquier referencia en código roto, el identificador de clase en las 2
escenas es un string serializado que Unity normalmente regenera solo, pero
conviene abrir ambas escenas y confirmar visualmente tras el cambio antes de
darlo por bueno (no es un fallo silencioso grave, pero sí merece
verificación manual).

**Recomendación:** renombrar el **archivo** a `BackButton.cs` (dejar la
clase como está) es la opción de menor fricción — cero cambios de código,
solo mover el archivo con `move_asset` (mismo patrón ya usado hoy).

---

## 2. Assets/Prefab/ — por qué hay prefabs sueltos sin carpeta propia

Actualmente `Assets/Prefab/` tiene una carpeta (`PowerUps/`) y **16 prefabs
sueltos en la raíz**, sin ningún criterio visible de por qué esos sí y los
de `PowerUps/` no — simplemente `PowerUps/` se creó en la sesión de hoy
siguiendo el mismo impulso que ya guió #132 en `Assets/Script/`, pero nunca
se aplicó retroactivamente al resto de `Assets/Prefab/`.

Propuesta: aplicar el mismo criterio de `Assets/Script/` (una carpeta por
responsabilidad) a `Assets/Prefab/`, en paralelo a quitar los separadores.

| Ruta actual | Ruta/nombre nuevo propuesto | Tipo | Riesgo |
|---|---|---|---|
| `Assets/Prefab/Player.prefab` | *(ver sección 0 — pendiente decisión)* | Prefab | **Alto** (base de 4 personajes) |
| `Assets/Prefab/Cyrus Variant.prefab` | *(ver sección 0)* | Prefab Variant | **Alto** |
| `Assets/Prefab/Nyx Variant.prefab` | *(ver sección 0)* | Prefab Variant | **Alto** |
| `Assets/Prefab/Atlas Variant.prefab` | *(ver sección 0)* | Prefab Variant | **Alto** |
| `Assets/Prefab/CharacterSlot.prefab` | `Assets/Prefab/UI/CharacterSlot.prefab` | Prefab (UI) | Bajo — solo referenciado por `CharacterSelectController.cs`/`CharacterSelect.unity` |
| `Assets/Prefab/Stat_Attack.prefab` | `Assets/Prefab/UI/StatAttack.prefab` | Prefab (UI) | Bajo |
| `Assets/Prefab/FriendRow.prefab` | `Assets/Prefab/UI/FriendRow.prefab` | Prefab (UI) | Bajo (ya cumple PascalCase, solo cambia de carpeta) — referenciado por `MultiplayerUI.cs` |
| `Assets/Prefab/RankingRowPrefab .prefab` (**nótese el espacio sobrante antes de `.prefab`**) | `Assets/Prefab/UI/RankingRow.prefab` | Prefab (UI) | Bajo — referenciado por `RankingUI.cs`/`RankingRow.cs` |
| `Assets/Prefab/Bullet_Player.prefab` | `Assets/Prefab/Projectiles/BulletPlayer.prefab` | Prefab | **Medio** — referenciado por `WeaponController` en `Player.prefab` (campo serializado `bulletPrefab`) |
| `Assets/Prefab/Bullet_Enemy.prefab` | `Assets/Prefab/Projectiles/BulletEnemy.prefab` | Prefab | **Medio** — referenciado por `WardenAI`/`StrikerAI` (campo `bulletPrefab`) en `Enemy_Warden`/`Enemy_Striker` |
| `Assets/Prefab/Enemy_Striker.prefab` | `Assets/Prefab/Enemies/EnemyStriker.prefab` | Prefab | **Medio** — referenciado por `EnemySpawner.cs`/`WaveManager.cs` (listas de prefabs serializadas) y por el propio `EnemyController` (drop de power-ups, ver sesión de hoy) |
| `Assets/Prefab/Enemy_Warden.prefab` | `Assets/Prefab/Enemies/EnemyWarden.prefab` | Prefab | **Medio** — mismo tipo de referencias que Striker |
| `Assets/Prefab/Collectible_Chip.prefab` | `Assets/Prefab/Collectibles/CollectibleChip.prefab` | Prefab | **Medio** — referenciado por `CollectibleSpawner.cs` (`scorePrefabs`) |
| `Assets/Prefab/Collectible_Crystal.prefab` | `Assets/Prefab/Collectibles/CollectibleCrystal.prefab` | Prefab | **Medio** — igual |
| `Assets/Prefab/Collectible_Star.prefab` | `Assets/Prefab/Collectibles/CollectibleStar.prefab` | Prefab | **Medio** — igual |
| `Assets/Prefab/Collectible_Health.prefab` | `Assets/Prefab/Collectibles/CollectibleHealth.prefab` | Prefab | **Medio** — referenciado por `CollectibleSpawner.cs` (`healthPrefab`) |
| `Assets/Prefab/PowerUps/PowerUpShield.prefab` | *(sin cambio — ya cumple)* | Prefab | — |
| `Assets/Prefab/PowerUps/PowerUpHeavyWeapon.prefab` | *(sin cambio — ya cumple)* | Prefab | — |
| `Assets/Prefab/PowerUps/PowerUpTripleShot.prefab` | *(sin cambio — ya cumple)* | Prefab | — |
| `Assets/Prefab/PowerUps/PowerUpSpeedBoost.prefab` | *(sin cambio — ya cumple)* | Prefab | — |

**Por qué "riesgo medio" y no "bajo" en los prefabs referenciados por otros
prefabs/scripts:** mover o renombrar el `.prefab` en sí con `move_asset`
preserva el GUID (como hoy con los scripts), así que las referencias
**no deberían romperse solas**. El riesgo es "medio" porque son referencias
**serializadas** (listas de `GameObject` en `CollectibleSpawner`,
`EnemySpawner`, campos `bulletPrefab` en `WeaponController`/`WardenAI`/
`StrikerAI`) — si algo falla, **falla en silencio** (el campo queda `None`
en el Inspector) en vez de dar un error de compilación. Hay que verificar
cada uno abriendo el Inspector tras el movimiento, no basta con compilar.

---

## 3. Assets/Art/ — por subcarpeta

### 3.1 `Art/Character/` (sprites de personajes)

Patrón dominante: `Nombre_Estado.png` (con guion bajo) o, para dos de los
cuatro personajes, ya sin separador (`BulletCyrus.png`, `BulletNyx.png`,
`BulletAtlas.png`) — inconsistente incluso entre sprites del mismo tipo.

| Ruta actual | Nombre nuevo propuesto | Riesgo |
|---|---|---|
| `Art/Character/Violet/Violet.png` | `Violet.png` *(ya cumple)* | — |
| `Art/Character/Violet/Violet_Shooting.png` | `VioletShooting.png` | Bajo — referenciado solo por el sprite en el Animator (ver Animations) |
| `Art/Character/Violet/Violet_FlyShooting.png` | `VioletFlyShooting.png` | Bajo |
| `Art/Character/Violet/Bullet_Violet.png` | `BulletViolet.png` | Bajo |
| `Art/Character/Cyrus/Cyrus.png` | *(ya cumple)* | — |
| `Art/Character/Cyrus/Cyrus_Shooting.png` | `CyrusShooting.png` | Bajo |
| `Art/Character/Cyrus/Cyrus_shootingFly.png` | `CyrusShootingFly.png` (nótese minúscula inicial actual, otra inconsistencia) | Bajo |
| `Art/Character/Cyrus/BulletCyrus.png` | *(ya cumple)* | — |
| `Art/Character/Nyx/Nyx.png` | *(ya cumple)* | — |
| `Art/Character/Nyx/Nyx_shootingFly.png` | `NyxShootingFly.png` | Bajo |
| `Art/Character/Nyx/BulletNyx.png` | *(ya cumple)* | — |
| `Art/Character/Atlas/Atlas.png` | *(ya cumple)* | — |
| `Art/Character/Atlas/Atlas_Shooting.png` | `AtlasShooting.png` | Bajo |
| `Art/Character/Atlas/Atlas_shootingFly.png` | `AtlasShootingFly.png` | Bajo |
| `Art/Character/Atlas/BulletAtlas.png` | *(ya cumple)* | — |

Riesgo **bajo** en todo este bloque: son texturas referenciadas únicamente
desde `SpriteRenderer`/Animator por GUID+fileID interno del sprite, no por
nombre de archivo. `move_asset`/`rename_asset` preserva el GUID.

### 3.2 `Art/Animations/` (clips, controllers, override controllers)

Mismo patrón `Nombre_Estado.anim`, uno por personaje/enemigo. Listado
completo (28 clips de personajes + 12 de enemigos + 3 controllers + 3 override
controllers = 46 archivos):

**Violet** (`Art/Animations/Player/Violet/`): `Violet_Idle.anim`,
`Violet_Hit.anim`, `Violet_Die.anim`, `Violet_Shooting.anim`,
`Violet_Fly.anim`, `Violet_Fly_Shooting.anim` → quitar guion bajo en los 6
(`VioletIdle.anim`, etc.). Más `Player_Controller.controller` →
`PlayerController.controller` (**ojo**: este nombre colisionaría
visualmente, que no técnicamente, con la clase `PlayerController.cs` — son
tipos de asset distintos y Unity no los confunde, pero puede confundir a un
humano buscando en el Project window; alternativa: `PlayerBaseController.controller` si se adopta la Opción A/B de la sección 0).

**Cyrus** (`Art/Animations/Player/Cyrus/`): `Cyrus_Idle.anim`,
`Cyrus_Die.anim`, `Cyrus_Fly.anim`, `Cyrus_Shooting.anim`,
`Cyrus_Fly_Shooting.anim`, `Cyrus_Hit.anim` (6 clips) +
`Cyrus_AnimatorOverride.overrideController` → mismo tratamiento
(`CyrusIdle.anim`... `CyrusAnimatorOverride.overrideController`).

**Nyx** (`Art/Animations/Player/Nyx/`): `Nyx_Die.anim`, `Nyx_Fly.anim`,
`Nyx_Hit.anim`, `Nyx_Idle.anim`, `Nyx_Shooting.anim`,
`Nyx_Shooting_Fly.anim` (6 clips, nótese que este último invierte el orden
respecto a los demás personajes — "Shooting_Fly" vs "Fly_Shooting" — otra
inconsistencia a resolver) + `Nyx_AnimatorOverride.overrideController`.

**Atlas** (`Art/Animations/Player/Atlas/`): `Atlas_Die.anim`,
`Atlas_Fly.anim`, `Atlas_Hit.anim`, `Atlas_Idle.anim`,
`Atlas_Shooting.anim`, `Atlas_Shooting_Fly.anim` (6 clips, mismo orden
invertido que Nyx) + `Atlas_AnimatorOverride.overrideController`.

**Striker** (`Art/Animations/Enemies/Striker/`): `Striker_Die.anim`,
`Striker_Fly.anim`, `Striker_Hit.anim`, `Striker_Idle.anim` (4 clips) +
`Striker_Animator.controller` → `StrikerAnimator.controller`.

**Warden** (`Art/Animations/Enemies/Warden/`): `Warden_Die.anim`,
`Warden_Fly.anim`, `Warden_Fly_Shooting.anim`, `Warden_Hit.anim`,
`Warden_Idle.anim`, `Warden_Shooting.anim` (6 clips) +
`Warden_Animator.controller` → `WardenAnimator.controller`.

**Riesgo de todo el bloque de animaciones: bajo.** Los `AnimatorController`
referencian sus clips internamente por GUID (igual que un prefab referencia
un script), así que renombrar el `.anim` no rompe el `.controller` que lo
usa. El único riesgo real es de nuevo el `Player_Controller.controller` /
`AnimatorOverrideController` — están referenciados desde `CharacterData`
(`animatorController` field) y desde `Animator.runtimeAnimatorController`
en cada prefab — mismo nivel de riesgo medio que el resto de referencias
serializadas.

### 3.3 `Art/Enemies/` (sprites sueltos, sin Animator de por medio)

| Ruta actual | Nombre nuevo propuesto | Riesgo |
|---|---|---|
| `Art/Enemies/Striker/Striker_Die.png` | `StrikerDie.png` | Bajo |
| `Art/Enemies/Striker/Striker_Fly.png` | `StrikerFly.png` | Bajo |
| `Art/Enemies/Striker/Striker_Hit.png` | `StrikerHit.png` | Bajo |
| `Art/Enemies/Striker/Striker_Idle.png` | `StrikerIdle.png` | Bajo |
| `Art/Enemies/Warden/Warden_*.png` (7 archivos: `Bullet`, `Fly_Shooting`, `Die`, `Fly`, `Hit`, `Idle`, `Shooting`) | `Warden*.png` quitando guion bajo (`WardenBullet.png`, `WardenFlyShooting.png`, etc.) | Bajo |

### 3.4 `Art/PowerUps/` y `Art/Collectibles/`

| Ruta actual | Nombre nuevo propuesto | Riesgo |
|---|---|---|
| `Art/PowerUps/PowerUpShield.png` | *(ya cumple)* | — |
| `Art/PowerUps/PowerUpHeavyWeapon.png` | *(ya cumple)* | — |
| `Art/PowerUps/PowerUpTripleShot.png` | *(ya cumple)* | — |
| `Art/PowerUps/PowerUpSpeedBoost.png` | *(ya cumple)* | — |
| `Art/Collectibles/Collectible_Health.png` | `CollectibleHealth.png` | Bajo |
| `Art/Collectibles/Coleccionables_Background.png` | `CollectiblesBackground.png` (además de quitar el guion bajo, corrige el nombre a inglés para ser consistente con el resto del proyecto — a confirmar con Alberto si quiere mantener nombres en español en algún sitio) | Bajo |

Nota: `Collectible_Health.png` ya se renombró de `Collectibe_Health.png`
(typo) en la sesión anterior (#133) — aquí solo queda quitar el guion bajo
restante bajo la nueva convención "sin separadores" que pide esta tarea.
El **nombre interno del sub-sprite** dentro del `.meta` sigue siendo
`CollectibeHeart_0` (con la errata original) — no se tocó entonces porque
es solo una etiqueta cosmética en el selector de sprites de Unity, sin
efecto funcional, pero si se quiere pulir del todo habría que re-cortar el
sprite en el Sprite Editor (riesgo bajo pero manual, no hay atajo por API).

### 3.5 `Art/Canvas/` (UI genérica)

| Ruta actual | Nombre nuevo propuesto | Riesgo |
|---|---|---|
| `Art/Canvas/DashButton.png` | *(ya cumple)* | — |
| `Art/Canvas/PauseIcon.png` | *(ya cumple)* | — |
| `Art/Canvas/Background.png` | *(ya cumple)* | — |
| `Art/Canvas/Walls.png` | *(ya cumple)* | — |
| `Art/Canvas/BG_Bar.png` | `BgBar.png` (o `BackgroundBar.png` si "BG" se considera una abreviatura a evitar — a decidir) | Bajo |
| `Art/Canvas/PanelExitBackground.png` | *(ya cumple)* | — |
| `Art/Canvas/PanelOptionsBackgorund.png` | `PanelOptionsBackground.png` (**typo**: "Backgorund" → "Background", no solo un tema de separadores) | Bajo |
| `Art/Canvas/handle-Photoroom (1).png` | **⚠️ no está claro qué es este archivo** — nombre de export de una herramienta online (Photoroom), con espacio y paréntesis. Antes de renombrarlo hay que confirmar con Alberto si sigue en uso o es un resto de edición que se puede borrar. No propongo nombre nuevo hasta saberlo. | — |
| `Art/Canvas/Prueba_de_slider-removebg-preview.png` | **⚠️ mismo caso** — el propio nombre dice "Prueba" (prueba/test) y "preview". Probablemente un descarte de una herramienta de quitar fondos, nunca renombrado. Confirmar con Alberto si se usa en algo o se borra directamente en vez de renombrar. | — |

### 3.6 `Art/UI/Icons/`

| Ruta actual | Nombre nuevo propuesto | Riesgo |
|---|---|---|
| `Art/UI/Icons/logo.png` | `Logo.png` (mayúscula inicial, único archivo del proyecto en minúscula pura) | Bajo |

### 3.7 `Art/Enviroment/` — la carpeta en sí tiene una errata

| Ruta actual | Nombre nuevo propuesto | Riesgo |
|---|---|---|
| `Art/Enviroment/` (carpeta) | `Art/Environment/` (**"Enviroment" → "Environment", falta la segunda "n"**) | Bajo — carpetas también se mueven preservando GUIDs de todo su contenido |
| `Art/Enviroment/Ground.jpeg` | `Art/Environment/Ground.jpeg` *(el nombre del archivo en sí ya cumple, solo cambia de carpeta)* | Bajo |

---

## 4. CharacterData (`Assets/Script/Core/Data/`)

Esto es lo que hace de "Assets/Character" en este proyecto — no existe una
carpeta con ese nombre exacto, los `CharacterData` (ScriptableObjects) viven
en `Assets/Script/Core/Data/`.

| Ruta actual | Nombre nuevo propuesto | Riesgo |
|---|---|---|
| `Assets/Script/Core/Data/Violet_Data.asset` | `Assets/Script/Core/Data/VioletData.asset` | **Medio** — referenciado desde `PlayerSpawner.cs` (array `characters` en el Inspector) |
| `Assets/Script/Core/Data/Cyrus_Data.asset` | `CyrusData.asset` | **Medio** — igual |
| `Assets/Script/Core/Data/Nyx_Data.asset` | `NyxData.asset` | **Medio** — igual |
| `Assets/Script/Core/Data/Atlas_Data.asset` | `AtlasData.asset` | **Medio** — igual |

Riesgo medio por la misma razón que los prefabs de la sección 2: referencia
serializada (array en el Inspector de `PlayerSpawner`), no de código — el
GUID se preserva con `rename_asset`, pero hay que verificar visualmente
que el array `characters` de `PlayerSpawner` en `Level1.unity` sigue
teniendo las 4 entradas asignadas tras el cambio.

`CharacterData.cs` (la clase/script en sí) ya cumple la convención, no se
toca.

---

## 5. Resumen de riesgo

| Nivel | Qué incluye | Por qué |
|---|---|---|
| **Alto** | `Player.prefab` y sus 3 "Variant" | Base compartida de los 4 personajes; cualquier fallo afecta a todo el juego jugable a la vez. Requiere decisión de diseño previa (sección 0), no solo ejecución mecánica. |
| **Medio** | Prefabs/assets referenciados por campos serializados en otros prefabs o scripts (Bullets, Enemies, Collectibles, `CharacterData`, `AnimatorController`/`OverrideController`, `ButtonBack.cs`) | El GUID se preserva con las herramientas de Unity MCP, pero un fallo se manifiesta como una referencia `None` en el Inspector — **silencioso**, no lo detecta el compilador. Necesitan verificación manual tras cada cambio. |
| **Bajo** | Sprites sueltos sin más referencia que su propio `SpriteRenderer`/Animator, carpetas, scripts sin más uso que su propia clase | Referenciados solo por GUID interno del propio asset; el compilador o una simple inspección visual basta para confirmar. |
| **Sin clasificar (pendiente de info)** | `handle-Photoroom (1).png`, `Prueba_de_slider-removebg-preview.png` | No está claro si siguen en uso — antes de tocarlos hace falta que Alberto confirme si se usan o se borran. |

---

## Total de items inventariados

- Scripts: 49 (48 conformes, 1 con mismatch archivo/clase)
- Prefabs: 20 (4 de "alto riesgo" por la sección 0, resto medio/bajo)
- Sprites (`.png`/`.jpeg`): ~40
- Animaciones/Controllers (`.anim`/`.controller`/`.overrideController`): 46
- CharacterData (`.asset`): 4
- Carpetas con errata o a reorganizar: `Enviroment/` → `Environment/`, más las
  nuevas carpetas propuestas en `Assets/Prefab/` (`UI/`, `Projectiles/`,
  `Enemies/`, `Collectibles/`)

No se propone ningún orden de ejecución, número de PRs, ni agrupación por
sesión — eso queda pendiente de que Alberto autorice el plan y decida cómo
lo quiere trocear.
