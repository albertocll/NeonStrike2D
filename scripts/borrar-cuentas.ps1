# Borra TODAS las cuentas de NeonStrike2D y sus datos (issue #284), con copia de seguridad previa.
#
# Uso (PowerShell, desde la raiz del repo):
#   $env:DATABASE_URL = "postgresql://usuario:contrasena@host:puerto/base"   # la URL publica de Railway
#   ./scripts/borrar-cuentas.ps1
#   Remove-Item Env:DATABASE_URL
#
# Que hace:
#   1. Copia de seguridad completa con pg_dump en backups/ (no se sube al repo).
#   2. Comprueba que la copia no esta vacia; si falla, se para sin borrar nada.
#   3. Muestra cuantas filas hay y pide escribir BORRAR.
#   4. Vacia Users, Friendships, GameSessions y GameResults en una transaccion y reinicia los ids.
#      No toca __EFMigrationsHistory ni el esquema: el backend arranca igual.
#
# Restaurar la copia (borra las tablas actuales y las vuelve a crear con los datos de la copia):
#   psql "$env:DATABASE_URL" -v ON_ERROR_STOP=1 -f backups/<archivo>.sql
#   (sin psql: Get-Content backups/<archivo>.sql -Raw | docker run --rm -i -e DATABASE_URL postgres:17-alpine sh -c 'psql -d "$DATABASE_URL" -v ON_ERROR_STOP=1')
#
# Usa pg_dump y psql si estan instalados; si no, la imagen oficial de PostgreSQL en Docker.

$ErrorActionPreference = 'Stop'

$PgImage = 'postgres:17-alpine'   # pg_dump tiene que ser igual o mas nuevo que el servidor
$Tables = @('Users', 'Friendships', 'GameSessions', 'GameResults')

if ([string]::IsNullOrWhiteSpace($env:DATABASE_URL)) {
    Write-Error 'Falta la variable de entorno DATABASE_URL (la URL publica de PostgreSQL en Railway).'
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$backupDir = Join-Path $repoRoot 'backups'
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
$backupName = "neonstrike2d-$(Get-Date -Format 'yyyyMMdd-HHmmss').sql"
$backupPath = Join-Path $backupDir $backupName

$useDocker = -not (Get-Command pg_dump -ErrorAction SilentlyContinue) -or -not (Get-Command psql -ErrorAction SilentlyContinue)
if ($useDocker -and -not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error 'No hay pg_dump/psql ni Docker. Instala las herramientas de PostgreSQL o abre Docker Desktop.'
}

# La URL va por variable de entorno, no por la linea de comandos, para que no salga en la lista de procesos.
function Invoke-PgDump {
    if ($useDocker) {
        docker run --rm -e DATABASE_URL -v "${backupDir}:/backup" $PgImage `
            sh -c "pg_dump --clean --if-exists --no-owner --no-privileges -d `"`$DATABASE_URL`" -f /backup/$backupName"
    } else {
        pg_dump --clean --if-exists --no-owner --no-privileges -d $env:DATABASE_URL -f $backupPath
    }
    if ($LASTEXITCODE -ne 0) { Write-Error "pg_dump ha fallado (codigo $LASTEXITCODE). No se ha borrado nada." }
}

function Invoke-Psql([string]$sql) {
    if ($useDocker) {
        $out = $sql | docker run --rm -i -e DATABASE_URL $PgImage sh -c "psql -d `"`$DATABASE_URL`" -v ON_ERROR_STOP=1 -At"
    } else {
        $out = $sql | psql -d $env:DATABASE_URL -v ON_ERROR_STOP=1 -At
    }
    if ($LASTEXITCODE -ne 0) { Write-Error "psql ha fallado (codigo $LASTEXITCODE)." }
    return $out
}

function Show-Counts([string]$title) {
    $sql = ($Tables | ForEach-Object { "SELECT '$_', count(*) FROM `"$_`";" }) -join "`n"
    Write-Host $title
    Invoke-Psql $sql | ForEach-Object { $p = $_ -split '\|'; Write-Host ("  {0,-14} {1}" -f $p[0], $p[1]) }
}

# 1-2. Copia de seguridad
Write-Host "Copia de seguridad en $backupPath ..."
Invoke-PgDump
if (-not (Test-Path $backupPath) -or (Get-Item $backupPath).Length -eq 0 -or -not (Select-String -Path $backupPath -Pattern 'CREATE TABLE' -Quiet)) {
    Write-Error 'La copia de seguridad no existe, esta vacia o no tiene tablas. No se ha borrado nada.'
}
Write-Host ("Copia hecha: {0:N0} KB" -f ((Get-Item $backupPath).Length / 1KB))

# 3. Confirmacion
Show-Counts 'Filas actuales:'
$answer = Read-Host 'Esto borra TODAS las cuentas, amistades y partidas. Escribe BORRAR para seguir'
if ($answer -cne 'BORRAR') {
    Write-Host 'Cancelado. No se ha borrado nada.'
    exit 0
}

# 4. Borrado en una transaccion
$quoted = ($Tables | ForEach-Object { "`"$_`"" }) -join ', '
Invoke-Psql "BEGIN; TRUNCATE $quoted RESTART IDENTITY; COMMIT;" | Out-Null
Show-Counts 'Filas despues del borrado:'
Write-Host "Hecho. Copia de seguridad: $backupPath"
