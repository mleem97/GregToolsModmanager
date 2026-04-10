# gregModmanager

**GregTools Modmanager** — desktop app for **Steam Workshop** management, mod browsing, and publishing for **Data Center** (Steamworks API, App ID `4170200`).

[![Sponsor mleem97](https://img.shields.io/badge/Sponsor-mleem97-EA4AAA?style=for-the-badge&logo=GitHub-Sponsors&logoColor=white)](https://github.com/sponsors/mleem97)
[![Build & Sign](https://img.shields.io/github/actions/workflow/status/mleem97/GregToolsModmanager/build-and-sign.yml?branch=main&style=for-the-badge&label=Build%20%26%20Sign)](https://github.com/mleem97/GregToolsModmanager/actions/workflows/build-and-sign.yml)
[![Discord Notify](https://img.shields.io/github/actions/workflow/status/mleem97/GregToolsModmanager/discord-release-notify.yml?branch=main&style=for-the-badge&label=Discord%20Notify)](https://github.com/mleem97/GregToolsModmanager/actions/workflows/discord-release-notify.yml)
[![Daily Security Scan](https://img.shields.io/github/actions/workflow/status/mleem97/GregToolsModmanager/daily-malicious-code-scan.yml?branch=main&style=for-the-badge&label=Daily%20Security%20Scan)](https://github.com/mleem97/GregToolsModmanager/actions/workflows/daily-malicious-code-scan.yml)

| | |
|:---|:---|
| **Im Workspace** | Pfad `gregFramework/gregModmanager/`. Überblick: [gregFramework README](../README.md). |
| **Remote** | [`mleem97/GregToolsModmanager`](https://github.com/mleem97/GregToolsModmanager) |

---

## Open-source and external dependencies

This project is developed in the open and uses many open-source libraries (.NET, MAUI, Facepunch.Steamworks, and more). It also ships Valve’s closed-source `steam_api64.dll` (Steamworks), which is governed by Valve terms.

See [EXTERNAL_DEPENDENCIES.md](./EXTERNAL_DEPENDENCIES.md) for a full license and redistribution breakdown.

---

## Features

- **Mod Store:** Browse, search, subscribe, favorite, and vote on Workshop items.
- **Mod Manager:** Dependency health checks, MelonLoader status, and FMF plugin channels.
- **Authoring tools:** Create projects from templates, edit metadata, and publish with change notes.
- **Template scaffolding:** Modded templates create `content/Mods`, `content/Plugins`, and `content/ModFramework/` (including `ModFramework/FMF/Plugins`) to mirror `{GameRoot}` layout.
- **Post-upload sync:** Re-downloads from Steam after publish to keep local content in sync.
- **Headless CLI:** Supports scripted/CI publish flows.
- **Pagination:** All major list views support paging.

---

## Open in Visual Studio

Use `WorkshopUploader.sln` in this repository root.

If you open only `WorkshopUploader.csproj` from another solution context, Visual Studio may pick a different solution unexpectedly.

---

## Build

```powershell
dotnet build WorkshopUploader.csproj -c Debug
```

Or:

```powershell
dotnet build WorkshopUploader.sln -c Debug
```

Target: **.NET 9 + .NET MAUI (Windows)**. The project uses `WindowsAppSDKSelfContained` so required Windows App SDK components are shipped with the app.

---

## Run (recommended)

- Use **Visual Studio 2022** with the **.NET MAUI workload** and **Windows App SDK** components.
- Open `WorkshopUploader.sln`.
- Set `WorkshopUploader` as startup project.
- Press `F5`.

---

## Publish (win10-x64)

```powershell
dotnet publish WorkshopUploader.csproj -c Release
```

Output:

`bin/Release/net9.0-windows10.0.19041.0/win10-x64/publish/WorkshopUploader.exe`

### Publish to a custom self-contained folder

```powershell
dotnet publish WorkshopUploader.csproj -c Release -p:SelfContained=true -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -o .\publish-out
```

---

## Create installer (Setup.exe via Inno Setup)

1. Install [Inno Setup 6](https://jrsoftware.org/isdl.php) (includes `ISCC.exe`).
2. Run:

```powershell
.\scripts\build.ps1
```

This runs `dotnet publish` and creates:

`installer\Output\GregToolsModmanager-<Version>-Setup.exe`

The build also produces by default:

- `installer\Output\win64-v<Version>-portable.zip`
- `installer\Output\win64-v<Version>-setup.exe` (or `...-setup-signed.exe` when signing)
- matching `*.sha256` files for each artifact

Installer behavior:

- Wizard install/uninstall integration in Windows Apps settings.
- Start menu entry and optional desktop shortcut.
- Default install path: `C:\Program Files\GregTools Modmanager` (admin required).

Useful options:

- Skip publish and only rebuild setup: `./scripts/build.ps1 -SkipPublish`
- Linux source bundles (e.g. Debian/Kali): `./scripts/build.ps1 -SkipPublish -LinuxDistros Debian,Kali`
- Inno script path: `installer\GregToolsModmanager.iss`

**Linux note:** The MAUI app itself is currently Windows-focused (`net9.0-windows...`).
`-LinuxDistros` therefore produces signed source bundles for distributions (e.g. Debian/Kali), not a native Linux GUI binary.

### Official Linux packages (`.deb`, `.rpm`, `.apk`, `.pkg.tar.zst`)

After a signed build you can build official package formats from the Linux bundles:

```bash
bash scripts/linux/build-linux-packages.sh "$HOME/GregTools-Releases" "$HOME/GregTools-Releases/packages" "deb,rpm,apk,archlinux"
```

On **Windows (with WSL)**:

```powershell
.\scripts\linux\build-linux-packages.ps1 -SourceDir .\installer\Output -OutputDir .\installer\Output\linux-packages -Formats "deb,rpm,apk,archlinux"
```

Optional: explicit WSL distro:

```powershell
.\scripts\linux\build-linux-packages.ps1 -WslDistro Ubuntu -SourceDir .\installer\Output -OutputDir .\installer\Output\linux-packages
```

The script expects these signature sidecar files for each Linux bundle:

- `Linux-<Distro>-v<Version>-signed.zip`
- `Linux-<Distro>-v<Version>-signed.zip.sha256`
- `Linux-<Distro>-v<Version>-signed.zip.sig`
- `Linux-<Distro>-v<Version>-signed.zip.sig.cer`

For `.deb` installs, runtime dependencies are declared in the package.
Install with `apt` so dependencies are pulled in automatically:

```bash
sudo apt install ./gregtools-modmanager-debian_<Version>_amd64.deb
```

If the same version is already installed, use reinstall:

```bash
sudo apt install --reinstall ./gregtools-modmanager-debian_<Version>_amd64.deb
```

Desktop menu entries after installation:

- `GregTools Modmanager (Debian)` (opens the installed bundle folder)
- `GregTools Debian Bundle Verify` (signature/hash verification)

The output is installable Linux packages in the target folder, for example:

- `gregtools-modmanager-debian_<Version>_amd64.deb`
- `gregtools-modmanager-debian-<Version>-1.x86_64.rpm`
- `gregtools-modmanager-debian-<Version>-r1.apk`
- `gregtools-modmanager-debian-<Version>-1-x86_64.pkg.tar.zst`

### Update/reinstall behavior

- Setup uses the same `AppId`, detects existing installs, and overwrites the target folder.
- Running `WorkshopUploader.exe` is closed through Windows Restart Manager (`CloseApplications`).
- Portable install via `scripts/install-local.ps1` also closes the app and replaces the install directory.

### App does not start after setup

- The installer runs elevated, but final launch uses `runasoriginaluser` so the app starts non-elevated (important for WinUI/WebView2/MAUI stability).
- If problems continue, check Event Viewer and test `CloseApplications=no` in `GregToolsModmanager.iss`.

---

## Code signing, SmartScreen, and trust notice

- **Current state:** We currently use **self-signed** code signing for CI/community builds.
- **Why:** I cannot afford an official OV/EV certificate at the moment.
- **Impact for users:** Windows/SmartScreen may still show warnings (for example, *Unknown Publisher* or reputation prompts), even when the file is signed.
- **Runtime note:** This app targets **.NET 9 + .NET MAUI (Windows)** and depends on Windows runtime components; if startup fails on end-user systems, install/repair the Visual C++ Redistributable and Windows App SDK runtime (see troubleshooting below).
- **Rotation policy:** CI rotates/recreates self-signed certificates on a **7-day cadence** (or earlier if close to expiry), and refreshes the rolling `latest` prerelease artifacts after successful signing.

### Signing commands and references

- Official OV/EV code signing is currently not enabled due to certificate cost.
- A self-signed CI path is available for community/testing builds.
- Manual signing docs: `installer\CODE_SIGNING.md`.
- Create a self-signed certificate:

```powershell
.\installer\create-selfsigned-codesign-cert.ps1
```

- Sign only (without rebuilding setup):

```powershell
.\scripts\build.ps1 -SignOnly
```

Set `CODE_SIGN_THUMBPRINT` (or use `-SetupPath` when needed).

With `./scripts/build.ps1 -Sign`, additionally:

- All Windows payload binaries (`*.exe`, `*.dll`) in the portable build are Authenticode-signed (unsigned files get signed; already valid signatures are left as-is).
- Installers (`GregToolsModmanager-<Version>-Setup.exe`, `win64-v<Version>-setup-signed.exe`) are signed.
- All distributable archives (`win64-v<Version>-portable.zip`, `Linux-<Distro>-v<Version>-signed.zip`) get detached signatures (`*.sig` + `*.sig.cer`) in addition to `*.sha256`.
- Before release, archives are checked for extractability (Windows: `WorkshopUploader.exe` must be in the ZIP; Linux bundle: `README.md` must be present).

For release-ready Windows + Linux artifacts, run:

```powershell
.\build.ps1 -Sign -LinuxDistros Debian,Kali
```

That produces signed, verified artifacts for:

- Windows installer (`*.exe`, including setup alias)
- Windows portable (`win64-v<Version>-portable.zip`)
- Linux distribution bundles (`Linux-<Distro>-v<Version>-signed.zip`)

---

## Portable install (no Setup.exe)

```powershell
.\scripts\install-local.ps1
```

Installs per-user to `%LOCALAPPDATA%\Programs\GregTools Modmanager\` (no admin).

Uninstall:

```powershell
.\scripts\install-local.ps1 -Uninstall
```

---

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

---

## Deploy all mods to Workshop folders

```powershell
pwsh -File scripts/Deploy-Release-ToWorkshop.ps1
```

Builds framework/plugins/mods and creates Steamworks-compatible project folders under `<GameRoot>/workshop/`.

---

## Troubleshooting

1. Open **Event Viewer** → **Windows Logs** → **Application** and look for `WorkshopUploader.exe` faults.
2. Install/repair the [Microsoft Visual C++ Redistributable](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist).
3. Install the [Windows App SDK Runtime](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads).
4. Prefer running with `F5` from Visual Studio on the same machine you use to build.
5. Ensure Windows 10 version 1809+ (OS build `17763+`).

---

## Deploy next to the game

Copy the publish output to:

`{GameRoot}/WorkshopUploader/`

Place it next to the game executable (not inside `Mods` or `MelonLoader`).

---

## VirusTotal

Third-party scan for transparency (self-contained .NET apps may be flagged heuristically; always compare checksums from official releases):

- **SHA-256:** `c0ea7929eee9d754e81363e9ec81c601e763e65f7db1eb0d971edf2c2036f0af`
- **Report:** [VirusTotal file relations](https://www.virustotal.com/gui/file/c0ea7929eee9d754e81363e9ec81c601e763e65f7db1eb0d971edf2c2036f0af/relations)

---

## Sponsorship

If this project helps you and you want to support ongoing maintenance and improvements:

- **Sponsor:** [github.com/sponsors/mleem97](https://github.com/sponsors/mleem97)

---

## See also

- [External dependencies and distribution notes](./EXTERNAL_DEPENDENCIES.md)
- [Workshop-Uploader (gregWiki)](../gregWiki/docs/tools/workshop-uploader.md)
- [End-user guide](../gregWiki/docs/guides/players/enduser-workshop.md)
- [Contributor guide](../gregWiki/docs/guides/contributors/contributor-workshop.md)
