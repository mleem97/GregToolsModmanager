namespace WorkshopUploader;

public partial class App : Application
{
	private readonly AppShell _shell;

	public App(AppShell shell)
	{
		// #region agent log
		DebugNdjsonSessionLog.Write("H5", "Maui.App.ctor", "before_init", null);
		DebugSessionLog.Write("H2", "App.ctor", "before_init", null);
		// #endregion
		InitializeComponent();
		// #region agent log
		DebugNdjsonSessionLog.Write("H5", "Maui.App.ctor", "after_init", null);
		DebugSessionLog.Write("H2", "App.ctor", "after_init", null);
		// #endregion
		_shell = shell;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// #region agent log
		DebugNdjsonSessionLog.Write("H5", "App.CreateWindow", "entry", null);
		// #endregion
		var window = new Window(_shell) { Title = "GregTools Modmanager" };

		window.Created += (_, _) =>
		{
			if (!OperatingSystem.IsWindows()) return;

			try
			{
				ApplyDarkTitleBar(window);
			}
			catch
			{
				// ignored on unsupported OS versions
			}
		};

		return window;
	}

	private static void ApplyDarkTitleBar(Window window)
	{
#if WINDOWS
		var nativeWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
		if (nativeWindow is null) return;

		var appWindow = nativeWindow.AppWindow;
		if (appWindow is null) return;

		if (!Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported()) return;

		var titleBar = appWindow.TitleBar;
		titleBar.ExtendsContentIntoTitleBar = false;

		var bg = global::Windows.UI.Color.FromArgb(255, 0, 17, 16);         // #001110
		var fg = global::Windows.UI.Color.FromArgb(255, 192, 252, 246);      // #C0FCF6
		var inactive = global::Windows.UI.Color.FromArgb(255, 90, 158, 150); // #5A9E96
		var hover = global::Windows.UI.Color.FromArgb(255, 0, 23, 21);       // #001715
		var pressed = global::Windows.UI.Color.FromArgb(255, 13, 56, 53);    // #0D3835

		titleBar.BackgroundColor = bg;
		titleBar.ForegroundColor = fg;
		titleBar.InactiveBackgroundColor = bg;
		titleBar.InactiveForegroundColor = inactive;

		titleBar.ButtonBackgroundColor = bg;
		titleBar.ButtonForegroundColor = fg;
		titleBar.ButtonHoverBackgroundColor = hover;
		titleBar.ButtonHoverForegroundColor = fg;
		titleBar.ButtonPressedBackgroundColor = pressed;
		titleBar.ButtonPressedForegroundColor = fg;
		titleBar.ButtonInactiveBackgroundColor = bg;
		titleBar.ButtonInactiveForegroundColor = inactive;
#endif
	}
}
