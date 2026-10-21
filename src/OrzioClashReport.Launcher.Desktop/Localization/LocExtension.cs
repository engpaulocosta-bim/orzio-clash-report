using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace OrzioClashReport.Launcher.Desktop.Localization
{
    /// <summary>
    /// <c>{loc:Loc some.key}</c>: fixed interface text in XAML, looked up by key and kept in step with the
    /// interface language. A view never carries a sentence; it carries a key, and a test fails if a key does
    /// not exist or if a view holds literal text.
    /// </summary>
    public sealed class LocExtension : MarkupExtension
    {
        public LocExtension()
        {
            Key = string.Empty;
        }

        public LocExtension(string key)
        {
            Key = key;
        }

        public string Key { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // A reflection binding on purpose: its source is the key's own LocalizedText, which has no
            // relation to the surrounding x:DataType, so it must not be compiled against it.
            var binding = new ReflectionBindingExtension(nameof(LocalizedText.Text))
            {
                Source = LocalizedText.For(Key),
                Mode = BindingMode.OneWay,
            };

            return binding.ProvideValue(serviceProvider);
        }
    }
}
