# Gate 4 — Updater

Status: corrected-source checks PASS; exact legacy one-click upgrade BLOCKED by the old installed binary.

Source 4d00071. Real old Check button observed Checking -> Up to date against current published 0.2.2 feed. Compatible target setup/checksum/manifest prepared for desktop-v0.2.3. Tests pass for ordering, official URLs, invalid hash, cancellation/network recovery, stream disposal and start failure. Actual manual upgrade and rollback passed without state loss.

The already-installed 0.2.2 binary cannot rename a verified download because its hash-verification handle remains open. A new release cannot fix that old running code before it installs. First upgrade requires a verified manual official-installer bootstrap; subsequent source is fixed. This legacy exception needs explicit acceptance for rollout. Unsigned-distribution/authenticity limits remain documented; no TLS/hash/channel rule was weakened.
