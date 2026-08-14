using System;
using System.Collections.Generic;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Results;

namespace OrzioClashReport.Launcher.Tests
{
    public sealed class OrderedFileListTests
    {
        [Fact]
        public void TheDeclaredOrderIsPreservedExactly()
        {
            var list = new OrderedFileList();

            // An order no automatic rule would produce: reverse-alphabetical, mixed dates in the name.
            list.AddRange(new[]
            {
                "/snapshots/2026-03-run.json",
                "/snapshots/2026-01-run.json",
                "/snapshots/2026-02-run.json",
            });

            Assert.Equal(
                new[]
                {
                    "/snapshots/2026-03-run.json",
                    "/snapshots/2026-01-run.json",
                    "/snapshots/2026-02-run.json",
                },
                list.Paths);
        }

        [Fact]
        public void AddingNeverReordersWhatIsAlreadyThere()
        {
            var list = new OrderedFileList();
            list.Add("/z.json");
            list.Add("/a.json");
            list.Add("/m.json");

            Assert.Equal(new[] { "/z.json", "/a.json", "/m.json" }, list.Paths);
        }

        [Fact]
        public void DuplicatesAreKeptAndReportedRatherThanSilentlyRemoved()
        {
            var list = new OrderedFileList();
            list.AddRange(new[] { "/a.json", "/b.json", "/a.json" });

            Assert.Equal(new[] { "/a.json", "/b.json", "/a.json" }, list.Paths);

            LauncherWarning warning = Assert.Single(list.Warnings());
            Assert.Equal(LauncherWarningKind.DuplicateOrderedInput, warning.Kind);
            Assert.Contains("posição 3", warning.Message);
            Assert.Contains("posição 1", warning.Message);
        }

        [Fact]
        public void NoWarningWhenEveryEntryIsDistinct()
        {
            var list = new OrderedFileList();
            list.AddRange(new[] { "/a.json", "/b.json", "/c.json" });

            Assert.Empty(list.Warnings());
        }

        [Fact]
        public void MovingIsTheOnlyWayOrderChanges()
        {
            var list = new OrderedFileList();
            list.AddRange(new[] { "/a.json", "/b.json", "/c.json" });

            Assert.True(list.MoveDown(0));
            Assert.Equal(new[] { "/b.json", "/a.json", "/c.json" }, list.Paths);

            Assert.True(list.MoveUp(2));
            Assert.Equal(new[] { "/b.json", "/c.json", "/a.json" }, list.Paths);
        }

        [Fact]
        public void MovingPastEitherEndIsRefusedWithoutChangingAnything()
        {
            var list = new OrderedFileList();
            list.AddRange(new[] { "/a.json", "/b.json" });

            Assert.False(list.MoveUp(0));
            Assert.False(list.MoveDown(1));
            Assert.Equal(new[] { "/a.json", "/b.json" }, list.Paths);
        }

        [Fact]
        public void RemovingLeavesTheRestInTheSameOrder()
        {
            var list = new OrderedFileList();
            list.AddRange(new[] { "/a.json", "/b.json", "/c.json" });

            list.RemoveAt(1);

            Assert.Equal(new[] { "/a.json", "/c.json" }, list.Paths);
        }

        [Fact]
        public void TheExposedListIsACopySoCallersCannotReorderItFromOutside()
        {
            var list = new OrderedFileList();
            list.AddRange(new[] { "/a.json", "/b.json" });

            IReadOnlyList<string> paths = list.Paths;
            Assert.IsNotType<List<string>>(paths);

            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(5));
        }
    }
}
