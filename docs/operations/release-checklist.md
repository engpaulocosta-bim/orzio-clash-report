# Internal Preview Release Checklist

Use this checklist before publishing an internal preview artifact or prerelease.

## Source Gates

- [ ] Source commit is contained in `master`.
- [ ] Tag, when used, points to a commit contained in `master`.
- [ ] CI is green.
- [ ] Application version from `orzioclash.exe --version` matches the intended tag.
- [ ] `workflow_dispatch` was treated as a packaging dry run only.
- [ ] `workflow_dispatch` did not publish a GitHub Release.
- [ ] No tag was created by automation.
- [ ] All external GitHub Actions are pinned by full 40-character commit SHA.

## Build and Package Gates

- [ ] Release build completed in `Release` configuration.
- [ ] CLI was published for `win-x64`.
- [ ] Publish is self-contained.
- [ ] Publish uses `PublishSingleFile=true`.
- [ ] Debug symbols are disabled in the release artifact.
- [ ] ZIP contains `orzioclash.exe`.
- [ ] ZIP contains `README.md`.
- [ ] ZIP contains `CHANGELOG.md`.
- [ ] ZIP contains `smoke-release.ps1`.
- [ ] ZIP contains `docs/operations/internal-preview.md`.
- [ ] ZIP contains `docs/operations/project-catalog.md`.
- [ ] ZIP contains `docs/operations/release-checklist.md`.
- [ ] ZIP contains `docs/operations/identity-governance-cli.md`.
- [ ] ZIP contains `docs/operations/identity-governance-validation.md`.
- [ ] ZIP contains `docs/operations/identity-governance-review-report.md`.
- [ ] ZIP contains `docs/operations/pilot-evaluation.md`.
- [ ] ZIP contains `samples/sample-clash.xml`.
- [ ] ZIP contains `samples/sample-clash.run-manifest.json`.
- [ ] ZIP contains `samples/run-manifest.sample.json`.
- [ ] ZIP contains `samples/run-index.template.json`.
- [ ] ZIP contains zero `.pdb` files.
- [ ] ZIP contains zero temporary files.
- [ ] ZIP contains zero forbidden artifacts such as source code, private XML/HTML/PDF,
  images, NWD/NWF/NWC/RVT, or smoke workspaces.
- [ ] SHA-256 checksum file was generated.
- [ ] Checksum line format is `{{64 lowercase hex}}  {{package-name}}.zip`.
- [ ] ZIP and checksum file names use the executable version.
- [ ] CI and package jobs run with read-only repository permissions.
- [ ] `contents: write` is granted only to the tag-only prerelease publication job.

## Smoke Gates

- [ ] Clean-machine smoke test completed.
- [ ] `orzioclash.exe --version` returned `0.1.0-preview.3`.
- [ ] `orzioclash.exe --help` succeeded.
- [ ] Single-run HTML output was created and is non-empty.
- [ ] Three snapshots were created.
- [ ] Run index was created.
- [ ] Longitudinal stdout was produced.
- [ ] Longitudinal HTML output was created and is non-empty.
- [ ] `create-project` succeeded.
- [ ] `project.json` is valid schema-v1 JSON with relative references.
- [ ] `render-project` succeeded with three snapshots.
- [ ] Fourth snapshot creation succeeded.
- [ ] `append-project-snapshot` succeeded.
- [ ] Updated run index contains four entries.
- [ ] Project catalog bytes were preserved during append.
- [ ] Snapshot bytes were preserved during append.
- [ ] Report bytes were preserved during append.
- [ ] No `.run-index-replace-*.tmp` file remained after append.
- [ ] `create-identity-governance` succeeded.
- [ ] Governance JSON is schema v1, UTF-8 without BOM, LF-only, and create-new.
- [ ] Confirmation append succeeded and persisted `persistentIdentityId`.
- [ ] Rejection append succeeded and omitted `persistentIdentityId`.
- [ ] No `.identity-governance-replace-*.tmp` file remained after append.
- [ ] `validate-identity-governance` succeeded with deterministic stdout.
- [ ] Validation preserved project catalog, run index, snapshots, governance JSON, and
  longitudinal report bytes.
- [ ] `render-identity-governance-report` succeeded with deterministic stdout.
- [ ] Review HTML is non-empty, LF-only, and UTF-8 without BOM.
- [ ] Review HTML contains confirmation and rejection output.
- [ ] Review HTML contains `Persistent identity id` exactly once.
- [ ] Review HTML excludes `Element A source model`, `Element B source model`, and raw
  private source-model paths.
- [ ] Repeated review rendering is byte-identical.
- [ ] No `.derived-html-report-*.tmp` file remained after review rendering.
- [ ] A controlled semantic failure preserved stdout/stderr contracts, preserved sentinel
  output bytes, preserved input hashes, and left no temporary files behind.
- [ ] Smoke stderr was empty for successful commands.
- [ ] Smoke results are understood as packaging smoke only, not real sequential validation.

## Documentation and Privacy Gates

- [ ] README review completed.
- [ ] Internal preview guide review completed.
- [ ] Project catalog guide review completed.
- [ ] Identity-governance CLI guide review completed.
- [ ] Identity-governance validation guide review completed.
- [ ] Identity-governance review-report guide review completed.
- [ ] Controlled pilot guide review completed.
- [ ] Changelog review completed.
- [ ] Privacy scan completed across changed files.
- [ ] Privacy scan reviewed matches for `C:\`, `\\`, `/home/`, `Users\`, `Repositorios`,
  `client`, `customer`, `email`, `@`, `NWD`, `NWF`, `NWC`, and `RVT`.
- [ ] No packaged or documented artifact leaks private paths, personal data, or real model
  data.
- [ ] Packaged documentation states that the review report does not project raw
  `ClashObject.SourceModel`.
- [ ] Release notes clearly state the longitudinal validation limitation.
- [ ] No document or release note claims `0.1.0-preview.3` is already published before the
  tag.
- [ ] Packaged documentation does not hard-code which GitHub prerelease is currently the
  latest.
- [ ] Release is marked as a prerelease.
- [ ] Post-download checksum verification was tested.
- [ ] Legal distribution terms remain documented as an owner decision.

## Product Claim Gates

- [ ] Single-run real validation is described as one private real export only.
- [ ] Longitudinal real validation is still described as not completed.
- [ ] No claim implies that the full product or full revision-aware workflow is validated.
- [ ] No document or release note claims production-ready, enterprise-ready, AI-verified,
  commercially released, latest release, automatic clash identity, or validated
  longitudinal MVP.
- [ ] The preview claims persistent identity only through explicit human
  `ConfirmSameIdentity` decisions carrying `persistentIdentityId`.
- [ ] No document claims automatic identity assignment, automatic propagation,
  transitivity, graph merge, a project-wide identity graph, Clash Ledger, longitudinal
  identity integration, `Reopened`, inferred chronology, automatic clash responsibility,
  interactive review, database, multi-user workflow, or auth.
