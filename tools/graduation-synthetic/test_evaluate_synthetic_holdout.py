import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from generate import generate as make_data, write_csv, PROJECT_FIELDS, HISTORY_FIELDS
from evaluate_synthetic_holdout import evaluate, load, predictions


class HoldoutEvaluationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.source = Path(self.temp.name)
        projects, transactions = make_data(300, 20261119)
        for name, headers, records in (
            ("projects.csv", PROJECT_FIELDS, projects),
            ("historical_transactions.csv", HISTORY_FIELDS, transactions),
        ):
            write_csv(self.source / name, headers, records)
        hashes = {
            name: hashlib.sha256((self.source / name).read_bytes()).hexdigest()
            for name in ("projects.csv", "historical_transactions.csv")
        }
        (self.source / "manifest.json").write_text(json.dumps({
            "data_origin": "SYNTHETIC",
            "files_sha256": hashes,
            "project_count": len(projects),
            "transaction_count": len(transactions),
        }), encoding="utf-8")

    def test_holdout_is_deterministic_and_disclosed(self):
        first = evaluate(self.source)
        second = evaluate(self.source)
        self.assertEqual(first, second)
        self.assertEqual(first["data_origin"], "SYNTHETIC")
        self.assertEqual(first["benchmark_kind"], "OFFLINE_PROXY_NOT_API")
        self.assertTrue(first["anomalous_training_samples_excluded"])
        self.assertGreater(first["holdout_baseline_records"], 0)
        metrics = first["metrics_by_method"]
        for name in ("weighted_quantile_proxy", "weighted_normal_proxy"):
            self.assertGreater(metrics[name]["n"], 0)
            self.assertGreaterEqual(metrics[name]["p15_p85_coverage_percent"], 0)
            self.assertLessEqual(metrics[name]["p15_p85_coverage_percent"], 100)

    def test_quantiles_are_ordered(self):
        empirical, gaussian = predictions([(0.2, 1), (0.5, 2), (0.8, 3)])
        self.assertEqual(empirical, sorted(empirical))
        self.assertEqual(gaussian, sorted(gaussian))

    def test_manifest_tamper_rejected(self):
        path = self.source / "historical_transactions.csv"
        path.write_bytes(path.read_bytes() + b"\n")
        with self.assertRaisesRegex(ValueError, "SHA256"):
            load(self.source)


if __name__ == "__main__":
    unittest.main()
