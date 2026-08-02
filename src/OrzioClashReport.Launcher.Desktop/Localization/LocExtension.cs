using System;
using Avalonia.Data;

namespace OrzioClashReport.Launcher.Desktop.Localization
{
    /// <summary>
    /// Binds a control to one piece of interface text: <c>Text="{l:Loc Home.StartCaption}"</c>.
    /// The binding re-reads when the language changes, so switching language updates the window
    /// without reopening it.
    /// </summary>
    public sealed class LocExtension : Avalonia.Markup.Xaml.MarkupExtension
    {
        public LocExtension()
        {
            Key = string.Empty;
        }

        public LocExtension(string key)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public string Key { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(Key))
            {
                throw new InvalidOperationException("A localized binding needs a key.");
            }

            // Fails fast at load time rather than showing an empty label at run time.
            if (!UiStrings.All.ContainsKey(Key))
            {
                throw new InvalidOperationException($"No interface text is defined for '{Key}'.");
            }

            return new Binding
            {
                Source = Localizer.Instance,
                Path = $"[{Key}]",
                Mode = BindingMode.OneWay,
            };
        }
    }
}
