using System.Collections.ObjectModel;
using System.Diagnostics;
using WorkshopUploader.Localization;
using WorkshopUploader.Models;
using WorkshopUploader.Services;

namespace WorkshopUploader;

public partial class ModManagerPage : ContentPage
{
	private readonly SteamWorkshopService _steam;
	private readonly ModDependencyService _deps;
	private readonly FfmPluginChannelRegistry _channels;
	private readonly AppLogService _log;

	private readonly ObservableCollection<DependencyCheckResult> _checks = new();
	private readonly ObservableCollection<PluginPackageInfo> _plugins = new();
	private readonly ObservableCollection<WorkshopItemDetailVm> _storeItems = new();
	private readonly ObservableCollection<WorkshopItemDetailVm> _installedItems = new();
	private readonly ObservableCollection<WorkshopItemDetailVm> _favoritesItems = new();

	private int _storePage = 1;
	private bool _storeHasMore;
	private int _installedPage = 1;
	private bool _installedHasMore;
	private int _favoritesPage = 1;
	private bool _favoritesHasMore;
	private string _currentTab = "store";

	public ModManagerPage(
		SteamWorkshopService steam,
		ModDependencyService deps,
		FfmPluginChannelRegistry channels,
		AppLogService log)
	{
		InitializeComponent();
		_steam = steam;
		_deps = deps;
		_channels = channels;
		_log = log;

		ChecksList.ItemsSource = _checks;
		PluginsList.ItemsSource = _plugins;
		StoreList.ItemsSource = _storeItems;
		InstalledList.ItemsSource = _installedItems;
		FavoritesList.ItemsSource = _favoritesItems;

		ChannelPicker.SelectedIndex = 0;
		TagFilter.SelectedIndex = 0;
		SortPicker.SelectedIndex = 0;

		SwitchToTab("store");
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_ = LoadStoreAsync();
	}

	#region Tab switching

	private void OnTabStore(object? sender, EventArgs e) => SwitchToTab("store");
	private void OnTabInstalled(object? sender, EventArgs e) => SwitchToTab("installed");
	private void OnTabFavorites(object? sender, EventArgs e) => SwitchToTab("favorites");
	private void OnTabHealth(object? sender, EventArgs e) => SwitchToTab("health");

	private void OnRefreshCurrentTab(object? sender, EventArgs e)
	{
		switch (_currentTab)
		{
			case "store":
				_ = LoadStoreAsync();
				break;
			case "installed":
				_ = LoadInstalledAsync();
				break;
			case "favorites":
				_ = LoadFavoritesAsync();
				break;
			case "health":
				RefreshChecks();
				RefreshPluginList();
				break;
		}
	}

	private void SwitchToTab(string tab)
	{
		_currentTab = tab;
		StoreView.IsVisible = tab == "store";
		InstalledView.IsVisible = tab == "installed";
		FavoritesView.IsVisible = tab == "favorites";
		HealthView.IsVisible = tab == "health";

		SetTabActive(TabStore, tab == "store");
		SetTabActive(TabInstalled, tab == "installed");
		SetTabActive(TabFavorites, tab == "favorites");
		SetTabActive(TabHealth, tab == "health");

		switch (tab)
		{
			case "store":
				_ = LoadStoreAsync();
				break;
			case "installed":
				_ = LoadInstalledAsync();
				break;
			case "favorites":
				_ = LoadFavoritesAsync();
				break;
			case "health":
				RefreshChecks();
				RefreshPluginList();
				break;
		}
	}

	private static void SetTabActive(Button btn, bool active)
	{
		if (active)
		{
			btn.BackgroundColor = Color.FromArgb("#61F4D8");
			btn.TextColor = Color.FromArgb("#001110");
		}
		else
		{
			btn.BackgroundColor = Colors.Transparent;
			btn.TextColor = Color.FromArgb("#61F4D8");
		}
	}

	#endregion

	#region Store tab

	private async Task LoadStoreAsync()
	{
		StoreStatusLabel.Text = S.Get("Loading");

		var searchText = SearchEntry.Text;
		WorkshopBrowseResultVm result;

		if (!string.IsNullOrWhiteSpace(searchText))
		{
			result = await _steam.SearchAsync(searchText, _storePage, CancellationToken.None);
		}
		else
		{
			var sort = GetSelectedSort();
			var tag = GetSelectedTag();
			result = await _steam.BrowseAsync(_storePage, sort, tag, CancellationToken.None);
		}

		_storeItems.Clear();
		foreach (var item in result.Items)
		{
			_storeItems.Add(item);
		}

		_storeHasMore = result.HasMorePages;
		StorePrevBtn.IsEnabled = _storePage > 1;
		StoreNextBtn.IsEnabled = _storeHasMore;
		StorePageLabel.Text = S.Format("PageWithTotal", _storePage, result.TotalResults);
		StoreStatusLabel.Text = result.Items.Count == 0 ? S.Get("Mod_NoItems") : "";
	}

