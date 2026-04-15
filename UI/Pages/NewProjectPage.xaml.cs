using GregModmanager.Localization;
using GregModmanager.Models;
using GregModmanager.Services;

namespace GregModmanager;

public partial class NewProjectPage : ContentPage
{
	private readonly WorkspaceService _workspace;
	private readonly AppLogService _log;

	public NewProjectPage(WorkspaceService workspace, AppLogService log)
	{
		InitializeComponent();
		_workspace = workspace;
		_log = log;
		TemplateKindPicker.ItemsSource = new[]
		{
			S.Get("NewProject_Vanilla"),
			S.Get("NewProject_Modded"),
			S.Get("NewProject_GCF"),
			S.Get("NewProject_Uxml"),
			S.Get("NewProject_Model"),
			S.Get("NewProject_Texture"),
			S.Get("NewProject_Audio"),
		};
		TemplateKindPicker.SelectedIndex = 0;
		NewProjectNameEntry.Text = "MyWorkshopMod";
	}

	private async void OnCreateTemplate(object? sender, EventArgs e)
	{
		try
		{
			var idx = TemplateKindPicker.SelectedIndex;
			if (idx < 0)
			{
				idx = 0;
			}

			var kind = (WorkshopTemplateKind)idx;
			var name = NewProjectNameEntry.Text ?? string.Empty;
			var path = _workspace.CreateTemplateProject(name, kind);
			_log.Append($"Created template: {path}");
			await DisplayAlert(S.Get("NewProject_Created"), path, S.Get("OK"));
		}
		catch (Exception ex)
		{
			_log.Append($"Create template failed: {ex.Message}");
			await DisplayAlert(S.Get("NewProject_CouldNotCreate"), ex.Message, S.Get("OK"));
		}
	}
}

