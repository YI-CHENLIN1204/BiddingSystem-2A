#!/usr/bin/env python3
"""Research-only reference-eligibility audit; never accesses production DB.

A simulation can illustrate the *rules*, not prove real records are eligible.
Actual eligibility ALWAYS fails when source_kind='synthetic', even if a scenario
models all other dimensions as valid. Hypothetical eligibility is explicitly
separate and must never be used as production-ready evidence.
"""
import argparse
import csv
import json
from collections import Counter
from pathlib import Path

from generate import produce

MIN_REFERENCE_COUNT = 5
REQUIRED_MATERIAL_UNIT = {"鋼筋": "kg", "混凝土": "m³"}
REJECTION_ORDER = (
    "same_project", "wrong_project_type", "wrong_structure",
    "wrong_material", "wrong_unit", "incomplete_scope",
    "not_approved", "unverified_source",
)
FIELDS = (
    "case_id", "material", "scenario_defect", "hypothetical_eligible",
    "production_eligible", "hypothetical_rejection_reasons",
    "production_rejection_reasons",
)

def evaluate_reference(target, candidate, require_same_structure=True,
                       hypothetical=False):
    """Return ALL disqualifying reasons; do not short-circuit or write to DB."""
    reasons = []
    if target["case_id"] == candidate["case_id"]:
        reasons.append("same_project")
    if target["project_type"] != candidate["project_type"]:
        reasons.append("wrong_project_type")
    if require_same_structure and target["structure_type"] != candidate["structure_type"]:
        reasons.append("wrong_structure")
    if target["material"] != candidate["material"]:
        reasons.append("wrong_material")
    if candidate["quantity_unit"] != REQUIRED_MATERIAL_UNIT.get(target["material"]):
        reasons.append("wrong_unit")
    if candidate.get("scope_quality") != "complete":
        reasons.append("incomplete_scope")
    if candidate.get("review_status") != "Approved":
        reasons.append("not_approved")
    # The hypothetical policy deliberately suspends only the provenance
    # requirement to isolate other gate effects in *synthetic* simulations.
    # It never changes candidate provenance nor grants production readiness.
    if not hypothetical and candidate.get("source_kind") != "verified_company":
        reasons.append("unverified_source")
    return reasons

def scenario_candidates(seed=20261111, per_combo=12, defect_cycle=True):
    """Produce deterministic fabricated examples with declared injected defects."""
    if per_combo < 6:
        raise ValueError("per_combo must be >= 6")
    all_rows = produce(seed, per_combo, "stable")
    selection = [r for r in all_rows if r["project_type"] == "住宅"
                 and r["material"] == "鋼筋"]
    target = dict(selection[-1])
    target.update({"source_kind": "synthetic", "scope_quality": "complete",
                   "review_status": "Approved"})
    pool = []
    defects = ("none", "wrong_structure", "wrong_unit", "incomplete_scope",
               "not_approved", "wrong_material", "wrong_project_type")
    for i, r in enumerate(selection[:-1]):
        candidate = dict(r)
        candidate.update({"source_kind": "synthetic",
                          "scope_quality": "complete",
                          "review_status": "Approved"})
        defect = defects[i % len(defects)] if defect_cycle else "none"
        if defect == "wrong_structure":
            candidate["structure_type"] = "SC" if target["structure_type"] != "SC" else "RC"
        elif defect == "wrong_unit":
            candidate["quantity_unit"] = "tonne"
        elif defect == "incomplete_scope":
            candidate["scope_quality"] = "partial"
        elif defect == "not_approved":
            candidate["review_status"] = "Draft"
        elif defect == "wrong_material":
            candidate["material"] = "混凝土"
        elif defect == "wrong_project_type":
            candidate["project_type"] = "廠房"
        candidate["scenario_defect"] = defect
        pool.append(candidate)
    return target, pool

