using GregModmanager.Models;

namespace GregModmanager.Services;

/// <summary>
/// Scans <c>{GameRoot}/FMF/Plugins</c> and <c>{GameRoot}/Mods</c> for locally installed
/// FMF plugins and the framework DLL. This is the "stable" channel — no network access.
/// </summary>
public sealed class StablePluginSource : IFfmPluginChannelSource
{
	private readonly ModDependencyService _deps;

	public StablePluginSource(ModDependencyService deps)
	{
		_deps = deps;
	}

	public string ChannelName => "stable";

	public IReadOnlyList<PluginPackageInfo> ListPlugins()
	{
		var list = new List<PluginPackageInfo>();

		ScanDirectory(list, _deps.FmfPluginsDir, "FFM.Plugin");
		ScanDirectory(list, _deps.ModsDir, "FMF.");

		return list;
	}

	private void ScanDirectory(List<PluginPackageInfo> list, string dir, string prefix)
	{
		if (!Directory.Exists(dir))
		{
			return;
		}

		foreach (var dll in Directory.GetFiles(dir, "*.dll"))
		{
			var name = Path.GetFileNameWithoutExtension(dll);
			if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
			    && !name.Equals("FrikaModdingFramework", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var version = "lokal";
			try
			{
				var fi = new FileInfo(dll);
				version = $"lokal ({fi.LastWriteTime:yyyy-MM-dd})";
			}
			catch
			{
				// ignored
			}

			list.Add(new PluginPackageInfo
			{
				PluginId = name,
				Version = version,
				Channel = ChannelName,
			});
		}
	}
}
