using WorkshopUploader.Localization;
using WorkshopUploader.Services;

namespace WorkshopUploader;

public partial class AppShell : Shell
{
	private readonly SteamWorkshopService _steam;
	private bool _steamStatusTimerStarted;

	public AppShell(ProjectsPage projects, NewProjectPage newProject, MyUploadsPage uploads, ModManagerPage modManager, SettingsPage settings, SteamWorkshopService steam)
	{
		_steam = steam;
		// #region agent log
		DebugSessionLog.Write("H2", "AppShell.ctor", "before_init", null);
		// #endregion
		InitializeComponent();
		// #region agent log
		DebugSessionLog.Write("H2", "AppShell.ctor", "after_init", null);
		// #endregion

		Loaded += OnShellLoaded;

		var tabBar = new TabBar();
		tabBar.Items.Add(new ShellContent
		{
			Title = S.Get("Tab_Projects"),
			Content = projects,
			Route = nameof(ProjectsPage),
		});
		tabBar.Items.Add(new ShellContent
		{
			Title = S.Get("Tab_New"),
			Content = newProject,
			Route = nameof(NewProjectPage),
		});
		tabBar.Items.Add(new ShellContent
		{
			Title = S.Get("Tab_MyUploads"),
			Content = uploads,
			Route = nameof(MyUploadsPage),
		});

		if (SettingsPage.IsModStoreEnabled())
		{
			tabBar.Items.Add(new ShellContent
			{
				Title = S.Get("Tab_ModStore"),
				Content = modManager,
				Route = nameof(ModManagerPage),
			});
		}

		tabBar.Items.Add(new ShellContent
		{
			Title = S.Get("Tab_Settings"),
			Content = settings,
			Route = nameof(SettingsPage),
		});

		Items.Add(tabBar);
		Routing.RegisterRoute(nameof(EditorPage), typeof(EditorPage));
		Routing.RegisterRoute(nameof(NativeConfigEditorPage), typeof(NativeConfigEditorPage));
		Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
	}

	private void OnShellLoaded(object? sender, EventArgs e)
	{
		if (_steamStatusTimerStarted)
		{
			return;
		}

		_steamStatusTimerStarted = true;
		Loaded -= OnShellLoaded;
		UpdateSteamConnectionUi();
		var dispatcher = Application.Current?.Dispatcher;
		if (dispatcher is null)
		{
			return;
		}

		dispatcher.StartTimer(TimeSpan.FromSeconds(2), () =>
		{
			MainThread.BeginInvokeOnMainThread(UpdateSteamConnectionUi);
			return true;
		});
	}

	private void UpdateSteamConnectionUi()
	{
		if (_steam.TryGetSteamReady(out var userName))
		{
			SteamStatusLed.Fill = new SolidColorBrush(Color.FromArgb("#61F4D8"));
			SteamLogoTile.BackgroundColor = Color.FromArgb("#0D3835");
			SteamStatusText.TextColor = Color.FromArgb("#C0FCF6");
			SteamStatusText.Text = string.IsNullOrEmpty(userName)
				? S.Get("Steam_Ok")
				: S.Format("Steam_User", userName);
		}
		else
		{
			SteamStatusLed.Fill = new SolidColorBrush(Color.FromArgb("#D7383B"));
			SteamLogoTile.BackgroundColor = Color.FromArgb("#1A2A1012");
			SteamStatusText.TextColor = Color.FromArgb("#7FBFB8");
			var hint = _steam.LastSteamConnectionHint;
			if (string.IsNullOrWhiteSpace(hint))
			{
				SteamStatusText.Text = S.Get("Steam_Offline");
			}
			else
			{
				const int maxLen = 72;
				if (hint.Length > maxLen)
				{
					hint = hint[..maxLen] + "…";
				}

				SteamStatusText.Text = S.Format("Steam_Hint", hint);
			}
		}
	}
}
