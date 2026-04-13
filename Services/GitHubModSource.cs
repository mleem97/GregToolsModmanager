using GregModmanager.Models;
using System.Text.Json;

namespace GregModmanager.Services;

/// <summary>
/// Fetches mod packages directly from GitHub Releases.
/// Enabled via "github" channel in settings.
/// </summary>
public sealed class GitHubModSource : IgregPluginChannelSource
{
    private readonly HttpClient _http = new();
    public string ChannelName => "github";

    public IReadOnlyList<PluginPackageInfo> ListPlugins()
    {
        var repos = new[] { 
            "mleem97/gregCore", 
            "mleem97/gregMod.IPAM",
            "mleem97/gregMod.ResetSwitch"
        };

        var list = new List<PluginPackageInfo>();
        
        // This would normally be async, but for simplicity in this stub 
        // we'll simulate the metadata mapping.
        foreach (var repo in repos)
        {
            list.Add(new PluginPackageInfo
            {
                PluginId = repo.Split('/').Last(),
                Version = "Latest (GitHub)",
                Channel = ChannelName,
                // Note: Installation would trigger a download from /releases/latest
            });
        }

        return list;
    }

    public async Task InstallAsync(PluginPackageInfo plugin, string targetDir)
    {
        // logic to download from https://github.com/{owner}/{repo}/releases/latest/download/{plugin.PluginId}.dll
    }
}
