## Findings: `DesktopUpdateService.DownloadAndStartInstallerAsync`

**1. Blocking defect: every verified download fails at the rename.** Not reproduced: I did not run it. The mechanism follows from the code and documented Windows behavior.

- **Mechanism:** Line 223 declares `await using FileStream verification = File.OpenRead(temporary);`. `File.OpenRead` uses `FileShare.Read`, which does not grant `FILE_SHARE_DELETE`. A C# `await using` declaration is disposed at the end of its enclosing block. That block is the `try` starting at line 187, which closes at line 238. So the handle is still open at `File.Move` on line 231. On NTFS, renaming a file that another handle has open without `FILE_SHARE_DELETE` fails with a sharing violation. `File.Move` throws `IOException`.
- **Consequence:** Line 232 (`Process.Start`) is never reached, so the installer never starts. The exception unwinds the `try`, the handle closes, and the `finally` at lines 239–245 deletes the `.download`. The caller gets an `IOException` on every valid update.
- **Fix:** Scope the verification stream to its own block so it is disposed before the move:
  ```csharp
  string actual;
  await using (FileStream verification = File.OpenRead(temporary))
  {
      actual = Convert.ToHexString(await SHA256.HashDataAsync(verification, cancellationToken).ConfigureAwait(false));
  }
  ```
  Keep the mismatch check and the `File.Move` after the block. Opening with `FileShare.Read | FileShare.Delete` would also work, but scoping is clearer.

**2. Minimal deterministic test that never executes an installer.** The current code cannot run this safely as written. `Environment.GetFolderPath(LocalApplicationData)` (line 178) cannot be redirected, and `Process.Start` (line 232) is unconditional. A test that expects success would launch the payload if the bug were fixed. The test needs two small `internal` seams: an updates directory and a `Func<string, bool>` installer starter. The existing `internal` `HttpClient` constructor (line 34) already covers HTTP.

```csharp
[Fact]
public async Task Verified_download_is_moved_and_handed_to_starter_without_running_it()
{
    byte[] payload = Encoding.ASCII.GetBytes("not an executable");
    string sha = Convert.ToHexString(SHA256.HashData(payload));
    string dir = Directory.CreateTempSubdirectory().FullName;
    var started = new List<string>();
    var service = new DesktopUpdateService(new HttpClient(new StubHandler(payload)), dir, p => { started.Add(p); return true; });
    var manifest = new DesktopUpdateManifest("0.2.0",
        "https://github.com/engpaulocosta-bim/orzio-clash-report/releases/download/desktop-v0.2.0/orzio-clash-report-desktop-v0.2.0-win-x64-setup.exe",
        sha, null);

    string installer = await service.DownloadAndStartInstallerAsync(manifest);

    Assert.Equal(payload, File.ReadAllBytes(installer));
    Assert.Equal(new[] { installer }, started);
    Assert.Empty(Directory.GetFiles(dir, "*.download"));
}
```

`StubHandler` returns a fixed 200 response with `payload`. The starter only records the path, so the test never launches a process. Against the current code, this test would fail with `IOException` at line 231 before the starter is called, which confirms the defect without executing anything.

**3. Secondary notes on the service**
- The comment at `installer/windows/OrzioClashReportLauncher.iss:66-67` says SmartScreen warns on first run. That is not true for the in-app path. The file is written by `FileStream` and started with `UseShellExecute=false`, so no Mark-of-the-Web is attached and the SHA-256 check is the only gate. Fix the comment or the claim.

## Findings: `installer/windows/OrzioClashReportLauncher.iss`

1. **No `MinVersion`** in `[Setup]` (lines 34–63). Setup will install on Windows versions older than the runtime supports. Add an explicit `MinVersion` matching the verified floor for the .NET 8 / Avalonia runtime.
2. **Per-user vs. machine-wide upgrade** (lines 3–5, 50–51). `PrivilegesRequired=lowest` with `PrivilegesRequiredOverridesAllowed=dialog` keeps the same `AppId` across both scopes. A previous machine-wide install may not be detected by a per-user run, which would leave two copies. Verify this on a machine that has both. I have not confirmed the Inno lookup behavior.
3. **Uninstall data prompt uses `{localappdata}`** (line 109). If the uninstaller runs elevated, this resolves to the administrator's profile, not the user's. Minor, but it can delete the wrong profile's data.
4. **Installer name contains "setup"** (line 59) and is launched via `CreateProcess`. Windows installer detection can require elevation unless the embedded manifest is `asInvoker`. Confirm the built binary's manifest.

No other serious compatibility defect found in the script: the `Portuguese.isl` reference, the `{{` escaping in `AppId`, the `CustomMessages` language prefixes, the `DelTree` signature, and the `*.download` / installer-name contract with the service all look correct.

Nothing was edited or executed.
