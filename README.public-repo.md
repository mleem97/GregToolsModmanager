# GregTools Modmanager (Workshop Uploader)

Windows **.NET MAUI** app for **Data Center** Steam Workshop: Mod Store, local upload projects, `metadata.json` / `content/config.json`, and Steam publish (Facepunch.Steamworks).

## Build

```powershell
dotnet build WorkshopUploader.sln -c Release
```

```powershell
dotnet publish WorkshopUploader.csproj -c Release -p:SelfContained=true -p:RuntimeIdentifier=win10-x64
```

See the original `README.md` in this directory for troubleshooting, Steam layout, and headless CLI.

## Releases (GitHub Actions)

In the **monorepo**, releases are driven by `.github/workflows/gregtools-modmanager-release.yml`. Push a tag:

`gregtools-modmanager-v1.0.0`

After this folder is its **own repository**, edit that workflow (or copy it to `.github/workflows/release.yml`) and switch the tag pattern to `v*` if you prefer.

**First releases** ship a **ZIP** of the self-contained `win10-x64` publish folder at **best compression**. Optional **MSIX / App Installer** packaging is documented in [EXTERNAL_DEPENDENCIES.md](./EXTERNAL_DEPENDENCIES.md) (CI job is off until signing is configured).

**VirusTotal:** [file relations](https://www.virustotal.com/gui/file/c0ea7929eee9d754e81363e9ec81c601e763e65f7db1eb0d971edf2c2036f0af/relations) (SHA-256 `c0ea7929eee9d754e81363e9ec81c601e763e65f7db1eb0d971edf2c2036f0af`) — see `README.md` for context.

## Code-Signing status

- Official OV/EV code signing is currently **not enabled** because certificate costs are too high at the moment.
- For testing/community builds, a **self-signed setup workflow** exists at `.github/workflows/selfsigned-setup.yml`.
- If you want to support the project: once GitHub Sponsors is enabled for this account/repo, you can use the **Sponsor** feature.

## Crash dumps (WER LocalDumps)

If the app hard-crashes (native exception / process abort), Windows can write `.dmp` files automatically:

```powershell
.\installer\configure-localdumps.ps1
```

Machine-wide (requires elevated PowerShell):

```powershell
.\installer\configure-localdumps.ps1 -Scope Machine
```

Disable again:

```powershell
.\installer\configure-localdumps.ps1 -Disable
```

Default dump folders:

- CurrentUser: `%LOCALAPPDATA%\GregToolsModmanager\dumps`
- Machine: `C:\ProgramData\GregToolsModmanager\dumps`

## Open source & external dependencies

See **[EXTERNAL_DEPENDENCIES.md](./EXTERNAL_DEPENDENCIES.md)** for licenses, **Steamworks** / **steam_api64.dll**, and distribution notes.

## License

Use the same license as the parent [DataCenterExporter / gregFramework](https://github.com/mleem97/gregFramework) project unless you add a dedicated `LICENSE` to this repository.
