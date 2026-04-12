namespace GregModmanager.Services;

/// <summary>
/// Synchronizes downloaded Workshop content from Steam's cache into
/// <c>{GameRoot}/Mods/Workshop/{PublishedFileId}/</c> using atomic copy.
/// </summary>
public sealed class ModsFolderSyncService
{
	public event Action<SyncProgressArgs>? SyncProgress;

	/// <summary>
	/// Sync a single downloaded Workshop item into the game's Mods folder.
	/// </summary>
	public SyncResult SyncItem(ulong publishedFileId, string steamLocalDir, string gameRoot)
	{
		if (string.IsNullOrEmpty(gameRoot))
			return SyncResult.Fail("Game root path is not configured.");

		if (!Directory.Exists(steamLocalDir))
			return SyncResult.Fail($"Source directory does not exist: {steamLocalDir}");

		var destDir = Path.Combine(gameRoot, "Mods", "Workshop", publishedFileId.ToString());
		var tempDir = destDir + ".tmp";

		try
		{
			if (Directory.Exists(tempDir))
				Directory.Delete(tempDir, recursive: true);

			Directory.CreateDirectory(tempDir);
			CopyDirectoryRecursive(steamLocalDir, tempDir);

			if (Directory.Exists(destDir))
				Directory.Delete(destDir, recursive: true);

			Directory.Move(tempDir, destDir);

			SyncProgress?.Invoke(new SyncProgressArgs(publishedFileId, true, destDir));
			return SyncResult.Ok(destDir);
		}
		catch (Exception ex)
		{
			try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true); }
			catch { /* cleanup best-effort */ }

			SyncProgress?.Invoke(new SyncProgressArgs(publishedFileId, false, null));
			return SyncResult.Fail($"Sync failed for {publishedFileId}: {ex.Message}");
		}
	}

	/// <summary>
	/// Sync multiple items downloaded via <see cref="WorkshopDownloadService"/>.
	/// </summary>
	public IReadOnlyList<SyncResult> SyncItems(
		IReadOnlyList<(ulong Id, string LocalDir)> items,
		string gameRoot,
		IProgress<string>? log = null)
	{
		var results = new List<SyncResult>(items.Count);
		foreach (var (id, localDir) in items)
		{
			log?.Report($"Syncing {id}…");
			var result = SyncItem(id, localDir, gameRoot);
			if (result.Success)
				log?.Report($"Synced {id} → {result.DestinationPath}");
			else
				log?.Report($"Failed {id}: {result.ErrorMessage}");
			results.Add(result);
		}

		return results;
	}

	/// <summary>
	/// Removes a Workshop item from the Mods folder.
	/// </summary>
	public bool RemoveItem(ulong publishedFileId, string gameRoot)
	{
		var destDir = Path.Combine(gameRoot, "Mods", "Workshop", publishedFileId.ToString());
		if (!Directory.Exists(destDir)) return true;

		try
		{
			Directory.Delete(destDir, recursive: true);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void CopyDirectoryRecursive(string sourceDir, string destDir)
	{
		foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
		{
			var relative = Path.GetRelativePath(sourceDir, dir);
			Directory.CreateDirectory(Path.Combine(destDir, relative));
		}

		foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
		{
			var relative = Path.GetRelativePath(sourceDir, file);
			File.Copy(file, Path.Combine(destDir, relative), overwrite: true);
		}
	}
}

public readonly record struct SyncResult(bool Success, string? DestinationPath, string? ErrorMessage)
{
	public static SyncResult Ok(string path) => new(true, path, null);
	public static SyncResult Fail(string error) => new(false, null, error);
}

public readonly record struct SyncProgressArgs(ulong PublishedFileId, bool Success, string? DestinationPath);
