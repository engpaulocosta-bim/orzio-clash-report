using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Platform;
using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Desktop.ViewModels;

namespace OrzioClashReport.Launcher.Desktop.Tests;

public sealed class HomeViewModelTests
{
    [Fact]
    public async Task UnavailableOutputShowsFeedbackAndRemainsInHistory()
    {
        HomeViewModel home = Create(new HistoryStore());
        await home.RefreshRecentOutputsAsync(CancellationToken.None);

        await home.OpenRecentCommand.ExecuteAsync(home.RecentOutputs[0]);

        Assert.True(home.HasRecentError);
        Assert.Single(home.RecentOutputs);
    }

    [Fact]
    public async Task HistoryReadFailureIsReportedWithoutDiscardingLoadedHistory()
    {
        var store = new HistoryStore();
        HomeViewModel home = Create(store);
        await home.RefreshRecentOutputsAsync(CancellationToken.None);
        store.Load = _ => throw new IOException("Unavailable history");

        Exception? error = await Record.ExceptionAsync(() => home.RefreshRecentOutputsAsync(CancellationToken.None));

        Assert.Null(error);
        Assert.Single(home.RecentOutputs);
        Assert.True(home.HasRecentError);
        Assert.False(home.IsLoadingRecentOutputs);
        Assert.False(home.IsRecentEmpty);
    }

    [Fact]
    public async Task PendingLoadIsDistinctFromEmptyAndClearsAfterCompletion()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<RecentOutputItem>>();
        var store = new HistoryStore { Load = _ => pending.Task };
        HomeViewModel home = Create(store);
        Task refresh = home.RefreshRecentOutputsAsync(CancellationToken.None);
        Assert.True(home.IsLoadingRecentOutputs);
        Assert.False(home.IsRecentEmpty);

        pending.SetResult(Array.Empty<RecentOutputItem>());
        await refresh;

        Assert.False(home.IsLoadingRecentOutputs);
        Assert.True(home.IsRecentEmpty);
        Assert.False(home.HasRecentError);
    }

    [Fact]
    public async Task CancellationDoesNotBecomeAnEmptyHistory()
    {
        var store = new HistoryStore();
        HomeViewModel home = Create(store);
        await home.RefreshRecentOutputsAsync(CancellationToken.None);
        store.Load = _ => throw new OperationCanceledException();

        await Assert.ThrowsAsync<OperationCanceledException>(() => home.RefreshRecentOutputsAsync(CancellationToken.None));
        Assert.Single(home.RecentOutputs);
    }

    internal static HomeViewModel Create(IRecentItemsStore store, IOutputRevealer? revealer = null) => new(
        new EngineStatusViewModel(), store, revealer ?? new NullOutputRevealer(), _ => { },
        new UpdatesViewModel(new FakeDesktopUpdateClient(), new ActiveJobTracker(), new FixedClock(),
            "0.2.2-launcher-preview.1", _ => { }, () => { }));

    internal sealed class HistoryStore : IRecentItemsStore
    {
        public Func<CancellationToken, Task<IReadOnlyList<RecentOutputItem>>> Load { get; set; } = _ =>
            Task.FromResult<IReadOnlyList<RecentOutputItem>>(new[] { new RecentOutputItem(
                Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".html"), "Report",
                OrzioClashReport.Launcher.Contracts.Operations.LauncherOperationKind.QuickReport,
                OrzioClashReport.Launcher.Contracts.Results.LauncherArtifactKind.HtmlReport,
                DateTimeOffset.Parse("2026-01-02T12:30:00Z")) });

        public Task<IReadOnlyList<RecentOutputItem>> LoadAsync(CancellationToken token) => Load(token);
        public Task<IReadOnlyList<RecentOutputItem>> AddAsync(RecentOutputItem item, CancellationToken token) => Load(token);
        public Task ClearAsync(CancellationToken token) => Task.CompletedTask;
    }
}
