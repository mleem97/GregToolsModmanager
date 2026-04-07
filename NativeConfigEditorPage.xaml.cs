using System.Globalization;
using System.Text.Json;
using WorkshopUploader.Localization;
using WorkshopUploader.Models;
using WorkshopUploader.Services;

namespace WorkshopUploader;

[QueryProperty(nameof(ProjectPath), "ProjectPath")]
public partial class NativeConfigEditorPage : ContentPage
{
	private string _projectRoot = "";
	private NativeModConfig _native = new();
	private ModOptionsConfigFile _modOptions = new();
	private readonly List<ShopItemUi> _shopUis = new();
	private readonly List<StaticItemUi> _staticUis = new();
	private readonly List<DllItemUi> _dllUis = new();
	private readonly List<SettingRowUi> _settingRows = new();
	private readonly WorkspaceService _workspace;
	private bool _isDecorationProfile = true;

	public string ProjectPath
	{
		set
		{
			_projectRoot = Uri.UnescapeDataString(value ?? "");
			_ = LoadAsync();
		}
	}

	public NativeConfigEditorPage(WorkspaceService workspace)
	{
		_workspace = workspace;
		InitializeComponent();
	}

	private async Task LoadAsync()
	{
		if (string.IsNullOrEmpty(_projectRoot))
		{
			return;
		}

		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			HeaderPathLabel.Text = _projectRoot;
			Title = S.Get("NativeConfig_Title");
		});

		try
		{
			_native = NativeModConfigStore.LoadConfig(_projectRoot);
			_modOptions = NativeModConfigStore.LoadModOptions(_projectRoot);
			var meta = _workspace.LoadMetadata(_projectRoot);
			var p = meta.NativeConfigProfile?.Trim().ToLowerInvariant();
			_isDecorationProfile = string.IsNullOrEmpty(p) || p == "decoration";
		}
		catch (Exception ex)
		{
			await MainThread.InvokeOnMainThreadAsync(async () =>
				await DisplayAlert(S.Get("Error"), ex.Message, S.Get("OK")));
			return;
		}

		await MainThread.InvokeOnMainThreadAsync(BindUi);
	}

	private void ApplyProfileVisibility()
	{
		DecorationProfileHintBorder.IsVisible = _isDecorationProfile;
		Il2CppNoticeBorder.IsVisible = !_isDecorationProfile;
		PanelShopDecoration.IsVisible = _isDecorationProfile;
		PanelStaticDecoration.IsVisible = _isDecorationProfile;
		PanelDllCode.IsVisible = !_isDecorationProfile;
	}

	private void BindUi()
	{
		ApplyProfileVisibility();
		ModNameEntry.Text = _native.ModName;
		RebuildShopUi();
		RebuildStaticUi();
		RebuildDllUi();

		SchemaVersionEntry.Text = _modOptions.SchemaVersion.ToString(CultureInfo.InvariantCulture);
		var kind = string.IsNullOrWhiteSpace(_modOptions.ModKind) ? "standalone" : _modOptions.ModKind.Trim().ToLowerInvariant();
		ModKindPicker.SelectedItem = kind == "fmf" ? "fmf" : "standalone";
		ModOptionsNotesEditor.Text = _modOptions.Notes;
		RebuildSettingsUi();

		SetTab(true);
	}

	private void SetTab(bool nativeTab)
	{
		PanelNative.IsVisible = nativeTab;
		PanelOptions.IsVisible = !nativeTab;
		if (nativeTab)
		{
			TabNativeBtn.BackgroundColor = Color.FromArgb("#61F4D8");
			TabNativeBtn.TextColor = Color.FromArgb("#001110");
			TabOptionsBtn.BackgroundColor = Colors.Transparent;
			TabOptionsBtn.TextColor = Color.FromArgb("#61F4D8");
		}
		else
		{
			TabOptionsBtn.BackgroundColor = Color.FromArgb("#61F4D8");
			TabOptionsBtn.TextColor = Color.FromArgb("#001110");
			TabNativeBtn.BackgroundColor = Colors.Transparent;
			TabNativeBtn.TextColor = Color.FromArgb("#61F4D8");
		}
	}

	private void OnTabNative(object? sender, EventArgs e) => SetTab(true);

	private void OnTabOptions(object? sender, EventArgs e) => SetTab(false);

	private void RebuildShopUi()
	{
		ShopItemsStack.Children.Clear();
		_shopUis.Clear();
		foreach (var item in _native.ShopItems)
		{
			var ui = new ShopItemUi(item, RemoveShop);
			_shopUis.Add(ui);
			ShopItemsStack.Children.Add(ui.Root);
		}
	}

	private void RemoveShop(ShopItemUi ui)
	{
		_native.ShopItems.Remove(ui.Model);
		_shopUis.Remove(ui);
		ShopItemsStack.Children.Remove(ui.Root);
	}

	private void OnAddShopItem(object? sender, EventArgs e)
	{
		var item = new NativeShopItem();
		_native.ShopItems.Add(item);
		var ui = new ShopItemUi(item, RemoveShop);
		_shopUis.Add(ui);
		ShopItemsStack.Children.Add(ui.Root);
	}

	private void RebuildStaticUi()
	{
		StaticItemsStack.Children.Clear();
		_staticUis.Clear();
		foreach (var item in _native.StaticItems)
		{
			var ui = new StaticItemUi(item, RemoveStatic);
			_staticUis.Add(ui);
			StaticItemsStack.Children.Add(ui.Root);
		}
	}

	private void RemoveStatic(StaticItemUi ui)
	{
		_native.StaticItems.Remove(ui.Model);
		_staticUis.Remove(ui);
		StaticItemsStack.Children.Remove(ui.Root);
	}

	private void OnAddStaticItem(object? sender, EventArgs e)
	{
		var item = new NativeStaticItem();
		_native.StaticItems.Add(item);
		var ui = new StaticItemUi(item, RemoveStatic);
		_staticUis.Add(ui);
		StaticItemsStack.Children.Add(ui.Root);
	}

	private void RebuildDllUi()
	{
		DllsStack.Children.Clear();
		_dllUis.Clear();
		foreach (var item in _native.Dlls)
		{
			var ui = new DllItemUi(item, RemoveDll);
			_dllUis.Add(ui);
			DllsStack.Children.Add(ui.Root);
		}
	}

	private void RemoveDll(DllItemUi ui)
	{
		_native.Dlls.Remove(ui.Model);
		_dllUis.Remove(ui);
		DllsStack.Children.Remove(ui.Root);
	}

	private void OnAddDll(object? sender, EventArgs e)
	{
		var item = new NativeDllRef();
		_native.Dlls.Add(item);
		var ui = new DllItemUi(item, RemoveDll);
		_dllUis.Add(ui);
		DllsStack.Children.Add(ui.Root);
	}

	private void RebuildSettingsUi()
	{
		SettingsStack.Children.Clear();
		_settingRows.Clear();
		foreach (var kv in _modOptions.Settings)
		{
			var row = new SettingRowUi(kv.Key, kv.Value.GetRawText(), RemoveSetting);
			_settingRows.Add(row);
			SettingsStack.Children.Add(row.Root);
		}
	}

	private void RemoveSetting(SettingRowUi row)
	{
		_settingRows.Remove(row);
		SettingsStack.Children.Remove(row.Root);
	}

	private void OnAddSetting(object? sender, EventArgs e)
	{
		var row = new SettingRowUi("newKey", "\"value\"", RemoveSetting);
		_settingRows.Add(row);
		SettingsStack.Children.Add(row.Root);
	}

	private async void OnSave(object? sender, EventArgs e)
	{
		try
		{
			_native.ModName = ModNameEntry.Text ?? "";
			if (_isDecorationProfile)
			{
				foreach (var ui in _shopUis)
				{
					ui.ApplyToModel();
				}

				foreach (var ui in _staticUis)
				{
					ui.ApplyToModel();
				}

				_native.Dlls.Clear();
			}
			else
			{
				foreach (var ui in _dllUis)
				{
					ui.ApplyToModel();
				}

				_native.ShopItems.Clear();
				_native.StaticItems.Clear();
			}

			NativeModConfigStore.SaveConfig(_projectRoot, _native);

			if (int.TryParse(SchemaVersionEntry.Text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ver))
			{
				_modOptions.SchemaVersion = ver;
			}

			_modOptions.ModKind = ModKindPicker.SelectedItem as string ?? "standalone";
			_modOptions.Notes = ModOptionsNotesEditor.Text ?? "";
			_modOptions.Settings.Clear();
			foreach (var row in _settingRows)
			{
				var key = row.KeyEntry.Text?.Trim() ?? "";
				if (key.Length == 0)
				{
					continue;
				}

				_modOptions.Settings[key] = ParseJsonValue(row.ValueEntry.Text ?? "");
			}

			NativeModConfigStore.SaveModOptions(_projectRoot, _modOptions);
			await DisplayAlert(S.Get("NativeConfig_SavedTitle"), S.Get("NativeConfig_SavedMsg"), S.Get("OK"));
		}
		catch (Exception ex)
		{
			await DisplayAlert(S.Get("Error"), ex.Message, S.Get("OK"));
		}
	}

	private static JsonElement ParseJsonValue(string raw)
	{
		raw = raw.Trim();
		if (raw.Length == 0)
		{
			return JsonSerializer.SerializeToElement("");
		}

		if (bool.TryParse(raw, out var b))
		{
			return JsonSerializer.SerializeToElement(b);
		}

		if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
		{
			return JsonSerializer.SerializeToElement(i);
		}

		if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
		{
			return JsonSerializer.SerializeToElement(d);
		}

		if ((raw.StartsWith('{') && raw.EndsWith('}')) || (raw.StartsWith('[') && raw.EndsWith(']')))
		{
			try
			{
				using var doc = JsonDocument.Parse(raw);
				return doc.RootElement.Clone();
			}
			catch
			{
				// fall through to string
			}
		}

		return JsonSerializer.SerializeToElement(raw);
	}

	private async void OnBack(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

	private static Label H(string text) =>
		new()
		{
			Text = text,
			FontSize = 11,
			TextColor = Color.FromArgb("#7FBFB8"),
			VerticalOptions = LayoutOptions.Center,
			WidthRequest = 140,
		};

	private static Entry E(string text) =>
		new()
		{
			Text = text,
			TextColor = Color.FromArgb("#C0FCF6"),
			BackgroundColor = Color.FromArgb("#001110"),
		};

	private static HorizontalStackLayout Row(View label, View field)
	{
		field.HorizontalOptions = LayoutOptions.Fill;
		var h = new HorizontalStackLayout { Spacing = 8 };
		h.Children.Add(label);
		h.Children.Add(field);
		return h;
	}

	private static double[] ReadVec(Entry x, Entry y, Entry z)
	{
		static double Rd(string? s) =>
			double.TryParse(s?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

		return [Rd(x.Text), Rd(y.Text), Rd(z.Text)];
	}

	private static void WriteVec(double[]? v, Entry x, Entry y, Entry z)
	{
		double vx = 0, vy = 0, vz = 0;
		if (v is { Length: > 0 })
		{
			vx = v[0];
		}

		if (v is { Length: > 1 })
		{
			vy = v[1];
		}

		if (v is { Length: > 2 })
		{
			vz = v[2];
		}

		x.Text = vx.ToString(CultureInfo.InvariantCulture);
		y.Text = vy.ToString(CultureInfo.InvariantCulture);
		z.Text = vz.ToString(CultureInfo.InvariantCulture);
	}

	private sealed class ShopItemUi
	{
		private readonly Action<ShopItemUi> _remove;
		public NativeShopItem Model { get; }
		public Border Root { get; }

		private readonly Entry _itemName, _price, _xp, _sizeU, _mass, _scale;
		private readonly Entry _cx, _cy, _cz, _ox, _oy, _oz;
		private readonly Entry _model, _tex, _icon;
		private readonly Entry _objType;

		public ShopItemUi(NativeShopItem model, Action<ShopItemUi> remove)
		{
			Model = model;
			_remove = remove;

			_itemName = E(model.ItemName);
			_price = E(model.Price.ToString(CultureInfo.InvariantCulture));
			_xp = E(model.XpToUnlock.ToString(CultureInfo.InvariantCulture));
			_sizeU = E(model.SizeInU.ToString(CultureInfo.InvariantCulture));
			_mass = E(model.Mass.ToString(CultureInfo.InvariantCulture));
			_scale = E(model.ModelScale.ToString(CultureInfo.InvariantCulture));
			_cx = new Entry();
			_cy = new Entry();
			_cz = new Entry();
			WriteVec(model.ColliderSize, _cx, _cy, _cz);
			_ox = new Entry();
			_oy = new Entry();
			_oz = new Entry();
			WriteVec(model.ColliderCenter, _ox, _oy, _oz);
			_model = E(model.ModelFile);
			_tex = E(model.TextureFile);
			_icon = E(model.IconFile);
			_objType = E(model.ObjectType.ToString(CultureInfo.InvariantCulture));

			var inner = new VerticalStackLayout { Spacing = 6 };
			foreach (var (lab, ctrl) in new (string, View)[]
			         {
				         ("itemName", _itemName), ("price", _price), ("xpToUnlock", _xp), ("sizeInU", _sizeU),
				         ("mass", _mass), ("modelScale", _scale),
			         })
			{
				inner.Children.Add(Row(H(lab), ctrl));
			}

			inner.Children.Add(H("colliderSize [x,y,z]"));
			inner.Children.Add(Row(H("x"), _cx));
			inner.Children.Add(Row(H("y"), _cy));
			inner.Children.Add(Row(H("z"), _cz));
			inner.Children.Add(H("colliderCenter [x,y,z]"));
			inner.Children.Add(Row(H("x"), _ox));
			inner.Children.Add(Row(H("y"), _oy));
			inner.Children.Add(Row(H("z"), _oz));
			inner.Children.Add(Row(H("modelFile"), _model));
			inner.Children.Add(Row(H("textureFile"), _tex));
			inner.Children.Add(Row(H("iconFile"), _icon));
			inner.Children.Add(Row(H("objectType"), _objType));

			var rm = new Button
			{
				Text = S.Get("NativeConfig_Remove"),
				Style = Application.Current?.Resources["SecondaryButton"] as Style,
			};
			rm.Clicked += (_, _) => _remove(this);

			inner.Children.Add(rm);

			Root = new Border
			{
				Padding = 12,
				BackgroundColor = Color.FromArgb("#001715"),
				StrokeThickness = 0,
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
				Content = inner,
			};
		}

		public void ApplyToModel()
		{
			Model.ItemName = _itemName.Text ?? "";
			Model.Price = int.TryParse(_price.Text, out var p) ? p : 0;
			Model.XpToUnlock = int.TryParse(_xp.Text, out var x) ? x : 0;
			Model.SizeInU = int.TryParse(_sizeU.Text, out var s) ? s : 0;
			Model.Mass = double.TryParse(_mass.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var m)
				? m
				: 0;
			Model.ModelScale = double.TryParse(_scale.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var sc)
				? sc
				: 1;
			Model.ColliderSize = ReadVec(_cx, _cy, _cz);
			Model.ColliderCenter = ReadVec(_ox, _oy, _oz);
			Model.ModelFile = _model.Text ?? "";
			Model.TextureFile = _tex.Text ?? "";
			Model.IconFile = _icon.Text ?? "";
			Model.ObjectType = int.TryParse(_objType.Text, out var o) ? o : 0;
		}
	}

	private sealed class StaticItemUi
	{
		private readonly Action<StaticItemUi> _remove;
		public NativeStaticItem Model { get; }
		public Border Root { get; }

		private readonly Entry _name, _model, _tex, _scale;
		private readonly Entry _csx, _csy, _csz, _cox, _coy, _coz;
		private readonly Entry _px, _py, _pz, _rx, _ry, _rz;

		public StaticItemUi(NativeStaticItem model, Action<StaticItemUi> remove)
		{
			Model = model;
			_remove = remove;
			_name = E(model.ItemName);
			_model = E(model.ModelFile);
			_tex = E(model.TextureFile);
			_scale = E(model.ModelScale.ToString(CultureInfo.InvariantCulture));
			_csx = new Entry();
			_csy = new Entry();
			_csz = new Entry();
			WriteVec(model.ColliderSize, _csx, _csy, _csz);
			_cox = new Entry();
			_coy = new Entry();
			_coz = new Entry();
			WriteVec(model.ColliderCenter, _cox, _coy, _coz);
			_px = new Entry();
			_py = new Entry();
			_pz = new Entry();
			WriteVec(model.Position, _px, _py, _pz);
			_rx = new Entry();
			_ry = new Entry();
			_rz = new Entry();
			WriteVec(model.Rotation, _rx, _ry, _rz);

			var inner = new VerticalStackLayout { Spacing = 6 };
			inner.Children.Add(Row(H("itemName"), _name));
			inner.Children.Add(Row(H("modelFile"), _model));
			inner.Children.Add(Row(H("textureFile"), _tex));
			inner.Children.Add(Row(H("modelScale"), _scale));
			inner.Children.Add(H("colliderSize"));
			inner.Children.Add(Row(H("x"), _csx));
			inner.Children.Add(Row(H("y"), _csy));
			inner.Children.Add(Row(H("z"), _csz));
			inner.Children.Add(H("colliderCenter"));
			inner.Children.Add(Row(H("x"), _cox));
			inner.Children.Add(Row(H("y"), _coy));
			inner.Children.Add(Row(H("z"), _coz));
			inner.Children.Add(H("position"));
			inner.Children.Add(Row(H("x"), _px));
			inner.Children.Add(Row(H("y"), _py));
			inner.Children.Add(Row(H("z"), _pz));
			inner.Children.Add(H("rotation"));
			inner.Children.Add(Row(H("x"), _rx));
			inner.Children.Add(Row(H("y"), _ry));
			inner.Children.Add(Row(H("z"), _rz));

			var rm = new Button
			{
				Text = S.Get("NativeConfig_Remove"),
				Style = Application.Current?.Resources["SecondaryButton"] as Style,
			};
			rm.Clicked += (_, _) => _remove(this);
			inner.Children.Add(rm);

			Root = new Border
			{
				Padding = 12,
				BackgroundColor = Color.FromArgb("#001715"),
				StrokeThickness = 0,
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
				Content = inner,
			};
		}

		public void ApplyToModel()
		{
			Model.ItemName = _name.Text ?? "";
			Model.ModelFile = _model.Text ?? "";
			Model.TextureFile = _tex.Text ?? "";
			Model.ModelScale = double.TryParse(_scale.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var sc)
				? sc
				: 1;
			Model.ColliderSize = ReadVec(_csx, _csy, _csz);
			Model.ColliderCenter = ReadVec(_cox, _coy, _coz);
			Model.Position = ReadVec(_px, _py, _pz);
			Model.Rotation = ReadVec(_rx, _ry, _rz);
		}
	}

	private sealed class DllItemUi
	{
		private readonly Action<DllItemUi> _remove;
		public NativeDllRef Model { get; }
		public Border Root { get; }

		private readonly Entry _file, _entry;

		public DllItemUi(NativeDllRef model, Action<DllItemUi> remove)
		{
			Model = model;
			_remove = remove;
			_file = E(model.FileName);
			_entry = E(model.EntryClass);
			var inner = new VerticalStackLayout { Spacing = 6 };
			inner.Children.Add(Row(H("fileName"), _file));
			inner.Children.Add(Row(H("entryClass"), _entry));
			var rm = new Button
			{
				Text = S.Get("NativeConfig_Remove"),
				Style = Application.Current?.Resources["SecondaryButton"] as Style,
			};
			rm.Clicked += (_, _) => _remove(this);
			inner.Children.Add(rm);
			Root = new Border
			{
				Padding = 12,
				BackgroundColor = Color.FromArgb("#001715"),
				StrokeThickness = 0,
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
				Content = inner,
			};
		}

		public void ApplyToModel()
		{
			Model.FileName = _file.Text ?? "";
			Model.EntryClass = _entry.Text ?? "";
		}
	}

	private sealed class SettingRowUi
	{
		private readonly Action<SettingRowUi> _remove;
		public Entry KeyEntry { get; }
		public Entry ValueEntry { get; }
		public HorizontalStackLayout Root { get; }

		public SettingRowUi(string key, string value, Action<SettingRowUi> remove)
		{
			_remove = remove;
			KeyEntry = E(key);
			ValueEntry = E(value);
			var rm = new Button
			{
				Text = "×",
				WidthRequest = 36,
				Style = Application.Current?.Resources["TertiaryButton"] as Style,
			};
			rm.Clicked += (_, _) => _remove(this);
			KeyEntry.HorizontalOptions = LayoutOptions.Fill;
			ValueEntry.HorizontalOptions = LayoutOptions.Fill;
			Root = new HorizontalStackLayout { Spacing = 8 };
			Root.Children.Add(KeyEntry);
			Root.Children.Add(new Label { Text = "=", VerticalOptions = LayoutOptions.Center, TextColor = Color.FromArgb("#5A9E96") });
			Root.Children.Add(ValueEntry);
			Root.Children.Add(rm);
		}
	}
}
