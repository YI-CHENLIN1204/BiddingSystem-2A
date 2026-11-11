import unittest
from generate import produce
from compare import evaluate, weighted_quantile, quantile
from batch import benchmark, summarize
from production_weight_review import score, predict, run, summarize as summarize_weights
from report_weights import analyse, report

class SyntheticResearchTests(unittest.TestCase):
    def test_deterministic_seed(self):
        self.assertEqual(produce(42, 3, "stable"), produce(42, 3, "stable"))
        self.assertNotEqual(produce(42, 3, "stable"), produce(43, 3, "stable"))

    def test_six_combinations_no_duplicate_case_material(self):
        rows = produce(42, 4, "company_shift")
        self.assertEqual(len(rows), 6 * 4 * 2)
        self.assertEqual(len({(r["case_id"], r["material"]) for r in rows}), len(rows))
        self.assertTrue(all(r["is_synthetic"] == "true" for r in rows))
        self.assertTrue(all(r["reference_scope"] == "SIMULATED_MATERIAL_TOTAL_NOT_FINAL_BID" for r in rows))
        self.assertTrue(all(r["total_quantity"] > 0 for r in rows))

    def test_all_scenarios(self):
        for name in ("stable", "high_variance", "company_shift", "outliers"):
            with self.subTest(name=name):
                self.assertEqual(len(produce(101, 2, name)), 24)

    def test_exact_position_quantile(self):
        self.assertEqual(quantile([10.0, 20.0, 30.0], 0.5), 20.0)

    def test_weighted_quantiles(self):
        p = [(10.0, 2.0), (20.0, 1.0), (30.0, 1.0)]
        self.assertEqual(weighted_quantile(p, 0.5), 10.0)
        self.assertEqual(weighted_quantile(p, 0.85), 30.0)

    def test_batch_reproducible_and_separated(self):
        one = benchmark([31, 32], cases_per_combo=10, train_sizes=(3,))
        two = benchmark([31, 32], cases_per_combo=10, train_sizes=(3,))
        self.assertEqual(one, two)
        self.assertEqual(len(one), 4 * 2 * 12 * 4)
        for row in one:
            self.assertEqual(row["evaluated_cases"], 10)
            self.assertLessEqual(row["effective_train_cases"] or 0, 3)
        summary = summarize(one)
        self.assertEqual(len(summary), 4 * 1 * 4 * 2)
        self.assertTrue(all(x["scenario_group_runs"] == 2 * 6 for x in summary))

    def test_production_style_score_bounds_and_structure(self):
        base = [dict(row, floor_area_m2=float(row["floor_area_m2"]),
                     site_area_m2=float(row["site_area_m2"]),
                     ground_floors=int(row["ground_floors"]),
                     basement_floors=int(row["basement_floors"]),
                     project_year=int(row["project_year"]),
                     usage_per_m2=float(row["usage_per_m2"]))
                for row in produce(5, 10, "stable") if row["material"] == "鋼筋"]
        target = base[0]
        self.assertEqual(score(target, target), 100)
        other_structure = dict(target, case_id="TEST-SC", structure_type="SC")
        self.assertAlmostEqual(score(target, other_structure), 45 / 70 * 100, places=2)
        self.assertIsNone(predict(target, [other_structure], 3, "current_with_structure_gate"))
        self.assertIsNone(predict(target, [target], 3, "current_no_structure_gate"))
        self.assertTrue(0 <= score(target, base[1]) <= 100)

    def test_production_style_batch(self):
        first = run([33], cases_per_combo=10, train_sizes=(3,), scenarios=("stable",))
        second = run([33], cases_per_combo=10, train_sizes=(3,), scenarios=("stable",))
        self.assertEqual(first, second)
        self.assertEqual(len(first), 12 * 3)
        summarized = summarize_weights(first)
        self.assertEqual(len(summarized), 2 * 3)
        self.assertTrue(all(row["evaluated_cases"] == 10 for row in first))
        gated = [row for row in first if row["config"] == "current_with_structure_gate"]
        self.assertTrue(all(row["cross_structure_references_percent"] == 0 for row in gated))

    def test_weight_report_is_generated_from_actual_aggregates(self):
        raw = run([17], cases_per_combo=10, train_sizes=(3,), scenarios=("stable",))
        summary = summarize_weights(raw)
        self.assertEqual(len(summary), 6)
        sections, outcomes = analyse(summary)
        self.assertEqual(len(sections), 2)
        self.assertEqual(sum(outcomes["current_with_structure_gate"].values()), 2)
        output = report(summary, {"summary_file": "test.csv", "manifest_file": "test.json"})
        self.assertIn("test.csv", output)
        self.assertIn("模擬", output)
        self.assertIn("改善", output)

    def test_weight_report_rejects_missing_configuration(self):
        raw = run([17], cases_per_combo=10, train_sizes=(3,), scenarios=("stable",))
        summary = summarize_weights(raw)
        with self.assertRaises(ValueError):
            analyse(summary[:-1])

    def test_holdout_comparison(self):
        rows = [dict(r, floor_area_m2=float(r["floor_area_m2"]),
                     usage_per_m2=float(r["usage_per_m2"]),
                     ground_floors=int(r["ground_floors"]),
                     project_year=int(r["project_year"]))
                for r in produce(42, 6, "stable")
                if r["project_type"] == "住宅" and r["structure_type"] == "RC"
                and r["material"] == "鋼筋"]
        result = evaluate(rows, 3, "weighted", "area_floors")
        self.assertEqual(result["evaluated_cases"], 6)
        self.assertEqual(result["train_cases_per_target"], 3)
        self.assertTrue(1 <= result["average_effective_training_cases"] <= 3)
        self.assertTrue(result["mean_absolute_percentage_error"] >= 0)

if __name__ == "__main__":
    unittest.main()
