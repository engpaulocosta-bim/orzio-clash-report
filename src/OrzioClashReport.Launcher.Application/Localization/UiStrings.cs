using System;
using System.Collections.Generic;
using System.Linq;

namespace OrzioClashReport.Launcher.Application.Localization
{
    /// <summary>
    /// The translation table. One entry per key, each carrying the Portuguese and the English text side by
    /// side so a missing translation is impossible to add by accident. The entries live in the partial
    /// files next to this one, grouped by area.
    /// </summary>
    /// <remarks>
    /// The Portuguese is European Portuguese, as the application has always been written. Placeholders
    /// are positional (<c>{0}</c>) and must appear identically in both languages; a test enforces it.
    /// What is never in this table: canonical engine values, command names and option names.
    /// </remarks>
    internal static partial class UiStrings
    {
        private static readonly IReadOnlyDictionary<string, (string Portuguese, string English)> Table = Build();

        internal static IReadOnlyCollection<string> Keys =>
            Table.Keys.OrderBy(key => key, StringComparer.Ordinal).ToList();

        internal static bool Contains(string key) => Table.ContainsKey(key);

        internal static bool TryGet(string key, UiLanguage language, out string text)
        {
            if (key != null && Table.TryGetValue(key, out (string Portuguese, string English) entry))
            {
                text = language == UiLanguage.English ? entry.English : entry.Portuguese;
                return true;
            }

            text = string.Empty;
            return false;
        }

        private static IReadOnlyDictionary<string, (string Portuguese, string English)> Build()
        {
            var table = new Dictionary<string, (string Portuguese, string English)>(StringComparer.Ordinal);

            AddNavigationAndShell(table);
            AddHomeAndQuickReport(table);
            AddSettingsAndUpdates(table);
            AddJobAndCommon(table);
            AddOperationForms(table);
            AddMessages(table);

            return table;
        }

        private static void Add(
            Dictionary<string, (string Portuguese, string English)> table,
            string key,
            string portuguese,
            string english)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("A UI string key cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(portuguese) || string.IsNullOrWhiteSpace(english))
            {
                throw new InvalidOperationException($"UI string '{key}' needs both translations.");
            }

            if (!table.TryAdd(key, (portuguese, english)))
            {
                throw new InvalidOperationException($"Duplicate UI string key '{key}'.");
            }
        }
    }
}
