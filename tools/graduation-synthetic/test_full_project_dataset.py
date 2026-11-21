import hashlib
import json
import tempfile
import unittest
from collections import Counter, defaultdict
from pathlib import Path
from zipfile import ZipFile

from generate_full_project_dataset import generate_full, BOQ_FIELDS, PROJECT_FIELDS, HISTORY_FIELDS, write_csv
from export_full_project_excel import export
from evaluate_full_project_holdout import evaluate


class FullProjectDatasetTests(unittest.TestCase):
    def test_variety_history_alignment_and_scope(self):
        projects, history, detail = generate_full(60, 20261119)
        self.assertEqual(len(projects), 60)
        self.assertEqual(len(history), len(detail))
        self.assertGreater(len(history), 2500)
        counts = Counter(row["synthetic_project_id"] for row in detail)
        self.assertGreater(min(counts.values()), 35)
        self.assertGreater(len(set(counts.values())), 8)
        self.assertGreater(len({row["material_code"] for row in detail}), 25)
        self.assertGreater(len({row["major_category"] for row in detail}), 10)
        self.assertTrue(any(int(p["basement_floor_count"]) == 0 for p in projects))
        project_map = {p["synthetic_project_id"]:p for p in projects}
        for row, tx in zip(detail, history):
            self.assertEqual(row["synthetic_transaction_id"], tx["synthetic_transaction_id"])
            self.assertEqual(row["quantity"], tx["actual_quantity"])
            self.assertEqual(row["unit_price"], tx["unit_price"])
            self.assertEqual(row["split"], tx["split"])
            project = project_map[row["synthetic_project_id"]]
            self.assertGreater(float(project["site_area"]), 0)
            self.assertEqual(row["structure_type"], project["structure_type"])
            if int(project["basement_floor_count"]) == 0:
                for word in ("地下", "基坑", "連續壁", "擋土", "筏式"):
                    self.assertNotIn(word, row["item_name"])
                self.assertFalse(row["floor_label"].startswith("B"))
            if row["floor_label"].endswith("F") and row["floor_label"] != "RF":
                self.assertLessEqual(int(row["floor_label"][:-1]), int(project["floor_count"]))
            if project["structure_type"] == "RC":
                self.assertNotEqual(row["material_code"], "SYN-STEEL")
            else:
                self.assertNotEqual(row["material_code"], "SYN-REBAR")

    def test_deterministic_generation(self):
        self.assertEqual(generate_full(25, 5),generate_full(25, 5))

    def test_export_same_history_and_holdout(self):
        with tempfile.TemporaryDirectory() as temp:
            source=Path(temp)/"source"
            source.mkdir()
            projects, history, detail=generate_full(75, 20261119)
            extended=PROJECT_FIELDS+["site_area","facade_system","finish_level","foundation_system","site_condition"]
            for name, fields, records in (
                ("projects.csv",extended,projects),
                ("historical_transactions.csv",HISTORY_FIELDS,history),
                ("project_boq_rows.csv",BOQ_FIELDS,detail)):
                write_csv(source/name,fields,records)
            digests={p:hashlib.sha256((source/p).read_bytes()).hexdigest()
                     for p in ("projects.csv","historical_transactions.csv","project_boq_rows.csv")}
            (source/"manifest.json").write_text(json.dumps({
                "data_origin":"SYNTHETIC","generator_version":"2.0.0",
                "files_sha256":digests,"project_count":len(projects),
                "transaction_count":len(history),"boq_line_count":len(detail)}),encoding="utf-8")
            selection=next(p for p in projects if p["split"]=="test")
            output=Path(temp)/"bid.xlsx"
            truth=Path(temp)/"truth.json"
            response=export(source,output,truth,selection["synthetic_project_id"])
            matching=[row for row in detail if row["synthetic_project_id"]==selection["synthetic_project_id"]]
            self.assertEqual(response["detail_rows"],len(matching))
            answer=json.loads(truth.read_text(encoding="utf-8"))
            self.assertEqual({x["synthetic_line_id"] for x in matching},
                             {x["synthetic_line_id"] for x in answer["cases"]})
            # Current AnalysisCase/from-upload rejects uncategorized lines;
            # each synthetic line must supply an explicit supported major group.
            approved = {
                "結構體工程", "內外牆裝修工程", "假設工程",
                "土方及基礎工程", "安全觀測工程", "防水工程",
                "門窗工程", "外部裝修工程", "雜項工程", "景觀工程"
            }
            self.assertTrue(all(x["analysis_major_category"] in approved
                                for x in answer["cases"]))
            self.assertEqual(answer["xlsx_sha256"],hashlib.sha256(output.read_bytes()).hexdigest())
            with ZipFile(output) as workbook:
                self.assertIsNone(workbook.testzip())
                self.assertIn("xl/styles.xml",workbook.namelist())
            result=evaluate(source)
            self.assertEqual(result["boq_line_count"],len(detail))
            self.assertEqual(result["historical_line_count"],len(history))
            self.assertGreater(result["eligible_test_project_material_pairs"],0)
            self.assertGreater(result["method_comparison"]["weighted_quantile_proxy"]["n"],0)
            self.assertGreater(result["method_comparison"]["weighted_normal_proxy"]["n"],0)
            with self.assertRaisesRegex(ValueError,"overwrite"):
                export(source,output,truth,selection["synthetic_project_id"])
            (source/"project_boq_rows.csv").write_bytes(
                (source/"project_boq_rows.csv").read_bytes()+b"\n")
            with self.assertRaisesRegex(ValueError,"SHA256"):
                evaluate(source)


if __name__=="__main__":
    unittest.main()
