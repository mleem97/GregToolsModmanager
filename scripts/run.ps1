# Startet GregModmanager (MAUI Windows). Ausführen aus diesem Ordner: .\run.ps1
# Optional: .\run.ps1 -- -h  (Argumente nach -- gehen an die App)
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $repoRoot
dotnet run -c Release -f net9.0-windows10.0.19041.0 @args
