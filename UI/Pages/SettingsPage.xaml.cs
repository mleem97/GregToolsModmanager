using System.Diagnostics;
using GregModmanager.Localization;
using GregModmanager.Services;

namespace GregModmanager;

public partial class SettingsPage : ContentPage
{
	public const string ModStoreEnabledKey = "ModStoreEnabled";
	public const string GameRootPathKey = "GameRootPath";

	private const string DiscordInviteUrl = "https://discord.gg/greg";
	private const string WasekuDCUrl = "https://discord.gg/nEGpsHqr";
	private const string WebsiteUrl = "https://gregframework.eu";

	private readonly WorkspaceService _workspace;
	private readonly ReproBundleService _reproBundle;

	public SettingsPage(WorkspaceService workspace, ReproBundleService reproBundle)
	{
		InitializeComponent();
		_workspace = workspace;
		_reproBundle = reproBundle;

		LanguagePicker.ItemsSource = S.SupportedLanguages.Select(l => l.DisplayName).ToList();
		var savedCode = S.GetSavedLanguage();
		var idx = Array.FindIndex(S.SupportedLanguages, l => l.Code == savedCode);
		LanguagePicker.SelectedIndex = idx >= 0 ? idx : 0;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		ModStoreSwitch.IsToggled = Preferences.Default.Get(ModStoreEnabledKey, false);

		var gameRoot = GetGameRootPath();
		GameRootEntry.Text = Preferences.Default.Get(GameRootPathKey, "");
		CurrentGameRootLabel.Text = string.IsNullOrEmpty(gameRoot)
			? S.Get("Settings_GameRootNotSet")
			: S.Format("Settings_CurrentPath", gameRoot);
		GameRootHint.Text = "";

		var custom = Preferences.Default.Get(WorkspaceService.CustomWorkspacePathKey, "");
		CustomPathEntry.Text = custom;
		CurrentPathLabel.Text = S.Format("Settings_CurrentPath", _workspace.WorkspaceRoot);
		PathHint.Text = "";
	}

	public static string GetGameRootPath()
	{
		return Preferences.Default.Get(GameRootPathKey, "");
	}

	private void OnModStoreToggled(object? sender, ToggledEventArgs e)
	{
		Preferences.Default.Set(ModStoreEnabledKey, e.Value);
		ModStoreHint.Text = S.Get("Settings_RestartEffect");
		ModStoreRestartBtn.IsVisible = true;
	}

	#region Game Root path

	private void OnBrowseGameRoot(object? sender, EventArgs e)
	{
		if (!OperatingSystem.IsWindows()) return;

		try
		{
			var dialog = new Windows.Storage.Pickers.FolderPicker();
			dialog.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
			dialog.FileTypeFilter.Add("*");

			var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(
				(Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window)!);
			WinRT.Interop.InitializeWithWindow.Initialize(dialog, hwnd);

			var folder = dialog.PickSingleFolderAsync().AsTask().GetAwaiter().GetResult();
			if (folder != null)
			{
				GameRootEntry.Text = folder.Path;
			}
		}
		catch (Exception ex)
		{
			GameRootHint.Text = S.Format("Settings_PickerFailed", ex.Message);
		}
	}

	private void OnApplyGameRoot(object? sender, EventArgs e)
	{
		var path = GameRootEntry.Text?.Trim() ?? "";

		if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
		{
			GameRootHint.Text = S.Get("Settings_GameRootNotFound");
			return;
		}

		Preferences.Default.Set(GameRootPathKey, path);
		CurrentGameRootLabel.Text = string.IsNullOrEmpty(path)
			? S.Get("Settings_GameRootNotSet")
			: S.Format("Settings_CurrentPath", path);
		GameRootHint.Text = S.Get("Settings_PathUpdated");
	}

	private void OnResetGameRoot(object? sender, EventArgs e)
	{
		Preferences.Default.Remove(GameRootPathKey);
		GameRootEntry.Text = "";
		CurrentGameRootLabel.Text = S.Get("Settings_GameRootNotSet");
		GameRootHint.Text = S.Get("Settings_PathReset");
	}

	#endregion

	#region Workspace path

