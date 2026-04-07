using System.Collections.ObjectModel;

namespace WorkshopUploader.Services;

public sealed class AppLogService
{
	private const int MaxLines = 500;

	public ObservableCollection<string> Lines { get; } = new();

	public event EventHandler? LineAppended;

	public void Append(string message)
	{
		var line = $"{DateTime.Now:HH:mm:ss} {message}";
		AppFileLog.Info(message);
		MainThread.BeginInvokeOnMainThread(() =>
		{
			Lines.Add(line);
			while (Lines.Count > MaxLines)
			{
				Lines.RemoveAt(0);
			}

			LineAppended?.Invoke(this, EventArgs.Empty);
		});
	}
}
