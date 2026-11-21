"""Permanent offline contract test: synthetic UI workbook dependency and safety."""
import csv
import hashlib
import json
import tempfile
import unittest
from pathlib import Path
from zipfile import ZipFile

from prepare_ui_case import select_project
from export_full_project_excel import export, read_source


class PrepareUiCaseTests(unittest.TestCase):
    def test_exporter_and_dependencies_produce_test_only_workbook(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / "source"
            source.mkdir()
            projects = [{
                "synthetic_project_id": "SYN-00987",
                "project_name": "SYNTHETIC-測試建案",
                "project_type": "住宅",
                "structure_type": "SRC",
                "city": "臺中市",
                "total_floor_area": "12000",
                "site_area": "900",
                "floor_count": "12",
                "basement_floor_count": "2",
                "start_date": "2025-01-01",
                "data_origin": "SYNTHETIC",
                "split": "test",
            }]
            boq = [{
                "synthetic_line_id": "SYN-LINE-1",
                "synthetic_transaction_id": "SYN-TX-1",
                "synthetic_project_id": "SYN-00987",
                "material_code": "SYN-REBAR",
                "major_category": "結構體工程",
                "item_name": "合成鋼筋 D19",
                "spec": "D19",
                "unit": "kg",
                "quantity": "18000.000",
                "unit_price": "40.00",
                "amount": "720000.00",
                "work_zone": "1F",
                "floor_label": "1F",
                "structure_type": "SRC",
                "data_origin": "SYNTHETIC",
                "split": "test",
            }]
            history = [{
                "synthetic_transaction_id": "SYN-TX-1",
                "synthetic_project_id": "SYN-00987",
                "material_code": "SYN-REBAR",
                "actual_quantity": "18000.000",
                "unit_price": "40.00",
                "transaction_date": "2025-02-01",
                "data_origin": "SYNTHETIC",
                "synthetic_label": "baseline",
                "split": "test",
            }]
            def write(name, rows):
                path = source / name
                with path.open("w", encoding="utf-8-sig", newline="") as file:
                    writer = csv.DictWriter(file, fieldnames=list(rows[0]))
                    writer.writeheader()
                    writer.writerows(rows)
                return hashlib.sha256(path.read_bytes()).hexdigest()

            hashes = {
                "projects.csv": write("projects.csv", projects),
                "historical_transactions.csv": write("historical_transactions.csv", history),
                "project_boq_rows.csv": write("project_boq_rows.csv", boq),
            }
            (source / "manifest.json").write_text(json.dumps({
                "generator_version": "2.0.0",
                "data_origin": "SYNTHETIC",
                "project_count": 1,
                "transaction_count": 1,
                "boq_line_count": 1,
                "files_sha256": hashes,
            }), encoding="utf-8")

            selected = select_project(projects, [], "SRC")
            self.assertEqual(selected["synthetic_project_id"], "SYN-00987")
            with self.assertRaisesRegex(ValueError, "No unused"):
                select_project(projects, ["SYNTHETIC-UI-驗收-SYN-00987"], "SRC")
            self.assertEqual(len(read_source(source)[1]), 1)

            path = root / "ui.xlsx"
            truth = root / "ui-truth.json"
            result = export(source, path, truth, project_id="SYN-00987")
            self.assertEqual(result["detail_rows"], 1)
            self.assertTrue(path.is_file())
            with ZipFile(path) as workbook:
                self.assertIsNone(workbook.testzip())
                self.assertIn("xl/workbook.xml", workbook.namelist())
            metadata = json.loads(truth.read_text(encoding="utf-8"))
            self.assertEqual(metadata["project"]["split"], "test")
            self.assertEqual(metadata["cases"][0]["analysis_major_category"], "結構體工程")
            with self.assertRaisesRegex(ValueError, "overwrite"):
                export(source, path, truth, project_id="SYN-00987")

    def test_select_project_cannot_use_train_split(self):
        projects = [{
            "synthetic_project_id": "SYN-00999",
            "split": "train",
            "structure_type": "SRC",
            "total_floor_area": "1000",
            "site_area": "200"
        }]
        with self.assertRaisesRegex(ValueError, "No unused"):
            select_project(projects, [], "SRC")


if __name__ == "__main__":
    unittest.main()