	private void OnSearchSubmit(object? sender, EventArgs e)
	{
		_storePage = 1;
		_ = LoadStoreAsync();
	}

	private void OnFilterChanged(object? sender, EventArgs e)
	{
		_storePage = 1;
		_ = LoadStoreAsync();
	}

	private void OnStorePrev(object? sender, EventArgs e)
	{
		if (_storePage > 1)
		{
			_storePage--;
			_ = LoadStoreAsync();
		}
	}

	private void OnStoreNext(object? sender, EventArgs e)
	{
		if (_storeHasMore)
		{
			_storePage++;
			_ = LoadStoreAsync();
		}
	}

	private async void OnQuickSubscribe(object? sender, EventArgs e)
	{
		if (sender is not Button { BindingContext: WorkshopItemDetailVm vm }) return;
		var ok = await _steam.SubscribeAsync(vm.PublishedFileId);
		if (ok)
		{
			_log.Append($"Subscribed to {vm.Title}");
			if (sender is Button btn) btn.Text = S.Get("Mod_Subscribed");
		}
	}

	private async void OnStoreItemTapped(object? sender, TappedEventArgs e)
	{
		var vm = (sender as BindableObject)?.BindingContext as WorkshopItemDetailVm;
		if (vm is null) return;
		await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?FileId={vm.PublishedFileId}");
	}

	private WorkshopSortMode GetSelectedSort()
	{
		return (SortPicker.SelectedIndex) switch
		{
			1 => WorkshopSortMode.CreationDate,
			2 => WorkshopSortMode.VoteScore,
			3 => WorkshopSortMode.Trending,
			4 => WorkshopSortMode.Subscriptions,
			5 => WorkshopSortMode.TitleAsc,
			_ => WorkshopSortMode.UpdateDate,
		};
	}

	private string? GetSelectedTag()
	{
		if (TagFilter.SelectedIndex <= 0) return null;
		return TagFilter.SelectedItem as string;
	}

	#endregion

	#region Installed tab

	private async Task LoadInstalledAsync()
	{
		InstalledStatusLabel.Text = S.Get("Mod_LoadingSubscribed");
		var result = await _steam.ListSubscribedAsync(_installedPage, CancellationToken.None);

		_installedItems.Clear();
		foreach (var item in result.Items)
		{
			_installedItems.Add(item);
		}

		_installedHasMore = result.HasMorePages;
		InstalledPrevBtn.IsEnabled = _installedPage > 1;
		InstalledNextBtn.IsEnabled = _installedHasMore;
		InstalledPageLabel.Text = S.Format("PageWithTotal", _installedPage, result.TotalResults);
		InstalledStatusLabel.Text = result.Items.Count == 0 ? S.Get("Mod_NoSubscribed") : S.Format("Mod_SubscribedCount", result.TotalResults);
	}

	private void OnInstalledPrev(object? sender, EventArgs e)
	{
		if (_installedPage > 1) { _installedPage--; _ = LoadInstalledAsync(); }
	}

	private void OnInstalledNext(object? sender, EventArgs e)
	{
		if (_installedHasMore) { _installedPage++; _ = LoadInstalledAsync(); }
	}

	private async void OnQuickUnsubscribe(object? sender, EventArgs e)
	{
		if (sender is not Button { BindingContext: WorkshopItemDetailVm vm }) return;
		var ok = await _steam.UnsubscribeAsync(vm.PublishedFileId);
		if (ok)
		{
			_log.Append($"Unsubscribed from {vm.Title}");
			_installedItems.Remove(vm);
		}
	}

	#endregion

	#region Favorites tab

	private async Task LoadFavoritesAsync()
	{
		FavoritesStatusLabel.Text = S.Get("Mod_LoadingFavorites");
		var result = await _steam.ListFavoritedAsync(_favoritesPage, CancellationToken.None);

		_favoritesItems.Clear();
		foreach (var item in result.Items)
		{
			_favoritesItems.Add(item);
		}

		_favoritesHasMore = result.HasMorePages;
		FavoritesPrevBtn.IsEnabled = _favoritesPage > 1;
		FavoritesNextBtn.IsEnabled = _favoritesHasMore;
		FavoritesPageLabel.Text = S.Format("PageWithTotal", _favoritesPage, result.TotalResults);
		FavoritesStatusLabel.Text = result.Items.Count == 0 ? S.Get("Mod_NoFavorites") : S.Format("Mod_FavoritedCount", result.TotalResults);
	}

