# Gate 4 — Updater

Status: PASS for corrected-source checks, published legacy detection and manual official bootstrap. The exact legacy one-click path remains limited by the old installed binary.

Post-publication closure: actual installed 0.2.2 Check detected 0.2.3; the verified official released setup completed manually and preserved original settings/history byte-for-byte. The user authorized publication after disclosure of the manual bootstrap. See T17 for immutable asset and installed-version evidence. The following original notes explain why no legacy Install-button PASS is asserted.

Source 4d00071. Real old Check button observed Checking -> Up to date against current published 0.2.2 feed. Compatible target setup/checksum/manifest prepared for desktop-v0.2.3. Tests pass for ordering, official URLs, invalid hash, cancellation/network recovery, stream disposal and start failure. Actual manual upgrade and rollback passed without state loss.

The already-installed 0.2.2 binary cannot rename a verified download because its hash-verification handle remains open. A new release cannot fix that old running code before it installs. First upgrade requires a verified manual official-installer bootstrap; subsequent source is fixed. This legacy exception needs explicit acceptance for rollout. Unsigned-distribution/authenticity limits remain documented; no TLS/hash/channel rule was weakened.
