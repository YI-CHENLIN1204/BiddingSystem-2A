"""Read-only replay for issue #3. Writes paired rows and aggregate evidence."""
import argparse
import csv
import hashlib
import json
import random
import subprocess
from collections import Counter, defaultdict
from pathlib import Path

MANIFEST = "e04d7fa10e012825561031befff1f9d7f63b6e88272c9e528eb523fdcab6bfd0"
EXCLUDED = "FAC-20261122-SC-0052"


def read(source, name):
    with (source / name).open(encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


def main(source, out):
    manifest = (source / "manifest.json").read_bytes()
    assert hashlib.sha256(manifest).hexdigest() == MANIFEST
    for name, digest in json.loads(manifest)["files_sha256"].items():
        assert hashlib.sha256((source / name).read_bytes()).hexdigest() == digest, name

    all_rows = read(source, "project_boq_rows.csv")
    # Frozen specs embed generated scenario AND the truth token. Remove both from
    # the strict experiment. The proxy sensitivity retains only the scenario.
    def masked_spec(row, keep_scenario=False):
        parts = row["spec"].split("/")
        if len(parts) == 3 and parts[0] == "合成情境":
            return parts[1] if keep_scenario else ""
        return row["spec"]
    catalog = {r["material_code"]: r["unit"] for r in read(source, "material_catalog.csv")}
    materials = {code: i for i, code in enumerate(sorted(catalog), 1)}
    units = {unit: i for i, unit in enumerate(sorted(set(catalog.values())), 1)}
    legacy = set()
    qualified = defaultdict(set)
    for row in all_rows:
        if row["split"] != "train":
            continue
        name, spec, code = (row[k].strip() for k in ("item_name", "spec", "material_code"))
        legacy.add((name, code))
        qualified[name + " | " + masked_spec(row, True)].add((code, row["price_scope"], row["synthetic_project_id"]))
    # Simulate confirmed exact aliases with unambiguous material-only train records.
    # The generated specification strings are synthetic, with no external approval implied.
    reviewed = {(key, next(iter(labels))[0]) for key, labels in qualified.items()
                if len({(code, scope) for code, scope, _ in labels}) == 1
                and next(iter(labels))[1] == "MaterialOnly"
                and len({source_id for _, _, source_id in labels}) >= 2}
    targets = [r for r in all_rows if r["split"] == "test" and r["synthetic_project_id"] != EXCLUDED]
    assert len(targets) == 3386
    out.mkdir(parents=True, exist_ok=False)
    project = Path(__file__).resolve().parents[1] / "tests/WorkbookParserTests/WorkbookParserTests.csproj"
    subprocess.run(["dotnet", "build", str(project), "-c", "Release", "-p:NuGetAudit=false"], check=True)

    def evaluate(label, examples, terms):
        request = {
            "Dictionary": [{"Keyword": key, "MaterialId": materials[code],
                            "UnitId": units[catalog[code]], "IsActive": True}
                           for key, code in sorted(terms)],
            "Rows": [{"Id": str(i), "Name": r["item_name"], "Spec": r["spec"], "Unit": r["unit"],
                      "Quantity": float(r["quantity"]), "Category": r["major_category"],
                      "Amount": float(r["amount"]), "UnitPrice": float(r["unit_price"])} for i, r in enumerate(examples)],
            "Units": units,
        }
        request_path, result_path = (out / f"{label}-{suffix}.json" for suffix in ("input", "result"))
        request_path.write_text(json.dumps(request, ensure_ascii=False), encoding="utf-8")
        subprocess.run(["dotnet", "run", "--project", str(project), "-c", "Release", "--no-build",
                        "--", "--material-replay", str(request_path), str(result_path)], check=True)
        outputs = json.loads(result_path.read_text(encoding="utf-8"))
        assert len(outputs) == len(examples)
        rows = []
        for i, (source_row, result) in enumerate(zip(examples, outputs, strict=True)):
            assert result["Id"] == str(i)
            rows.append(dict(project=source_row["synthetic_project_id"], line=source_row["synthetic_line_id"],
                             case=source_row.get("case", "frozen"), spec=source_row["spec"],
                             unit=source_row["unit"], scope=source_row["price_scope"],
                             structure=source_row["structure_type"],
                             name_seen=source_row["item_name"] in legacy_names,
                             truth=materials[source_row["material_code"]], **result))
        with (out / f"{label}-paired.csv").open("w", encoding="utf-8-sig", newline="") as stream:
            writer = csv.DictWriter(stream, fieldnames=rows[0])
            writer.writeheader()
            writer.writerows(rows)
        return rows

    def summarize(rows, key):
        n = len(rows)
        auto = sum(r[key] is not None for r in rows)
        id_correct = sum(r[key] == r["truth"] for r in rows)
        quantity_safe = sum(r[key] == r["truth"] and r["scope"] == "MaterialOnly" for r in rows)
        return dict(n=n, auto=auto, pending=n-auto, auto_coverage=auto/n,
                    review_rate=(n-auto)/n,
                    material_id=dict(correct_auto=id_correct, wrong_auto=auto-id_correct,
                                     precision=id_correct/auto if auto else None),
                    pure_material_quantity_safety=dict(safe_auto=quantity_safe, unsafe_auto=auto-quantity_safe,
                                                       precision=quantity_safe/auto if auto else None))

    def compare(rows):
        return dict(before=summarize(rows, "Before"), after=summarize(rows, "After"),
                    correct_material_id_auto_to_pending=sum(r["Before"] == r["truth"] and r["After"] is None for r in rows))

    legacy_names = {name for name, _ in legacy}
    strict_targets = [dict(r, spec=masked_spec(r)) for r in targets]
    proxy_targets = [dict(r, spec=masked_spec(r, True)) for r in targets]
    frozen = evaluate("frozen-strict", strict_targets, legacy | reviewed)
    qualification = evaluate("frozen-scenario-proxy", proxy_targets, legacy | reviewed)
    # Start from held-out material-only rows with train-confirmed qualifiers.
    controls = [r for r in proxy_targets if r["material_code"] in {"SYN-REBAR", "SYN-CONCRETE"}
                and (r["item_name"] + " | " + r["spec"], r["material_code"]) in reviewed][:156]
    assert len(controls) == 156
    def alternate_material(row):
        return "SYN-CONCRETE" if row["material_code"] == "SYN-REBAR" else "SYN-REBAR"
    stress = []
    for r in controls:
        for label, changes in (("unqualified_control", {}),
                               ("identical_visible_other_material", {"material_code": alternate_material(r)})):
            stress.append(dict(r, case=label) | changes)
    unqualified = evaluate("hidden-ambiguity", stress, legacy)
    assert all(r["After"] is None for r in unqualified)
    stress = []
    for r in controls:
        for label, changes in (("control", {}), ("different_spec", {"spec": "SD490 / UNVERIFIED"}),
                               ("wrong_unit", {"unit": catalog[alternate_material(r)]}), ("unknown_alias", {"item_name": "未見工項"})):
            stress.append(dict(r, case=label) | changes)
    guarded = evaluate("qualified-boundaries", stress, legacy | reviewed)
    assert all((r["After"] == r["truth"]) if r["case"] == "control" else r["After"] is None for r in guarded)

    groups = defaultdict(list)
    for r in qualification:
        groups[r["project"]].append(r)
    rng = random.Random(20261123)
    samples = []
    for _ in range(2000):
        chosen = [r for group in rng.choices(list(groups.values()), k=len(groups)) for r in group]
        samples.append((sum(r["After"] is not None for r in chosen)
                        - sum(r["Before"] is not None for r in chosen))/len(chosen))
    samples.sort()
    strata = []
    for field in ("spec", "unit", "scope", "structure", "name_seen", "Code"):
        for value in sorted({r[field] for r in qualification}, key=str):
            strata.append(dict(field=field, value=value, **compare([r for r in qualification if r[field] == value])))
    boundary = unqualified + guarded
    train = [r for r in all_rows if r["split"] == "train"]
    assert len(train) == 16786 and len({r["synthetic_project_id"] for r in train}) == 126
    draft = evaluate("train-draft-legacy-only", [dict(r, spec=masked_spec(r)) for r in train], legacy)
    quantity_candidate_ids = {materials["SYN-REBAR"], materials["SYN-CONCRETE"]}
    scope = [r for r, source_row in zip(draft, train, strict=True)
             if r["Before"] in quantity_candidate_ids and source_row["major_category"] == "結構體工程"
             and source_row["unit"] == catalog[source_row["material_code"]]
             and float(source_row["quantity"]) > 0]
    groups_pending = Counter((r["project"], source_row["item_name"], r["spec"], r["unit"], r["Candidate"])
                             for r, source_row in zip(qualification, targets, strict=True)
                             if r["After"] is None and r["Code"] == "UNVERIFIED_SPEC"
                             and r["Candidate"] is not None)
    def by_scope(rows):
        result = {}
        for scope_name in ("MaterialOnly", "SupplyAndInstall"):
            subset = [r for r in rows if r["scope"] == scope_name]
            after = summarize(subset, "After")
            after.update(price_eligible=sum(r["PriceEligible"] for r in subset),
                         price_eligible_rate=sum(r["PriceEligible"] for r in subset)/len(subset),
                         explicit_composite=sum(r["ExplicitComposite"] for r in subset))
            if scope_name == "SupplyAndInstall":
                after["quantity_exclusion_rate"] = after["pending"]/len(subset)
            result[scope_name] = after
        return result
    result = dict(manifest_sha256=MANIFEST,
                  recorded_round2=dict(n=3386, safe_auto=207, material_only_n=207, composite_n=3179),
                  label_masked_strict=by_scope(frozen),
                  label_masked_scenario_sensitivity=by_scope(qualification),
                  simulated_draft=dict(projects=126, rows=len(draft),
                                       legacy_auto_blocked=sum(r["Before"] is not None and r["After"] is None for r in draft),
                                       estimated_quantity_scope_before=len(scope),
                                       estimated_quantity_scope_after=sum(r["After"] is not None for r in scope)),
                  workflow_estimate=dict(pending_rows=sum(r["After"] is None for r in qualification),
                                         possible_candidate_rows=sum(groups_pending.values()),
                                         minimum_same_project_prefills=len(groups_pending),
                                         maximum_prefill_savings=sum(groups_pending.values())-len(groups_pending),
                                         required_confirmation_per_row=True), legacy_terms=len(legacy), qualified_train_aliases=len(reviewed),
                  frozen_legacy=compare(frozen), frozen_reviewed=compare(qualification),
                  coverage_delta_project_bootstrap_95=[samples[49], samples[1949]],
                  projects=len(groups), strata=strata, pressure={
                      case: compare([r for r in boundary if r["case"] == case])
                      for case in sorted({r["case"] for r in boundary})})
    (out / "summary.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({k: v for k, v in result.items() if k != "strata"}, ensure_ascii=False))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    main(args.source.resolve(), args.output.resolve())
