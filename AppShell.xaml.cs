using GregModmanager.Localization;
using GregModmanager.Services;

namespace GregModmanager;

public partial class AppShell : Shell
{
	private readonly SteamWorkshopService _steam;
	private bool _steamStatusTimerStarted;

	public AppShell(ProjectsPage projects, NewProjectPage newProject, MyUploadsPage uploads, ModManagerPage modManager, SettingsPage settings, SteamWorkshopService steam)
	{
		_steam = steam;
		DebugSessionLog.Write("H2", "AppShell.ctor", "before_init", null);
		InitializeComponent();
		DebugSessionLog.Write("H2", "AppShell.ctor", "after_init", null);

		Loaded += OnShellLoaded;

		Items.Add(new FlyoutItem
		{
			Title = S.Get("Tab_Projects"),
			Items = { new ShellContent { Content = projects, Route = nameof(ProjectsPage) } },
		});
		Items.Add(new FlyoutItem
		{
			Title = S.Get("Tab_New"),
			Items = { new ShellContent { Content = newProject, Route = nameof(NewProjectPage) } },
		});
		Items.Add(new FlyoutItem
		{
			Title = S.Get("Tab_MyUploads"),
			Items = { new ShellContent { Content = uploads, Route = nameof(MyUploadsPage) } },
		});

		if (SettingsPage.IsModStoreEnabled())
		{
			Items.Add(new FlyoutItem
			{
				Title = S.Get("Tab_ModStore"),
				Items = { new ShellContent { Content = modManager, Route = nameof(ModManagerPage) } },
			});
		}

		Items.Add(new FlyoutItem
		{
			Title = S.Get("Tab_Settings"),
			Items = { new ShellContent { Content = settings, Route = nameof(SettingsPage) } },
		});

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
