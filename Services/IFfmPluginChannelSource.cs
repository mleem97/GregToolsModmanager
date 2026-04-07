using WorkshopUploader.Models;

namespace WorkshopUploader.Services;

/// <summary>
/// Provides a list of available FMF plugin packages from a specific distribution channel.
/// Implementations: <see cref="StablePluginSource"/> (local scan) and <see cref="BetaPluginSource"/> (server, TODO).
/// </summary>
public interface IFfmPluginChannelSource
{
	string ChannelName { get; }
	IReadOnlyList<PluginPackageInfo> ListPlugins();
}
