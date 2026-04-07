# Erstellt ein Release-Publish und kompiliert eine echte Setup-EXE mit Inno Setup 6
# (Assistent, Eintrag unter „Apps“, Deinstallieren, Desktop-Verknüpfung optional).
#
# Voraussetzung: Inno Setup 6 installieren — https://jrsoftware.org/isdl.php
#
# Aus diesem Ordner: .\build.ps1
# Nur Setup neu bauen (Publish schon vorhanden): .\build.ps1 -SkipPublish
# Nur signieren (ohne Inno Setup): .\build.ps1 -SignOnly  (+ CODE_SIGN_THUMBPRINT)
# Nach dem Setup Authenticode-Signatur: .\build.ps1 -Sign
#   Umgebung: CODE_SIGN_THUMBPRINT=... oder CODE_SIGN_PFX=... + CODE_SIGN_PFX_PASSWORD
#Requires -Version 5.1
param(
    [switch]$SkipPublish,
    [switch]$Sign,
    [switch]$SignOnly,
    [string]$SetupPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$isWindowsHost = $IsWindows -or [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)

function Invoke-BuildSign {
    param([Parameter(Mandatory)][string]$TargetPath)
    $signScript = Join-Path $PSScriptRoot 'installer\sign-authenticode.ps1'
    if (-not (Test-Path -LiteralPath $signScript)) {
        throw "Signierskript fehlt: $signScript"
    }
    $thumb = $env:CODE_SIGN_THUMBPRINT
    $pfx = $env:CODE_SIGN_PFX
    if ([string]::IsNullOrWhiteSpace($thumb) -eq [string]::IsNullOrWhiteSpace($pfx)) {
        throw "CODE_SIGN_THUMBPRINT oder CODE_SIGN_PFX setzen (siehe installer\CODE_SIGNING.md)."
    }
    if (-not (Test-Path -LiteralPath $TargetPath)) {
        throw "Datei zum Signieren nicht gefunden: $TargetPath"
    }
    Write-Host "[build] Authenticode-Signatur: $TargetPath"
    if (-not [string]::IsNullOrWhiteSpace($thumb)) {
        $t = $thumb.Trim()
        if ($t -match '<|>') {
            throw "CODE_SIGN_THUMBPRINT ist noch ein Platzhalter — den echten 40-stelligen Hex-Thumbprint aus create-selfsigned-codesign-cert.ps1 einsetzen."
        }
        & $signScript -Path $TargetPath -Thumbprint $t
    } else {
        & $signScript -Path $TargetPath -PfxPath $pfx.Trim()
    }
}

if ($SignOnly) {
    if (-not $isWindowsHost) {
        throw "-SignOnly is only supported on Windows (Authenticode requires Windows tooling)."
    }

    $outDir = Join-Path $PSScriptRoot 'installer\Output'
    $resolved = $SetupPath
    if ([string]::IsNullOrWhiteSpace($resolved)) {
        if (-not (Test-Path -LiteralPath $outDir)) {
            throw "Ordner fehlt: $outDir — Setup-EXE bereitstellen oder -SetupPath `"C:\pfad\Setup.exe`" angeben."
        }
        $latest = Get-ChildItem -LiteralPath $outDir -Filter 'GregToolsModmanager-*-Setup.exe' -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1
        if (-not $latest) {
            throw "Keine GregToolsModmanager-*-Setup.exe unter $outDir. Inno-Build ausführen oder -SetupPath verwenden."
        }
        $resolved = $latest.FullName
    }
    Write-Host "[build] -SignOnly → $resolved"
    Invoke-BuildSign -TargetPath $resolved
    exit 0
}

$isccCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
)
$iscc = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $iscc) {
    throw @"
Inno Setup 6 Compiler (ISCC.exe) nicht gefunden.

Erwartet unter:
  $($isccCandidates[0])
  $($isccCandidates[1])
  $($isccCandidates[2]) (z. B. winget: JRSoftware.InnoSetup)

Download: https://jrsoftware.org/isdl.php
Nur signieren (ohne Inno): .\build.ps1 -SignOnly

Nach der Installation ggf. PowerShell neu starten.
"@
}

$projPath = Join-Path $PSScriptRoot 'WorkshopUploader.csproj'
$csproj = [xml](Get-Content -LiteralPath $projPath -Raw)
$ver = (
    $csproj.Project.PropertyGroup |
    ForEach-Object { $_.ApplicationDisplayVersion } |
    Where-Object { $_ } |
    Select-Object -First 1
).Trim()
if ([string]::IsNullOrWhiteSpace($ver)) {
    $ver = '1.0.0'
}

$publishDir = Join-Path $PSScriptRoot 'bin\Release\net9.0-windows10.0.19041.0\win10-x64\publish'
$iss = Join-Path $PSScriptRoot 'installer\GregToolsModmanager.iss'
$outDir = Join-Path $PSScriptRoot 'installer\Output'

if (-not $isWindowsHost) {
    if ($Sign) {
        throw "-Sign is only supported on Windows (Authenticode/signing certificate store is Windows-specific)."
    }

    if (Test-Path -LiteralPath $publishDir) {
        Write-Host "[build] Cleaning old publish output: $publishDir"
        Remove-Item -LiteralPath $publishDir -Recurse -Force
    }

    Write-Host '[build] Non-Windows environment detected: creating portable publish only.'
    & dotnet publish $projPath -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (-not (Test-Path -LiteralPath $publishDir)) {
        throw "Publish output not found: $publishDir"
    }

    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    $zipPath = Join-Path $outDir ("GregToolsModmanager-$ver-Portable.zip")
    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Host "[build] Portable package created: $zipPath"
    Write-Host '[build] Setup/Authenticode signing is skipped on non-Windows hosts.'
    exit 0
}

if (-not $SkipPublish) {
    if (Test-Path -LiteralPath $publishDir) {
        Write-Host "[build] Bereinige alte Publish-Ausgabe: $publishDir"
        Remove-Item -LiteralPath $publishDir -Recurse -Force
    }
    Write-Host '[build] dotnet publish -c Release ...'
    & dotnet publish $projPath -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} else {
    Write-Warning "-SkipPublish aktiv: Es wird eine bestehende Publish-Ausgabe verpackt. Bei Startproblemen bitte ohne -SkipPublish neu bauen."
}

if (-not (Test-Path -LiteralPath $publishDir)) {
    throw "Publish-Ausgabe nicht gefunden: $publishDir"
}

if (-not (Test-Path -LiteralPath $iss)) {
    throw "Inno-Skript fehlt: $iss"
}

Write-Host "[build] Inno Setup ($iscc) — Version $ver ..."
$argList = @(
    $iss
    "/DMyAppVersion=$ver"
)
& $iscc @argList
if ($LASTEXITCODE -ne 0) {
    throw "ISCC beendet mit Code $LASTEXITCODE"
}

$setupName = "GregToolsModmanager-$ver-Setup.exe"
$setupPath = Join-Path $outDir $setupName
if (Test-Path -LiteralPath $setupPath) {
    $len = (Get-Item -LiteralPath $setupPath).Length
    $mb = [math]::Round($len / 1MB, 2)
    Write-Host ''
    Write-Host "[build] Fertig: $setupPath ($mb MB)"
} else {
    Write-Host '[build] ISCC ohne Fehler — Ausgabedatei bitte unter installer\Output prüfen.'
}

$wantSign = $Sign -or $env:CODE_SIGN_THUMBPRINT -or $env:CODE_SIGN_PFX
if ($wantSign) {
    Write-Host ''
    Invoke-BuildSign -TargetPath $setupPath

    $appExePath = Join-Path $publishDir 'WorkshopUploader.exe'
    if (-not (Test-Path -LiteralPath $appExePath)) {
        throw "Publish-EXE nicht gefunden: $appExePath"
    }

    Invoke-BuildSign -TargetPath $appExePath
}
