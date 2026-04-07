namespace WorkshopUploader.Services;

public static class AppFileLog
{
	private static readonly object Gate = new();
	private static string? _logPath;

	public static string LogPath
	{
		get
		{
			if (!string.IsNullOrEmpty(_logPath))
			{
				return _logPath;
			}

			try
			{
				var root = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					"GregToolsModmanager",
					"logs");
				Directory.CreateDirectory(root);
				_logPath = Path.Combine(root, $"app-{DateTime.Now:yyyyMMdd}.log");
			}
			catch
			{
				_logPath = Path.Combine(Path.GetTempPath(), $"GregToolsModmanager-app-{DateTime.Now:yyyyMMdd}.log");
			}

			return _logPath;
		}
	}

	public static void Info(string message) => Write("INFO", message, null);

	public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

	private static void Write(string level, string message, Exception? ex)
	{
		try
		{
			var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
			if (ex is not null)
			{
				line += $" | {ex.GetType().FullName}: {ex.Message}";
			}

			lock (Gate)
			{
				File.AppendAllText(LogPath, line + Environment.NewLine);
				if (ex is not null && !string.IsNullOrWhiteSpace(ex.StackTrace))
				{
					File.AppendAllText(LogPath, ex.StackTrace + Environment.NewLine);
				}
			}
		}
		catch
		{
			// logging must never crash the app
		}
	}
}
