**Verdict: no critical or high issues remain in these changes.** This review is static only. I did not build or run tests, so the parameterized tests are unexecuted.

The `InvalidDataException` handling now covers the structural failures:

- **`HomeViewModel.RefreshRecentOutputsAsync`** (`HomeViewModel.cs:119-120`) catches `InvalidDataException` alongside the other I/O and JSON exceptions. It sets `home.recent.loadFailed` and leaves the list as it was. Startup continues because `ShellViewModel.cs:140` no longer sees an exception, and the fire-and-forget call at `ShellViewModel.cs:228` is also covered.
- **`LauncherOperationExecutor.ExecuteAsync`** (`LauncherOperationExecutor.cs:105-106`) catches `InvalidDataException` around `RecordArtifactsAsync`. A successful engine result is still returned, and the `recent.writeFailed` warning is logged.
- **`JsonRecentItemsStore`** throws `InvalidDataException` for an unsupported schema (`:92`) and for an invalid item (`:99`). Both throws happen inside `Load()`, which runs before `Save()` in `AddAsync`, so the file is not overwritten. Only `ClearAsync` replaces unreadable history, and that is a user action in Definições.
- **Constructor guard:** `RecentOutputItem`'s constructor cannot throw for loader-produced input. `TryReadString` rejects blank path and display name, and `TryReadItem` checks the enum values first, so no unhandled `ArgumentException` or `ArgumentOutOfRangeException` can escape.
- **Other callers:** the only load and add callers are `HomeViewModel:110`, `LauncherOperationExecutor:237` and the `ClearAsync` path. No other uncaught path was found.

The tests match the claim in the response:

- `RealCorruptHistoryShowsTheErrorStateAndPreservesTheFile` (`HomeViewModelTests.cs:15-31`) covers syntax error, unsupported schema (`schemaVersion: 2`) and an invalid item (`[{}]`). Each case checks the error state and that the file is byte-identical.
- `CorruptHistoryDoesNotEraseHistoryOrFailAnOtherwiseSuccessfulReport` (`QuickReportEndToEndTests.cs:65-80`) uses the same three inputs. It checks a succeeded result, that the HTML exists, that history is preserved, the `recent.writeFailed` log entry, and that no journal entry remains.

One minor gap, not a blocker: neither test asserts the specific `home.recent.loadFailed` key. They assert `HasRecentError` only. The test suite would not catch a regression to a different error key.
