# Orzio Clash Report Desktop — private pilot

This guide covers the private pilot of `v0.2.0-launcher-preview.1`, the first desktop application
built on the existing `orzioclash` engine. It is written for two or three invited evaluators.

## What this is

A Windows desktop application that runs the existing engine for you, so the operations that used to
require a terminal now happen through forms and file pickers.

The engine itself is unchanged. The desktop application starts it as a normal program, passes the
same arguments the documented commands already use, and shows you what it printed.

## What this is not

- Not a production release. It is a private pilot build for invited evaluators.
- **Not code signed.** Windows SmartScreen will warn the first time you run the installer. Verify
  the published SHA-256 before installing, and report exactly what SmartScreen showed you.
- Not validated for longitudinal comparison. Comparing revisions, comparing an index, and
  regenerating a project report exercise engine behaviour that has **not** been validated against
  three real historical exports. The application labels those operations experimental.
- Not a replacement for coordination judgement. The engine suggests; a human decides.

## Installing

1. Verify the installer's SHA-256 against the one published with the build.
2. Run the installer. It installs **per user** and asks for no administrator rights.
   The default location is `%LOCALAPPDATA%\Programs\Orzio\ClashReportLauncher`.
3. SmartScreen will most likely warn. Choose "More info" then "Run anyway" only if the SHA-256
   matched in step 1.
4. A desktop shortcut is offered but is **unchecked** by default.
5. The installer offers to delete the application's own settings and logs when you later
   uninstall. That option is also **unchecked** by default.

If your organisation uses AppLocker and blocks execution from `%LOCALAPPDATA%`, the installer can be
run elevated to install machine-wide instead. That is a fallback, not the intended path; please tell
us if you needed it.

## Where your data lives

- The application keeps its **own** settings, logs, and job state in
  `%LOCALAPPDATA%\Orzio\ClashReportLauncher`.
- Your exports, manifests, snapshots, run indexes, project catalogs, governance documents, and
  reports live wherever **you** put them. The application never writes them into its installation
  directory, and uninstalling never deletes them.

## What it does with your data

- Everything stays on your machine. There is no telemetry, no upload, and no account.
- The local log records what ran and how it ended. It records a destination's extension, a SHA-256
  of the path, and the kind of folder it lives under — never the path itself, never the folder
  chain, never a client name, and never the contents of an export.
- A diagnostic bundle is produced only when you ask for one, and only after you have seen the
  complete list of files it would contain and the exact log text that would go into it.

## The seven sections

| Section | What it does |
| --- | --- |
| Início | Engine state, the three most common starting points, and your last five results. |
| Relatório rápido | One XML export to one grouped HTML report. |
| Snapshots | Create an immutable run snapshot; compare two persisted snapshots. |
| Longitudinal | Compare two revisions; create an ordered run index; compare an index. |
| Projetos | Create a project catalog; append a snapshot; regenerate the project's report. |
| Governança | Create a governance document; record a human decision; validate; render the review. |
| Definições | Theme, warnings, where local data lives, and the diagnostic bundle. |

## Things worth knowing before you start

- **Run order is always yours.** When you build a run index, the order you declare is the order that
  is used. Nothing is sorted by date, name, or revision. If you list the same snapshot twice, it
  stays listed twice and is flagged as repeated — it is not removed.
- **Previous and current are roles you declare**, not something inferred from timestamps.
- **A suggestion is never a decision.** In Governança, a confirmation is something you record, with
  a persistent identity id. A rejection never carries one. High algorithmic confidence is not a
  confirmation, and the application will not turn one into the other.
- **An existing report is never replaced silently.** If the HTML destination already exists, you are
  asked, and "choose another name" is the default. Snapshots, run indexes, project catalogs, and
  governance documents are never offered for replacement at all: the engine refuses to overwrite
  them, because they are evidence.
- **If the application closes while an operation is running**, the next start says so. It does not
  resume anything: check the destination file before repeating the operation.

## Reporting back

Please answer all of these, even where the answer is "no problem".

### Installation

1. Did the installer run without an administrator prompt?
2. Exactly what did SmartScreen show, and what did you have to click?
3. Does your organisation use AppLocker? Did installing into `%LOCALAPPDATA%` work, or did you need
   the machine-wide fallback?
4. Did anything about the install feel unsafe or unclear?

### Using it

5. How easy was Relatório rápido? Could you produce a report without asking anyone for help?
6. Which of the other operations did you understand from the screen alone, and which did you not?
7. Which sections did you actually use, and which did you never open?
8. Was moving around the application easy or confusing? Where did you get lost?

### Governança

9. Was the difference between confirming and rejecting an identity clear before you clicked?
10. Was it clear that an algorithmic suggestion is not a decision?

### When things went wrong

11. What errors did you hit? Copy the message exactly as shown.
12. Was each error message actionable — did it tell you what to do next?
13. Did anything crash, hang, or close unexpectedly? What did the next start tell you?

### Impressions

14. Does it look like a professional tool? What specifically looks unfinished?
15. What would stop you using this on a real project tomorrow?
16. What is missing that you expected to find?

## Honest status

- Single-run parsing, grouping, and HTML were human-validated on one private real export.
- Longitudinal matching, lifecycle, continuity links, continuity paths, and the longitudinal report
  have **not** been validated against three real historical exports and remain experimental.
- The desktop application's operations are covered by automated tests, including tests that drive a
  real child process. That is not the same as validation on a real project, and this pilot is how
  the second claim gets earned.
- There is no Clash Ledger, no `Reopened`, no automatic identity propagation, no transitivity, no
  automatic chronology, and no automatic responsibility.
- Legal distribution terms remain an owner decision. This build is for
  authorised private distribution only.
