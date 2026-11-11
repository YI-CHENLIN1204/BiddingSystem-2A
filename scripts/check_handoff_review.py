#!/usr/bin/env python3
"""Ensure every changed file has a *new* documented handoff impact review.

Run from repository root:
    python scripts/check_handoff_review.py --base <pre-change-sha>
    python scripts/check_handoff_review.py --base HEAD~1
No third-party dependencies; works on Windows and GitHub Actions.
This is a procedural gate, not proof of factual correctness.
"""
import argparse
import json
import subprocess
import sys
from pathlib import Path

LOG = "docs/HANDOFF_CHANGE_REVIEWS.jsonl"
REQUIRED = {
    "date", "change_id", "summary", "changed_paths",
    "handoff_update_required", "updated_documents", "reason", "verification",
}

def git(*args):
    p = subprocess.run(["git", *args], capture_output=True, text=True,
                       encoding="utf-8", errors="replace")
    if p.returncode:
        raise RuntimeError(f"git {' '.join(args)} failed: {p.stderr.strip()}")
    return p.stdout

def added_records(base, head):
    diff = git("diff", "--no-ext-diff", "--unified=0", f"{base}..{head}", "--", LOG)
    result = []
    for line in diff.splitlines():
        if not line.startswith("+") or line.startswith("+++"):
            continue
        raw = line[1:].strip()
        if not raw or not raw.startswith("{"):
            continue
        try:
            result.append(json.loads(raw))
        except json.JSONDecodeError as e:
            raise ValueError(f"invalid newly appended review JSON: {e}") from e
    return result

def inspect(base, head):
    changes = {
        p.strip() for p in git("diff", "--name-only", f"{base}..{head}").splitlines()
        if p.strip() and p.strip() != LOG
    }
    if not changes:
        return ["No non-log changes to review."], []
    changed_in_diff = {x.strip() for x in
        git("diff", "--name-only", f"{base}..{head}").splitlines()}
    if LOG not in changed_in_diff:
        return [], [f"Missing changed {LOG} in this diff range. Every change requires a NEW record."]
    records = added_records(base, head)
    problems = []
    if not records:
        problems.append("No newly added JSONL review records found.")
    covered = set()
    all_doc_updates = set()
    for index, rec in enumerate(records, 1):
        if not isinstance(rec, dict):
            problems.append(f"Record {index} is not a JSON object")
            continue
        missing = REQUIRED - set(rec)
        if missing:
            problems.append(f"Record {index} missing required keys: {sorted(missing)}")
            continue
        if not isinstance(rec["changed_paths"], list) or not rec["changed_paths"]:
            problems.append(f"Record {index}: changed_paths must be a nonempty list")
            continue
        if not isinstance(rec["handoff_update_required"], bool):
            problems.append(f"Record {index}: handoff_update_required must be boolean")
            continue
        if not isinstance(rec["updated_documents"], list):
            problems.append(f"Record {index}: updated_documents must be an array")
            continue
        if not all(isinstance(rec[x], str) and rec[x].strip() for x in
                   ("date", "change_id", "summary", "reason", "verification")):
            problems.append(f"Record {index}: blank metadata value")
        covered.update(rec["changed_paths"])
        if rec["handoff_update_required"]:
            if not rec["updated_documents"]:
                problems.append(f"Record {index}: handoff_update_required=true without updated_documents")
            for path in rec["updated_documents"]:
                if path not in changes:
                    problems.append(f"Record {index}: claimed updated handoff file not changed: {path}")
            all_doc_updates.update(rec["updated_documents"])
        elif rec["updated_documents"]:
            problems.append(f"Record {index}: false requires updated_documents=[]")
    missed = changes - covered
    if missed:
        problems.append(f"Changed paths without NEW review record: {sorted(missed)}")
    not_changed = covered - changes
    if not_changed:
        problems.append(f"Review claims paths not changed in this range: {sorted(not_changed)}")
    messages = [f"Reviewed {len(changes)} files with {len(records)} new audit record(s)."]
    if all_doc_updates:
        messages.append("Handoff files updated: " + ", ".join(sorted(all_doc_updates)))
    else:
        messages.append("Handoff not updated: review reason documented; assess substantively.")
    return messages, problems

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", required=True, help="Git base ref/SHA before the change")
    parser.add_argument("--head", default="HEAD", help="Git head ref/SHA")
    args = parser.parse_args()
    try:
        messages, problems = inspect(args.base, args.head)
    except (RuntimeError, ValueError) as e:
        print(f"FAIL: {e}", file=sys.stderr)
        return 1
    for line in messages:
        print(line)
    for problem in problems:
        print("FAIL:", problem, file=sys.stderr)
    return 1 if problems else 0

if __name__ == "__main__":
    raise SystemExit(main())
