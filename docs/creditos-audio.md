# Música y efectos de sonido: origen y licencia

> **Borrador para publicar (#294).** Los archivos no traen metadatos de autor ni de licencia, así que el origen se ha deducido del nombre original de cada archivo en el historial de git. Antes de publicar en Google Play, Alberto tiene que confirmar las filas marcadas como **Por confirmar**, guardar el justificante de cada licencia (captura de la página de descarga o recibo) y, si alguna exige atribución, añadirla a los créditos.

## Música (`Assets/Music`)

| Archivo | Dónde suena | Nombre original | Origen | Licencia | Atribución | Estado |
|---|---|---|---|---|---|---|
| `MusicMainMenu.ogg` | Menú principal | `new_factory_129bpm.ogg` (commit inicial, 27/02/2026) | Desconocido. Exportado con FL Studio, sin autor en los metadatos. | — | — | **Por confirmar**: si es propia, poner «Composición propia»; si no, el sitio y la licencia. |
| `MusicGameplay.mp3` | Niveles | `leberch-cyberpunk-437545.mp3` (13/04/2026) | Probablemente Pixabay: el nombre sigue su formato `autor-título-id` (autor «leberch», tema «Cyberpunk», id 437545). | Si es de Pixabay: [Licencia de contenido de Pixabay](https://pixabay.com/service/license-summary/). Permite uso comercial en juegos sin pagar. | No obligatoria en Pixabay | **Por confirmar**: buscar el id 437545 en pixabay.com/music y guardar la página. |
| `MusicGameplayIntense.mp3` | Oleadas intensas y jefes | Se añadió ya con este nombre (30/09/2026) | Desconocido | — | — | **Por confirmar** |

## Efectos de sonido (`Assets/SFX`)

Los 9 se añadieron juntos el 19/09/2026 (#104) con su nombre actual. Están recodificados con FFmpeg (`Lavf59.27.100`), así que el archivo no conserva el origen.

| Archivo | Acción | Origen | Licencia | Estado |
|---|---|---|---|---|
| `SfxPlayerShoot.mp3` | Disparo del jugador | Desconocido | — | **Por confirmar** |
| `SfxEnemyHit.mp3` | Impacto en enemigo | Desconocido | — | **Por confirmar** |
| `SfxEnemyDeath.mp3` | Muerte de enemigo | Desconocido | — | **Por confirmar** |
| `SfxCollectibleScore.mp3` | Recoger puntos | Desconocido | — | **Por confirmar** |
| `SfxCollectibleHealth.mp3` | Recoger vida | Desconocido | — | **Por confirmar** |
| `SfxPlayerDamage.mp3` | El jugador recibe daño | Desconocido | — | **Por confirmar** |
| `SfxPlayerDeath.mp3` | Muerte del jugador | Desconocido | — | **Por confirmar** |
| `SfxWaveStart.mp3` | Empieza una oleada | Desconocido | — | **Por confirmar** |
| `SfxWaveBonus.mp3` | Bonus de fin de oleada | Desconocido | — | **Por confirmar** |

## Otros recursos con licencia de terceros

| Recurso | Licencia | Atribución |
|---|---|---|
| Fuente Roboto (`Assets/Fonts/Roboto-VariableFont_wdth,wght.ttf`, de Google Fonts) | SIL Open Font License 1.1 | No obligatoria |
| Fuente `Assets/Fonts/Orbitronio.ttf` | Si es Orbitron de Google Fonts: SIL Open Font License 1.1. **Por confirmar**: el nombre del archivo no coincide con el oficial. | No obligatoria |
| Sprites EmojiOne de ejemplo de TextMesh Pro (`Assets/TextMesh Pro/Sprites`) | Los de EmojiOne (ver `EmojiOne Attribution.txt`); vienen con el paquete de Unity | Solo si se usan en el juego; si no, se pueden quitar del build |

## Qué aceptar y qué no

- **Vale para Google Play:** licencias que permiten uso comercial. Por ejemplo, Pixabay, CC0, CC BY (con atribución), packs comprados con licencia para juegos o audio propio.
- **No vale:** CC BY-NC (no comercial), CC BY-ND si se ha editado el audio, o audio sacado de otros juegos, películas o YouTube sin licencia.
- Si una pista no se puede confirmar, lo seguro es sustituirla por otra de origen conocido antes de publicar.
