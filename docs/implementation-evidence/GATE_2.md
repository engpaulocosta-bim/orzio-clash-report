# Gate 2 — Functional regression

Status: PASS (automated/public-fixture scope only).

Executor: Codex; independent source reviewer: Claude CLI. Source `f52ecd5`.

Release suite: 1473 engine + 265 launcher + 68 desktop = 1806 passed, exit 0. Published-engine smoke: exit 0. Existing XML/HTML/snapshot/index/project/governance coverage passes. Core, XML input, report renderer and snapshot adapters have no diff from `7fe5c49`.

Real-file corrupt history and unsupported schema remain byte-identical; real child-process reporting still succeeds and clears its journal. No schema migration. No claim of new human validation on private historical models or installed persistence across an upgrade.
