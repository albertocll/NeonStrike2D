# Licencias de la música

Registro de los packs de música revisados para NeonStrike2D. El texto de cada licencia está copiado tal cual
de la página de origen (consultada el 24/11/2026). Solo se usan packs gratuitos que permiten el uso comercial
en un juego publicado en Google Play sin pedir atribución.

La música nueva sale de un único pack: Retro Synthwave Music Pack (CC0). Además se mantienen las tres pistas que
ya tenía el juego (ver "Pistas anteriores").

## Pack usado

### Retro Synthwave Music Pack (Swarajthegreat)

- Origen: https://swarajthegreat.itch.io/retro-synthwave-music-pack
- Archivo descargado: `Retro Synthwave.zip` (62 MB, 20 pistas)
- Licencia: CC0 (dominio público). Uso comercial permitido, sin atribución.
- Texto literal de la página:

  > Fully licensed under CC0, so you're free to use them in commercial and private projects without attribution.
  >
  > Ideal for games, videos, podcasts, streams, or any creative endeavor needing a retro-futuristic edge.

- Contenido del zip: 20 pistas en MP3 (48 kHz, estéreo), sin archivo de licencia aparte. La licencia es la de
  la página.
- Nota sobre IA: la ficha de itch.io declara "AI Disclosure: AI Assisted, Graphics, Sounds". No cambia la
  licencia CC0 ni impide el uso comercial; queda anotado por si Google Play u otra tienda lo pregunta.
- Tratamiento: las pistas se convierten de MP3 a OGG Vorbis. Solo se recorta el silencio digital del principio y
  del final. La CC0 permite modificarlas sin límites.

## Packs descartados

### Synthwave Music Pack (AD Sounds)

- Origen: https://ad-sounds.itch.io/synthwave-music-pack
- Motivo: la licencia dice "No license to remix". Pasar a OGG y recortar podría entenderse como modificar la obra.
  El pack CC0 tiene pistas de sobra para los 16 huecos, así que se descarta para no depender de esa
  interpretación (decisión del supervisor, 24/11/2026).
- Licencia: no exclusiva, uso comercial en juegos permitido, sin créditos.
- Texto literal de la página:

  > Hi! This is a Synthwave/Electronic music pack for free for you! You can use it at your commercial games if
  > you want, no credits needed.
  >
  > License:
  >
  > non-exclusive license – you’re free to use these packs in your commercial games. No license to remix or
  > claim as your own work.


### Free Cyber Game Music (Michael Kalra / earentech)

- Origen: https://earentech.itch.io/free-cyber-game-music
- Motivo: pide atribución y se declara equivalente a CC BY-SA 4.0. Fuera de lo permitido (nada de packs CC-BY).
- Texto literal de la página:

  > All assets in this pack are completely free to use — both for commercial and non-commercial projects.
  > Feel free to remix, edit, or reshape them to fit your creative vision.
  >
  > Please credit the music as Michael Kalra
  >
  > The license is essentially CC BY-SA 4.0, with just a few simple rules below.

### Free Asset Pack #13 - Cyberpunk Synth Tracks (François Gérin-Lajoie / oragus)

- Origen: https://oragus.itch.io/free-asset-pack-13-cyberpunk-synth-tracks
- Motivo: la ficha de itch.io indica "Asset license: Creative Commons Attribution v4.0 International" y la
  sección "How to Use the Tracks" pide mencionar al autor. Fuera de lo permitido (nada de packs CC-BY).
- Texto literal de la sección "How to Use the Tracks":

  > These are my original compositions. You are welcomed to use these tracks in any desired ways in your video
  > games, without cost or permission. The only thing that I would ask is for you to give credit where it is due
  > by mentioning by name, François Gérin-Lajoie. Thank you! And if you want, you could also mention which tracks
  > you used in which game in the "comments" section below.

## Pistas usadas

Las 15 pistas nuevas son del Retro Synthwave Music Pack (CC0). El menú conserva su pista anterior.

| Hueco | Archivo en `Assets/Music/` | Pista original |
|---|---|---|
| Menú principal | `MusicMainMenu.ogg` | Pista anterior (ver abajo) |
| Zona Level1 | `MusicZoneLevel1.ogg` | Skyline |
| Zona TheGrid | `MusicZoneTheGrid.ogg` | Sunset |
| Zona PlasmaFrontier | `MusicZonePlasmaFrontier.ogg` | Neon |
| Zona DroneHive | `MusicZoneDroneHive.ogg` | Synthwave |
| Zona CorruptedSignalZone | `MusicZoneCorruptedSignalZone.ogg` | Midnight |
| Zona CoreChamber | `MusicZoneCoreChamber.ogg` | Nostalgia |
| Zona Sector7 | `MusicZoneSector7.ogg` | Chase |
| Crescendo A (niveles 1, 3 y 5) | `MusicCrescendoA.ogg` | Future |
| Crescendo B (niveles 2, 4 y 6) | `MusicCrescendoB.ogg` | Cruise |
| Jefe Sentinel (Level1) | `MusicBossSentinel.ogg` | Traveler |
| Jefe DataCrawler (TheGrid) | `MusicBossDataCrawler.ogg` | Cosmic |
| Jefe Bulwark (PlasmaFrontier) | `MusicBossBulwark.ogg` | Arcade |
| Jefe Hivemind (DroneHive) | `MusicBossHivemind.ogg` | Heartbeat |
| Jefe Specter (CorruptedSignalZone) | `MusicBossSpecter.ogg` | Dreams |
| Jefe NexusCore (CoreChamber) | `MusicBossNexusCore.ogg` | Pixel |

Pistas del pack que no se usan ni entran en el repositorio: Digital, Nights, Electric, Waves y Time.

## Pistas anteriores

Las tres pistas que ya tenía el juego se mantienen por decisión de Alberto (24/11/2026).
Se importan en estéreo, Streaming, Vorbis y calidad 0.9.

Ninguna trae autor, título, comentario ni URL en sus metadatos. Nombre con el que entraron en el repositorio:

- `MusicMainMenu.ogg`: `new_factory_129bpm.ogg` (commit 4f6e53f, 27/02/2026). Etiquetas vacías salvo
  `encoder = FL Studio`.
- `MusicGameplay.mp3`: `leberch-cyberpunk-437545.mp3` (commit d47af2c, 13/04/2026). Sin etiquetas ID3.
- `MusicGameplayIntense.mp3`: entró ya con ese nombre exacto (commit f18fe63, 30/09/2026) y no aparece con otro
  nombre en ninguna rama del historial. Sin etiquetas ID3.

| Archivo en `Assets/Music/` | Uso | Origen y licencia |
|---|---|---|
| `MusicMainMenu.ogg` | Menú principal (`menuMusic`) | Pixabay, licencia de contenido de Pixabay (según Alberto; nombre original no conservado) |
| `MusicGameplay.mp3` | Sustituto de zona (`gameMusic`), sin uso mientras todos los niveles tengan `zoneMusic` | Pixabay, autor leberch, ID 437545, licencia de contenido de Pixabay |
| `MusicGameplayIntense.mp3` | Sustituto de crescendo y jefe (`gameMusicIntense`), sin uso mientras esos huecos tengan pista | Pixabay, licencia de contenido de Pixabay (según Alberto; nombre original no conservado) |
