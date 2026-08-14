using System;
using System.Collections.Generic;
using System.Linq;
using OrzioClashReport.Launcher.Application.Presentation;
using OrzioClashReport.Launcher.Contracts.Engine;

namespace OrzioClashReport.Launcher.Tests
{
    public sealed class LauncherPresentationTests
    {
        [Fact]
        public void ThereAreExactlySevenSectionsInAFixedOrder()
        {
            Assert.Equal(
                new[]
                {
                    LauncherSection.Home,
                    LauncherSection.QuickReport,
                    LauncherSection.Snapshots,
                    LauncherSection.Longitudinal,
                    LauncherSection.Projects,
                    LauncherSection.Governance,
                    LauncherSection.Settings,
                },
                LauncherSectionPresentation.All.Select(section => section.Section));
        }

        [Fact]
        public void EverySectionHasALabelAGlyphAndADescription()
        {
            foreach (LauncherSectionPresentation presentation in LauncherSectionPresentation.All)
            {
                Assert.NotEmpty(presentation.Label);
                Assert.NotEmpty(presentation.Glyph);
                Assert.NotEmpty(presentation.Description);
            }
        }

        [Fact]
        public void EveryEngineStateHasItsOwnGlyphAndItsOwnLabel()
        {
            var glyphs = new List<string>();
            var labels = new List<string>();

            foreach (EngineStatusKind status in Enum.GetValues<EngineStatusKind>())
            {
                EngineStatusPresentation presentation = EngineStatusPresentation.For(status);

                Assert.NotEmpty(presentation.Glyph);
                Assert.NotEmpty(presentation.Label);
                Assert.NotEmpty(presentation.Explanation);

                glyphs.Add(presentation.Glyph);
                labels.Add(presentation.Label);
            }

            // Colour is never the differentiator: four states share only three severities, so the
            // glyph and the label are what actually distinguish them.
            Assert.Equal(glyphs.Count, glyphs.Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(labels.Count, labels.Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public void OnlyAReadyEngineIsPresentedAsPositive()
        {
            foreach (EngineStatusKind status in Enum.GetValues<EngineStatusKind>())
            {
                EngineStatusPresentation presentation = EngineStatusPresentation.For(status);

                if (status == EngineStatusKind.Ready)
                {
                    Assert.Equal(LauncherSeverity.Positive, presentation.Severity);
                }
                else
                {
                    Assert.NotEqual(LauncherSeverity.Positive, presentation.Severity);
                }
            }
        }
    }
}
