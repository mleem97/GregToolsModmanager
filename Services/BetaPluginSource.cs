using GregModmanager.Models;

namespace GregModmanager.Services;

/// <summary>
/// Stub for a future beta distribution channel served from a custom backend.
/// TODO: Implement when the server API is available. Configure base URL via Preferences.
/// </summary>
public sealed class BetaPluginSource : IgregPluginChannelSource
{
	/// <summary>Preferences key for the beta server base URL.</summary>
	public const string PrefKeyBetaServerUrl = "greg_beta_server_url";

	public string ChannelName => "beta";

	public IReadOnlyList<PluginPackageInfo> ListPlugins()
	{
		var url = Preferences.Default.Get(PrefKeyBetaServerUrl, string.Empty);
		if (string.IsNullOrWhiteSpace(url))
		{
			throw new NotImplementedException(
				"Beta-Kanal: Server-URL ist noch nicht konfiguriert. " +
				"Setze die URL unter Einstellungen (Preferences-Key: greg_beta_server_url).");
		}

		// TODO: HTTP GET {url}/api/plugins → deserialize → return list
		throw new NotImplementedException(
			$"Beta-Kanal: Backend-Anbindung noch nicht implementiert (URL: {url}).");
	}
}
