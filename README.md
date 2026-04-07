# WorkshopManager

Desktop app for **Steam Workshop** management, mod browsing, and publishing for **Data Center** (Steamworks API, App ID 4170200).

### Open source and external dependencies

This project is developed in the open alongside **many open-source libraries** (.NET, MAUI, Facepunch.Steamworks, etc.). It also **ships Valve’s closed-source `steam_api64.dll`** (Steamworks) subject to Valve’s terms—not everything in the distributed app is “open source” in the OSI sense. See **[EXTERNAL_DEPENDENCIES.md](./EXTERNAL_DEPENDENCIES.md)** for a full breakdown, licenses, and redistribution notes.

## Features

- **Mod Store** — browse, search, subscribe, favorite, and vote on Workshop items.
- **Mod Manager** — dependency health checks, MelonLoader status, FMF plugin channels.
- **Author tools** — create workshop projects from templates, edit metadata, publish with change notes. Modded templates scaffold **`content/Mods`**, **`content/Plugins`**, and **`content/ModFramework/`** (including **`ModFramework/FMF/Plugins`**) so the uploaded tree mirrors **`{GameRoot}`** layout; Steam still receives only the **`content/`** folder — the game copies it into **`WorkshopUploadContent`** (junctions from the game root to those paths remain the player’s responsibility).
- **Post-upload sync** — after publishing, re-downloads from Steam to keep your local copy in sync.
- **Headless CLI** — publish from scripts or CI.
- **Pagination** — all list views support paging through results.

## Open in Visual Studio (without the full monorepo solution)

Use **`WorkshopUploader\WorkshopUploader.sln`** — it contains only this project. Opening **`WorkshopUploader.csproj`** from the repo root can make Visual Studio pick **`FrikaMF.sln`** instead.

## Build

```bash
dotnet build WorkshopUploader.csproj -c Debug
```

Or from this folder:

```bash
dotnet build WorkshopUploader.sln -c Debug
```

Targets **.NET 9** with **.NET MAUI** (Windows). The project sets **`WindowsAppSDKSelfContained`** so the **Windows App SDK** is copied next to the app.

## Run (recommended)

- **Visual Studio 2022** with the **.NET Multi-platform App UI workload** and **Windows App SDK** components: open **`WorkshopUploader.sln`**, confirm **WorkshopUploader** is the startup project, press **F5**.

## Publish (single-file, win10-x64)

```bash
dotnet publish WorkshopUploader.csproj -c Release
```

Output: `bin/Release/net9.0-windows10.0.19041.0/win10-x64/publish/WorkshopUploader.exe`

### Fully self-contained folder

```bash
dotnet publish WorkshopUploader.csproj -c Release -p:SelfContained=true -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -o ./publish-out
```

### Setup-EXE (Inno Setup — echter Installer)

1. [Inno Setup 6](https://jrsoftware.org/isdl.php) installieren (liefert `ISCC.exe`).
2. Im Ordner `WorkshopUploader`:

```powershell
.\build.ps1
```

Führt `dotnet publish` aus und erzeugt **`installer\Output\GregToolsModmanager-<Version>-Setup.exe`** (Assistent, **Deinstallieren** unter Windows-Einstellungen → Apps, Startmenü-Eintrag, optionale Desktop-Verknüpfung). Standardinstallationspfad: **`Program Files\GregTools Modmanager`** (Administrator nötig).

Nur neu kompilieren, wenn Publish schon da ist: `.\build.ps1 -SkipPublish`. Das Inno-Skript liegt unter **`installer\GregToolsModmanager.iss`**.

**Update / Neuinstallation:** Gleiche **`AppId`** wie zuvor — Setup erkennt die bestehende Installation und **überschreibt** den Zielordner (`Program Files\GregTools Modmanager`). Vorher wird eine **laufende** `WorkshopUploader.exe` über den Windows-Restart-Manager geschlossen (`CloseApplications`). Die portable Variante **`install-local.ps1`** beendet die App ebenfalls und ersetzt den Installationsordner komplett.

**Startet nach dem Setup nicht:** Der Assistent läuft mit Admin-Rechten; die **Abschluss-Aktion** startet die App mit **`runasoriginaluser`**, damit sie **nicht** im erhöhten Kontext läuft (WinUI/WebView2/MAUI verträgt das oft nicht). Verknüpfungen starten ohnehin normal. Wenn es weiter hakt: Ereignisanzeige, und testweise `CloseApplications=no` in `GregToolsModmanager.iss`.

**Signatur:** Mit einer **öffentlichen CA** verschwindet der „Unbekannter Herausgeber“-Eindruck für Nutzer weitgehend; mit **Self-Signed** kannst du trotzdem z. B. **mleem97 / Greg Modding Team** im Zertifikat anzeigen — siehe **`installer\CODE_SIGNING.md`**. Self-Signed anlegen: **`.\installer\create-selfsigned-codesign-cert.ps1`**. Nur signieren **ohne Inno Setup**: **`.\build.ps1 -SignOnly`** (`CODE_SIGN_THUMBPRINT` setzen, Setup-EXE in `installer\Output\` oder `-SetupPath`).

### Schnell ohne Setup-EXE (nur Kopie + Verknüpfungen)

```powershell
.\install-local.ps1
```

Installiert benutzerweit nach `%LOCALAPPDATA%\Programs\GregTools Modmanager\` (ohne Admin). Deinstallation: `.\install-local.ps1 -Uninstall`.

## Deploy all mods to Workshop folders

```bash
pwsh -File scripts/Deploy-Release-ToWorkshop.ps1
```

Builds framework + plugins + mods and creates Steamworks-compatible project folders under `<GameRoot>/workshop/`.

## Troubleshooting

1. **Windows Event Viewer** -> *Windows Logs* -> *Application* -> look for **WorkshopUploader.exe** faults.
2. Install or repair: **[Microsoft Visual C++ Redistributable](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist)**.
3. Install the **[Windows App SDK runtime](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads)**.
4. Prefer **F5 from Visual Studio** on the same machine where you build.
5. Ensure **Windows 10 version 1809+** (OS build **17763+**).

## Deploy next to the game

Copy the publish folder to:

`{GameRoot}/WorkshopUploader/`

so it sits alongside the game executable (not inside `Mods` or `MelonLoader`).

## VirusTotal

A third-party scan is published for transparency (self-contained .NET builds are sometimes flagged heuristically; compare SHA-256 if you download from GitHub releases):

- **SHA-256:** `c0ea7929eee9d754e81363e9ec81c601e763e65f7db1eb0d971edf2c2036f0af`
- **Report:** [VirusTotal — file relations](https://www.virustotal.com/gui/file/c0ea7929eee9d754e81363e9ec81c601e763e65f7db1eb0d971edf2c2036f0af/relations)

## See also

- [External dependencies & distribution (licenses, Steamworks)](./EXTERNAL_DEPENDENCIES.md)
- [Workshop wiki page](../docs/wiki/tools/workshop-uploader.md)
- [End-User Guide](../wiki/docs/guides/enduser-workshop.md)
- [Contributor Guide](../wiki/docs/guides/contributor-workshop.md)
