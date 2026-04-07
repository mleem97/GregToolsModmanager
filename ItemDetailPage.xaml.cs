using WorkshopUploader.Localization;
using WorkshopUploader.Models;
using WorkshopUploader.Services;

namespace WorkshopUploader;

[QueryProperty(nameof(FileId), "FileId")]
public partial class ItemDetailPage : ContentPage
{
	private readonly SteamWorkshopService _steam;
	private WorkshopItemDetailVm? _item;
	private ulong _fileId;

	public string FileId
	{
		set
		{
			if (ulong.TryParse(value, out var id))
			{
				_fileId = id;
				_ = LoadItemAsync(id);
			}
		}
	}

	public ItemDetailPage(SteamWorkshopService steam)
	{
		InitializeComponent();
		_steam = steam;
	}

	private async void OnRefreshDetail(object? sender, EventArgs e)
	{
		if (_fileId == 0)
		{
			return;
		}

		await LoadItemAsync(_fileId);
	}

	private async Task LoadItemAsync(ulong fileId)
	{
		LoadingIndicator.IsRunning = true;
		LoadingIndicator.IsVisible = true;
		ContentRoot.IsVisible = false;

		_item = await _steam.GetItemDetailsAsync(fileId, CancellationToken.None);

		LoadingIndicator.IsRunning = false;
		LoadingIndicator.IsVisible = false;

		if (_item is null)
		{
			await DisplayAlert(S.Get("Error"), S.Get("Detail_CouldNotLoad"), S.Get("OK"));
			await Shell.Current.GoToAsync("..");
			return;
		}

		BindItem(_item);
		ContentRoot.IsVisible = true;
	}

	private void BindItem(WorkshopItemDetailVm item)
	{
		Title = item.Title;
		ItemTitle.Text = item.Title;
		AuthorLabel.Text = string.IsNullOrEmpty(item.OwnerName) ? $"Author: {item.OwnerSteamId}" : $"by {item.OwnerName}";
		SourceBadge.Text = item.SourceLabel;
		SourceBadge.TextColor = Color.FromArgb(item.SourceColor);
		VisibilityLabel.Text = item.Visibility;

		if (!string.IsNullOrEmpty(item.PreviewImageUrl))
		{
			PreviewImage.Source = ImageSource.FromUri(new Uri(item.PreviewImageUrl));
		}

		SubsCount.Text = FormatNumber(item.NumSubscriptions);
		FavsCount.Text = FormatNumber(item.NumFavorites);
		VotesLabel.Text = $"+{item.VotesUp} / -{item.VotesDown}";
		SizeLabel.Text = WorkspaceService.FormatBytes(item.SizeBytes);
		ScoreLabel.Text = $"{item.Score:P0}";
		CommentsCount.Text = FormatNumber(item.NumComments);
		CreatedLabel.Text = item.Created.ToString("d");
		UpdatedLabel.Text = item.Updated.ToString("d");
		DescriptionLabel.Text = string.IsNullOrWhiteSpace(item.Description) ? S.Get("Detail_NoDescription") : item.Description;

		DependencyCard.IsVisible = item.HasDependencyHints;
		DependenciesBodyLabel.Text = item.DependencyHintBlock;

		VisibilitySidebarLabel.Text = item.Visibility;
		FileIdLabel.Text = item.PublishedFileId.ToString();

		VersionInfoLabel.Text = S.Format("Detail_LastUpdated", item.Updated.ToString("g"));
		CommentsInfoLabel.Text = item.NumComments == 0
			? S.Get("Detail_NoComments")
			: S.Format("Detail_CommentsCount", item.NumComments);

		UpdateSubscribeButton(item.IsSubscribed);
		UpdateFavoriteButton(false);

		TagsLayout.Children.Clear();
		foreach (var tag in item.Tags)
		{
			var border = new Border
			{
				StrokeThickness = 0,
				BackgroundColor = Color.FromArgb("#61F4D8"),
				Padding = new Thickness(10, 4),
				Margin = new Thickness(0, 0, 6, 6),
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 },
				Content = new Label
				{
					Text = tag,
					FontSize = 11,
					TextColor = Color.FromArgb("#001110"),
				}
			};
			TagsLayout.Children.Add(border);
		}

		GalleryLayout.Children.Clear();
		if (item.AdditionalPreviewUrls.Length > 0)
		{
			GalleryCard.IsVisible = true;
			foreach (var url in item.AdditionalPreviewUrls)
			{
				var tile = new Border
				{
					WidthRequest = 140,
					HeightRequest = 100,
					StrokeThickness = 0,
					BackgroundColor = Color.FromArgb("#001E1C"),
					Margin = new Thickness(0, 0, 8, 8),
					StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
					Content = new Image
					{
						Source = ImageSource.FromUri(new Uri(url)),
						Aspect = Aspect.AspectFill,
					},
				};
				GalleryLayout.Children.Add(tile);
			}
		}
		else
		{
			GalleryCard.IsVisible = false;
		}

