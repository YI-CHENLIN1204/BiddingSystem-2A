"""Isolated SRC/extreme fixture and non-promotion contracts."""
import csv
import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from generate_full_project_dataset import (
    BOQ_FIELDS, HISTORY_FIELDS, PROJECT_FIELDS, generate_full, write_csv)
from export_src_holdout_suite import plan
from export_full_project_excel import export
from prepare_training_seed import prepare
from audit_candidate_scope import audit


class SrcExtendedTests(unittest.TestCase):
    def test_src_train_seed_and_heldout_export_without_promotion(self):
        with tempfile.TemporaryDirectory() as temp:
            source = Path(temp) / "v3"
            source.mkdir()
            projects, history, boq = generate_full(
                120, 20261119, include_src=True, include_extremes=True)
            fields = PROJECT_FIELDS + [
                "site_area", "facade_system", "finish_level",
                "foundation_system", "site_condition", "extreme_scenario"]
            for name, headers, rows in (
                ("projects.csv", fields, projects),
                ("historical_transactions.csv", HISTORY_FIELDS, history),
                ("project_boq_rows.csv", BOQ_FIELDS, boq),
            ):
                write_csv(source / name, headers, rows)
            hashes = {
                name: hashlib.sha256((source / name).read_bytes()).hexdigest()
                for name in ("projects.csv", "historical_transactions.csv",
                             "project_boq_rows.csv")}
            (source / "manifest.json").write_text(json.dumps({
                "generator_version": "2.0.0", "data_origin": "SYNTHETIC",
                "project_count": len(projects),
                "transaction_count": len(history),
                "boq_line_count": len(boq),
                "files_sha256": hashes,
            }), encoding="utf-8")
            seed = prepare(source, target_database="bidding2a_graduation_src")
            self.assertEqual(seed["target_database"], "bidding2a_graduation_src")
            self.assertTrue(all(x["split"] == "train" for x in seed["project_rows"]))
            self.assertEqual(len(seed["transaction_rows"]), seed["train_transaction_count"])
            self.assertTrue(any(x["structure_type"] == "SRC"
                                for x in seed["project_rows"]))
            with self.assertRaisesRegex(ValueError, "unapproved"):
                prepare(source, target_database="production")
            targets = plan(source)
            self.assertEqual(len(targets), 7)
            self.assertTrue(all(x["split"] == "test" for x in targets))
            self.assertEqual(len({x["synthetic_project_id"] for x in targets}), 7)
            self.assertEqual(targets[0]["structure_type"], "SRC")
            xlsx, truth = Path(temp) / "src.xlsx", Path(temp) / "src-truth.json"
            result = export(source, xlsx, truth, targets[0]["synthetic_project_id"])
            self.assertGreater(result["detail_rows"], 0)
            self.assertEqual(json.loads(truth.read_text(encoding="utf-8"))["project"]["split"],
                             "test")
            review = audit(source)
            self.assertGreater(review["candidate_count"], 0)
            self.assertTrue(all(not row["formal_red_green"]
                                for row in review["candidates"]))


if __name__ == "__main__":
    unittest.main()
