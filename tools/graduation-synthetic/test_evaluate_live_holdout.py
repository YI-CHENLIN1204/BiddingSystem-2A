import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

import evaluate_live_holdout as benchmark


class LiveHoldoutSafetyTests(unittest.TestCase):
    def settings(self):
        return {"areaDifferenceCutoffRatio": 1, "siteAreaDifferenceCutoffRatio": 1,
                "groundFloorPenaltyRatio": .1, "basementFloorPenaltyRatio": .15,
                "yearDifferenceCutoffYears": 20}

    def project(self, pid="SYN-00001", split="train", structure="SRC"):
        return {"synthetic_project_id": pid, "split": split, "project_type": "住宅",
                "structure_type": structure, "total_floor_area": "1000", "site_area": "200",
                "floor_count": "5", "basement_floor_count": "1", "start_date": "2020-01-01",
                "city": "臺中市"}

    def test_source_reference_rejects_validation_test_and_cross_structure_src(self):
        target = self.project("SYN-00002", "test")
        ref = {"projectName": "SYNTHETIC:SYN-00001 source", "floorArea": 1000,
               "projectDate": "2020-01-01T00:00:00", "actualQuantity": 30}
        totals = {("SYN-00001", "rebar"): {"actual": 30, "clean_actual": 30}}
        for split, structure in (("test", "SRC"), ("validation", "SRC"), ("train", "RC")):
            with self.subTest(split=split, structure=structure), self.assertRaises(ValueError):
                benchmark.validate_reference(ref, target, "rebar", {"SYN-00001": self.project(split=split, structure=structure)}, totals)
        self.assertEqual(benchmark.validate_reference(ref, target, "rebar", {"SYN-00001": self.project()}, totals), "SYN-00001")

    def test_reference_metadata_and_numeric_totals_must_match_source(self):
        target = self.project("SYN-00002", "test")
        ref = {"projectName": "SYNTHETIC:SYN-00001 source", "floorArea": 1000,
               "projectDate": "2020-01-01", "actualQuantity": 900}
        totals = {("SYN-00001", "rebar"): {"actual": 30, "clean_actual": 30}}
        with self.assertRaisesRegex(ValueError, "numeric total"):
            benchmark.validate_reference(ref, target, "rebar", {"SYN-00001": self.project()}, totals)

    def test_six_factor_score_contract_includes_site_floor_year_and_city(self):
        target, prior = self.project(), self.project("SYN-00003")
        weights = [25, 30, 10, 15, 10, 10]
        self.assertEqual(benchmark.similarity(target, prior, self.settings(), weights), 100)
        prior.update(site_area="300", floor_count="7", basement_floor_count="2", start_date="2010-01-01", city="臺北市")
        # 25 structure + 30 area + 5 site + 9.75 floors + 5 year + 0 city.
        self.assertEqual(benchmark.similarity(target, prior, self.settings(), weights), 74.75)

    def test_quantile_boundary_and_invalid_weights(self):
        self.assertEqual(benchmark.weighted_quantiles([(10, 1), (20, 1)], [.15, .5, .85]), [10, 10, 20])
        for rows in ([], [(10, 0)], [(10, float("nan"))], [(float("inf"), 1)]):
            with self.subTest(rows=rows), self.assertRaises(ValueError):
                benchmark.weighted_quantiles(rows, [.5])

    def test_validation_selection_cannot_hide_insufficient_predictions(self):
        candidates = [{"config": {"id": "missing"}, "selection_score": .01,
                       "all_source_observations": {"rebar/SRC": {"insufficient": 2}}},
                      {"config": {"id": "covered"}, "selection_score": .5,
                       "all_source_observations": {"rebar/SRC": {"insufficient": 0}}}]
        self.assertEqual(benchmark.select_validation(candidates)["id"], "covered")

    def test_metrics_keep_failed_predictions_in_denominator_metadata(self):
        rows = [{"actual": 100, "bounds": [80, 90, 120]}, {"actual": 100, "bounds": None}]
        result = benchmark.metrics(rows)
        self.assertEqual((result["n"], result["insufficient"], result["coverage_percent"], result["mape_percent"]), (1, 1, 100, 10))

    def test_write_endpoints_are_refused_before_any_http_request(self):
        for method, path in (("PUT", "/api/settings"), ("POST", "/api/analysis-cases/from-upload"),
                             ("POST", "/api/projects/1/publish-history")):
            with self.subTest(path=path), self.assertRaisesRegex(ValueError, "non-whitelisted"):
                benchmark.api_json("http://127.0.0.1:5263", method, path)

    def test_handoff_gate_accepts_chinese_paths_with_git_quotepath_enabled(self):
        checker = Path(__file__).resolve().parents[2]/"scripts"/"check_handoff_review.py"
        with tempfile.TemporaryDirectory() as folder:
            repo = Path(folder)
            def git(*args):
                return subprocess.run(["git", *args], cwd=repo, capture_output=True,
                                      encoding="utf-8", check=True).stdout.strip()
            git("init", "-q")
            git("config", "user.name", "Contract Test")
            git("config", "user.email", "test@example.invalid")
            git("config", "core.quotePath", "true")
            (repo/"docs").mkdir()
            (repo/"docs/HANDOFF_CHANGE_REVIEWS.jsonl").write_text("", encoding="utf-8")
            git("add", ".")
            git("commit", "-qm", "baseline")
            base = git("rev-parse", "HEAD")
            filename = "崴正/測試.csproj"
            (repo/"崴正").mkdir()
            (repo/filename).write_text("<Project />\n", encoding="utf-8")
            record = {"date": "2026-11-22", "change_id": "unicode-path-contract",
                      "summary": "contract", "changed_paths": [filename],
                      "handoff_update_required": False, "updated_documents": [],
                      "reason": "Temporary fixture only", "verification": "actual git diff"}
            (repo/"docs/HANDOFF_CHANGE_REVIEWS.jsonl").write_text(json.dumps(record, ensure_ascii=False)+"\n", encoding="utf-8")
            git("add", ".")
            git("commit", "-qm", "unicode change")
            env = os.environ.copy()
            env["PYTHONIOENCODING"] = "utf-8"
            result = subprocess.run([sys.executable, str(checker), "--base", base], cwd=repo,
                                    env=env, capture_output=True, encoding="utf-8")
            self.assertEqual(result.returncode, 0, result.stdout+result.stderr)
            self.assertIn("Reviewed 1 files with 1 new audit record", result.stdout)


if __name__ == "__main__":
    unittest.main()
