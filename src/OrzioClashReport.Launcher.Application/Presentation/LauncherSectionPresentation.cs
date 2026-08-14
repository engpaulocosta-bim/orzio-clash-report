using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OrzioClashReport.Launcher.Application.Presentation
{
    /// <summary>
    /// The label and glyph of one navigation section. The glyph never stands alone: the rail keeps the
    /// label as a tooltip and the sidebar shows it as text.
    /// </summary>
    public sealed class LauncherSectionPresentation
    {
        public LauncherSection Section { get; }
        public string Label { get; }
        public string Glyph { get; }
        public string Description { get; }

        private LauncherSectionPresentation(LauncherSection section, string label, string glyph, string description)
        {
            Section = section;
            Label = label;
            Glyph = glyph;
            Description = description;
        }

        /// <summary>The seven sections, in navigation order. This order is the shell's only source of order.</summary>
        public static IReadOnlyList<LauncherSectionPresentation> All { get; } =
            new ReadOnlyCollection<LauncherSectionPresentation>(new[]
            {
                new LauncherSectionPresentation(
                    LauncherSection.Home, "Início", "⌂",
                    "Ações rápidas, estado do motor e últimos relatórios."),
                new LauncherSectionPresentation(
                    LauncherSection.QuickReport, "Relatório rápido", "▤",
                    "Um export XML do Clash Detective para um relatório HTML agrupado."),
                new LauncherSectionPresentation(
                    LauncherSection.Snapshots, "Snapshots", "◫",
                    "Criar snapshots imutáveis e comparar dois snapshots."),
                new LauncherSectionPresentation(
                    LauncherSection.Longitudinal, "Longitudinal", "⇉",
                    "Índice ordenado de runs e comparação de pares adjacentes."),
                new LauncherSectionPresentation(
                    LauncherSection.Projects, "Projetos", "❑",
                    "Catálogo operacional do projeto: criar, acrescentar run e re-renderizar."),
                new LauncherSectionPresentation(
                    LauncherSection.Governance, "Governança", "⚖",
                    "Decisões humanas explícitas de identidade, validação e revisão."),
                new LauncherSectionPresentation(
                    LauncherSection.Settings, "Definições", "⚙",
                    "Tema, dados locais, motor e diagnóstico."),
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
