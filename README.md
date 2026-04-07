# GregTools Modmanager

[![Sponsor mleem97](https://img.shields.io/badge/Sponsor-mleem97-EA4AAA?style=for-the-badge&logo=GitHub-Sponsors&logoColor=white)](https://github.com/sponsors/mleem97)
[![Desktop Build](https://img.shields.io/github/actions/workflow/status/mleem97/GregToolsModmanager/dotnet-desktop.yml?branch=main&style=for-the-badge&label=Desktop%20Build)](https://github.com/mleem97/GregToolsModmanager/actions/workflows/dotnet-desktop.yml)
[![Discord Notify](https://img.shields.io/github/actions/workflow/status/mleem97/GregToolsModmanager/discord-release-notify.yml?branch=main&style=for-the-badge&label=Discord%20Notify)](https://github.com/mleem97/GregToolsModmanager/actions/workflows/discord-release-notify.yml)
[![Daily Security Scan](https://img.shields.io/github/actions/workflow/status/mleem97/GregToolsModmanager/daily-malicious-code-scan.yml?branch=main&style=for-the-badge&label=Daily%20Security%20Scan)](https://github.com/mleem97/GregToolsModmanager/actions/workflows/daily-malicious-code-scan.yml)
[![Self-Signed Setup](https://img.shields.io/github/actions/workflow/status/mleem97/GregToolsModmanager/selfsigned-setup.yml?branch=main&style=for-the-badge&label=Self-Signed%20Setup)](https://github.com/mleem97/GregToolsModmanager/actions/workflows/selfsigned-setup.yml)

Desktop app for **Steam Workshop** management, mod browsing, and publishing for **Data Center** (Steamworks API, App ID `4170200`).

## Open-source and external dependencies

This project is developed in the open and uses many open-source libraries (.NET, MAUI, Facepunch.Steamworks, and more). It also ships Valve’s closed-source `steam_api64.dll` (Steamworks), which is governed by Valve terms.

See [EXTERNAL_DEPENDENCIES.md](./EXTERNAL_DEPENDENCIES.md) for a full license and redistribution breakdown.

## Features

- **Mod Store:** Browse, search, subscribe, favorite, and vote on Workshop items.
- **Mod Manager:** Dependency health checks, MelonLoader status, and FMF plugin channels.
- **Authoring tools:** Create projects from templates, edit metadata, and publish with change notes.
- **Template scaffolding:** Modded templates create `content/Mods`, `content/Plugins`, and `content/ModFramework/` (including `ModFramework/FMF/Plugins`) to mirror `{GameRoot}` layout.
- **Post-upload sync:** Re-downloads from Steam after publish to keep local content in sync.
- **Headless CLI:** Supports scripted/CI publish flows.
- **Pagination:** All major list views support paging.

## Open in Visual Studio

Use `WorkshopUploader.sln` in this repository root.

If you open only `WorkshopUploader.csproj` from another solution context, Visual Studio may pick a different solution unexpectedly.

## Build

```powershell
dotnet build WorkshopUploader.csproj -c Debug
```

Or:

```powershell
dotnet build WorkshopUploader.sln -c Debug
```

Target: **.NET 9 + .NET MAUI (Windows)**. The project uses `WindowsAppSDKSelfContained` so required Windows App SDK components are shipped with the app.

## Run (recommended)

- Use **Visual Studio 2022** with the **.NET MAUI workload** and **Windows App SDK** components.
- Open `WorkshopUploader.sln`.
- Set `WorkshopUploader` as startup project.
- Press `F5`.

## Publish (`win10-x64`)

```powershell
dotnet publish WorkshopUploader.csproj -c Release
```

Output:

`bin/Release/net9.0-windows10.0.19041.0/win10-x64/publish/WorkshopUploader.exe`

### Publish to a custom self-contained folder

```powershell
dotnet publish WorkshopUploader.csproj -c Release -p:SelfContained=true -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -o .\publish-out
```

## Create installer (`Setup.exe` via Inno Setup)

1. Install [Inno Setup 6](https://jrsoftware.org/isdl.php) (includes `ISCC.exe`).
2. Run:

```powershell
.\build.ps1
```

This runs `dotnet publish` and creates:

`installer\Output\GregToolsModmanager-<Version>-Setup.exe`

Installer behavior:

- Wizard install/uninstall integration in Windows Apps settings.
- Start menu entry and optional desktop shortcut.
- Default install path: `C:\Program Files\GregTools Modmanager` (admin required).

Useful options:

- Skip publish and only rebuild setup: `./build.ps1 -SkipPublish`
- Inno script path: `installer\GregToolsModmanager.iss`

### Update/reinstall behavior

- Setup uses the same `AppId`, detects existing installs, and overwrites the target folder.
- Running `WorkshopUploader.exe` is closed through Windows Restart Manager (`CloseApplications`).
- Portable install via `install-local.ps1` also closes the app and replaces the install directory.

### App does not start after setup

- The installer runs elevated, but final launch uses `runasoriginaluser` so the app starts non-elevated (important for WinUI/WebView2/MAUI stability).
- If problems continue, check Event Viewer and test `CloseApplications=no` in `GregToolsModmanager.iss`.

## Code signing

- Official OV/EV code signing is currently not enabled due to certificate cost.
- A self-signed CI path is available for community/testing builds.
- Manual signing docs: `installer\CODE_SIGNING.md`.
- Create a self-signed certificate:

```powershell
.\installer\create-selfsigned-codesign-cert.ps1
```

- Sign only (without rebuilding setup):

```powershell
.\build.ps1 -SignOnly
```

Set `CODE_SIGN_THUMBPRINT` (or use `-SetupPath` when needed).

## Portable install (no Setup.exe)

```powershell
.\install-local.ps1
```

Installs per-user to `%LOCALAPPDATA%\Programs\GregTools Modmanager\` (no admin).

Uninstall:

```powershell
.\install-local.ps1 -Uninstall
```

## Crash dumps (WER LocalDumps)

Enable local dumps:

```powershell
.\installer\configure-localdumps.ps1
```

Enable machine-wide (elevated shell):

```powershell
.\installer\configure-localdumps.ps1 -Scope Machine
```

Disable:

```powershell
.\installer\configure-localdumps.ps1 -Disable
```

Default dump directories:

- Current user: `%LOCALAPPDATA%\GregToolsModmanager\dumps`
- Machine: `C:\ProgramData\GregToolsModmanager\dumps`

## Deploy all mods to Workshop folders

```powershell
pwsh -File scripts/Deploy-Release-ToWorkshop.ps1
```

Builds framework/plugins/mods and creates Steamworks-compatible project folders under `<GameRoot>/workshop/`.

## Troubleshooting

1. Open **Event Viewer** → **Windows Logs** → **Application** and look for `WorkshopUploader.exe` faults.
2. Install/repair the [Microsoft Visual C++ Redistributable](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist).
3. Install the [Windows App SDK Runtime](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads).
4. Prefer running with `F5` from Visual Studio on the same machine you use to build.
5. Ensure Windows 10 version 1809+ (OS build `17763+`).

## Deploy next to the game

Copy the publish output to:

`{GameRoot}/WorkshopUploader/`

Place it next to the game executable (not inside `Mods` or `MelonLoader`).

## VirusTotal

Third-party scan for transparency (self-contained .NET apps may be flagged heuristically; always compare checksums from official releases):

- **SHA-256:** `c0ea7929eee9d754e81363e9ec81c601e763e65f7db1eb0d971edf2c2036f0af`
- **Report:** [VirusTotal file relations](https://www.virustotal.com/gui/file/c0ea7929eee9d754e81363e9ec81c601e763e65f7db1eb0d971edf2c2036f0af/relations)

## Sponsorship

If this project helps you and you want to support ongoing maintenance and improvements:

- **Sponsor:** [github.com/sponsors/mleem97](https://github.com/sponsors/mleem97)

## See also

- [External dependencies and distribution notes](./EXTERNAL_DEPENDENCIES.md)
- [Workshop wiki page](../docs/wiki/tools/workshop-uploader.md)
- [End-user guide](../wiki/docs/guides/enduser-workshop.md)
- [Contributor guide](../wiki/docs/guides/contributor-workshop.md)
