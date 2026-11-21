#!/usr/bin/env python3
"""Generate coherent, project-specific synthetic BOQ lines AND matching history.

No real project values, no database calls. This is a stress-test data generator,
not an engineering bill of quantities or empirically validated estimate.
"""
import argparse
import csv
import hashlib
import json
import math
import random
from collections import Counter, defaultdict
from datetime import date, timedelta
from pathlib import Path

from generate import generate as make_projects, write_csv, PROJECT_FIELDS, HISTORY_FIELDS
from generate_irregular_excel import EXTRA_ITEMS

BOQ_FIELDS = ["synthetic_line_id", "synthetic_transaction_id", "synthetic_project_id",
              "material_code", "major_category", "item_name", "spec", "unit",
              "quantity", "unit_price", "amount", "work_zone", "floor_label",
              "structure_type", "data_origin", "split"]
# These are illustrative amount-per-floor-area coefficients, NOT construction norms.
# Use distinct units only within a code, never combine kg, m2, m3, or lump sums.
UNIT_COEFF = {"m": .25, "m2": .43, "m3": .036, "kg": 14.0,
              "樘": .008, "組": .005, "式": .00032, "處": .016,
              "只": .028, "點": .003, "座": .0009}
UNIT_PRICE_BAND = {"m": (80, 2300), "m2": (120, 3300), "m3": (500, 4500),
                   "kg": (20, 120), "樘": (2500, 25000), "組": (1500, 35000),
                   "式": (7000, 90000), "處": (450, 7000), "只": (350, 5000),
                   "點": (1100, 9000), "座": (2500, 28000)}
CATEGORIES = {
    "臨時工程": "假設工程", "基礎": "基礎工程", "結構": "結構體工程"
}
CORE = {
    "RC": [("SYN-REBAR", "SD420W 竹節鋼筋及加工綁紮", "kg", "結構體工程", 65.0),
           ("SYN-CONCRETE", "預拌混凝土 f'c=280（結構體）", "m3", "結構體工程", .34)],
    "SC": [("SYN-STEEL", "H型結構鋼骨製作及吊裝", "kg", "結構體工程", 54.0),
           ("SYN-CONCRETE", "樓版與基礎預拌混凝土 f'c=280", "m3", "結構體工程", .22)],
}
UNDERGROUND = ("地下", "基坑", "連續壁", "擋土", "筏式")
FINISH_KEYWORDS = ("磁磚", "天花板", "油漆", "地坪耐磨", "隔間")
FACADE_KEYWORDS = ("石材乾掛", "鋁窗", "外牆防水", "外牆")
WORK_ZONES = ("A棟", "B棟", "公共空間", "屋頂", "戶外", "機電機房")
SPECS = ("依設計圖施工", "含材料工資與施工", "含配件及測試",
         "標準規格", "耐候規格", "防火規格")


def choose_templates(project):
    basement = int(project["basement_floor_count"])
    structure = project["structure_type"]
    floors = int(project["floor_count"])
    candidates = []
    for n, (name, unit, category) in enumerate(EXTRA_ITEMS, 1):
        if basement == 0 and any(word in name for word in UNDERGROUND):
            continue
        if floors <= 6 and ("電梯" in name or "弱電線槽與機櫃" in name):
            continue
        if structure == "SC" and ("鋁模" in name or "普通模板" in name):
            continue
        if project["facade_system"] == "paint" and "石材乾掛" in name:
            continue
        if project["facade_system"] == "stone" and "外牆防水底塗" in name:
            continue
        if project["finish_level"] == "basic" and "石材乾掛" in name:
            continue
        candidates.append((f"SYN-WORK-{n:03d}", name, unit, category,
                           UNIT_COEFF.get(unit, .04)))
    return CORE[structure] + candidates


def enrich_project(project, rng):
    project = dict(project)
    project["facade_system"] = rng.choice(("paint", "tile", "stone"))
    project["finish_level"] = rng.choice(("basic", "standard", "premium"))
    project["foundation_system"] = ("deep" if int(project["basement_floor_count"]) >= 2
                                    else "shallow")
    project["site_condition"] = rng.choice(("normal", "restricted", "complex"))
    return project


