import argparse
import csv
import hashlib
import json
import unicodedata
from collections import defaultdict
from pathlib import Path


def normalize(name):
    return unicodedata.normalize("NFKC", name).strip().casefold()


def main():
    parser = argparse.ArgumentParser(description="Replay the factory exact-name dictionary baseline.")
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    manifest_bytes = (args.source / "manifest.json").read_bytes()
    manifest = json.loads(manifest_bytes)
    for name, expected in manifest["files_sha256"].items():
        if hashlib.sha256((args.source / name).read_bytes()).hexdigest() != expected:
            raise ValueError(f"Source hash mismatch: {name}")
    with (args.source / "project_boq_rows.csv").open(encoding="utf-8-sig") as source:
        rows = list(csv.DictReader(source))
    splits = defaultdict(set)
    words = defaultdict(lambda: defaultdict(set))
    for row in rows:
        project = row["synthetic_project_id"]
        split = row["split"]
        splits[split].add(project)
        if split == "train":
            words[normalize(row["item_name"])][row["material_code"]].add(project)
    if {k: len(v) for k, v in splits.items()} != {"train": 126, "validation": 27, "test": 27}:
        raise ValueError("Unexpected factory split")
    if len(set.union(*splits.values())) != 180:
        raise ValueError("Project ID overlap between splits")
    ranked = sorted((-len(next(iter(values.values()))), name, next(iter(values)))
                    for name, values in words.items() if len(values) == 1)
    excluded = "FAC-20261122-SC-0052"
    if excluded not in splits["test"]:
        raise ValueError("Expected diagnostic case missing from test")
    args.output.mkdir(parents=True, exist_ok=False)
    summary = []
    with (args.output / "mapping_per_item.csv").open("w", encoding="utf-8-sig", newline="") as file:
        writer = csv.writer(file)
        writer.writerow(["k", "split", "project", "line_id", "item_name", "matched_key", "truth", "prediction", "status"])
        for k in sorted({0, 10, 30, 50, 100, len(ranked)}):
            if k > len(ranked):
                continue
            dictionary = {name: code for _, name, code in ranked[:k]}
            for split in ("validation", "test"):
                total = correct = wrong = unseen = 0
                for row in rows:
                    project = row["synthetic_project_id"]
                    if row["split"] != split or project == excluded:
                        continue
                    name = normalize(row["item_name"])
                    truth = row["material_code"]
                    predicted = dictionary.get(name)
                    status = "pending" if predicted is None else "correct" if predicted == truth else "wrong"
                    total += 1
                    correct += status == "correct"
                    wrong += status == "wrong"
                    unseen += name not in words
                    writer.writerow([k, split, project, row["synthetic_line_id"], row["item_name"],
                                     name if predicted else "", truth, predicted, status])
                summary.append(dict(k=k, split=split, total=total, correct=correct, wrong=wrong,
                                    pending=total-correct-wrong, unseen=unseen))
    with (args.output / "dictionary_growth.csv").open("w", encoding="utf-8-sig", newline="") as file:
        writer = csv.DictWriter(file, fieldnames=list(summary[0]))
        writer.writeheader()
        writer.writerows(summary)
    (args.output / "run_manifest.json").write_text(json.dumps({
        "source_manifest_sha256": hashlib.sha256(manifest_bytes).hexdigest(),
        "program_sha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
        "source_files_sha256": manifest["files_sha256"], "data_origin": "SYNTHETIC",
        "excluded_test_project": excluded, "normalization": "NFKC, strip, casefold",
        "train_ambiguous_names": sum(len(values) > 1 for values in words.values()),
    }, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
