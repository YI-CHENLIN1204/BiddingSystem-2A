"""Audit the frozen factory package and replay fixed-interval boundary cases."""
import argparse
import csv
import hashlib
import json
import math
from collections import Counter
from datetime import date
from decimal import Decimal
from pathlib import Path

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def read(path):
    with path.open(encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))

def write(path, rows):
    with path.open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, rows[0].keys())
        writer.writeheader()
        writer.writerows(rows)

def audit(source, comparisons, out):
    out.mkdir(parents=True, exist_ok=False)
    manifest_path = source / "manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    assert sha(manifest_path) == "e04d7fa10e012825561031befff1f9d7f63b6e88272c9e528eb523fdcab6bfd0"
    assert manifest["data_origin"] == "SYNTHETIC"
    for name, expected in manifest["files_sha256"].items():
        assert sha(source / name) == expected, name
    project_rows = read(source / "projects.csv")
    projects = {r["synthetic_project_id"]: r for r in project_rows}
    assert len(projects) == len(project_rows) == manifest["project_count"]
    assert Counter(r["split"] for r in project_rows) == manifest["split_counts"]
    boq = read(source / "project_boq_rows.csv")
    historical = read(source / "historical_transactions.csv")
    assert len(boq) == len(historical) == manifest["boq_line_count"]
    assert len({r["synthetic_line_id"] for r in boq}) == len(boq)
    boq_tx = {r["synthetic_transaction_id"]: r for r in boq}
    hist_tx = {r["synthetic_transaction_id"]: r for r in historical}
    assert len(boq_tx) == len(hist_tx) == len(boq) and boq_tx.keys() == hist_tx.keys()
    line_counts = Counter()
    for key, r in boq_tx.items():
        project = projects[r["synthetic_project_id"]]
        assert r["split"] == project["split"] and r["data_origin"] == "SYNTHETIC"
        for field in ("synthetic_project_id", "material_code", "unit", "unit_price", "split"):
            assert r[field] == hist_tx[key][field], (key, field)
        assert Decimal(r["quantity"]) == Decimal(hist_tx[key]["actual_quantity"])
        date.fromisoformat(hist_tx[key]["transaction_date"])
        assert all(Decimal(r[field]) > 0 for field in ("quantity", "unit_price", "amount"))
        line_counts[r["synthetic_project_id"]] += 1
    write(out / "t00-projects.csv", [dict(project=k, split=r["split"], structure=r["structure_type"],
          start_date_proxy=r["start_date"], rows=line_counts[k], data_origin=r["data_origin"])
          for k, r in sorted(projects.items())])
    refs, stress = [], []
    for regime, path in comparisons:
        for row in read(path):
            target, scope = row["project"], row["scope"]
            project = projects[target]
            keys = row["refs"].split("|")
            assert len(keys) == len(set(keys)) == len(row["weights"].split("|"))
            assert project["split"] == "test" and target not in keys
            for reference in keys:
                p = projects[reference]
                assert p["split"] == "train"
                later = date.fromisoformat(p["start_date"]) >= date.fromisoformat(project["start_date"])
                assert regime != "earlier" or not later
                refs.append(dict(regime=regime, target=target, scope=scope, reference=reference,
                                 reference_split=p["split"], target_date=project["start_date"],
                                 reference_date=p["start_date"], same_or_later=int(later)))
            if regime != "earlier":
                continue
            for method in ("a0", "a1"):
                low, high = (float(row[f"{method}_{bound}"]) for bound in ("low", "high"))
                assert math.isfinite(low) and math.isfinite(high) and 0 <= low <= high
                for label, number, expected in (
                    ("lower-edge", low, "inside"), ("upper-edge", high, "inside"),
                    ("just-below", math.nextafter(low, -math.inf), "below"),
                    ("just-above", math.nextafter(high, math.inf), "above"),
                    ("half-of-truth", float(row["truth"])*.5, None),
                    ("double-truth", float(row["truth"])*2, None)):
                    actual = "below" if number < low else "above" if number > high else "inside"
                    assert expected is None or actual == expected
                    stress.append(dict(project=target, structure=project["structure_type"], scope=scope, method=method,
                                       scenario=label, value=number, lower=low, upper=high, direction=actual))
    write(out / "t00-reference-membership.csv", refs)
    write(out / "t06-boundary-cases.csv", stress)
    summary = dict(manifest_sha256=sha(manifest_path), source_sha256=manifest["files_sha256"],
                   project_splits=dict(Counter(r["split"] for r in project_rows)), boq_lines=len(boq),
                   refs={regime:dict(total=sum(r["regime"] == regime for r in refs),
                                    same_or_later=sum(r["same_or_later"] for r in refs if r["regime"] == regime))
                         for regime, _ in comparisons},
                   input_sha256={regime:sha(p) for regime,p in comparisons},
                   stress_rows=len(stress), boundary_checks=sum(r["scenario"] in
                       ("lower-edge","upper-edge","just-below","just-above") for r in stress))
    (out / "summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(summary))

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    for argument in ("source", "retrospective", "earlier", "output"):
        parser.add_argument(argument, type=Path)
    a = parser.parse_args()
    audit(a.source, [("retrospective", a.retrospective), ("earlier", a.earlier)], a.output)
