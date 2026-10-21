using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OrzioClashReport.Launcher.Application.Localization;

namespace OrzioClashReport.Launcher.Application.Presentation
{
    /// <summary>
    /// The label and glyph of one navigation section. The glyph never stands alone: the rail keeps the
    /// label as a tooltip and the sidebar shows it as text.
    /// </summary>
    /// <remarks>
    /// <see cref="Label"/> and <see cref="Description"/> are looked up each time they are read, so they
    /// follow the interface language when it changes. The glyph is language-independent.
    /// </remarks>
    public sealed class LauncherSectionPresentation
    {
        private readonly string _labelKey;
        private readonly string _descriptionKey;

        public LauncherSection Section { get; }
        public string Glyph { get; }

        public string Label => Loc.T(_labelKey);
        public string Description => Loc.T(_descriptionKey);

        private LauncherSectionPresentation(LauncherSection section, string keyPrefix, string glyph)
        {
            Section = section;
            Glyph = glyph;
            _labelKey = keyPrefix + ".label";
            _descriptionKey = keyPrefix + ".description";
        }

        /// <summary>The seven sections, in navigation order. This order is the shell's only source of order.</summary>
        public static IReadOnlyList<LauncherSectionPresentation> All { get; } =
            new ReadOnlyCollection<LauncherSectionPresentation>(new[]
            {
                new LauncherSectionPresentation(LauncherSection.Home, "nav.home", "⌂"),
                new LauncherSectionPresentation(LauncherSection.QuickReport, "nav.quickreport", "▤"),
                new LauncherSectionPresentation(LauncherSection.Snapshots, "nav.snapshots", "◫"),
                new LauncherSectionPresentation(LauncherSection.Longitudinal, "nav.longitudinal", "⇉"),
                new LauncherSectionPresentation(LauncherSection.Projects, "nav.projects", "❑"),
                new LauncherSectionPresentation(LauncherSection.Governance, "nav.governance", "⚖"),
                new LauncherSectionPresentation(LauncherSection.Settings, "nav.settings", "⚙"),
            });

        public static LauncherSectionPresentation For(LauncherSection section)
        {
            foreach (LauncherSectionPresentation presentation in All)
            {
                if (presentation.Section == section)
                {
                    return presentation;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(section), section, "Unknown launcher section.");
        }
    }
}
