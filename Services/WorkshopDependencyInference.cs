using WorkshopUploader.Localization;

namespace WorkshopUploader.Services;

/// <summary>
/// Infers MelonLoader / FrikaModFramework requirements from Steam tags and item description
/// (Workshop items do not expose uploader metadata.json).
/// </summary>
public static class WorkshopDependencyInference
{
	public readonly record struct DependencyHints(string CompactLine, string BulletBlock, bool HasAny);

	public static DependencyHints Infer(IReadOnlyList<string>? tags, string? description)
	{
		var tagSet = new HashSet<string>(
			(tags ?? Array.Empty<string>()).Select(t => t.Trim().ToLowerInvariant()),
			StringComparer.OrdinalIgnoreCase);
		var desc = description ?? "";

		var fmf = InferNeedsFmf(tagSet, desc);
		var melon = !fmf && InferNeedsMelonLoaderOnly(tagSet, desc);

		var parts = new List<string>();
		if (fmf)
		{
			parts.Add(S.Get("Mod_Dep_MelonLoader"));
			parts.Add(S.Get("Mod_Dep_Fmf"));
		}
		else if (melon)
		{
			parts.Add(S.Get("Mod_Dep_MelonLoader"));
		}

		if (parts.Count == 0)
		{
			return new DependencyHints("", "", false);
		}

		var joined = string.Join(" · ", parts);
		var compact = S.Format("Mod_StoreDepsLine", joined);
		var block = string.Join("\n", parts.Select(p => "• " + p));
		return new DependencyHints(compact, block, true);
	}

	private static bool InferNeedsFmf(HashSet<string> tagSet, string desc)
	{
		if (tagSet.Contains("fmf") || tagSet.Contains("frika-mod-framework"))
		{
			return true;
		}

		if (desc.Contains("FrikaModFramework", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return false;
	}

	private static bool InferNeedsMelonLoaderOnly(HashSet<string> tagSet, string desc)
	{
		if (tagSet.Contains("melonloader"))
		{
			return true;
		}

		if (tagSet.Contains("modded"))
		{
			return true;
		}

		if (desc.Contains("MelonLoader", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return false;
	}
}
