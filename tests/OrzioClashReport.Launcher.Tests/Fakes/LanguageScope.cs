using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Desktop.Localization;

namespace OrzioClashReport.Launcher.Tests;

/// <summary>
/// Switches the interface language for the duration of an assertion and puts it back afterwards,
/// so a test can check both languages without leaking a setting into the next test.
/// </summary>
internal sealed class LanguageScope : IDisposable
{
    private readonly InterfaceLanguage _previous;

    private LanguageScope(InterfaceLanguage language)
    {
        _previous = Localizer.Instance.Language;
        Localizer.Instance.Language = language;
    }

    internal static LanguageScope Use(InterfaceLanguage language) => new(language);

    public void Dispose() => Localizer.Instance.Language = _previous;
}
