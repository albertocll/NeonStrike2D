# Reorganización de carpetas y nomenclatura (#132, #133)

Resumen de los movimientos y renombrados aplicados en la rama
`chore/reorganize-and-rename`. Todos los cambios se hicieron con las
herramientas de Unity MCP (`move_asset` / `rename_asset`), que preservan el
GUID del asset — ninguna referencia en escenas, prefabs o el Inspector se ha
roto. No se ha renombrado ninguna clase C#, solo archivos.

## #132 — Reorganización de `Assets/Script/`

| Antes | Después | GUID (sin cambios) |
|---|---|---|
| `Assets/Script/Core/Collectible.cs` | `Assets/Script/Collectibles/Collectible.cs` | `097e66f4a1e27a74b8dcb9f7182dd1b0` |
| `Assets/Script/Core/CollectibleSpawner.cs` | `Assets/Script/Collectibles/CollectibleSpawner.cs` | `6f86232f999fdd94aba4f95fc7f8f427` |
| `Assets/Script/UI/Joystick.cs` | `Assets/Script/UI/HUD/Joystick.cs` | `39e6983562d554e4b82c7257f08d9d81` |
| `Assets/Script/UI/MultiplayerUI.cs` | `Assets/Script/UI/Menu/MultiplayerUI.cs` | `14fb0983f5ad37d4a8e4c748f81b06b6` |

**Motivación:**
- `Collectible.cs`/`CollectibleSpawner.cs` vivían sueltos en `Core/`, que el
  propio issue #132 señala como una carpeta que "mezcla managers,
  collectibles y power-ups". Se les da una carpeta propia (`Collectibles/`),
  exactamente el mismo patrón que ya existe para `PowerUps/`.
- `Joystick.cs` y `MultiplayerUI.cs` eran los dos scripts que seguían sueltos
  directamente en `UI/` (el issue menciona "UI/ tenía scripts sueltos hasta
  la reorganización parcial de HUD/"). `Joystick.cs` es un control del HUD
  in-game → `UI/HUD/`. `MultiplayerUI.cs` es una pantalla de invitaciones/
  amigos, en la línea de las demás pantallas de `UI/Menu/` (login, opciones,
  etc.) → `UI/Menu/`.

**Alcance reducido a propósito:** no se ha tocado el resto de `Core/`
(`GameData`, `CharacterData`, `ScoreManager`, `MusicManager`, `PlayerSpawner`,
`Language/`) — siguen siendo sistemas transversales coherentes con la
descripción de `CLAUDE.md`. Mover `PlayerSpawner.cs` a `Player/` se consideró
pero se descartó: orquesta el spawn de ambos jugadores a partir de
`GameData`, encaja igual de bien como sistema "Core".

## #133 — Nomenclatura

**Renombrados aplicados** (los 2 ejemplos concretos que cita el propio issue
como motivación):

| Antes | Después | GUID (sin cambios) |
|---|---|---|
| `Assets/Art/Collectibles/Collectibe_Health.png` | `Assets/Art/Collectibles/Collectible_Health.png` | `67c98fb4cc8c7914fb98faafd71bc935` |
| `Assets/Art/Canvas/Dashbutton.png` | `Assets/Art/Canvas/DashButton.png` | `09d392c36adeffb48ba6b11d8a615b54` |

Nota de proceso: el renombrado de `Dashbutton.png` → `DashButton.png` sólo
difiere en mayúsculas, y Windows/Unity lo detectan como "ya existe" al ser
case-insensitive. Se resolvió con un paso intermedio
(`Dashbutton.png` → `DashButton_tmp.png` → `DashButton.png`), sin perder el
GUID en ningún momento.

**Alcance reducido a propósito, documentado como pendiente:** el issue #133
habla de una convención "aplicada de forma retroactiva a los assets
existentes", pero el proyecto tiene decenas de prefabs/sprites ya
consistentes-pero-con-estilos distintos entre sí (p. ej. `PowerUpShield.prefab`
sin separador vs. `Collectible_Health.prefab`/`Enemy_Striker.prefab` con
guion bajo entre categoría y nombre). Renombrar todo eso de una sentada, sin
supervisión de Alberto, es un cambio de superficie muy grande para el mismo
riesgo que ya se evitó en #132 (por eso no se tocaron clases). Se ha dejado
así, deliberadamente, y documentado aquí como pendiente para una pasada futura
con más tiempo/revisión.

**Convención documentada en `CLAUDE.md`** (no versionado, sigue en
`.gitignore`, pero sirve de referencia para el equipo y para sesiones futuras
de Claude Code):
- Scripts C#: `PascalCase.cs`, el nombre del archivo coincide siempre con el
  de la clase (ya se cumplía en todo el proyecto).
- Prefabs y sprites agrupados por categoría: `Categoria_Nombre` en
  PascalCase con guion bajo como separador entre categoría y nombre
  específico (patrón ya dominante: `Collectible_Chip`, `Collectible_Crystal`,
  `Collectible_Health`, `Collectible_Star`, `Enemy_Striker`, `Enemy_Warden`).
- Carpetas: `PascalCase`, sin espacios ni guiones.
- Sin erratas ni palabras pegadas sin separador (motivo original de este
  issue).

## Verificación

- `recompile()` vía Unity MCP → `up_to_date`, sin errores de compilación.
- Consola de Unity revisada tras cada movimiento/renombrado → sin errores
  nuevos.
- `CollectibleSpawner` en `Level1.unity` sigue resolviendo correctamente tras
  el movimiento (mismo componente, mismos campos, mismas referencias a
  prefabs).
- `Joystick` sigue encontrándose como componente en
  `/UI_Canvas/JoystickArea` en `Level1.unity`.
- `MultiplayerUI` no vive en `Level1.unity` (es de la pantalla de selección
  de personaje) — confirmado que su GUID sigue referenciado correctamente en
  `Assets/Scenes/CharacterSelect.unity`.
