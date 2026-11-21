"""Small offline fixture; no DB, API or original synthetic dataset required."""
import csv
import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from audit_holdout_provenance import audit


class HoldoutProvenanceTests(unittest.TestCase):
    def build_fixture(self, root):
        paths = {
            "projects.csv": [
                {"synthetic_project_id": "SYN-00001", "split": "train", "structure_type": "RC", "extreme_scenario": ""},
                {"synthetic_project_id": "SYN-00002", "split": "test", "structure_type": "SRC", "extreme_scenario": ""},
                {"synthetic_project_id": "SYN-00003", "split": "test", "structure_type": "SC", "extreme_scenario": "deep_basement"},
            ],
            "historical_transactions.csv": [
                {"synthetic_transaction_id": "t0", "synthetic_project_id": "SYN-00001", "material_code": "SYN-CONCRETE", "synthetic_label": "baseline"},
                {"synthetic_transaction_id": "t1", "synthetic_project_id": "SYN-00002", "material_code": "SYN-CONCRETE", "synthetic_label": "baseline"},
                {"synthetic_transaction_id": "t2", "synthetic_project_id": "SYN-00002", "material_code": "SYN-REBAR", "synthetic_label": "injected_anomaly"},
                {"synthetic_transaction_id": "t3", "synthetic_project_id": "SYN-00003", "material_code": "SYN-STEEL", "synthetic_label": "baseline"},
            ],
            "project_boq_rows.csv": [
                {"synthetic_transaction_id": "t0", "synthetic_project_id": "SYN-00001", "material_code": "SYN-CONCRETE", "major_category": "結構體工程"},
                {"synthetic_transaction_id": "t1", "synthetic_project_id": "SYN-00002", "material_code": "SYN-CONCRETE", "major_category": "結構體工程"},
                {"synthetic_transaction_id": "t2", "synthetic_project_id": "SYN-00002", "material_code": "SYN-REBAR", "major_category": "結構體工程"},
                {"synthetic_transaction_id": "t3", "synthetic_project_id": "SYN-00003", "material_code": "SYN-STEEL", "major_category": "結構體工程"},
            ],
        }
        hashes = {}
        for filename, rows in paths.items():
            with (root / filename).open("w", newline="", encoding="utf-8") as f:
                writer = csv.DictWriter(f, fieldnames=list(rows[0]))
                writer.writeheader()
                writer.writerows(rows)
            hashes[filename] = hashlib.sha256((root / filename).read_bytes()).hexdigest()
        manifest = {
            "data_origin": "SYNTHETIC", "generator_version": "2.0.0",
            "project_count": 3, "transaction_count": 4, "boq_line_count": 4,
            "files_sha256": hashes,
        }
        (root / "manifest.json").write_text(json.dumps(manifest), encoding="utf-8")

    def test_shocks_are_scoped_and_not_normal_truth(self):
        with tempfile.TemporaryDirectory() as name:
            root = Path(name)
            self.build_fixture(root)
            result = audit(root)
            self.assertEqual(result["testProjectCount"], 2)
            src = next(x for x in result["projects"] if x["structureType"] == "SRC")
            self.assertEqual(src["scopes"]["rebar"]["generatorShockedLineCount"], 1)
            self.assertTrue(src["scopes"]["concrete"]["noRecordedGeneratorShock"])
            self.assertFalse(src["scopes"]["concrete"]["engineeringNormalCertified"])
            sc = next(x for x in result["projects"] if x["structureType"] == "SC")
            self.assertFalse(sc["scopes"]["rebar"]["applicable"])
            self.assertFalse(result["readyForFalsePositiveRate"])

    def test_tampered_source_rejected(self):
        with tempfile.TemporaryDirectory() as name:
            root = Path(name)
            self.build_fixture(root)
            with (root / "historical_transactions.csv").open("a", encoding="utf-8") as f:
                f.write("tamper\n")
            with self.assertRaisesRegex(ValueError, "SHA256 mismatch"):
                audit(root)

    def test_actual_red_is_not_called_false_positive(self):
        with tempfile.TemporaryDirectory() as name:
            root = Path(name)
            self.build_fixture(root)
            report = {
                "source": "LOCAL_REAL_ASPNET_API", "data_origin": "SYNTHETIC",
                "results": [{"scenario": "baseline", "baseProjectId": "SYN-00002",
                             "quantities": {"鋼筋": {"analysisStatus": "Low"},
                                            "混凝土": {"analysisStatus": "Normal"}},
                             "price": {"analysisStatus": "Low"}}],
            }
            report_path = root / "paired.json"
            report_path.write_text(json.dumps(report), encoding="utf-8")
            result = audit(root, report_path)
            self.assertEqual(result["pairedBaselines"][0]["observedApi"]["rebar"]["observedStatus"], "Low")
            self.assertFalse(result["pairedBaselines"][0]["observedApi"]["rebar"]["engineeringNormalCertified"])
            self.assertFalse(result["readyForFalsePositiveRate"])


if __name__ == "__main__":
    unittest.main()
