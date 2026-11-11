#!/usr/bin/env python3
"""Reproducible synthetic construction dataset, exclusively for research/demo.

No database access, no real project records, no claim of engineering validity.
Standard library only. Generated quantities are a parameterized simulation,
NOT observations from Taiwanese projects or calibrated engineering norms.
"""
import argparse
import csv
import hashlib
import json
import math
import random
from collections import Counter
from pathlib import Path

STRUCTURES = ("RC", "SC", "SRC")
PROJECT_TYPES = ("住宅", "廠房")
MATERIALS = ("鋼筋", "混凝土")
# Synthetic scenario assumptions, not engineering unit-consumption standards.
BASE_USAGE = {
    ("住宅", "RC"): (92.0, 0.62),
    ("住宅", "SC"): (35.0, 0.28),
    ("住宅", "SRC"): (75.0, 0.51),
    ("廠房", "RC"): (78.0, 0.53),
    ("廠房", "SC"): (25.0, 0.20),
    ("廠房", "SRC"): (60.0, 0.42),
}
FIELDS = (
    "case_id", "dataset_type", "scenario", "company_group", "project_type",
    "structure_type", "city", "project_year", "floor_area_m2",
    "site_area_m2", "ground_floors", "basement_floors", "material",
    "quantity_unit", "total_quantity", "usage_per_m2", "line_unit_price_ntd",
    "line_amount_ntd", "reference_scope", "is_synthetic",
)

def lognormal_factor(rng, sigma):
    return math.exp(rng.gauss(-0.5 * sigma * sigma, sigma))

def produce(seed, cases_per_combo, scenario):
    rng = random.Random(seed)
    rows = []
    for kind in PROJECT_TYPES:
        for structure in STRUCTURES:
            base_rebar, base_concrete = BASE_USAGE[(kind, structure)]
            for index in range(cases_per_combo):
                area = round(rng.uniform(2500, 24000), 2)
                ground = rng.randint(3, 16) if kind == "住宅" else rng.randint(1, 8)
                basement = rng.randint(0, 3)
                site_area = round(area / rng.uniform(1.4, 5.2), 2)
                # Company effect deliberately varies across research scenarios.
                company = "崴正風格_虛構" if index % 4 == 0 else "外部風格_虛構"
                company_factor = (1.12 if company.startswith("崴正") else 1.0) if scenario == "company_shift" else 1.0
                slope = 1.0 + 0.012 * (ground - 8) + 0.025 * basement
                year = rng.randint(2016, 2025)
                city = rng.choice(("台中市", "雲林縣", "桃園市", "高雄市"))
                for material, base, unit, unit_price in (
                    ("鋼筋", base_rebar, "kg", 29.0),
                    ("混凝土", base_concrete, "m³", 3700.0),
                ):
                    sigma = {"stable": 0.08, "high_variance": 0.28, "company_shift": 0.14,
                             "outliers": 0.13}.get(scenario)
                    if sigma is None:
                        raise ValueError("unsupported scenario")
                    outlier = 2.0 if scenario == "outliers" and rng.random() < 0.07 else 1.0
                    usage = base * company_factor * max(0.25, slope) * lognormal_factor(rng, sigma) * outlier
                    quantity = round(area * usage, 2)
                    price = round(unit_price * lognormal_factor(rng, 0.10) *
                                  (1.0 + 0.025 * (year - 2020)), 2)
                    case_id = f"SYN-{kind}-{structure}-{index+1:04d}"
                    rows.append({
                        "case_id": case_id, "dataset_type": "synthetic_research",
                        "scenario": scenario, "company_group": company,
                        "project_type": kind, "structure_type": structure,
                        "city": city,
                        "project_year": year, "floor_area_m2": area,
                        "site_area_m2": site_area, "ground_floors": ground,
                        "basement_floors": basement, "material": material,
                        "quantity_unit": unit, "total_quantity": quantity,
                        "usage_per_m2": round(quantity / area, 6),
                        "line_unit_price_ntd": price,
                        "line_amount_ntd": round(quantity * price, 2),
                        "reference_scope": "SIMULATED_MATERIAL_TOTAL_NOT_FINAL_BID",
                        "is_synthetic": "true",
                    })
    return rows

def atomic_write(path, content):
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_text(content, encoding="utf-8")
    tmp.replace(path)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--seed", type=int, default=20261111)
    parser.add_argument("--cases-per-combo", type=int, default=30)
    parser.add_argument("--scenario", choices=("stable", "high_variance", "company_shift", "outliers"),
                        default="company_shift")
    parser.add_argument("--output", type=Path, default=Path("research/2a_synthetic/output"))
    args = parser.parse_args()
    if not 2 <= args.cases_per_combo <= 10000:
        parser.error("cases-per-combo must be between 2 and 10000")
    rows = produce(args.seed, args.cases_per_combo, args.scenario)
    path = args.output.resolve()
    path.mkdir(parents=True, exist_ok=True)
    csv_path = path / f"synthetic_{args.scenario}_seed{args.seed}.csv"
    with csv_path.open("w", newline="", encoding="utf-8-sig") as f:
        writer = csv.DictWriter(f, fieldnames=FIELDS)
        writer.writeheader()
        writer.writerows(rows)
    file_sha256 = hashlib.sha256(csv_path.read_bytes()).hexdigest()
    manifest = {
        "classification": "SYNTHETIC_RESEARCH_ONLY",
        "not_approved_history": True,
        "not_final_bid": True,
        "not_real_project": True,
        "engineering_calibrated": False,
        "do_not_import_to": ["project_master", "historical_transaction", "final_bid_item"],
        "seed": args.seed, "scenario": args.scenario,
        "cases_per_combo": args.cases_per_combo,
        "independent_cases": 6 * args.cases_per_combo,
        "material_rows": len(rows), "case_groups": dict(Counter(
            f"{r['project_type']}_{r['structure_type']}" for r in rows if r["material"] == "鋼筋"
        )), "file": csv_path.name, "sha256": file_sha256,
        "formula_note": "quantity=area * scenario baseline * design/floor factor * company factor * lognormal noise * outlier; numeric baselines are hypothetical",
    }
    atomic_write(path / f"synthetic_{args.scenario}_seed{args.seed}.manifest.json",
                 json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")
    print(f"{csv_path} | cases={manifest['independent_cases']} rows={len(rows)} sha256={file_sha256}")

if __name__ == "__main__":
    main()
