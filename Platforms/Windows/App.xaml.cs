using Microsoft.UI.Xaml;
using WorkshopUploader.Services;
using WorkshopUploader;

namespace WorkshopUploader.WinUI;

public partial class App : MauiWinUIApplication
{
	public App()
	{
		AppFileLog.Info("WinUI App ctor start");
		UnhandledException += OnUnhandledException;
		// #region agent log
		DebugNdjsonSessionLog.Write("H2", "WinUI.App.ctor", "before_init", new { baseDir = AppContext.BaseDirectory, tempLog = DebugNdjsonSessionLog.LogPath });
		DebugSessionLog.Write("H3", "WinUI.App.ctor", "before_init", new { baseDir = AppContext.BaseDirectory });
		// #endregion
		InitializeComponent();
		AppFileLog.Info("WinUI App ctor after InitializeComponent");
		// #region agent log
		DebugNdjsonSessionLog.Write("H2", "WinUI.App.ctor", "after_init", new { ok = true });
		DebugSessionLog.Write("H3", "WinUI.App.ctor", "after_init", null);
		// #endregion
	}

	private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
	{
		AppFileLog.MarkCrash("WinUI.UnhandledException", e.Exception);
		AppFileLog.Error("WinUI UnhandledException", e.Exception);
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
