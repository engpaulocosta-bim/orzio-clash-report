# Changelog

All notable changes for OrzioClashReport are recorded here.

## Unreleased

- Added a strict schema-v1 operational project catalog JSON adapter.
- Added `create-project` and `render-project` CLI commands for regenerable longitudinal
  project workflows built from immutable snapshots and an explicit run index.

## 0.1.0-preview.1

Internal preview release for Windows `win-x64`.

### Added

- Single-run Clash Detective XML parsing and grouped self-contained HTML reporting.
- Immutable coordination-run snapshots.
- Strict schema-v2 run manifests with explicit executed clash test coverage.
- Explicit ordered run-index JSON.
- Snapshot-to-snapshot and run-index adjacent comparison commands.
- Longitudinal stdout summary and self-contained longitudinal HTML.
- `orzioclash --help` and `orzioclash --version`.
- Windows release smoke script for packaged binaries.
- CI packaging smoke for the `win-x64` self-contained single-file executable.
- Release workflow for internal preview packaging and future tag-triggered prereleases.

### Validation Status

- Single-run parsing, grouping, and HTML presentation were human-validated on one private
  real export.
- Longitudinal matching, lifecycle classification, continuity links, continuity paths, and
  longitudinal HTML have not been validated against three real historical exports.
- Longitudinal behavior remains experimental.

### Known Limitations

- No persistent clash identity.
- No Clash Ledger.
- No `Reopened` lifecycle state.
- No aggregate multi-run lifecycle.
- No automatic chronology.
- No automatic clash responsibility.
- No PDF export, embedded clash images, WPF UI, licensing, or live Navisworks API adapter.
- Private validation XML, HTML, PDF, paths, project names, model names, and personal names
  are not part of the repository or release artifacts.