		if (item.IsBanned)
		{
			StatusLabel.Text = S.Get("Detail_ItemBanned");
			StatusLabel.TextColor = Color.FromArgb("#D7383B");
		}
	}

	private void UpdateSubscribeButton(bool isSubscribed)
	{
		SubscribeBtn.Text = isSubscribed ? S.Get("Detail_Unsubscribe") : S.Get("Detail_Subscribe");
	}

	private void UpdateFavoriteButton(bool isFavorited)
	{
		FavoriteBtn.Text = isFavorited ? S.Get("Detail_Unfavorite") : S.Get("Detail_Favorite");
	}

	private async void OnSubscribeToggle(object? sender, EventArgs e)
	{
		if (_item is null) return;

		StatusLabel.Text = S.Get("Working");
		bool success;
		var wasSubscribed = _item.IsSubscribed;
		if (wasSubscribed)
		{
			success = await _steam.UnsubscribeAsync(_item.PublishedFileId);
		}
		else
		{
			success = await _steam.SubscribeAsync(_item.PublishedFileId);
		}

		if (success)
		{
			var nowSubscribed = !wasSubscribed;
			UpdateSubscribeButton(nowSubscribed);
			StatusLabel.Text = nowSubscribed ? S.Get("Detail_SubscribedMsg") : S.Get("Detail_UnsubscribedMsg");
			await ReloadItemAsync();
		}
		else
		{
			StatusLabel.Text = S.Get("Detail_ActionFailed");
		}
	}

	private async Task ReloadItemAsync()
	{
		if (_item is null) return;
		var refreshed = await _steam.GetItemDetailsAsync(_item.PublishedFileId, CancellationToken.None);
		if (refreshed is not null)
		{
			_item = refreshed;
		}
	}

	private async void OnFavoriteToggle(object? sender, EventArgs e)
	{
		if (_item is null) return;

		StatusLabel.Text = S.Get("Working");
		var isFav = FavoriteBtn.Text == S.Get("Detail_Unfavorite");
		bool success = isFav
			? await _steam.RemoveFavoriteAsync(_item.PublishedFileId)
			: await _steam.AddFavoriteAsync(_item.PublishedFileId);

		if (success)
		{
			UpdateFavoriteButton(!isFav);
			StatusLabel.Text = !isFav ? S.Get("Detail_AddedFavorites") : S.Get("Detail_RemovedFavorites");
		}
		else
		{
			StatusLabel.Text = S.Get("Detail_ActionFailed");
		}
	}

	private async void OnVoteUp(object? sender, EventArgs e)
	{
		if (_item is null) return;
		StatusLabel.Text = S.Get("Detail_Voting");
		var ok = await _steam.VoteAsync(_item.PublishedFileId, true);
		StatusLabel.Text = ok ? S.Get("Detail_VotedUp") : S.Get("Detail_VoteFailed");
	}

	private async void OnVoteDown(object? sender, EventArgs e)
	{
		if (_item is null) return;
		StatusLabel.Text = S.Get("Detail_Voting");
		var ok = await _steam.VoteAsync(_item.PublishedFileId, false);
		StatusLabel.Text = ok ? S.Get("Detail_VotedDown") : S.Get("Detail_VoteFailed");
	}

	private void OnOpenInSteam(object? sender, EventArgs e)
	{
		if (_item is not null)
			_steam.OpenItemInBrowser(_item.PublishedFileId);
	}

	private void OnOpenChangelog(object? sender, EventArgs e)
	{
		if (_item is not null && !string.IsNullOrEmpty(_item.ChangelogUrl))
			OpenUrl(_item.ChangelogUrl);
	}

	private void OnOpenComments(object? sender, EventArgs e)
	{
		if (_item is not null && !string.IsNullOrEmpty(_item.CommentsUrl))
			OpenUrl(_item.CommentsUrl);
	}

	private static void OpenUrl(string url)
	{
		try
		{
			System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
			{
				FileName = url,
				UseShellExecute = true,
			});
		}
		catch
		{
			// ignored
		}
	}

	private static string FormatNumber(ulong n)
	{
		return n switch
		{
			>= 1_000_000 => $"{n / 1_000_000.0:0.#}M",
			>= 1_000 => $"{n / 1_000.0:0.#}K",
			_ => n.ToString(),
		};
	}
}