def audit(target, candidates, minimum_count=MIN_REFERENCE_COUNT,
          require_same_structure=True):
    if minimum_count < 1:
        raise ValueError("minimum_count must be positive")
    rows = []
    hypothetical_ok, production_ok = 0, 0
    hypothetical_reasons, production_reasons = Counter(), Counter()
    for candidate in candidates:
        hypo = evaluate_reference(target, candidate, require_same_structure,
                                  hypothetical=True)
        prod = evaluate_reference(target, candidate, require_same_structure,
                                  hypothetical=False)
        hypothetical_ok += not hypo
        production_ok += not prod
        hypothetical_reasons.update(hypo)
        production_reasons.update(prod)
        rows.append({
            "case_id": candidate["case_id"], "material": candidate["material"],
            "scenario_defect": candidate.get("scenario_defect", "none"),
            "hypothetical_eligible": not hypo,
            "production_eligible": not prod,
            "hypothetical_rejection_reasons": ";".join(hypo),
            "production_rejection_reasons": ";".join(prod),
        })
    summary = {
        "classification": "SYNTHETIC_RESEARCH_ONLY",
        "not_real_engineering_accuracy": True,
        "no_production_database_access": True,
        "target_case_id": target["case_id"],
        "candidate_count": len(candidates),
        "minimum_reference_count": minimum_count,
        "same_structure_required": require_same_structure,
        "hypothetical_eligible_count": hypothetical_ok,
        "hypothetical_count_gate_pass": hypothetical_ok >= minimum_count,
        "actual_production_eligible_count": production_ok,
        "actual_production_count_gate_pass": production_ok >= minimum_count,
        "hypothetical_rejection_counts": dict(sorted(hypothetical_reasons.items())),
        "production_rejection_counts": dict(sorted(production_reasons.items())),
        "provenance_warning": "All generated candidates are synthetic: no production eligibility is granted",
    }
    return rows, summary

def save_csv(path, rows):
    with path.open("w", newline="", encoding="utf-8-sig") as stream:
        writer = csv.DictWriter(stream, fieldnames=FIELDS)
        writer.writeheader()
        writer.writerows(rows)

def render(summary):
    return "\n".join([
        "# 2A 歷史參考案資格與可比性診斷（模擬研究）",
        "",
        "> 本次所有候選皆為虛構資料；沒有任何一筆獲得正式產學模型參考資格。",
        "> 「假設來源真實」欄位只用於隔離其他條件的研究影響，**不代表真實來源已核實**。",
        "",
        f"- 模擬候選數：{summary['candidate_count']}",
        f"- 同結構限制：{'有' if summary['same_structure_required'] else '無'}",
        f"- 現行參考筆數門檻：{summary['minimum_reference_count']}",
        f"- 假設來源真實時，其他條件合格數：{summary['hypothetical_eligible_count']}",
        f"- 研究假設下達最低筆數：{'是' if summary['hypothetical_count_gate_pass'] else '否'}",
        f"- **可作為產學正式基準的實際數：{summary['actual_production_eligible_count']}**",
        "",
        "## 可比性拒絕原因（可能一案同時有多項）",
        "",
        "| 原因 | 虛構來源放行的診斷次數 | 正式來源條件下拒絕次數 |",
        "|---|---:|---:|",
    ] + [
        f"| {key} | {summary['hypothetical_rejection_counts'].get(key, 0)} | "
        f"{summary['production_rejection_counts'].get(key, 0)} |"
        for key in REJECTION_ORDER
    ] + [
        "", "## 研究限制與下一個步驟", "",
        "1. 結構、用途、材料、標準單位、工程範圍、審核與來源是研究用示意欄位，未對接正式資料庫實際欄位。",
        "2. 不可藉由將來源標記為 Approved，讓合成資料通過 production eligibility。",
        "3. 本次只驗證資料資格規則；尚無同口徑真實工程數量或價格驗收。",
        "4. 後續以真實歷史樣本的唯讀欄位對照建立適配器，來源不明則拒絕正式判定。",
        "",
    ])

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--seed", type=int, default=20261111)
    p.add_argument("--cases-per-combo", type=int, default=12)
    p.add_argument("--minimum-count", type=int, default=MIN_REFERENCE_COUNT)
    p.add_argument("--allow-cross-structure", action="store_true")
    p.add_argument("--output", type=Path,
                   default=Path("research/2a_synthetic/output/eligibility"))
    args = p.parse_args()
    if args.minimum_count < 1 or args.cases_per_combo < 6:
        p.error("minimum-count must be positive and cases-per-combo >= 6")
    target, candidates = scenario_candidates(args.seed, args.cases_per_combo)
    rows, summary = audit(target, candidates, args.minimum_count,
                          require_same_structure=not args.allow_cross_structure)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    csv_path = args.output.with_name(args.output.name+"_detail.csv")
    summary_path = args.output.with_name(args.output.name+"_summary.json")
    report_path = args.output.with_name(args.output.name+"_report.md")
    save_csv(csv_path, rows)
    summary_path.write_text(json.dumps(summary, ensure_ascii=False, indent=2)+"\n",
                            encoding="utf-8")
    report_path.write_text(render(summary), encoding="utf-8")
    print(f"Eligibility audit: {len(rows)} fabricated candidates; "
          f"hypothetical={summary['hypothetical_eligible_count']}; "
          f"production={summary['actual_production_eligible_count']} "
          f"(synthetic-only, no DB)")
if __name__ == "__main__":
    main()
