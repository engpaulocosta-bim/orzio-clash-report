# Gate 4 — Updater

Status: BLOCKED (corrected-source tests PASS; old-installed e2e blocked).

Executor: Codex; independent static review: Claude CLI. Source `f52ecd5`.

Installed base version and feed were inspected read-only. Desktop target 0.2.3 is newer under the actual comparer; engine channel remains separate. Unit tests cover version ordering, official URLs, invalid hash, cancelled/failed download, file-handle disposal and installer-start refusal; all pass in Release.

Old 0.2.2 installed code has the reproduced rename-sharing defect. It cannot bootstrap the correction through its own broken download path. No feed/publication/install/relaunch proof exists. A verified manual initial installer upgrade is required; no claim of seamless old-button success.

Unsigned release and same-user file replacement/authenticity limits predate this branch and remain documented. No integrity/TLS/channel rule was weakened.
