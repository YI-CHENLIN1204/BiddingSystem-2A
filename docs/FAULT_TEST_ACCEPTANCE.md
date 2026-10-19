# Isolated PostgreSQL persistence (opt-in; migration, not a live hot-switch)

The existing `compose.fault-test.yaml` uses tmpfs for PostgreSQL and intentionally stays unchanged. **Never swap the storage mount of an existing running DB container**: this can erase the in-memory fixture, including Project #1 and receipts.

## Current acceptance evidence (user-run, 2026-10-19)
- Isolated real API + PostgreSQL: Publish and Reopen succeeded; same Idempotency-Key replay preserved receipt/audit counts.
- Controlled precommit connection loss: Publish and Reopen returned expected HTTP 500, marker `[FAULT_TEST_DROP_BEFORE_COMMIT]` appeared, and Draft/Approved + history/receipt/audit counts remained unchanged.
- Same-ID retry after failure: Publish returned Approved with 2 active history, 5 receipts and 5 audit records; Reopen returned Draft with 0 active history, 6 receipts and 6 audits.
- Publish with response intentionally discarded then same-ID replay: Approved, 2 active history, 7 receipts, 7 audits.
- **Not tested**: actual socket interruption AFTER commit; repeated real failure scenarios beyond controlled failpoint; restart persistence of tmpfs DB.
- RollbackAsync emitted ObjectDisposedException following deliberate connection close; server showed no partial commits. This should be separately improved without treating it as transaction corruption.

## Local configuration drift
The checked-in `compose.fault-test.yaml` still has backend attached to `isolated` alone. The user's local working copy adds an `api_access` bridge network; necessary for host port 18080 on their Docker setup. **Do not overwrite that local copy with Git pull without preserving it.**

## Optional durable storage - fresh test environment only
A separate compose override, `compose.fault-volume.yaml`, switches the disposable DB data directory to a Docker *named volume*. Do not apply it to the currently running `bidding-2a-fault` stack: mount replacements require container recreation and the tmpfs fixture may be lost. For a fresh environment:
1. Retain and review the schema-only SQL and synthetic seed SQL locally; never upload secrets or business data.
2. Stop using the old in-memory test stack only after preserving any evidence needed.
3. Use a **different Compose project name** with the volume override, and change/remove backend's fixed host port 18080 before starting a second stack.
4. Validate the rendered Compose config, ensure `db` mount is a volume and no tmpfs data directory remains; ensure no Supabase hostname appears.
5. Import reviewed schema + synthetic seed to this *new* DB and verify 17 tables and fixture identity before invoking POST APIs.
6. Confirm restart persistence in the new stack and document recovery process.

The override file alone does NOT set up new stack wiring, import seed, or guarantee safe parallel execution. No production data is used.

## 2026-10-19 — Compile-time test isolation (unverified build)

- Publish and Reopen failpoints are now inside `#if FAULT_INJECTION_TEST` blocks. Normal Release excludes the failpoint source from compilation.
- `崴正/Dockerfile` accepts optional `FAULT_INJECTION_TEST=true`; default is false. The opt-in Docker Compose build override is `compose.fault-build.yaml`.
- To build fault-enabled backend: `docker compose -f compose.fault-test.yaml -f compose.fault-build.yaml build backend`. Keep `compose.fault-drop.yaml` separate: it enables runtime injection after deploying a fault-enabled image.
- **Do not deploy to the running test backend yet.** At present local Project #1 is Approved with 7 receipts and 7 audits; DB is tmpfs, so never recreate db merely to update backend.
- Before concluding success, build both default and opt-in images and inspect the compiled output / test trigger behavior in a disposable environment. No such builds executed by the GitHub edits.
- Existing local `compose.fault-test.yaml` contains necessary network modifications absent upstream. Avoid overwriting local changes; use `git status` before pulling.
