using System.Collections.ObjectModel;
using WorkshopUploader.Localization;
using WorkshopUploader.Models;
using WorkshopUploader.Services;

namespace WorkshopUploader;

public partial class ProjectsPage : ContentPage
{
	private const string PrefLogExpandedKey = "ui.projects_log_expanded";

	private readonly WorkspaceService _workspace;
	private readonly AppLogService _log;
	private readonly ObservableCollection<ProjectTileVm> _projects = new();
	private readonly List<ProjectTileVm> _allProjects = new();
	private string _searchQuery = "";

	public ProjectsPage(WorkspaceService workspace, AppLogService log)
	{
		// #region agent log
		DebugSessionLog.Write("H5", "ProjectsPage.ctor", "before_init", null);
		// #endregion
		try
		{
			InitializeComponent();
		}
		catch (Exception ex)
		{
			// #region agent log
			DebugSessionLog.Write("H5", "ProjectsPage.ctor", "init_exception", new { ex.GetType().FullName, ex.Message });
			// #endregion
			throw;
		}

		// #region agent log
		DebugSessionLog.Write("H5", "ProjectsPage.ctor", "after_init", null);
		// #endregion
		_workspace = workspace;
		_log = log;
		ProjectList.ItemsSource = _projects;
		_workspace.EnsureWorkspaceStructure();

		var migrated = _workspace.MigrateLegacyProjects();
		if (migrated > 0)
			_log.Append(S.Format("Projects_Migrated", migrated));

		WorkspacePathLabel.Text = _workspace.WorkspaceRoot;
		_log.LineAppended += OnLogAppended;
		ApplyLogPanelStateFromPreferences();
		ReloadProjects();
		_log.Append(S.Get("Projects_Ready"));
	}

	private void ApplyLogPanelStateFromPreferences()
	{
		var expanded = Preferences.Default.Get(PrefLogExpandedKey, true);
		LogPanelContent.IsVisible = expanded;
		LogToggleBtn.Text = expanded ? S.Get("Projects_LogCollapse") : S.Get("Projects_LogExpand");
	}

	private void OnLogToggle(object? sender, EventArgs e)
	{
		var expanded = !LogPanelContent.IsVisible;
		LogPanelContent.IsVisible = expanded;
		LogToggleBtn.Text = expanded ? S.Get("Projects_LogCollapse") : S.Get("Projects_LogExpand");
		Preferences.Default.Set(PrefLogExpandedKey, expanded);
	}

	protected override void OnDisappearing()
	{
		_log.LineAppended -= OnLogAppended;
		base.OnDisappearing();
	}

	private void OnLogAppended(object? sender, EventArgs e)
	{
		LogLabel.Text = string.Join(Environment.NewLine, _log.Lines);
		LogScroll.ScrollToAsync(LogLabel, ScrollToPosition.End, false);
	}

	private void ReloadProjects()
	{
		_allProjects.Clear();
		foreach (var p in _workspace.ScanProjects())
		{
			_allProjects.Add(new ProjectTileVm(p, _workspace));
		}

		ApplySearchFilter();
	}

	private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
	{
		_searchQuery = SearchEntry.Text ?? "";
		ApplySearchFilter();
	}

	private void ApplySearchFilter()
	{
		_projects.Clear();
		var q = _searchQuery.Trim();
		foreach (var vm in _allProjects)
		{
			if (string.IsNullOrEmpty(q) ||
			    vm.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
			    vm.RootPath.Contains(q, StringComparison.OrdinalIgnoreCase) ||
			    vm.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
			    (!string.IsNullOrEmpty(vm.Tags) && vm.Tags.Contains(q, StringComparison.OrdinalIgnoreCase)))
			{
				_projects.Add(vm);
			}
		}
	}

	private void OnRefreshProjects(object? sender, EventArgs e)
	{
		ReloadProjects();
		_log.Append(S.Get("Projects_Refreshed"));
	}

	private void OnRefreshing(object? sender, EventArgs e)
	{
		try
		{
			ReloadProjects();
			_log.Append("Workspace refreshed.");
		}
		finally
		{
			RefreshView.IsRefreshing = false;
		}
	}

	private async void OnProjectSelected(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is not ProjectTileVm vm)
		{
			return;
		}

		ProjectList.SelectedItem = null;
		await Shell.Current.GoToAsync($"{nameof(EditorPage)}?ProjectPath={Uri.EscapeDataString(vm.RootPath)}");
	}
}
