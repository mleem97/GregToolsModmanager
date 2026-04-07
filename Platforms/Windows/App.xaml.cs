using Microsoft.UI.Xaml;
using WorkshopUploader;

namespace WorkshopUploader.WinUI;

public partial class App : MauiWinUIApplication
{
	public App()
	{
		// #region agent log
		DebugNdjsonSessionLog.Write("H2", "WinUI.App.ctor", "before_init", new { baseDir = AppContext.BaseDirectory, tempLog = DebugNdjsonSessionLog.LogPath });
		DebugSessionLog.Write("H3", "WinUI.App.ctor", "before_init", new { baseDir = AppContext.BaseDirectory });
		// #endregion
		InitializeComponent();
		// #region agent log
		DebugNdjsonSessionLog.Write("H2", "WinUI.App.ctor", "after_init", new { ok = true });
		DebugSessionLog.Write("H3", "WinUI.App.ctor", "after_init", null);
		// #endregion
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
