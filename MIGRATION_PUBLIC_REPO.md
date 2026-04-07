# Moving the app to `workshopuploader/` (public repo layout)

Tooling and scripts expect the MAUI project at:

`workshopuploader/WorkshopUploader.csproj`  
(i.e. the former `WorkshopUploader/` folder **renamed** to `workshopuploader/` at the monorepo root).

## Before you start

1. Close **Visual Studio** / **Cursor** if they have the project open.
2. Exit **WorkshopUploader.exe** (and any process using `WorkshopUploader\.vs` or `bin\`).

## Rename in Git (preserves history)

From the repository root:

```powershell
git mv WorkshopUploader workshopuploader
```

If Git reports **Permission denied**, unlock the folders above, then retry. As a last resort, reboot and run the command again.

## Add a root README for the standalone clone (optional)

After the rename, copy `README.public-repo.md` to `workshopuploader/README.md` (or merge), and add `workshopuploader/.gitignore` from the same folder if you want a stricter ignore list for the split repo.

## Publish a new GitHub repository

```powershell
cd workshopuploader
git init
git add .
git commit -m "Initial import: GregTools Modmanager"
git branch -M main
git remote add origin https://github.com/YOUR_ORG/workshopuploader.git
git push -u origin main
```

To **keep history** from the monorepo, use `git subtree split` or `git filter-repo` from the parent repo instead of `git init` in the subfolder.

## Monorepo scripts

`scripts/Package-WorkshopUploaderRelease.ps1`, `Deploy-Release-ToDataCenter.ps1`, and `Deploy-Release-ToWorkshop.ps1` resolve the project via `scripts/Resolve-WorkshopUploaderMonorepoDir.ps1`: **`workshopuploader\WorkshopUploader.csproj` is preferred** (canonical layout); **`WorkshopUploader\`** is still accepted until `git mv` succeeds.

`framework/FrikaMF.csproj` excludes both `..\workshopuploader\**\*.cs` and `..\WorkshopUploader\**\*.cs` so the framework project does not compile MAUI sources.
