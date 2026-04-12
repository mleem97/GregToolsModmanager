namespace GregModmanager.Services;

/// <summary>
/// Central registry for plugin distribution channels (stable, beta, …).
/// Injected as singleton; channels are registered at startup.
/// </summary>
public sealed class FfmPluginChannelRegistry
{
	private readonly Dictionary<string, IFfmPluginChannelSource> _sources = new(StringComparer.OrdinalIgnoreCase);

	public void Register(IFfmPluginChannelSource source)
	{
		_sources[source.ChannelName] = source;
	}

	public IFfmPluginChannelSource? GetSource(string channelName)
	{
		_sources.TryGetValue(channelName, out var source);
		return source;
	}

	public IReadOnlyList<string> AvailableChannels => _sources.Keys.ToList();
}
