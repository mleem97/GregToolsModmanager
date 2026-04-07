using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Text;
using WorkshopUploader.Localization;
using WorkshopUploader.Models;
using WorkshopUploader.Services;
using WorkshopUploader.Steam;

namespace WorkshopUploader;

[QueryProperty(nameof(ProjectPath), "ProjectPath")]
public partial class EditorPage : ContentPage
{
	private const int MaxWorkshopTags = 20;
	private static string FmfNotice => "\n\n---\n" + S.Get("Editor_FmfNotice");

	private static string MelonLoaderNotice => "\n\n---\n" + S.Get("Editor_MelonLoaderNotice");

	private static readonly FilePickerFileType WorkshopPreviewTypes = new(
		new Dictionary<DevicePlatform, IEnumerable<string>>
		{
			{ DevicePlatform.WinUI, new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" } },
			{ DevicePlatform.macOS, new[] { "png", "jpg", "jpeg", "gif", "webp" } },
			{ DevicePlatform.iOS, new[] { "png", "jpg", "jpeg", "gif", "webp" } },
			{ DevicePlatform.Android, new[] { "image/png", "image/jpeg", "image/gif", "image/webp" } },
		});

	private readonly WorkspaceService _workspace;
	private readonly SteamWorkshopService _steam;
	private readonly AppLogService _log;
	private readonly ObservableCollection<string> _visibilityItems = new() { "Public", "FriendsOnly", "Private" };
	private readonly ObservableCollection<UploadCheckResult> _checkResults = new();
	private readonly ObservableCollection<WorkshopItemDetailVm> _workshopDepSearchResults = new();
	private readonly Dictionary<ulong, string> _workshopDepTitles = new();
	private CancellationTokenSource? _bbPreviewCts;

	private string _projectRoot = "";
	private WorkshopMetadata _metadata = new();

	public string ProjectPath
	{
		set
		{
			_projectRoot = Uri.UnescapeDataString(value);
			_ = LoadAsync();
		}
	}

	public EditorPage(WorkspaceService workspace, SteamWorkshopService steam, AppLogService log)
	{
		InitializeComponent();
		_workspace = workspace;
		_steam = steam;
		_log = log;
		VisibilityPicker.ItemsSource = _visibilityItems;
		NativeProfilePicker.ItemsSource = new[] { "decoration", "code" };
		CheckResultsList.ItemsSource = _checkResults;
		WorkshopDepSearchList.ItemsSource = _workshopDepSearchResults;
		SetEditorTab(0);
	}

	private async Task LoadAsync()
	{
		if (string.IsNullOrEmpty(_projectRoot)) return;

		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			Title = Path.GetFileName(_projectRoot.TrimEnd(Path.DirectorySeparatorChar));
			if (string.IsNullOrEmpty(Title)) Title = "Workshop item";
			PathLabel.Text = _projectRoot;
		});

		_metadata = _workspace.LoadMetadata(_projectRoot);

		var localSnapshot = new WorkshopMetadata
		{
			NeedsFmf = _metadata.NeedsFmf,
			NeedsMelonLoader = _metadata.NeedsMelonLoader,
			NativeConfigProfile = _metadata.NativeConfigProfile,
			PreviewImageRelativePath = _metadata.PreviewImageRelativePath,
			AdditionalPreviews = new List<string>(_metadata.AdditionalPreviews),
			WorkshopDependencyIds = new List<ulong>(_metadata.WorkshopDependencyIds),
		};

