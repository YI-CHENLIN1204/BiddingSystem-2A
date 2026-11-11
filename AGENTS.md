# BiddingSystem-2A agent instructions

This repository maintains a project-specific Skill:

**`.agents/skills/waizheng-2a-handoff-guardian/SKILL.md`**

**Mandatory:** Read that Skill before touching any source code, simulation, research, schema, tests, configuration, scripts, deployment, or documentation. After **every change**, perform a handoff impact review and append an audit entry to `docs/HANDOFF_CHANGE_REVIEWS.jsonl`; update the appropriate handoff documents when the change materially affects project state. Run `python scripts/check_handoff_review.py --base <start-of-change-SHA>` to verify coverage. Merely committing or saying "no change required" without a written review is insufficient.

Canonical handoff:
- `docs/CURRENT_HANDOFF_2026-11-11.md`
- `docs/RESEARCH_TRACEABILITY_2026-11-11.md`
- `docs/NEW_CHAT_START_2026-11-11.md`
- `docs/WORKLOG.md`
- `docs/DECISIONS.md`

Scope: `YI-CHENLIN1204/BiddingSystem-2A`; work on `feature/1d-api-integration` unless the user directs otherwise; do not merge into `main` without explicit permission.

Use Traditional Chinese for user-facing docs. **Never promote synthetic research data to company historical truth**, never claim simulated error metrics as real validation, and never put secrets in the repository.

CI checks changed file coverage in review records; it does **not** replace substantive engineering review or prove agents have read all docs.
