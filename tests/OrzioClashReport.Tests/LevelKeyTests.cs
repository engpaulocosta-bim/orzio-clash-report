using OrzioClashReport.Core.Grouping;

namespace OrzioClashReport.Tests
{
    public class LevelKeyTests
    {
        [Fact]
        public void Combine_ReturnsSharedLevel_WhenBothSidesMatch()
        {
            Assert.Equal("L1", LevelKey.Combine("L1", "L1"));
        }

        [Fact]
        public void Combine_ReturnsLevelA_WhenOnlyLevelAIsPresent()
        {
            Assert.Equal("L1", LevelKey.Combine("L1", null));
        }

        [Fact]
        public void Combine_ReturnsLevelB_WhenOnlyLevelBIsPresent()
        {
            Assert.Equal("L2", LevelKey.Combine(null, "L2"));
        }

        [Fact]
        public void Combine_ReturnsNull_WhenNeitherSideIsPresent()
        {
            Assert.Null(LevelKey.Combine(null, null));
        }

        [Fact]
        public void Combine_CombinesDifferentLevels_IndependentOfArgumentOrder()
        {
            string combinedForward = LevelKey.Combine("L1", "L2")!;
            string combinedBackward = LevelKey.Combine("L2", "L1")!;

            Assert.Equal(combinedForward, combinedBackward);
            Assert.Equal("L1 × L2", combinedForward);
        }
    }
}
