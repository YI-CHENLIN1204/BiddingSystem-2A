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

# Additional nonstructural disciplines broaden the per-project historical
# line universe. These descriptive templates are not extracted enterprise costs.
ADDITIONAL_ITEMS = [
    ("工地出入口警示與告示牌", "組", "假設工程"),
    ("施工臨時排水及沉砂設施", "式", "假設工程"),
    ("工地臨時照明與配電管線", "m", "假設工程"),
    ("地下室抽排水及集水井施作", "座", "基礎工程"),
    ("基礎開挖土方外運含處理", "m3", "土方工程"),
    ("基礎回填級配及分層夯實", "m3", "土方工程"),
    ("地下室止水鋼板與施工縫處理", "m", "防水工程"),
    ("柱梁版普通模板組立拆除", "m2", "結構體工程"),
    ("構造物植筋固定及接著材料", "處", "結構體工程"),
    ("輕質隔間磚砌築含勾縫", "m2", "泥作工程"),
    ("外牆粉刷底層打底整平", "m2", "泥作工程"),
    ("室內地坪水泥砂漿找平", "m2", "泥作工程"),
    ("陽台地坪洩水坡度處理", "m2", "泥作工程"),
    ("外牆防水塗膜兩道施工", "m2", "防水工程"),
    ("窗框四周填縫與防水封膠", "m", "防水工程"),
    ("浴廁牆地防水及試水", "m2", "防水工程"),
    ("屋頂女兒牆泛水與伸縮縫", "m", "防水工程"),
    ("鋁合金推射窗含五金", "樘", "門窗工程"),
    ("氣密固定玻璃窗組裝", "樘", "門窗工程"),
    ("雙扇防火門含閉門器", "樘", "門窗工程"),
    ("梯廳不鏽鋼門框及包邊", "樘", "金屬工程"),
    ("外牆鋁板包覆及骨架", "m2", "外牆工程"),
    ("牆面乳膠漆二底二度", "m2", "裝修工程"),
    ("公共區域石膏板輕鋼架天花", "m2", "裝修工程"),
    ("樓梯止滑條及踏面收邊", "m", "裝修工程"),
    ("室內木作踢腳板及收口", "m", "裝修工程"),
    ("大廳石材地坪及勾縫", "m2", "裝修工程"),
    ("廚房檯面與櫥櫃系統", "組", "裝修工程"),
    ("衛浴洗面盆龍頭及給水接頭", "組", "給排水工程"),
    ("廁所馬桶設備及安裝", "組", "給排水工程"),
    ("雨水立管與落水頭系統", "m", "給排水工程"),
    ("給水加壓泵浦及控制盤", "組", "給排水工程"),
    ("污水提升泵浦及閥件", "組", "給排水工程"),
    ("受電盤母線與主開關系統", "組", "電氣工程"),
    ("低壓電纜橋架與支撐", "m", "電氣工程"),
    ("接地網路及接地測試點", "點", "電氣工程"),
    ("樓梯間緊急照明燈具", "只", "電氣工程"),
    ("火警受信總機與控制盤", "組", "消防工程"),
    ("消防警報探測器及底座", "只", "消防工程"),
    ("消防水帶箱與消防栓", "組", "消防工程"),
    ("自動灑水頭與支管配設", "只", "消防工程"),
    ("排煙風機與排煙閘門", "組", "消防工程"),
    ("空調風管製作保溫與安裝", "m2", "空調工程"),
    ("室外空調冷凝機組", "組", "空調工程"),
    ("新風機與風量控制設備", "組", "空調工程"),
    ("網路資訊配線及端接", "m", "弱電工程"),
    ("門禁讀卡機及控制器", "組", "弱電工程"),
    ("監視攝影機及網路設備", "組", "弱電工程"),
    ("公共廣播擴大機與揚聲器", "組", "弱電工程"),
    ("電梯機坑與導軌設備安裝", "組", "電梯工程"),
    ("電梯車廂裝修與控制面板", "組", "電梯工程"),
    ("基地庭園植栽客土及支架", "m2", "景觀工程"),
    ("戶外排水側溝與鑄鐵蓋", "m", "景觀工程"),
    ("室外道路鋪面與路緣石", "m2", "景觀工程"),
    ("垃圾暫存室通風與排水", "組", "雜項工程"),
    ("地下停車場車道反光標示", "m", "雜項工程"),
]

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
    "RC": [("SYN-REBAR", "SD420W 竹節鋼筋 D19 材料", "kg", "結構體工程", 65.0),
           ("SYN-CONCRETE", "預拌混凝土 f'c=280（結構體）", "m3", "結構體工程", .34)],
    "SC": [("SYN-STEEL", "H型結構鋼骨製作及吊裝", "kg", "結構體工程", 54.0),
           ("SYN-CONCRETE", "樓版與基礎預拌混凝土 f'c=280", "m3", "結構體工程", .22)],
    # Experimental SRC fixture: three material families, independently
    # parameterized. This is NOT a quantity standard or a new approved model.
    "SRC": [("SYN-STEEL", "SRC 鋼骨構材示意", "kg", "結構體工程", 35.0),
            ("SYN-REBAR", "SRC 鋼筋材料示意", "kg", "結構體工程", 42.0),
            ("SYN-CONCRETE", "SRC 包覆及樓版混凝土示意", "m3", "結構體工程", .30)],
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
    for n, (name, unit, category) in enumerate(EXTRA_ITEMS + ADDITIONAL_ITEMS, 1):
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
    # Synthetic site footprint: total floor area and floor count are
    # related, but not identical. This is NOT an architectural calculation.
    floor_area = float(project["total_floor_area"])
    floors = int(project["floor_count"])
    basements = int(project["basement_floor_count"])
    footprint_ratio = rng.uniform(.55, .83)
    footprint = floor_area / max(2.5, floors + basements * .65)
    project["site_area"] = f"{max(100.0, footprint / footprint_ratio):.2f}"
    project["facade_system"] = rng.choice(("paint", "tile", "stone"))
    project["finish_level"] = rng.choice(("basic", "standard", "premium"))
    project["foundation_system"] = ("deep" if int(project["basement_floor_count"]) >= 2
                                    else "shallow")
    project["site_condition"] = rng.choice(("normal", "restricted", "complex"))
    return project


