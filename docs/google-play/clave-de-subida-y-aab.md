# Clave de subida y generación del AAB

Pasos para firmar y generar el paquete que se sube a Google Play. La clave la crea Alberto en
su equipo; **ni la clave ni sus contraseñas entran nunca en el repo** (`.gitignore` ya ignora
`*.keystore` y `*.jks`, pero la clave se guarda además fuera de la carpeta del proyecto).

Google Play usa *Firma de aplicaciones de Play*: Google guarda la clave con la que se firma la
app final y nosotros solo tenemos la **clave de subida**. Si la clave de subida se pierde,
Google permite cambiarla desde la Play Console. Aun así, conviene no perderla.

## 1. Crear la clave de subida (una sola vez)

En PowerShell, con el `keytool` que trae Unity:

```powershell
New-Item -ItemType Directory -Force "$env:USERPROFILE\ClavesAndroid"

& "C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe" `
  -genkeypair -v `
  -keystore "$env:USERPROFILE\ClavesAndroid\neonstrike2d-subida.keystore" `
  -storetype PKCS12 `
  -alias subida `
  -keyalg RSA -keysize 2048 `
  -validity 10000
```

- Pide una contraseña (la del almacén) y después tu nombre, organización, ciudad, provincia y
  país (`ES`). Con `PKCS12` la contraseña de la clave es la misma que la del almacén.
- La clave queda en `C:\Users\<usuario>\ClavesAndroid\neonstrike2d-subida.keystore`, fuera del
  proyecto.
- Haz una copia de ese archivo en otro sitio seguro (un USB o una nube cifrada) y guarda la
  contraseña en un gestor de contraseñas.

## 2. Configurarla en Unity sin dejar contraseñas en el repo

1. **Edit → Project Settings → Player → Android → Publishing Settings**.
2. Marca **Custom Keystore**, pulsa **Select** y elige
   `C:\Users\<usuario>\ClavesAndroid\neonstrike2d-subida.keystore`.
3. En **Project Key → Alias** elige `subida`.
4. No hace falta escribir las contraseñas ahí: el menú de build las lee de variables de entorno.
   Unity tampoco las guarda en `ProjectSettings.asset`; solo guarda la ruta del archivo y el
   alias, que no son secretos.

Las contraseñas van en dos variables de entorno que lee `Assets/Editor/BuildAndroidAab.cs`:

- `NEONSTRIKE_KEYSTORE_PASS`: contraseña del almacén.
- `NEONSTRIKE_KEY_PASS`: contraseña de la clave. Con `PKCS12` es la misma.

Opción recomendada, solo para la sesión: abre Unity desde una PowerShell en la que pongas las
variables. Así no quedan guardadas en ningún sitio:

```powershell
$env:NEONSTRIKE_KEYSTORE_PASS = Read-Host "Contraseña de la clave de subida"
$env:NEONSTRIKE_KEY_PASS = $env:NEONSTRIKE_KEYSTORE_PASS
& "C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe" -projectPath "C:\Users\alber\Desktop\NeonStrike2D"
```

(Cierra antes Unity si ya estaba abierto con el proyecto.)

Ojo: si las variables no están puestas, el menú pone las contraseñas en blanco, aunque las
hayas escrito en Publishing Settings, y la firma falla.

## 3. Generar el AAB (`chore/build-google-play`, PR #277, ya en master)

1. Comprueba en **Player → Other Settings**:
   - **Version** (`bundleVersion`, ahora `1.0.0`).
   - **Bundle Version Code** (ahora `1`). Hay que subirlo en cada AAB que se suba a Google
     Play: 1, 2, 3...
   - El identificador del paquete debe ser `com.albertocll.neonstrike2d`.
2. Menú **NeonStrike2D → Build Android (AAB)**. El menú:
   - cambia la plataforma a Android si hace falta;
   - pone IL2CPP y solo ARM64 (lo que pide Google Play);
   - activa el formato App Bundle;
   - firma con la clave de subida si **Custom Keystore** está marcado, con las contraseñas de las
     variables de entorno;
   - construye todas las escenas activas de Build Settings.
3. El resultado queda en `Builds/Android/NeonStrike2D-<versión>.aab` (la carpeta `Builds/` está
   en `.gitignore`).
4. Mira la consola:
   - `[BuildAndroidAab] Builds/Android/NeonStrike2D-1.0.0.aab (N MB)`: correcto.
   - Si termina en `SIN FIRMAR: falta la keystore de subida`, está firmado con la clave de
     depuración y Google Play lo rechazará. Repasa el paso 2.
   - `La build ha fallado`: el detalle está en la consola, encima de esa línea.
5. Sube el `.aab` en Play Console → **Pruebas → Prueba interna** (o la pista que toque) →
   **Crear versión**. La primera vez, acepta la *Firma de aplicaciones de Play*.
