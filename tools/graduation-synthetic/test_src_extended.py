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
from export_anomaly_holdout_suite import plan as plan_anomaly
from export_full_project_excel import ANOMALY_SCENARIOS


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


    def test_labeled_anomalies_only_change_test_workbook_and_keep_amount_consistent(self):
        from decimal import Decimal
        with tempfile.TemporaryDirectory() as temp:
            source = Path(temp) / "src"
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
            filenames = ["projects.csv", "historical_transactions.csv", "project_boq_rows.csv"]
            hashes = {name: hashlib.sha256((source / name).read_bytes()).hexdigest()
                      for name in filenames}
            (source / "manifest.json").write_text(json.dumps({
                "generator_version": "2.0.0", "data_origin": "SYNTHETIC",
                "project_count": len(projects), "transaction_count": len(history),
                "boq_line_count": len(boq), "files_sha256": hashes,
            }), encoding="utf-8")
            samples = plan_anomaly(source)
            self.assertEqual(len(samples), len(ANOMALY_SCENARIOS))
            self.assertTrue(all(p["split"] == "test" for _, p in samples))
            for scenario, selected in samples:
                xlsx = Path(temp) / (scenario + ".xlsx")
                truth = Path(temp) / (scenario + ".json")
                output = export(source, xlsx, truth,
                                selected["synthetic_project_id"],
                                anomaly_scenario=scenario)
                self.assertGreater(output["detail_rows"], 0)
                doc = json.loads(truth.read_text(encoding="utf-8"))
                edits = doc["injection"]["changed_lines"]
                self.assertGreater(len(edits), 0)
                self.assertEqual(doc["injection"]["source_split"], "test")
                for edit in edits:
                    changed = next(r for r in doc["cases"]
                                   if r["synthetic_line_id"] == edit["synthetic_line_id"])
                    self.assertEqual(changed["expected_quantity"], edit["injected_quantity"])
                    self.assertEqual(changed["expected_unit_price"], edit["injected_unit_price"])
                    expected_amount = (Decimal(edit["injected_quantity"]) *
                                       Decimal(edit["injected_unit_price"])).quantize(Decimal("0.01"))
                    self.assertEqual(Decimal(changed["expected_amount"]), expected_amount)
                    self.assertNotEqual(
                        (edit["original_quantity"], edit["original_unit_price"]),
                        (edit["injected_quantity"], edit["injected_unit_price"]))
                    self.assertGreater(
                        Decimal(edit["injected_quantity"]) * Decimal(edit["injected_unit_price"]), 0)
                with self.assertRaisesRegex(ValueError, "overwrite"):
                    export(source, xlsx, truth, selected["synthetic_project_id"],
                           anomaly_scenario=scenario)
            self.assertEqual(hashes, {name: hashlib.sha256((source / name).read_bytes()).hexdigest()
                                      for name in filenames})
            train_pid = next(p["synthetic_project_id"] for p in projects if p["split"] == "train")
            with self.assertRaisesRegex(ValueError, "test-only"):
                export(source, Path(temp) / "train.xlsx", Path(temp) / "train.json",
                       train_pid, anomaly_scenario="structural_price_high")
            with self.assertRaisesRegex(ValueError, "unknown scenario"):
                export(source, Path(temp) / "invalid.xlsx", Path(temp) / "invalid.json",
                       selected["synthetic_project_id"], anomaly_scenario="fake")


if __name__ == "__main__":
    unittest.main()
