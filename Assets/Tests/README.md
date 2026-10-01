# Tests (Unity Test Framework)

Tests automatizados del cliente Unity, usando el paquete integrado
`com.unity.test-framework` (ya estaba en `Packages/manifest.json`, no se ha
instalado nada externo). Ver issue #134.

## Estructura

- `Assets/Tests/EditMode/` — tests que no necesitan Play Mode, corren directos
  en el Editor. Cubren calculo puro (formulas, seleccion de datos), sin
  `Instantiate` real, red, audio ni UI.
  - `WaveManagerTests.cs` — formula de enemigos por oleada (`WaveManager.EnemiesForWave`, #165).
  - `EnemySpawnerTests.cs` — umbral de enemigo avanzado por oleada (`EnemySpawner.ChooseEnemyPrefab`, #165).
  - `WeaponControllerBulletScaleTests.cs` — asignacion de sprite/escala de bala por personaje (`WeaponController.SetBulletSprite`, #130).
- `Assets/Tests/PlayMode/` — tests que necesitan el bucle de juego real
  (`MonoBehaviour.Awake`, corutinas, `AddComponent` con `RequireComponent`).
  - `MusicManagerCrossfadeGuardTests.cs` — guard de reentrada del crossfade intenso (`MusicManager.StartIntenseCrossfade`, #148).
  - `PlayerSpawnerRemoteTagTests.cs` — el avatar remoto no se queda con el tag `Player` (`PlayerSpawner.SpawnRemotePlayer`, #150).

Fuera de alcance deliberadamente: tests de UI, de audio real (reproduccion/mezcla)
o de cualquier otra cosa visual/auditiva — solo logica de codigo verificable
por aserciones.

## Como correr los tests

En el Editor de Unity: **Window → General → Test Runner**.

- Pestaña **EditMode**: pulsar **Run All** corre los tests de `Assets/Tests/EditMode/`
  sin entrar en Play Mode.
- Pestaña **PlayMode**: pulsar **Run All** entra en Play Mode automaticamente,
  corre los tests de `Assets/Tests/PlayMode/` y sale al terminar.

Tambien por linea de comandos (CI o fuera del Editor), sustituyendo la ruta al
ejecutable de Unity por la que corresponda:

```bash
Unity -runTests -batchmode -projectPath . -testResults results-editmode.xml -testPlatform EditMode -quit
Unity -runTests -batchmode -projectPath . -testResults results-playmode.xml -testPlatform PlayMode -quit
```

## Notas

- Cada ensamblado de test (`NeonStrike2D.Tests.EditMode`/`NeonStrike2D.Tests.PlayMode`)
  referencia el ensamblado principal del proyecto (`Assembly-CSharp`), que no
  tiene su propio `.asmdef` — es el default del proyecto.
- Varios metodos bajo prueba eran `private`; donde tenia sentido se expusieron
  como `public` (p.ej. `WaveManager.EnemiesForWave`, `EnemySpawner.ChooseEnemyPrefab`)
  en vez de usar reflexion, porque son calculos puros sin efectos secundarios.
  Donde exponerlo no tenia sentido (p.ej. el campo `crossfadeRoutine` de
  `MusicManager`, o metodos internos de `PlayerSpawner`), los tests acceden
  por reflexion en vez de cambiar la visibilidad de la API de produccion.