	private void OnFavoritesPrev(object? sender, EventArgs e)
	{
		if (_favoritesPage > 1) { _favoritesPage--; _ = LoadFavoritesAsync(); }
	}

	private void OnFavoritesNext(object? sender, EventArgs e)
	{
		if (_favoritesHasMore) { _favoritesPage++; _ = LoadFavoritesAsync(); }
	}

	private async void OnQuickUnfavorite(object? sender, EventArgs e)
	{
		if (sender is not Button { BindingContext: WorkshopItemDetailVm vm }) return;
		var ok = await _steam.RemoveFavoriteAsync(vm.PublishedFileId);
		if (ok)
		{
			_log.Append($"Removed {vm.Title} from favorites");
			_favoritesItems.Remove(vm);
		}
	}

	#endregion

	#region Health tab

	private void OnRefreshChecks(object? sender, EventArgs e)
	{
		_deps.InvalidateCache();
		RefreshChecks();
	}

	private void RefreshChecks()
	{
		_checks.Clear();
		var results = _deps.RunChecks();
		foreach (var r in results)
		{
			_checks.Add(r);
		}

		UpdateMelonStatus();
	}

	private void UpdateMelonStatus()
	{
		var mlCheck = _checks.FirstOrDefault(c => c.Label == "MelonLoader");
		if (mlCheck is null)
		{
			MelonStatusLabel.Text = S.Get("Mod_MelonUnknown");
			return;
		}

		MelonStatusLabel.Text = mlCheck.Status switch
		{
			DependencyStatus.Ok => S.Format("Mod_MelonInstalled", mlCheck.Detail),
			_ => mlCheck.Detail,
		};
	}

	private void OnOpenFolder(object? sender, EventArgs e)
	{
		var path = (sender as Button)?.CommandParameter as string;
		if (string.IsNullOrEmpty(path) || !OperatingSystem.IsWindows()) return;

		try
		{
			if (File.Exists(path))
				path = Path.GetDirectoryName(path) ?? path;
			if (!Directory.Exists(path))
			{
				var parent = Path.GetDirectoryName(path);
				if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
					path = parent;
			}

			Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
		}
		catch (Exception ex)
		{
			_log.Append($"Could not open folder: {ex.Message}");
		}
	}

	private void OnMelonLoaderDownload(object? sender, EventArgs e)
	{
		try
		{
			Process.Start(new ProcessStartInfo("https://github.com/LavaGang/MelonLoader/releases") { UseShellExecute = true });
		}
		catch (Exception ex)
		{
			_log.Append($"Could not open browser: {ex.Message}");
		}
	}

	private void OnOpenGameFolder(object? sender, EventArgs e)
	{
		var root = _deps.GameRoot;
		if (string.IsNullOrEmpty(root) || !Directory.Exists(root) || !OperatingSystem.IsWindows()) return;

		try
		{
			Process.Start(new ProcessStartInfo("explorer.exe", $"\"{root}\"") { UseShellExecute = true });
		}
		catch (Exception ex)
		{
			_log.Append($"Could not open game folder: {ex.Message}");
		}
	}

	private void OnChannelChanged(object? sender, EventArgs e) => RefreshPluginList();

	private void RefreshPluginList()
	{
		_plugins.Clear();
		var channelName = ChannelPicker.SelectedIndex == 1 ? "beta" : "stable";
		var source = _channels.GetSource(channelName);

		if (source is null)
		{
			ChannelInfoLabel.Text = channelName == "beta"
				? S.Get("Mod_BetaNotConfigured")
				: S.Get("Mod_StableInfo");
			return;
		}

		ChannelInfoLabel.Text = channelName == "beta"
			? S.Get("Mod_BetaInfo")
			: S.Get("Mod_StableInfo");

		try
		{
			var list = source.ListPlugins();
			foreach (var p in list)
				_plugins.Add(p);
		}
		catch (NotImplementedException)
		{
			ChannelInfoLabel.Text = S.Format("Mod_ChannelNotImpl", channelName);
		}
		catch (Exception ex)
		{
			ChannelInfoLabel.Text = $"Error: {ex.Message}";
		}
	}

	#endregion
}
