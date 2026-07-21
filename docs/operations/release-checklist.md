# Internal Preview Release Checklist

Use this checklist before publishing an internal preview artifact or prerelease.

## Source Gates

- [ ] Source commit is contained in `master`.
- [ ] Tag, when used, points to a commit contained in `master`.
- [ ] CI is green.
- [ ] Application version from `orzioclash.exe --version` matches the intended tag.
- [ ] `workflow_dispatch` was treated as a packaging dry run only.
- [ ] No tag was created by automation.

## Build and Package Gates

- [ ] Release build completed in `Release` configuration.
- [ ] CLI was published for `win-x64`.
- [ ] Publish is self-contained.
- [ ] Publish uses `PublishSingleFile=true`.
- [ ] Debug symbols are disabled in the release artifact.
- [ ] ZIP contains `orzioclash.exe`.
- [ ] ZIP contains `README.md`.
- [ ] ZIP contains `CHANGELOG.md`.
- [ ] ZIP contains the internal preview guide.
- [ ] ZIP contains this release checklist.
- [ ] ZIP contains `smoke-release.ps1`.
- [ ] ZIP contains anonymized sample XML and manifest files.
- [ ] ZIP contains the run-index template.
- [ ] SHA-256 checksum file was generated.

## Smoke Gates

- [ ] Clean-machine smoke test completed.
- [ ] `orzioclash.exe --version` returned the expected version.
- [ ] `orzioclash.exe --help` succeeded.
- [ ] Single-run HTML output was created and is non-empty.
- [ ] Three snapshots were created.
- [ ] Run index was created.
- [ ] Longitudinal stdout was produced.
- [ ] Longitudinal HTML output was created and is non-empty.
- [ ] Smoke stderr was empty for successful commands.
- [ ] Smoke results are understood as packaging smoke only, not real sequential validation.

## Documentation and Privacy Gates

- [ ] README review completed.
- [ ] Internal preview guide review completed.
- [ ] Changelog review completed.
- [ ] Responsibility and authorship decision remains deferred.
- [ ] Privacy scan completed across changed files.
- [ ] No private XML, HTML, PDF, paths, project names, model names, personal names, email
  addresses, NWD, NWF, NWC, RVT, or image artifacts are included.
- [ ] Release notes clearly state the longitudinal validation limitation.
- [ ] Release is marked as a prerelease.
- [ ] Post-download checksum verification was tested.

## Product Claim Gates

- [ ] Single-run real validation is described as one private real export only.
- [ ] Longitudinal real validation is still described as not completed.
- [ ] No claim implies that the full product or full revision-aware workflow is validated.
- [ ] The preview does not claim persistent clash identity, Clash Ledger, `Reopened`,
  aggregate multi-run lifecycle, automatic chronology, or automatic clash responsibility.
