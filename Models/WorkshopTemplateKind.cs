namespace GregModmanager.Models;

/// <summary>Scaffold layout under <c>content/</c> for a new Workshop project.</summary>
public enum WorkshopTemplateKind
{
	/// <summary>Game Object / Decoration style assets (vanilla Workshop delivery).</summary>
	VanillaObjectDecoration,

	/// <summary>MelonLoader mods — <c>content/Mods</c> mirrors <c>{GameRoot}/Mods</c> when linked from Workshop delivery.</summary>
	ModdedMelonLoader,

	/// <summary>gregCoreModFramework — <c>content/ModFramework/greg/Plugins</c> mirrors <c>{GameRoot}/greg/Plugins</c>.</summary>
	ModdedgregCoreModFramework,
}