def generate_full(count, seed):
    if not 20 <= count <= 10000:
        raise ValueError("count must be between 20 and 10000")
    projects, _ = make_projects(count, seed)
    rng = random.Random(seed + 24791)
    projects = [enrich_project(p, rng) for p in projects]
    history, detail = [], []
    for project in projects:
        pid = project["synthetic_project_id"]
        project_rng = random.Random(f"{seed}:{pid}:full-boq-v2")
        area = float(project["total_floor_area"])
        floor_count = int(project["floor_count"])
        basement = int(project["basement_floor_count"])
        levels = [f"B{i}" for i in range(1, basement + 1)] + [
            f"{i}F" for i in range(1, floor_count + 1)] + ["RF"]
        templates = choose_templates(project)
        # Different buildings have different line counts, material families,
        # work scopes, locations and drawings. Do not repeat all 42 blindly.
        for template_index, (code, item_name, unit, category, coeff) in enumerate(templates):
            take_probability = .94 if code in ("SYN-REBAR", "SYN-CONCRETE", "SYN-STEEL") else .82
            if project_rng.random() > take_probability:
                continue
            repeats = (project_rng.randint(1, 3) if unit in ("式", "座", "點") else
                       project_rng.randint(2, 6) + min(5, floor_count // 5))
            # Total quantity per template is controlled by area, scope and
            # synthetic design settings; split over multiple line items.
            design = (1.0 + .08 * basement + .035 * (floor_count / 10) +
                      (.11 if project["site_condition"] == "complex" else 0) +
                      (.075 if project["finish_level"] == "premium" else 0))
            group_total = max(.1, area * coeff * design *
                              project_rng.uniform(.76, 1.24))
            group_anomaly = project_rng.random() < .055
            if group_anomaly:
                group_total *= project_rng.choice((.45, 1.8))
            shares = [project_rng.uniform(.45, 1.55) for _ in range(repeats)]
            share_total = sum(shares)
            band = UNIT_PRICE_BAND.get(unit, (100, 3000))
            for index, share in enumerate(shares):
                serial = len(detail) + 1
                sid = f"BOQ-{pid}-{serial:06d}"
                txn = f"SYNTX-{pid}-{serial:06d}"
                q = max(.001, round(group_total * share / share_total, 3))
                unit_price = round(project_rng.uniform(*band), 2)
                level = levels[(index + template_index) % len(levels)]
                # Work zones need not be equivalent to specific buildings.
                zone = WORK_ZONES[(index + template_index) % len(WORK_ZONES)]
                suffix = SPECS[(index + template_index + project_rng.randrange(4)) % len(SPECS)]
                rendered = f"{item_name}－{zone}/{level}（{suffix}）"
                record = {"synthetic_line_id": sid, "synthetic_transaction_id": txn,
                          "synthetic_project_id": pid, "material_code": code,
                          "major_category": category, "item_name": rendered,
                          "spec": suffix, "unit": unit,
                          "quantity": f"{q:.3f}", "unit_price": f"{unit_price:.2f}",
                          "amount": f"{q*unit_price:.2f}", "work_zone": zone,
                          "floor_label": level, "structure_type": project["structure_type"],
                          "data_origin": "SYNTHETIC", "split": project["split"]}
                detail.append(record)
                history.append({
                    "synthetic_transaction_id": txn, "synthetic_project_id": pid,
                    "material_code": code, "material_name": rendered, "unit": unit,
                    "actual_quantity": record["quantity"], "unit_price": record["unit_price"],
                    "transaction_date": (date.fromisoformat(project["start_date"]) +
                                         timedelta(days=60)).isoformat(),
                    "data_origin": "SYNTHETIC",
                    "synthetic_label": "injected_anomaly" if group_anomaly else "baseline",
                    "split": project["split"]})
    return projects, history, detail


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--count", type=int, default=300)
    parser.add_argument("--seed", type=int, default=20261119)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args(argv)
    if args.output.exists() and any(args.output.iterdir()):
        parser.error("output directory must be empty (refusing overwrite)")
    projects, histories, lines = generate_full(args.count, args.seed)
    args.output.mkdir(parents=True, exist_ok=True)
    extended = PROJECT_FIELDS + ["facade_system", "finish_level", "foundation_system", "site_condition"]
    output = [("projects.csv", extended, projects),
              ("historical_transactions.csv", HISTORY_FIELDS, histories),
              ("project_boq_rows.csv", BOQ_FIELDS, lines)]
    for name, headers, records in output:
        write_csv(args.output / name, headers, records)
    digests = {name: hashlib.sha256((args.output / name).read_bytes()).hexdigest()
               for name, _, _ in output}
    counts = Counter(row["synthetic_project_id"] for row in lines)
    catalog = {x["material_code"] for x in histories}
    manifest = {
        "generator_version": "2.0.0", "data_origin": "SYNTHETIC", "seed": args.seed,
        "project_count": len(projects), "transaction_count": len(histories),
        "boq_line_count": len(lines), "files_sha256": digests,
        "distinct_material_codes": len(catalog),
        "min_lines_per_project": min(counts.values()),
        "max_lines_per_project": max(counts.values()),
        "limitations": [
            "Quantities, design choices, prices and site conditions are synthetic, NOT measured construction facts.",
            "All historical rows correspond 1:1 to project_boq_rows.csv, but they are not real executed transactions.",
            "Template collection is illustrative, not an exhaustive BOQ; no actual supplier quotations.",
            "Do not report synthetic model errors as real-world engineering accuracy.",
            "No database writes. Do not import into the official collaboration database."
        ]
    }
    (args.output / "manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"projects": len(projects), "history_rows": len(histories),
                      "boq_rows": len(lines), "material_codes": len(catalog),
                      "min_rows_per_project": manifest["min_lines_per_project"],
                      "max_rows_per_project": manifest["max_lines_per_project"],
                      "output": str(args.output)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
