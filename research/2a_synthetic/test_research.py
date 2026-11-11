import unittest
from generate import produce
from compare import evaluate, weighted_quantile

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

    def test_weighted_quantiles(self):
        p = [(10.0, 2.0), (20.0, 1.0), (30.0, 1.0)]
        self.assertEqual(weighted_quantile(p, 0.5), 10.0)
        self.assertEqual(weighted_quantile(p, 0.85), 30.0)

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
