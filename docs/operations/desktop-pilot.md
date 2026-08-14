# Orzio Clash Report Desktop — private pilot guide

Target package: `v0.2.0-launcher-preview.1`, Windows `win-x64`.

This guide is for two or three named evaluators running the desktop application on their own
Windows machines. It states what the pilot is asking, what the build can and cannot do, and how to
report back.

---

## 1. What this pilot is for

The engine has been a command-line tool. This package is the first one an evaluator can install and
use without a terminal.

The pilot answers one question: **can a BIM coordinator install this, produce a report, and
understand what the application is telling them, without help?**

It is not asking whether the longitudinal analysis is correct. That still needs three real
sequential exports from one project, and that validation has not happened.

## 2. Maturity, stated plainly

| Claim | Status |
| --- | --- |
| Compiles | Yes |
| Automated tests pass | Yes |
| The launcher was smoke-tested on a clean Windows machine | **Pending — an evaluator does this** |
| Single-run parsing, grouping and HTML validated on a real export | Yes, once, on one private export |
| Longitudinal matching, lifecycle and continuity validated on real sequential exports | **No** |

Longitudinal comparison, continuity links and continuity paths remain experimental. The application
says so on the screens that use them. Do not treat a longitudinal report as a coordination decision.

## 3. Before you install

- Windows 10 or 11, 64-bit.
- No administrator rights are needed. The application installs into your own user profile.
- **The installer is not code signed.** Windows SmartScreen will warn you on first run. This is
  expected for this build, not a sign that the download is wrong.
- Verify the download against the published SHA-256 before installing.

```powershell
Get-FileHash -LiteralPath .\orzio-clash-report-desktop-v0.2.0-launcher-preview.1-win-x64-setup.exe -Algorithm SHA256
```

Compare the result with the `.sha256` file published alongside the installer.

## 4. What gets installed, and where

```text
%LOCALAPPDATA%\Programs\Orzio\ClashReportLauncher\
    OrzioClashReport.Launcher.Desktop.exe
    engine\win-x64\orzioclash.exe
    engine\win-x64\engine-manifest.json
    samples\
    docs\
```

Your own settings, recent list, logs and job records live separately:

```text
%LOCALAPPDATA%\Orzio\ClashReportLauncher\
```

Your reports, snapshots, run indexes, project catalogs and governance documents live **wherever you
choose to save them**. The application never moves them and the uninstaller never deletes them.

## 5. What the application does not do

- No telemetry, no upload, no account, no internet connection required.
- Local logs never record an absolute path. A path appears only as its file name, its extension, a
  SHA-256 of the full path, and the kind of location it came from.
- A diagnostic bundle is produced only when you ask for one, and only after you have been shown the
  exact list of files it would contain and the redacted log itself.
- Nothing is ever resumed automatically after a crash.

## 6. Try this, in order

1. Install, and open the application from the Start menu.
2. Check the engine badge in the status bar. It should read **Motor pronto**.
3. **Relatório rápido**: pick `samples\sample-clash.xml` from the installation folder, choose a
   destination in your own Documents folder, generate, and open the report.
4. Run it again to the same destination. The application should refuse and offer you a choice.
5. **Snapshots**: create a snapshot from the sample XML and `samples\sample-clash.run-manifest.json`.
6. Try to create the same snapshot again to the same destination. It must refuse outright.
7. **Longitudinal**: build a run index from two snapshots, reorder them, and compare the index.
8. **Projetos**: create a project, append a run, then render it.
9. **Governança**: create a governance document and record one confirmation and one rejection.
10. **Definições**: look at the diagnostic bundle preview. Read the redacted log.
11. Uninstall. Confirm your report is still where you saved it.

## 7. Questionnaire

Please answer all of these. Short answers are fine; a sentence of context is worth more than a
score.

### Installation

1. Did the installer run without asking for administrator rights?
2. How long did installation take, and did anything about it surprise you?

### SmartScreen

3. What exactly did Windows show you on first run?
4. Would that warning have stopped you, if this had come from outside your own company?

### AppLocker and managed machines

5. Is your machine under an AppLocker or similar policy?
6. If so, did anything fail to run from `%LOCALAPPDATA%`? What was the exact message?

### Quick report

7. Could you produce a report from the sample without reading any documentation?
8. What did you have to guess?

### Understanding the operations

9. After looking at Snapshots, Longitudinal, Projetos and Governança: in your own words, what is a
   snapshot for, and what is a run index for?
10. Which operation's purpose was least clear?

### Errors

11. List every error message you saw, and whether it told you what to do next.
12. Was there any point where the application stopped and you did not know why?

### Navigation

13. Was anything hard to find?
14. Did you ever end up on the wrong screen for what you were trying to do?

### Confirm and reject

15. In Governança, how confident were you about which option was which?
16. Would you have understood the difference without the colour? (Try it: the glyph and the label
    are meant to be enough.)
17. Was it clear why a confirmation asks for a persistent identifier and a rejection does not?

### Failure recovery

18. Close the application while an operation is running, then reopen it. What did it tell you?
19. Was that message enough for you to know what to do about the interrupted work?

### Visual perception

20. Does this look like a tool you would open in front of a client?
21. Anything that looked broken, cramped, cut off, or unreadable? Which screen, and at what window
    size?
22. If you use dark mode, did anything read badly there?

### What you actually used

23. Which parts did you use more than once?
24. Which parts would you never use?
25. What is the one thing that would make you keep this on your machine?

### Anything else

26. What would you tell a colleague about this build?

## 8. Reporting a problem

For anything that failed, please attach:

- the screen you were on, and what you clicked;
- the exact error text;
- a diagnostic bundle, from **Definições → Diagnóstico**.

Read the bundle preview before sending it. It is designed to contain nothing of yours, and if you
see anything in it that you would not want to send, that is itself a defect worth reporting.

**Never send a real client export, manifest, report, or model file.** Nothing in this pilot needs
one.

## 9. Known limitations in this package

- The installer is not code signed.
- The application drives the engine as a subprocess. That is a deliberate choice for this phase.
- Governança asks you to type run ids and occurrence indexes by hand. The application deliberately
  does not propose pairings, because an algorithmic suggestion is not a human decision.
- `create-project` requires the report's parent folder to already exist. The engine reports this
  clearly, but the form does not warn you beforehand.
- macOS is not part of this phase.
- There is no PDF output, no auto-update, no licensing and no cloud.
- Longitudinal behaviour has not been validated against three real historical exports.

Legal distribution terms remain an owner decision. This package is for private, authorised
evaluation only.
