using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Infrastructure.Storage;

namespace OrzioClashReport.Launcher.Tests;

public sealed class LocalStoreTests : IDisposable
{
    private readonly string _root;

    public LocalStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "orzio-store-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string SettingsPath => Path.Combine(_root, "settings.json");

    private string RecentPath => Path.Combine(_root, "recent-items.json");

    [Fact]
    public void MissingSettings_YieldTheDefaults()
    {
        LauncherSettings settings = new JsonSettingsStore(SettingsPath).Load();

        Assert.Equal(ThemePreference.System, settings.Theme);
        Assert.True(settings.ShowExperimentalWarnings);
        Assert.Null(settings.LastOutputDirectory);
    }

    [Fact]
    public void SettingsRoundTrip()
    {
        var store = new JsonSettingsStore(SettingsPath);

        store.Save(LauncherSettings.Default
            .WithTheme(ThemePreference.Dark)
            .WithShowExperimentalWarnings(false)
            .WithLastOutputDirectory(_root));

        LauncherSettings reloaded = new JsonSettingsStore(SettingsPath).Load();

        Assert.Equal(ThemePreference.Dark, reloaded.Theme);
        Assert.False(reloaded.ShowExperimentalWarnings);
        Assert.Equal(_root, reloaded.LastOutputDirectory);
    }

    [Fact]
    public void DamagedSettings_NeverStopTheApplicationFromOpening()
    {
        File.WriteAllText(SettingsPath, "{ not json");

        LauncherSettings settings = new JsonSettingsStore(SettingsPath).Load();

        Assert.Equal(ThemePreference.System, settings.Theme);
    }

    [Fact]
    public void SettingsAreWrittenWithoutAByteOrderMark()
    {
        new JsonSettingsStore(SettingsPath).Save(LauncherSettings.Default);

        byte[] bytes = File.ReadAllBytes(SettingsPath);

        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
    }

    [Fact]
    public void RecentItems_KeepTheNewestFirstAndDeduplicateByPath()
    {
        var store = new JsonRecentItemsStore(RecentPath);
        string first = Path.Combine(_root, "a.html");
        string second = Path.Combine(_root, "b.html");

        store.Add(new RecentOutputItem("a.html", first, DateTimeOffset.UnixEpoch));
        store.Add(new RecentOutputItem("b.html", second, DateTimeOffset.UnixEpoch.AddMinutes(1)));
        store.Add(new RecentOutputItem("a.html", first, DateTimeOffset.UnixEpoch.AddMinutes(2)));

        IReadOnlyList<RecentOutputItem> items = store.Load();

        Assert.Equal(2, items.Count);
        Assert.Equal(first, items[0].FullPath);
        Assert.Equal(second, items[1].FullPath);
    }

    [Fact]
    public void RecentItems_AreCapped()
    {
        var store = new JsonRecentItemsStore(RecentPath);

        for (int i = 0; i < JsonRecentItemsStore.MaximumItems + 7; i++)
        {
            store.Add(new RecentOutputItem(
                $"r{i}.html", Path.Combine(_root, $"r{i}.html"), DateTimeOffset.UnixEpoch.AddMinutes(i)));
        }

        Assert.Equal(JsonRecentItemsStore.MaximumItems, store.Load().Count);
    }

    [Fact]
    public void DamagedRecentItems_YieldAnEmptyList()
    {
        File.WriteAllText(RecentPath, "[[[");

        Assert.Empty(new JsonRecentItemsStore(RecentPath).Load());
    }

    [Fact]
    public void WritingLeavesNoTemporaryFileBehind()
    {
        new JsonSettingsStore(SettingsPath).Save(LauncherSettings.Default);
        new JsonRecentItemsStore(RecentPath).Add(
            new RecentOutputItem("a.html", Path.Combine(_root, "a.html"), DateTimeOffset.UnixEpoch));

        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
    }
}
