# Public preview publication checklist

## Source and packaging changes

The owner selected the MIT license on 2026-11-18. README.md now separates the public
desktop 0.2.3 preview, engine 0.1.0-preview.3, installed-machine checks and the pending
clean-machine/real longitudinal evaluations. CONTRIBUTING.md and SECURITY.md explain
synthetic reproductions and private-data handling.

THIRD-PARTY-NOTICES.md and licenses/inventory.json cover 29 component/build profiles,
including transitive/native packages, Inter's OFL license and .NET runtime 8.0.31/8.0.34.
The runtime notice texts for these two profiles are byte-identical; each version is
listed explicitly. Future unknown runtime versions fail packaging until reviewed.
Twelve deduplicated upstream notice texts are checked by SHA-256. New desktop installers
and future CLI ZIPs include the complete bundle. Existing release binaries are not replaced;
a matching notice ZIP can accompany them as an additional asset after publication approval.

Verification: 1808 Release tests passed (1473 engine, 266 launcher, 69 desktop).
An actual local desktop publish and unsigned installer build completed. Additional
negative checks refused altered/missing notices, unexpected files, path traversal and
an unlisted runtime version. A separate read-only Claude CLI audit found no blockers.
The generated candidate was not installed over the user's official application.

## Sensitive history: prepared locally, remote approval required

A separate mirror of all server refs was sanitized using git-filter-repo 2.47.0.
Exactly four original private blob versions were replaced with their already-anonymized
equivalents from the existing anonymization commit. The mirror includes five branches,
eight tags and 53 PR refs. Git integrity checks pass; the current master and all release
tip trees remain byte-identical. A scan of 1267 reachable file versions found none of
the identified private project/user markers or the credential formats checked.
This is a scoped scan, not proof that arbitrary secrets cannot exist.

Publishing that mirror changes commit/tag identifiers and requires explicit approval.
Before execution, compare every remote ref with its recorded old SHA and abort on drift.
Pause and restore the existing release workflows around the ref updates to avoid
rerunning old tag releases. Use per-ref leases; do not use a blind mirror push, delete
branches, overwrite release assets or relax repository protections.

After approval, the documentation/license commit must be applied on the sanitized master
ancestry before publication. Verify remote heads/tags, unchanged release-asset digests,
tag ancestry, a fresh-clone scan and CI. Replace stale local clones rather than merging
them into the cleaned history. Keep the private recovery bundle outside the public repo.

GitHub PR refs are read-only, and cached old commits may remain accessible after a push.
The maintainer must request removal of sensitive cached views/PR refs through
[GitHub Support](https://support.github.com/), supplying the private cleanup report.
Support decides whether the data qualifies; a history rewrite cannot remove third-party
copies. See [GitHub's sensitive-data removal guide](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository).
Do not mark the remote cleanup complete until those remaining references are addressed.
