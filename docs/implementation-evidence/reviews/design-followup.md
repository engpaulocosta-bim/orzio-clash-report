## Verdict: both previous high findings are fixed, but the history fix introduced one high regression

I read the code only. I did not run tests or build, and I have no installed-app QA.

**1. Corrupt history overwritten: fixed for syntactically broken JSON, not for structurally invalid history.**
- `JsonRecentItemsStore.AddAsync` calls `Load()` before `Save()` (`JsonRecentItemsStore.cs:50`, `:65`). A throw from `Load` therefore stops the write, and the file is preserved.
- `ClearAsync` overwrites without loading (`:69-73`). That is the only path that replaces unreadable history, and it is a user action in Definições, so I consider it acceptable.
- `schemaVersion` is still `1` (`:23`).

**2. `Win32Exception` leaves updater busy: resolved for that exception.**
- `UpdatesViewModel.IsExpectedFailure` now includes `Win32Exception` (`UpdatesViewModel.cs:258`). `Fail()` moves the state to `Failed`, so `IsBusy` clears.
- `HomeViewModel.OpenRecentAsync` also catches it (`HomeViewModel.cs:97`).
- The residual pattern is not fixed: any exception outside the filter, such as plain `NotSupportedException` or `OperationCanceledException`, still leaves `State` stuck in `Downloading` or `Checking`. This is pre-existing and not a regression.

**Regression (high): structurally invalid history now escapes two handlers.**
`JsonRecentItemsStore.Load` throws `InvalidDataException` for an unsupported schema or an invalid item (`JsonRecentItemsStore.cs:92`, `:99`). `InvalidDataException` derives from `SystemException`, not `IOException`, so neither handler below catches it.

- **Executor loses a successful result.** `LauncherOperationExecutor.cs:105-106` filters only `IOException`, `UnauthorizedAccessException`, `JsonException` and `SecurityException`. The `InvalidDataException` from `AddAsync` propagates out of `ExecuteAsync`. The engine succeeded, but the caller gets an exception instead of `EngineJobResult`. This contradicts the response's claim that the executor "retains successful engine results." It only holds for syntactically broken JSON, because `JsonDocument.Parse` throws `JsonException`.
  - Fix: add `or InvalidDataException` to the filter at `LauncherOperationExecutor.cs:105-106`.
- **Startup aborts and the error is invisible.** `HomeViewModel.RefreshRecentOutputsAsync` catches `JsonException`, `IOException`, `UnauthorizedAccessException` and `SecurityException` (`HomeViewModel.cs:119-120`), but not `InvalidDataException`. `ShellViewModel.InitializeAsync` awaits it first (`ShellViewModel.cs:140`), so `Settings.LoadAsync`, `LoadInterruptedOperationsAsync` and `RefreshEngineAsync` never run. `App.axaml.cs:171` discards the task, so the exception is unobserved. The Home list shows no `home.recent.loadFailed` message, because that key is set only in the catch block. The same unobserved path applies to `ShellViewModel.cs:228`.
  - Fix: add `or InvalidDataException` to the filter at `HomeViewModel.cs:119-120`.

**Test coverage gap:** `CorruptHistoryDoesNotEraseHistoryOrFailAnOtherwiseSuccessfulReport` (`QuickReportEndToEndTests.cs:65`) and `RealCorruptHistoryShowsTheErrorStateAndPreservesTheFile` (`HomeViewModelTests.cs:15`) both use `"{ corrupt history"`, which is syntactically invalid. Neither exercises the `InvalidDataException` path. `LocalStoreTests.cs:130-138` covers `schemaVersion: 2` only at the store level, not through the executor or Home. A test for `{"schemaVersion":2,"items":[]}` through the executor and Home would be the missing check. I have not run it.

No new data-loss path was found. `RecentOutputItem` rejects blank paths and display names (`RecentOutputItem.cs:26-34`), so the writer cannot produce what the loader rejects.
