using System;
using System.Collections.Generic;
using System.Globalization;
using OrzioClashReport.Launcher.Contracts.Settings;

namespace OrzioClashReport.Launcher.Application.Localization
{
    /// <summary>The languages the launcher interface is written in.</summary>
    public enum UiLanguage
    {
        Portuguese = 0,
        English = 1,
    }

    /// <summary>
    /// The one place interface text comes from. Every visible sentence has a stable key and exactly two
    /// translations, both in <see cref="UiStrings"/>; code and views hold keys, never sentences.
    /// </summary>
    /// <remarks>
    /// The language changes visible text only. Nothing the engine receives is translated: command names,
    /// option names and canonical values such as <c>ConfirmSameIdentity</c> are never keys.
    /// <para>
    /// This is deliberately a small ambient service rather than a dependency passed to every view model:
    /// the launcher has no dependency-injection container by design, and an interface language is a
    /// process-wide fact, like the culture. Tests set <see cref="Language"/> explicitly.
    /// </para>
    /// <para>
    /// A missing key returns the key itself, so a gap is visible rather than blank. The test suite fails
    /// on any key used by code or by a view that is not defined with both translations.
    /// </para>
    /// </remarks>
    public static class Loc
    {
        private static UiLanguage _language = UiLanguage.Portuguese;

        /// <summary>
        /// Raised after <see cref="Language"/> actually changed. Handlers run on the thread that changed
        /// it, which is the UI thread in the application.
        /// </summary>
        public static event Action? Changed;

        /// <summary>
        /// The active language. Portuguese until the application applies the stored preference, which
        /// is also the language every existing test was written against.
        /// </summary>
        public static UiLanguage Language
        {
            get => _language;
            set
            {
                if (!Enum.IsDefined(typeof(UiLanguage), value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown interface language.");
                }

                if (_language == value)
                {
                    return;
                }

                _language = value;
                Changed?.Invoke();
            }
        }

        /// <summary>The text for <paramref name="key"/> in the active language.</summary>
        public static string T(string key) => T(key, Language);

        /// <summary>The text for <paramref name="key"/> with <c>{0}</c>-style placeholders filled in.</summary>
        public static string T(string key, params object?[] arguments) =>
            string.Format(CultureInfo.InvariantCulture, T(key, Language), arguments);

        /// <summary>The text for <paramref name="key"/> in an explicit language.</summary>
        public static string T(string key, UiLanguage language) =>
            UiStrings.TryGet(key, language, out string text) ? text : key;

        /// <summary>True when the key has both translations.</summary>
        public static bool IsDefined(string key) => UiStrings.Contains(key);

        /// <summary>Every defined key, in a stable order, for the completeness tests.</summary>
        public static IReadOnlyCollection<string> AllKeys => UiStrings.Keys;

        /// <summary>
        /// Turns a stored preference into a concrete language. <see cref="LauncherLanguagePreference.System"/>
        /// follows the operating system: Portuguese when its language is Portuguese, English otherwise.
        /// </summary>
        public static UiLanguage Resolve(LauncherLanguagePreference preference, CultureInfo systemUiCulture)
        {
            if (systemUiCulture == null)
            {
                throw new ArgumentNullException(nameof(systemUiCulture));
            }

            switch (preference)
            {
                case LauncherLanguagePreference.Portuguese:
                    return UiLanguage.Portuguese;

                case LauncherLanguagePreference.English:
                    return UiLanguage.English;

                case LauncherLanguagePreference.System:
                    return string.Equals(
                        systemUiCulture.TwoLetterISOLanguageName, "pt", StringComparison.OrdinalIgnoreCase)
                        ? UiLanguage.Portuguese
                        : UiLanguage.English;

                default:
                    throw new ArgumentOutOfRangeException(nameof(preference), preference, "Unknown language preference.");
            }
        }
    }
}
