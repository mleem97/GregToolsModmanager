using System.Collections.ObjectModel;
using WorkshopUploader.Localization;
using WorkshopUploader.Models;
using WorkshopUploader.Services;

namespace WorkshopUploader;

public partial class MyUploadsPage : ContentPage
{
	private readonly SteamWorkshopService _steam;
	private readonly WorkspaceService _workspace;
	private readonly AppLogService _log;
	private readonly ObservableCollection<WorkshopItemDetailVm> _items = new();

	private int _page = 1;
	private bool _hasMore;

	public MyUploadsPage(SteamWorkshopService steam, WorkspaceService workspace, AppLogService log)
	{
		InitializeComponent();
		_steam = steam;
		_workspace = workspace;
		_log = log;
		UploadsList.ItemsSource = _items;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_ = LoadListAsync();
	}

	private void OnUploadsSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		UpdateSelectionCount();
	}

	private void UpdateSelectionCount()
	{
		var n = UploadsList.SelectedItems?.Count ?? 0;
		SelectionCountLabel.Text = S.Format("Uploads_Selected", n);
	}

	private async void OnRefresh(object? sender, EventArgs e) => await LoadListAsync();

	private async void OnRefreshPull(object? sender, EventArgs e)
	{
		try { await LoadListAsync(); }
		finally { UploadsRefresh.IsRefreshing = false; }
	}

	private async Task LoadListAsync()
	{
		try
		{
			var result = await _steam.ListMyPublishedPagedAsync(_page, CancellationToken.None);

			_items.Clear();
			foreach (var x in result.Items)
			{
				_items.Add(x);
			}

			_hasMore = result.HasMorePages;
			PrevBtn.IsEnabled = _page > 1;
			NextBtn.IsEnabled = _hasMore;
			PageLabel.Text = S.Format("PageWithTotal", _page, result.TotalResults);

			UploadsList.SelectedItems?.Clear();
			UpdateSelectionCount();
			_log.Append($"Workshop uploads: {result.TotalResults} item(s), page {_page}.");
		}
		catch (Exception ex)
		{
			await DisplayAlert(S.Get("Error"), ex.Message, S.Get("OK"));
		}
	}

	private void OnPrev(object? sender, EventArgs e)
	{
		if (_page > 1) { _page--; _ = LoadListAsync(); }
	}

	private void OnNext(object? sender, EventArgs e)
	{
		if (_hasMore) { _page++; _ = LoadListAsync(); }
	}

	private async void OnImportSelected(object? sender, EventArgs e)
	{
		var selected = UploadsList.SelectedItems?.OfType<WorkshopItemDetailVm>().ToList()
			?? new List<WorkshopItemDetailVm>();
		if (selected.Count == 0)
		{
			await DisplayAlert(S.Get("Import"), S.Get("Uploads_SelectFirst"), S.Get("OK"));
			return;
		}

		var ok = await DisplayAlert(
			S.Get("Uploads_BulkImport"),
			S.Format("Uploads_BulkImportMsg", selected.Count, _workspace.WorkspaceRoot),
			S.Get("Import"),
			S.Get("Cancel"));
		if (!ok) return;

		var lastPath = "";
		try
		{
			foreach (var vm in selected)
			{
				var progress = new Progress<float>(p => _log.Append($"[{vm.PublishedFileId}] {p:P0}"));
				var log = new Progress<string>(s => _log.Append($"[{vm.PublishedFileId}] {s}"));
				var outcome = await _steam.ImportPublishedToWorkspaceAsync(
					vm.PublishedFileId, null, _workspace, log, progress, CancellationToken.None);

				if (!outcome.Success)
				{
					await DisplayAlert(S.Get("Uploads_ImportFailed"), $"{vm.Title}: {outcome.Message}", S.Get("OK"));
					return;
				}

				lastPath = outcome.ProjectRoot;
			}

			await DisplayAlert(S.Get("Uploads_Imported"), $"{selected.Count} project(s) under:\n{_workspace.WorkspaceRoot}", S.Get("OK"));
			if (!string.IsNullOrEmpty(lastPath))
			{
				try
				{
					await Shell.Current.GoToAsync($"{nameof(EditorPage)}?ProjectPath={Uri.EscapeDataString(lastPath)}");
				}
				catch (Exception navEx)
				{
					AppFileLog.Error("MyUploadsPage navigation failed after bulk import", navEx);
					await DisplayAlert(S.Get("Error"), navEx.Message, S.Get("OK"));
				}
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert(S.Get("Error"), ex.Message, S.Get("OK"));
		}
	}

	private async void OnDownloadClicked(object? sender, EventArgs e)
	{
		if (sender is not Button { BindingContext: WorkshopItemDetailVm vm }) return;

		var suggested = $"{vm.Title}_{vm.PublishedFileId}";
		var folder = await DisplayPromptAsync(
			S.Get("Uploads_FolderName"),
			S.Format("Uploads_FolderPrompt", _workspace.WorkspaceRoot),
			S.Get("Import"), S.Get("Cancel"),
			initialValue: suggested, maxLength: 80, keyboard: Keyboard.Default);
		if (folder is null) return;

		try
		{
			var progress = new Progress<float>(p => _log.Append($"Download {p:P0}"));
			var log = new Progress<string>(s => _log.Append(s));
			var outcome = await _steam.ImportPublishedToWorkspaceAsync(
				vm.PublishedFileId,
				string.IsNullOrWhiteSpace(folder) ? null : folder,
				_workspace, log, progress, CancellationToken.None);

			if (!outcome.Success)
			{
				await DisplayAlert(S.Get("Uploads_ImportFailed"), outcome.Message, S.Get("OK"));
				return;
			}

			await DisplayAlert(S.Get("Uploads_Imported"), outcome.ProjectRoot, S.Get("OK"));
			try
			{
				await Shell.Current.GoToAsync($"{nameof(EditorPage)}?ProjectPath={Uri.EscapeDataString(outcome.ProjectRoot)}");
			}
			catch (Exception navEx)
			{
				AppFileLog.Error("MyUploadsPage navigation failed after single import", navEx);
				await DisplayAlert(S.Get("Error"), navEx.Message, S.Get("OK"));
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", ex.Message, "OK");
		}
	}

	private void OnViewOnSteam(object? sender, EventArgs e)
	{
		if (sender is Button { BindingContext: WorkshopItemDetailVm vm })
		{
			_steam.OpenItemInBrowser(vm.PublishedFileId);
		}
	}
}