	private void OnBrowseFolder(object? sender, EventArgs e)
	{
		if (!OperatingSystem.IsWindows()) return;

		try
		{
			var dialog = new Windows.Storage.Pickers.FolderPicker();
			dialog.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
			dialog.FileTypeFilter.Add("*");

			var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(
				(Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window)!);
			WinRT.Interop.InitializeWithWindow.Initialize(dialog, hwnd);

			var folder = dialog.PickSingleFolderAsync().AsTask().GetAwaiter().GetResult();
			if (folder != null)
			{
				CustomPathEntry.Text = folder.Path;
			}
		}
		catch (Exception ex)
		{
			PathHint.Text = S.Format("Settings_PickerFailed", ex.Message);
		}
	}

	private void OnApplyPath(object? sender, EventArgs e)
	{
		var path = CustomPathEntry.Text?.Trim() ?? "";

		if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
		{
			try
			{
				Directory.CreateDirectory(path);
			}
			catch (Exception ex)
			{
				PathHint.Text = S.Format("Settings_CannotCreate", ex.Message);
				return;
			}
		}

		Preferences.Default.Set(WorkspaceService.CustomWorkspacePathKey, path);
		_workspace.InvalidateCache();
		CurrentPathLabel.Text = S.Format("Settings_CurrentPath", _workspace.WorkspaceRoot);
		PathHint.Text = S.Get("Settings_PathUpdated");
	}

	private void OnResetPath(object? sender, EventArgs e)
	{
		Preferences.Default.Remove(WorkspaceService.CustomWorkspacePathKey);
		CustomPathEntry.Text = "";
		_workspace.InvalidateCache();
		CurrentPathLabel.Text = S.Format("Settings_CurrentPath", _workspace.WorkspaceRoot);
		PathHint.Text = S.Get("Settings_PathReset");
	}

	#endregion

	#region Language

	private void OnLanguageChanged(object? sender, EventArgs e)
	{
		var idx = LanguagePicker.SelectedIndex;
		if (idx < 0 || idx >= S.SupportedLanguages.Length) return;

		var code = S.SupportedLanguages[idx].Code;
		S.SetLanguage(code);
		LanguageHint.Text = S.Get("Settings_LanguageRestart");
		LanguageRestartBtn.IsVisible = true;
	}

	#endregion

	#region Community links

	private void OnOpenDiscord(object? sender, EventArgs e) => _ = SafeProcess.OpenUrlAsync(DiscordInviteUrl);

	private void OnOpenModdingChannel(object? sender, EventArgs e) => _ = SafeProcess.OpenUrlAsync(WasekuDCUrl);

	private void OnOpenWebsite(object? sender, EventArgs e) => _ = SafeProcess.OpenUrlAsync(WebsiteUrl);

	private async void OnOpenLogs(object? sender, EventArgs e)
	{
		try
		{
			var logPath = AppFileLog.LogPath;
			var logDirectory = Path.GetDirectoryName(logPath);
			if (string.IsNullOrWhiteSpace(logDirectory))
			{
				throw new InvalidOperationException("Log directory is unavailable.");
			}

			Directory.CreateDirectory(logDirectory);
			SafeProcess.OpenFolder(logDirectory);
		}
		catch (Exception ex)
		{
			AppFileLog.Error("Failed to open logs folder from settings.", ex);
			await DisplayAlert(S.Get("Error"), S.Format("Settings_OpenLogsFailed", ex.Message), S.Get("OK"));
		}
	}

	private async void OnCreateReproBundle(object? sender, EventArgs e)
	{
		try
		{
			var zipPath = await _reproBundle.CreateBundleAsync();

			if (OperatingSystem.IsWindows())
			{
				SafeProcess.OpenExplorerAndSelect(zipPath);
			}
			else
			{
				var dir = Path.GetDirectoryName(zipPath);
				if (!string.IsNullOrWhiteSpace(dir))
				{
					SafeProcess.OpenFolder(dir);
				}
			}

			await DisplayAlert(S.Get("OK"), S.Format("Settings_ReproBundleCreated", zipPath), S.Get("OK"));
		}
		catch (Exception ex)
		{
			AppFileLog.Error("Failed to create repro bundle from settings.", ex);
			await DisplayAlert(S.Get("Error"), S.Format("Settings_ReproBundleCreateFailed", ex.Message), S.Get("OK"));
		}
	}

	#endregion

	#region Restart

	private static void OnRestartApp(object? sender, EventArgs e)
	{
		var exe = Environment.ProcessPath;
		if (string.IsNullOrEmpty(exe)) return;

		SafeProcess.LaunchApp(exe);
		Application.Current?.Quit();
	}

	#endregion

	public static bool IsModStoreEnabled()
	{
		return Preferences.Default.Get(ModStoreEnabledKey, false);
	}
}