# Deliberate edge cases for independent robustness validation, not realistic engineering norms.
# Allocate the same scenario families across train, validation and test; never
# re-label a held-out project as training data.
EXTREME_CASES = (
    ("tiny_lowrise", "SRC", 3, 0, 1200.0, 550.0, "臺中市", None),
    ("large_highrise", "RC", 55, 5, 85000.0, 10000.0, "桃園市", None),
    ("deep_basement", "SC", 13, 7, 24000.0, 4700.0, "高雄市", None),
    ("constrained_site", "SRC", 25, 3, 32000.0, 620.0, "臺南市", None),
    ("wide_lowrise", "RC", 4, 0, 9000.0, 19000.0, "臺中市", None),
    ("older_remote_city", "SC", 11, 2, 15000.0, 2500.0, "花蓮縣", "2006-06-15"),
)


def apply_extreme_projects(projects):
    by_split = defaultdict(list)
    for project in projects:
        by_split[project["split"]].append(project)
    if any(len(by_split[split]) < len(EXTREME_CASES) for split in ("train", "validation", "test")):
        raise ValueError("include_extremes requires at least six projects in each split (try --count 120)")
    for split in ("train", "validation", "test"):
        for project, scenario in zip(by_split[split], EXTREME_CASES):
            name, structure, floors, basements, area, site, city, start_date = scenario
            project.update(structure_type=structure, floor_count=floors,
                           basement_floor_count=basements, total_floor_area=f"{area:.2f}",
                           city=city, extreme_scenario=name)
            if start_date:
                project["start_date"] = start_date
            project["extreme_site_area"] = site
    return projects