		if (_metadata.PublishedFileId != 0)
		{
			await MainThread.InvokeOnMainThreadAsync(() =>
				SyncStatusLabel.Text = S.Get("Editor_LoadingFromSteam"));

			var steam = await _steam.GetItemDetailsAsync(_metadata.PublishedFileId, CancellationToken.None);

			if (steam is not null)
			{
				SteamWorkshopService.ApplySteamWorkshopToMetadata(steam, _metadata, localSnapshot, MaxWorkshopTags);
				try
				{
					_workspace.SaveMetadata(_projectRoot, _metadata);
					await MainThread.InvokeOnMainThreadAsync(() =>
						SyncStatusLabel.Text = S.Get("Editor_LoadedFromSteam"));
				}
				catch (Exception ex)
				{
					await MainThread.InvokeOnMainThreadAsync(() =>
						SyncStatusLabel.Text = S.Format("Editor_SteamSaveFailed", ex.Message));
				}
			}
			else
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
					SyncStatusLabel.Text = S.Get("Editor_SteamRefreshFailed"));
			}
		}
		else
		{
			await MainThread.InvokeOnMainThreadAsync(() => SyncStatusLabel.Text = "");
		}

		await MainThread.InvokeOnMainThreadAsync(BindEditorFromMetadata);
	}

	private void BindEditorFromMetadata()
	{
		TitleEntry.Text = _metadata.Title;
		DescriptionEditor.Text = _metadata.Description;
		VisibilityPicker.SelectedItem = _visibilityItems.Contains(_metadata.Visibility)
			? _metadata.Visibility
			: "Public";
		TagsEntry.Text = string.Join(", ", _metadata.Tags);
		NeedsFmfSwitch.IsToggled = _metadata.NeedsFmf;
		NeedsMelonLoaderSwitch.IsToggled = _metadata.NeedsMelonLoader;
		var profile = string.IsNullOrWhiteSpace(_metadata.NativeConfigProfile)
			? "decoration"
			: _metadata.NativeConfigProfile.Trim().ToLowerInvariant();
		NativeProfilePicker.SelectedItem = profile == "code" ? "code" : "decoration";
		_metadata.WorkshopDependencyIds = _metadata.WorkshopDependencyIds.Where(x => x > 0).Distinct().ToList();
		PreviewPathLabel.Text = Path.Combine(_projectRoot, _metadata.PreviewImageRelativePath);

		var isUpdate = _metadata.PublishedFileId != 0;
		PublishedIdLabel.Text = isUpdate
			? S.Format("Editor_FileId", _metadata.PublishedFileId)
			: S.Get("Editor_NotPublished");

		ChangeLogHintLabel.Text = isUpdate
			? S.Get("Editor_ChangeNotesHint")
			: S.Get("Editor_ChangelogRequiredHint");
		ViewOnSteamBtn.IsVisible = isUpdate;

		UpdateContentSizeUi();
		UpdateCounts();
		UpdateTagsHint();
		RebuildScreenshotGallery();
		RunUploadCheck();
		RefreshDescriptionPreviewImmediate();
		_workshopDepTitles.Clear();
		foreach (var id in _metadata.WorkshopDependencyIds)
		{
			_workshopDepTitles[id] = "";
		}

		RebuildWorkshopDepRows();
		_ = EnrichWorkshopDependencyTitlesAsync();
	}

	private void UpdateContentSizeUi()
	{
		var content = Path.Combine(_projectRoot, "content");
		if (!Directory.Exists(content))
		{
			ContentStatusLabel.Text = S.Get("Editor_ContentMissing");
			ContentSizeBody.Text = "";
			return;
		}

		var st = _workspace.GetContentStats(_projectRoot);
		if (!st.Exists)
		{
			ContentStatusLabel.Text = "content/ — could not analyze.";
			ContentSizeBody.Text = "";
			return;
		}

		var fileCount = CountFilesQuick(content);
		ContentStatusLabel.Text = S.Format("Editor_ContentFiles", fileCount, WorkspaceService.FormatBytes(st.TotalBytes));

		var sb = new StringBuilder();
		foreach (var entry in st.TopEntries.Take(6))
		{
			sb.AppendLine($"  {entry.Name}  {WorkspaceService.FormatBytes(entry.Bytes)}");
		}

		ContentSizeBody.Text = sb.ToString().TrimEnd();
	}

	private static int CountFilesQuick(string dir)
	{
		var n = 0;
		foreach (var _ in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
		{
			if (++n >= 5_000_000) return 5_000_000;
		}
		return n;
	}

	#region Workshop dependencies (workshop_dependency)

	private void RebuildWorkshopDepRows()
	{
		WorkshopDepStack.Children.Clear();
		foreach (var id in _metadata.WorkshopDependencyIds)
		{
			var grid = new Grid { ColumnSpacing = 8 };
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			var lbl = new Label
			{
				Text = FormatWorkshopDepLabel(id),
				FontSize = 12,
				TextColor = Color.FromArgb("#C0FCF6"),
				LineBreakMode = LineBreakMode.TailTruncation,
				VerticalOptions = LayoutOptions.Center,
			};
			Grid.SetColumn(lbl, 0);
			grid.Children.Add(lbl);
			var rm = new Button
			{
				Text = S.Get("Editor_WorkshopDepRemove"),
				Style = Application.Current?.Resources["TertiaryButton"] as Style,
				CommandParameter = id,
			};
			rm.Clicked += OnRemoveWorkshopDepClicked;
			Grid.SetColumn(rm, 1);
			grid.Children.Add(rm);
			WorkshopDepStack.Children.Add(grid);
		}
	}

	private string FormatWorkshopDepLabel(ulong id)
	{
		if (_workshopDepTitles.TryGetValue(id, out var t) && !string.IsNullOrWhiteSpace(t))
		{
			return $"{id} · {t}";
		}

		return id.ToString(CultureInfo.InvariantCulture);
	}

	private async Task EnrichWorkshopDependencyTitlesAsync()
	{
		foreach (var id in _metadata.WorkshopDependencyIds.ToList())
		{
			if (_workshopDepTitles.TryGetValue(id, out var t) && !string.IsNullOrWhiteSpace(t))
			{
				continue;
			}

			var item = await _steam.GetItemDetailsAsync(id, CancellationToken.None).ConfigureAwait(false);
			if (item is null)
			{
				continue;
			}

			_workshopDepTitles[id] = item.Title;
			await MainThread.InvokeOnMainThreadAsync(RebuildWorkshopDepRows);
		}
	}

	private async Task EnrichSingleWorkshopDepTitleAsync(ulong id)
	{
		var item = await _steam.GetItemDetailsAsync(id, CancellationToken.None).ConfigureAwait(false);
		if (item is null)
		{
			return;
		}

		_workshopDepTitles[id] = item.Title;
		await MainThread.InvokeOnMainThreadAsync(RebuildWorkshopDepRows);
	}

	private async void OnWorkshopDepAddById(object? sender, EventArgs e)
	{
		var s = WorkshopDepIdEntry.Text?.Trim() ?? "";
		if (!ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) || id == 0)
		{
			await DisplayAlert(S.Get("Error"), S.Get("Editor_WorkshopDepInvalidId"), S.Get("OK"));
			return;
		}

		if (!await TryAddWorkshopDependencyAsync(id, null))
		{
			return;
		}

		WorkshopDepIdEntry.Text = "";
	}

	private async Task<bool> TryAddWorkshopDependencyAsync(ulong id, string? titleHint)
	{
		if (_metadata.PublishedFileId != 0 && id == _metadata.PublishedFileId)
		{
			await DisplayAlert(S.Get("Error"), S.Get("Editor_WorkshopDepSelf"), S.Get("OK"));
			return false;
		}

		if (_metadata.WorkshopDependencyIds.Contains(id))
		{
			await DisplayAlert(S.Get("Error"), S.Get("Editor_WorkshopDepDuplicate"), S.Get("OK"));
			return false;
		}

		_metadata.WorkshopDependencyIds.Add(id);
		_workshopDepTitles[id] = titleHint ?? "";
		RebuildWorkshopDepRows();
		RunUploadCheck();
		if (string.IsNullOrWhiteSpace(titleHint))
		{
			_ = EnrichSingleWorkshopDepTitleAsync(id);
		}

		return true;
	}

	private void OnRemoveWorkshopDepClicked(object? sender, EventArgs e)
	{
		if (sender is not Button b || b.CommandParameter is null)
		{
			return;
		}

		var id = Convert.ToUInt64(b.CommandParameter, CultureInfo.InvariantCulture);
		_metadata.WorkshopDependencyIds.Remove(id);
		_workshopDepTitles.Remove(id);
		RebuildWorkshopDepRows();
		RunUploadCheck();
	}

	private async void OnWorkshopDepSearch(object? sender, EventArgs e) => await RunWorkshopDepSearchAsync();

	private async void OnWorkshopDepSearchCompleted(object? sender, EventArgs e) => await RunWorkshopDepSearchAsync();

	private async Task RunWorkshopDepSearchAsync()
	{
		var q = WorkshopDepSearchEntry.Text?.Trim() ?? "";
		if (q.Length < 2)
		{
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				_workshopDepSearchResults.Clear();
				WorkshopDepSearchList.IsVisible = false;
			});
			return;
		}

		var result = await _steam.SearchAsync(q, 1, CancellationToken.None).ConfigureAwait(false);
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			_workshopDepSearchResults.Clear();
			foreach (var item in result.Items)
			{
				_workshopDepSearchResults.Add(item);
			}

			WorkshopDepSearchList.IsVisible = _workshopDepSearchResults.Count > 0;
		});
	}

	private async void OnWorkshopDepSearchItemTapped(object? sender, TappedEventArgs e)
	{
		if ((sender as BindableObject)?.BindingContext is not WorkshopItemDetailVm vm)
		{
			return;
		}

		if (!await TryAddWorkshopDependencyAsync(vm.PublishedFileId, vm.Title))
		{
			return;
		}

		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			_workshopDepSearchResults.Clear();
			WorkshopDepSearchList.IsVisible = false;
		});
	}

	#endregion

	#region Upload Dependency Checker

	private void RunUploadCheck()
	{
		ApplyMetadataFromUi();
		var results = UploadDependencyChecker.Check(_projectRoot, _metadata, ChangeLogEditor.Text);
		_checkResults.Clear();
		foreach (var r in results) _checkResults.Add(r);

		var ready = UploadDependencyChecker.IsReadyToUpload(results);
		ReadinessStatusLabel.Text = ready ? S.Get("Editor_ReadyToUpload") : S.Get("Editor_IssuesFound");
		ReadinessStatusLabel.TextColor = ready
			? Color.FromArgb("#61F4D8")
			: Color.FromArgb("#D7383B");
	}

	private void OnRunUploadCheck(object? sender, EventArgs e) => RunUploadCheck();

	#endregion

	#region Editor sub-tabs

	private void OnTabDetails(object? sender, EventArgs e) => SetEditorTab(0);

	private void OnTabAssets(object? sender, EventArgs e) => SetEditorTab(1);

	private void OnTabPublish(object? sender, EventArgs e) => SetEditorTab(2);

	private void SetEditorTab(int index)
	{
		PanelDetails.IsVisible = index == 0;
		PanelAssets.IsVisible = index == 1;
		PanelPublish.IsVisible = index == 2;
		SetEditorTabStyle(TabBtnDetails, index == 0);
		SetEditorTabStyle(TabBtnAssets, index == 1);
		SetEditorTabStyle(TabBtnPublish, index == 2);
	}

	private static void SetEditorTabStyle(Button btn, bool active)
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

	private void OnNeedsFmfToggled(object? sender, ToggledEventArgs e) => RunUploadCheck();

	private void OnNeedsMelonLoaderToggled(object? sender, ToggledEventArgs e) => RunUploadCheck();

	private void OnNativeProfileChanged(object? sender, EventArgs e) => RunUploadCheck();

	private void OnChangeLogTextChanged(object? sender, TextChangedEventArgs e) => RunUploadCheck();

	#endregion

	#region Field events

	private void OnTitleChanged(object? sender, TextChangedEventArgs e) => UpdateCounts();

	private void OnDescriptionChanged(object? sender, TextChangedEventArgs e)
	{
		UpdateCounts();
		ScheduleDescriptionPreview();
	}

	private void ScheduleDescriptionPreview()
	{
		_bbPreviewCts?.Cancel();
		var cts = new CancellationTokenSource();
		_bbPreviewCts = cts;
		_ = Task.Run(async () =>
		{
			try
			{
				await Task.Delay(120, cts.Token).ConfigureAwait(false);
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					if (cts.IsCancellationRequested)
					{
						return;
					}

					RefreshDescriptionPreviewImmediate();
				});
			}
			catch (OperationCanceledException)
			{
				// next keystroke scheduled a new preview
			}
		});
	}

	private void RefreshDescriptionPreviewImmediate()
	{
		var html = SteamBbCodePreview.ToHtmlDocument(DescriptionEditor.Text ?? "");
		DescriptionBbPreview.Source = new HtmlWebViewSource { Html = html, BaseUrl = "https://steamcommunity.com/" };
	}
	private void OnTagsChanged(object? sender, TextChangedEventArgs e) => UpdateTagsHint();

	private void UpdateCounts()
	{
		var t = TitleEntry.Text?.Length ?? 0;
		var d = DescriptionEditor.Text?.Length ?? 0;
		TitleCountLabel.Text = $"{t} / {SteamConstants.MaxTitleLength}";
		DescriptionCountLabel.Text = $"{d} / {SteamConstants.MaxDescriptionLength}";
	}

	private void UpdateTagsHint()
	{
		var raw = TagsEntry.Text ?? "";
		var count = ParseTags(raw).Count;
		TagsHintLabel.Text = $"{count} / {MaxWorkshopTags} tags";
	}

	private static List<string> ParseTags(string? raw)
	{
		if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
		return raw
			.Split(',', StringSplitOptions.RemoveEmptyEntries)
			.Select(s => s.Trim())
			.Where(s => s.Length > 0)
			.Take(MaxWorkshopTags)
			.ToList();
	}

	#endregion

	#region BBCode formatting

	private void OnBbBold(object? s, EventArgs e) => InsertBbTag("b");
	private void OnBbItalic(object? s, EventArgs e) => InsertBbTag("i");
	private void OnBbUnderline(object? s, EventArgs e) => InsertBbTag("u");
	private void OnBbStrike(object? s, EventArgs e) => InsertBbTag("strike");
	private void OnBbH1(object? s, EventArgs e) => InsertBbTag("h1");
	private void OnBbH2(object? s, EventArgs e) => InsertBbTag("h2");
	private void OnBbH3(object? s, EventArgs e) => InsertBbTag("h3");
	private void OnBbCode(object? s, EventArgs e) => InsertBbTag("code");
	private void OnBbQuote(object? s, EventArgs e) => InsertBbTag("quote");
	private void OnBbSpoiler(object? s, EventArgs e) => InsertBbTag("spoiler");

	private void OnBbUrl(object? s, EventArgs e)
	{
		var text = DescriptionEditor.Text ?? "";
		var cursor = DescriptionEditor.CursorPosition;
		var sel = DescriptionEditor.SelectionLength;
		if (sel > 0 && cursor + sel <= text.Length)
		{
			var selected = text.Substring(cursor, sel);
			var insert = $"[url={selected}]{selected}[/url]";
			DescriptionEditor.Text = text.Remove(cursor, sel).Insert(cursor, insert);
			DescriptionEditor.CursorPosition = cursor + insert.Length;
		}
		else
		{
			var insert = "[url=https://]link text[/url]";
			DescriptionEditor.Text = text.Insert(cursor, insert);
			DescriptionEditor.CursorPosition = cursor + 5; // position after [url=
		}
	}

	private void OnBbImg(object? s, EventArgs e)
	{
		var text = DescriptionEditor.Text ?? "";
		var cursor = DescriptionEditor.CursorPosition;
		var insert = "[img]https://[/img]";
		DescriptionEditor.Text = text.Insert(cursor, insert);
		DescriptionEditor.CursorPosition = cursor + 5; // position after [img]
	}

	private void OnBbList(object? s, EventArgs e)
	{
		var text = DescriptionEditor.Text ?? "";
		var cursor = DescriptionEditor.CursorPosition;
		var insert = "[list]\n[*] Item 1\n[*] Item 2\n[/list]";
		DescriptionEditor.Text = text.Insert(cursor, insert);
		DescriptionEditor.CursorPosition = cursor + insert.Length;
	}

	private void OnBbHr(object? s, EventArgs e)
	{
		var text = DescriptionEditor.Text ?? "";
		var cursor = DescriptionEditor.CursorPosition;
		var insert = "[hr][/hr]";
		DescriptionEditor.Text = text.Insert(cursor, insert);
		DescriptionEditor.CursorPosition = cursor + insert.Length;
	}

	private void OnBbTable(object? s, EventArgs e)
	{
		var text = DescriptionEditor.Text ?? "";
		var cursor = DescriptionEditor.CursorPosition;
		var insert = "[table]\n[tr]\n[th]Header[/th]\n[th]Header[/th]\n[/tr]\n[tr]\n[td]Cell[/td]\n[td]Cell[/td]\n[/tr]\n[/table]";
		DescriptionEditor.Text = text.Insert(cursor, insert);
		DescriptionEditor.CursorPosition = cursor + insert.Length;
	}

	private void InsertBbTag(string tag)
	{
		var text = DescriptionEditor.Text ?? "";
		var cursor = DescriptionEditor.CursorPosition;
		var sel = DescriptionEditor.SelectionLength;

		if (sel > 0 && cursor + sel <= text.Length)
		{
			var selected = text.Substring(cursor, sel);
			var wrapped = $"[{tag}]{selected}[/{tag}]";
			DescriptionEditor.Text = text.Remove(cursor, sel).Insert(cursor, wrapped);
			DescriptionEditor.CursorPosition = cursor + wrapped.Length;
		}
		else
		{
			var open = $"[{tag}]";
			var close = $"[/{tag}]";
			DescriptionEditor.Text = text.Insert(cursor, open + close);
			DescriptionEditor.CursorPosition = cursor + open.Length;
		}
	}

	#endregion

	#region Screenshots

	private void RebuildScreenshotGallery()
	{
		ScreenshotsGallery.Children.Clear();
		foreach (var relPath in _metadata.AdditionalPreviews)
		{
			var absPath = Path.Combine(_projectRoot, relPath);
			AddScreenshotTile(relPath, absPath);
		}
		UpdateScreenshotCount();
	}

	private void AddScreenshotTile(string relPath, string absPath)
	{
		var tile = new Border
		{
			WidthRequest = 100,
			HeightRequest = 72,
			StrokeThickness = 0,
			BackgroundColor = Color.FromArgb("#001E1C"),
			Margin = new Thickness(0, 0, 8, 8),
			StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
		};

		var grid = new Grid();

		if (File.Exists(absPath))
		{
			grid.Children.Add(new Image
			{
				Source = ImageSource.FromFile(absPath),
				Aspect = Aspect.AspectFill,
			});
		}
		else
		{
			grid.Children.Add(new Label
			{
				Text = Path.GetFileName(relPath),
				FontSize = 9,
				TextColor = Color.FromArgb("#5A9E96"),
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
			});
		}

		var removeBtn = new Button
		{
			Text = "✕",
			FontSize = 10,
			Padding = new Thickness(4, 2),
			BackgroundColor = Color.FromArgb("#D7383B"),
			TextColor = Colors.White,
			CornerRadius = 3,
			HorizontalOptions = LayoutOptions.End,
			VerticalOptions = LayoutOptions.Start,
			Margin = new Thickness(0, 2, 2, 0),
			MinimumHeightRequest = 20,
			MinimumWidthRequest = 20,
			CommandParameter = relPath,
		};
		removeBtn.Clicked += OnRemoveScreenshot;
		grid.Children.Add(removeBtn);

		tile.Content = grid;
		ScreenshotsGallery.Children.Add(tile);
	}

	private void UpdateScreenshotCount()
	{
		ScreenshotCountLabel.Text = $"{_metadata.AdditionalPreviews.Count} / 9";
	}

	private async void OnAddScreenshot(object? sender, EventArgs e)
	{
		if (_metadata.AdditionalPreviews.Count >= 9)
		{
			await DisplayAlert(S.Get("Editor_Screenshots"), S.Get("Editor_MaxScreenshots"), S.Get("OK"));
			return;
		}

		var result = await FilePicker.Default.PickAsync(new PickOptions
		{
			PickerTitle = S.Get("Editor_ScreenshotPicker"),
			FileTypes = WorkshopPreviewTypes,
		});
		if (result is null) return;

		var ext = Path.GetExtension(result.FileName)?.ToLowerInvariant() ?? ".png";
		if (ext is not (".png" or ".jpg" or ".jpeg" or ".gif" or ".webp")) ext = ".png";

		var screenshotsDir = Path.Combine(_projectRoot, "screenshots");
		Directory.CreateDirectory(screenshotsDir);

		var fileName = $"screenshot_{_metadata.AdditionalPreviews.Count + 1}_{DateTime.Now:HHmmss}{ext}";
		var dest = Path.Combine(screenshotsDir, fileName);
		using (var src = await result.OpenReadAsync())
		using (var dst = File.Create(dest))
		{
			await src.CopyToAsync(dst);
		}

		var relPath = Path.Combine("screenshots", fileName);
		_metadata.AdditionalPreviews.Add(relPath);

		AddScreenshotTile(relPath, dest);
		UpdateScreenshotCount();
		_log.Append($"Added screenshot: {relPath}");
	}

	private void OnRemoveScreenshot(object? sender, EventArgs e)
	{
		if (sender is not Button { CommandParameter: string relPath }) return;

		_metadata.AdditionalPreviews.Remove(relPath);
		RebuildScreenshotGallery();
		_log.Append($"Removed screenshot: {relPath}");
	}

	#endregion

	#region Actions

	private async void OnReloadFromDisk(object? sender, EventArgs e)
	{
		await LoadAsync();
		_log.Append($"Reloaded from disk: {_projectRoot}");
	}

	private async void OnOpenInExplorer(object? sender, EventArgs e)
	{
		if (!OperatingSystem.IsWindows())
		{
			await DisplayAlert(S.Get("Editor_Explorer"), S.Get("Editor_OnlyWindows"), S.Get("OK"));
			return;
		}

		try
		{
			Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{_projectRoot}\"", UseShellExecute = true });
		}
		catch (Exception ex)
		{
			await DisplayAlert(S.Get("Error"), ex.Message, S.Get("OK"));
		}
	}

	private async void OnOpenNativeConfigEditor(object? sender, EventArgs e)
	{
		if (string.IsNullOrEmpty(_projectRoot))
		{
			return;
		}

		await Shell.Current.GoToAsync(
			$"{nameof(NativeConfigEditorPage)}?ProjectPath={Uri.EscapeDataString(_projectRoot)}");
	}

	private async void OnExportContentZip(object? sender, EventArgs e)
	{
		var content = Path.Combine(_projectRoot, "content");
		if (!Directory.Exists(content))
		{
			await DisplayAlert(S.Get("Editor_ExportZip"), S.Get("Editor_ContentMissing"), S.Get("OK"));
			return;
		}

		try
		{
			var zipName = $"content-export-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
			var zipPath = Path.Combine(_projectRoot, zipName);
			if (File.Exists(zipPath)) File.Delete(zipPath);

			ZipFile.CreateFromDirectory(content, zipPath, CompressionLevel.Fastest, false);
			_log.Append($"Exported {zipPath}");
			await DisplayAlert(S.Get("Editor_Exported"), zipPath, S.Get("OK"));
		}
		catch (Exception ex)
		{
			await DisplayAlert(S.Get("Editor_ExportFailed"), ex.Message, S.Get("OK"));
		}
	}

	private async void OnPickPreview(object? sender, EventArgs e)
	{
		var result = await FilePicker.Default.PickAsync(new PickOptions
		{
			PickerTitle = S.Get("Editor_PreviewPicker"),
			FileTypes = WorkshopPreviewTypes,
		});
		if (result is null) return;

		var ext = Path.GetExtension(result.FileName);
		if (string.IsNullOrEmpty(ext)) ext = ".png";
		ext = ext.ToLowerInvariant();
		if (ext is not (".png" or ".jpg" or ".jpeg" or ".gif" or ".webp")) ext = ".png";

		var fileName = $"preview{ext}";
		var dest = Path.Combine(_projectRoot, fileName);
		using (var src = await result.OpenReadAsync())
		using (var dst = File.Create(dest))
		{
			await src.CopyToAsync(dst);
		}

		_metadata.PreviewImageRelativePath = fileName;
		PreviewPathLabel.Text = dest;
		_log.Append($"Preview saved: {dest}");
		RunUploadCheck();
	}

	private void ApplyMetadataFromUi()
	{
		_metadata.Title = TitleEntry.Text ?? "";
		_metadata.Description = DescriptionEditor.Text ?? "";
		_metadata.Visibility = VisibilityPicker.SelectedItem as string ?? "Public";
		_metadata.Tags = ParseTags(TagsEntry.Text);
		_metadata.NeedsFmf = NeedsFmfSwitch.IsToggled;
		_metadata.NeedsMelonLoader = NeedsMelonLoaderSwitch.IsToggled;
		_metadata.NativeConfigProfile = NativeProfilePicker.SelectedItem as string ?? "decoration";
		_metadata.WorkshopDependencyIds = _metadata.WorkshopDependencyIds.Where(x => x > 0).Distinct().ToList();
	}

	private static string BuildUploadDescription(WorkshopMetadata meta)
	{
		var desc = meta.Description ?? "";
		if (meta.NeedsMelonLoader && !desc.Contains("MelonLoader", StringComparison.OrdinalIgnoreCase))
		{
			desc += MelonLoaderNotice;
		}

		if (meta.NeedsFmf && !desc.Contains("FrikaModFramework", StringComparison.OrdinalIgnoreCase))
		{
			desc += FmfNotice;
		}

		return desc;
	}

	private void OnSave(object? sender, EventArgs e)
	{
		try
		{
			ApplyMetadataFromUi();
			_workspace.SaveMetadata(_projectRoot, _metadata);
			_log.Append($"Saved metadata for {_projectRoot}");
			RunUploadCheck();
			DisplayAlert(S.Get("Editor_Saved"), S.Get("Editor_MetaUpdated"), S.Get("OK"));
		}
		catch (Exception ex)
		{
			DisplayAlert(S.Get("Error"), ex.Message, S.Get("OK"));
		}
	}

	private async void OnSaveAndUpload(object? sender, EventArgs e)
	{
		try
		{
			ApplyMetadataFromUi();
			RunUploadCheck();

			var checks = UploadDependencyChecker.Check(_projectRoot, _metadata, ChangeLogEditor.Text);
			if (!UploadDependencyChecker.IsReadyToUpload(checks))
			{
				await DisplayAlert(S.Get("Editor_NotReady"), S.Get("Editor_NotReadyMsg"), S.Get("OK"));
				return;
			}

			_workspace.SaveMetadata(_projectRoot, _metadata);

			var content = Path.Combine(_projectRoot, "content");
			SyncStatusLabel.Text = S.Get("Editor_Uploading");
			var changeLog = ChangeLogEditor.Text;

			var originalDesc = _metadata.Description;
			_metadata.Description = BuildUploadDescription(_metadata);

			var upload = new Progress<float>(p =>
			{
				MainThread.BeginInvokeOnMainThread(() =>
					SyncStatusLabel.Text = S.Format("Editor_UploadProgress", p));
			});
			var log = new Progress<string>(s => _log.Append(s));

			var outcome = await _steam.PublishAsync(
				_projectRoot, _metadata, content, changeLog,
				upload, log, CancellationToken.None);

			_metadata.Description = originalDesc;

			if (!outcome.Success)
			{
				SyncStatusLabel.Text = S.Format("Editor_PublishFailed", outcome.Message);
				await DisplayAlert(S.Get("Error"), outcome.Message, S.Get("OK"));
				return;
			}

			_workspace.SaveMetadata(_projectRoot, _metadata);
			PublishedIdLabel.Text = S.Format("Editor_FileId", _metadata.PublishedFileId);
			ChangeLogHintLabel.Text = S.Get("Editor_ChangeNotesHint");
			ViewOnSteamBtn.IsVisible = true;

			if (_metadata.AdditionalPreviews.Count > 0 && SteamUgcPreviews.IsAvailable)
			{
				SyncStatusLabel.Text = S.Get("Editor_UploadingScreenshots");
				var absPaths = _metadata.AdditionalPreviews
					.Select(p => Path.Combine(_projectRoot, p))
					.ToList();
				await SteamUgcPreviews.UploadAdditionalPreviewsAsync(
					_metadata.PublishedFileId, absPaths, log, CancellationToken.None);
			}

			SyncStatusLabel.Text = S.Get("Editor_PublishedSyncing");

			var synced = await _steam.SyncAfterPublishAsync(
				_metadata.PublishedFileId, _projectRoot, _metadata, _workspace, log, CancellationToken.None);

			SyncStatusLabel.Text = synced
				? S.Get("Editor_SyncComplete")
				: S.Get("Editor_SyncIncomplete");

			PreviewPathLabel.Text = Path.Combine(_projectRoot, _metadata.PreviewImageRelativePath);
			RebuildScreenshotGallery();
			UpdateContentSizeUi();
			RunUploadCheck();

			await DisplayAlert(S.Get("Editor_Publish"),
				S.Format("Editor_FileId", _metadata.PublishedFileId) + "\n\n" +
				(synced ? S.Get("Editor_SyncComplete") : S.Get("Editor_SyncIncomplete")),
				S.Get("OK"));
		}
		catch (Exception ex)
		{
			SyncStatusLabel.Text = "";
			await DisplayAlert(S.Get("Error"), ex.Message, S.Get("OK"));
		}
	}

	private void OnViewOnSteam(object? sender, EventArgs e)
	{
		if (_metadata.PublishedFileId != 0)
			_steam.OpenItemInBrowser(_metadata.PublishedFileId);
	}

	#endregion
}
