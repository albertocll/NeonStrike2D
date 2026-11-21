# Imágenes de la ficha de Google Play

Lo que pide la Play Console en **Presencia en Google Play → Ficha principal → Recursos
gráficos**, con los requisitos de cada imagen y su estado.

| Recurso | ¿Obligatorio? | Tamaño y formato | Estado |
|---|---|---|---|
| Icono de la aplicación | Sí | 512 × 512 px, PNG de 32 bits (con o sin transparencia), máximo 1 MB | Hecho: `imagenes/icono-512.png` y alternativa `imagenes/icono-512-sin-texto.png` |
| Gráfico de portada (*feature graphic*) | Sí | 1024 × 500 px, JPEG o PNG de 24 bits **sin transparencia** | Hecho: `imagenes/grafico-portada-1024x500.png` |
| Capturas de teléfono | Sí, de 2 a 8 | JPEG o PNG de 24 bits sin transparencia; lado corto de al menos 320 px y lado largo de 3840 px como máximo; el lado largo no puede medir más del doble del corto | Pendiente (Alberto) |
| Capturas de tablet de 7" | No | Mismos requisitos | Opcional |
| Capturas de tablet de 10" | No | Mismos requisitos | Opcional |
| Vídeo promocional | No | Enlace de YouTube, público o no listado, sin anuncios | Opcional |

## Las imágenes generadas

Están hechas con el arte que ya había en el proyecto, sin elementos nuevos:

- `icono-512.png`: `Assets/Art/UI/Icons/Logo.png` (el logo con el emblema y la palabra
  NEONSTRIKE) escalado a 512 × 512.
- `icono-512-sin-texto.png`: el mismo logo recortado al emblema del rayo, sin la palabra. Google
  recomienda que el icono no lleve texto, porque a tamaño pequeño no se lee. **Elegir uno de los
  dos es decisión de Alberto.**
- `grafico-portada-1024x500.png`: el fondo del menú (`Assets/Art/UI/Backgrounds/Background.png`)
  recortado a 1024 × 500, oscurecido en el centro, con el logo encima.

El icono se sube como cuadrado completo, sin esquinas redondeadas ni sombra: Google le aplica la
máscara y la sombra al mostrarlo.

## Capturas: qué hacer

Para que el juego pueda salir en las recomendaciones de Google Play, Google pide al menos
**4 capturas en horizontal de 1920 × 1080** (16:9). Propuesta, a falta de que Alberto elija:

1. Menú principal con el logo.
2. Selección de personaje.
3. Una oleada normal con varios enemigos y disparos (Level1 o TheGrid).
4. Un jefe (por ejemplo, el NexusCore en CoreChamber).
5. Opcional: partida cooperativa con las dos naves.
6. Opcional: el ranking o la lista de amigos.

Cómo sacarlas: en el móvil, con la captura de pantalla del sistema (salen al tamaño de la
pantalla, que suele valer). En el Editor de Unity, con la vista Game en 1920 × 1080 y
`ScreenCapture.CaptureScreenshot`, o desde **Window → General → Recorder** si está instalado.
Que no salgan barras de depuración ni el FPS.