def generate_full(count, seed, include_src=False, include_extremes=False):
    if not 20 <= count <= 10000:
        raise ValueError("count must be between 20 and 10000")
    if include_extremes and not include_src:
        raise ValueError("include_extremes requires include_src")
    projects, _ = make_projects(count, seed)
    if include_src:
        # Opt-in and deterministic: preserve all existing RC/SC v2 fixtures.
        # No database import; SRC must pass held-out verification first.
        for index, project in enumerate(projects):
            if index % 3 == 2:
                project["structure_type"] = "SRC"
    if include_extremes:
        projects = apply_extreme_projects(projects)
    rng = random.Random(seed + 24791)
    projects = [enrich_project(p, rng) for p in projects]
    if include_extremes:
        for project in projects:
            if "extreme_site_area" in project:
                project["site_area"] = f'{project.pop("extreme_site_area"):.2f}'
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
            take_probability = (1.0 if include_src and code in ("SYN-REBAR", "SYN-CONCRETE", "SYN-STEEL")
                                else .94 if code in ("SYN-REBAR", "SYN-CONCRETE", "SYN-STEEL") else .82)
            if project_rng.random() > take_probability:
                continue
            repeats = (project_rng.randint(1, 3) if unit in ("式", "座", "點") else
                       project_rng.randint(2, 6) + min(5, floor_count // 5))
            # Total quantity per template is controlled by area, scope and
            # synthetic design settings; split over multiple line items.
            design = (1.0 + .08 * basement + .035 * (floor_count / 10) +
                      (.11 if project["site_condition"] == "complex" else 0) +
                      (.075 if project["finish_level"] == "premium" else 0))
            # Vary engineering trades within a project; never use the same
            # m2 coefficient across all work items as an engineering claim.
            trade_factor = .55 + (template_index % 11) * .085
            group_total = max(.1, area * coeff * design * trade_factor *
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
                # Region/time parameters are only scenario variation, not
                # recorded market prices or an inflation index.
                region_factor = {"臺中市":1.03,"臺南市":.97,"高雄市":1.00,"桃園市":1.04}.get(project["city"],1.0)
                age_factor = 1.0 + .012*(date.fromisoformat(project["start_date"]).year-2016)
                unit_price = round(project_rng.uniform(*band)*region_factor*age_factor, 2)
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
                duration = 210 + floor_count * 23 + basement * 35
                phase = (.08 if category in ("假設工程", "土方工程", "基礎工程")
                         else .34 if category in ("結構體工程", "泥作工程")
                         else .60 if category in ("防水工程", "屋頂工程", "門窗工程", "外牆工程")
                         else .75 if category in ("電氣工程", "給排水工程", "消防工程", "空調工程", "弱電工程")
                         else .88)
                transaction_date = date.fromisoformat(project["start_date"]) + timedelta(
                    days=max(1,int(duration*phase)+project_rng.randint(-16,16)))
                history.append({
                    "synthetic_transaction_id": txn, "synthetic_project_id": pid,
                    "material_code": code, "material_name": rendered, "unit": unit,
                    "actual_quantity": record["quantity"], "unit_price": record["unit_price"],
                    "transaction_date": transaction_date.isoformat(),
                    "data_origin": "SYNTHETIC",
                    "synthetic_label": "injected_anomaly" if group_anomaly else "baseline",
                    "split": project["split"]})
    return projects, history, detail


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--count", type=int, default=300)
    parser.add_argument("--seed", type=int, default=20261119)
    parser.add_argument("--include-src", action="store_true",
                        help="opt-in experimental SRC fixtures; use NEW output directory")
    parser.add_argument("--include-extremes", action="store_true",
                        help="opt-in 6 tagged edge cases per train/validation/test split; requires --include-src")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args(argv)
    if args.output.exists() and any(args.output.iterdir()):
        parser.error("output directory must be empty (refusing overwrite)")
    try:
        projects, histories, lines = generate_full(args.count, args.seed,
            include_src=args.include_src, include_extremes=args.include_extremes)
    except ValueError as exc:
        parser.error(str(exc))
    args.output.mkdir(parents=True, exist_ok=True)
    extended = PROJECT_FIELDS + ["site_area", "facade_system", "finish_level", "foundation_system", "site_condition"]
    if args.include_extremes:
        extended.append("extreme_scenario")
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
        "experimental_src_enabled": args.include_src,
        "extreme_cases_enabled": args.include_extremes,
        "extreme_scenario_counts": dict(Counter(p.get("extreme_scenario", "") for p in projects if p.get("extreme_scenario"))),
        "extreme_cases_per_split": dict(Counter(p["split"] for p in projects if p.get("extreme_scenario"))),
        "structure_type_counts": dict(Counter(p["structure_type"] for p in projects)),
        "min_lines_per_project": min(counts.values()),
        "max_lines_per_project": max(counts.values()),
        "limitations": [
            "Quantities, design choices, prices and site conditions are synthetic, NOT measured construction facts.",
            "SRC templates are research-only: no automatic production scope/traffic-light expansion.",
            "Extreme cases are deliberate synthetic stress tests, NOT plausible benchmark estimates; their flags must not leak into model features.",
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
