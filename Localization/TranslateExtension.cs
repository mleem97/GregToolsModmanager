namespace GregModmanager.Localization;

[AcceptEmptyServiceProvider]
[ContentProperty(nameof(Key))]
public sealed class TranslateExtension : IMarkupExtension<string>
{
	public string Key { get; set; } = "";

	public string ProvideValue(IServiceProvider serviceProvider)
		=> S.Get(Key);

	object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
		=> ProvideValue(serviceProvider);
}
