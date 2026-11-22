import csv
import hashlib
import json
import tempfile
import unittest
from collections import Counter
from decimal import Decimal
from pathlib import Path
from zipfile import ZipFile
import generate_factory_dataset as factory
import export_full_project_excel as excel
import prepare_factory_delivery as delivery

class FactoryDatasetTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.data = factory.produce()
        cls.summary = factory.validate(cls.data)

    def test_project_level_splits_and_independent_factory_identity(self):
        ps = self.data["projects"]
        self.assertEqual(len({p["synthetic_project_id"] for p in ps}), 180)
        self.assertEqual(Counter(p["project_type"] for p in ps), {"廠房":180})
        self.assertEqual(Counter(p["split"] for p in ps), {"train":126,"validation":27,"test":27})
        self.assertEqual(Counter(p["structure_type"] for p in ps), {"RC":60,"SC":60,"SRC":60})
        self.assertTrue(any(Decimal(p["office_area"])==0 for p in ps))
        self.assertTrue(any(Decimal(p["office_area"])>0 for p in ps))

    def test_geometry_and_scenarios_drive_applicability(self):
        truth = {(a["synthetic_project_id"],a["material_code"]):a for a in self.data["applicability"]}
        counts = Counter()
        for p in self.data["projects"]:
            pid=p["synthetic_project_id"]
            self.assertEqual(truth[pid,"SYN-STEEL"]["status"],"NotApplicableAssumed" if p["structure_type"]=="RC" else "ApplicablePresent")
            self.assertEqual(truth[pid,"FAC-METAL-ROOF"]["status"],"ApplicablePresent" if p["roof_system"]=="metal_panel" else "NotApplicableAssumed")
            self.assertEqual(truth[pid,factory.code_for_id(29)]["status"],"ApplicablePresent" if Decimal(p["office_area"])>0 else "NotApplicableAssumed")
            counts[pid]=sum(1 for a in truth.values() if a["synthetic_project_id"]==pid and a["status"]=="ApplicablePresent")
        self.assertGreater(len(set(counts.values())),1)
        self.assertLess(max(counts.values()),len(self.data["catalog"]))

    def test_unknown_scope_is_not_zero_or_not_applicable(self):
        unmodelled = [a for a in self.data["applicability"] if a["status"]=="OutOfScope"]
        self.assertTrue(unmodelled)
        self.assertTrue(all(a["expected_quantity"]=="" for a in unmodelled))
        self.assertTrue(all(a["expected_quantity"]=="" for a in self.data["applicability"] if a["status"]=="NotApplicableAssumed"))
        self.assertEqual(len({(a["synthetic_project_id"],a["material_code"]) for a in self.data["applicability"]}),len(self.data["applicability"]))

    def test_complete_boq_prices_amounts_and_discrete_units(self):
        for r in self.data["boq"]:
            q,price=Decimal(r["quantity"]),Decimal(r["unit_price"])
            self.assertGreater(q,0);self.assertGreater(price,0)
            self.assertEqual(factory.quant(q*price,".01"),Decimal(r["amount"]))
            if r["unit"] in factory.COUNT_UNITS:self.assertEqual(q,int(q))

    def test_history_boq_lineage_and_no_duplicate_alternatives(self):
        self.assertEqual(self.summary["boq_line_count"],self.summary["transaction_count"])
        self.assertEqual(len({r["synthetic_transaction_id"] for r in self.data["boq"]}),len(self.data["boq"]))
        # validate also checks exactly one excavation and one formwork variant.
        self.assertEqual(factory.validate(self.data),self.summary)

    def test_missing_item_and_recorded_zero_are_separate_test_only_variants(self):
        projects,rows,truth=factory.diagnostics(self.data)
        self.assertEqual(len(projects),6)
        self.assertTrue(all(p["source_split"]=="test" and not p["independent_project"] and not p["history_eligible"] for p in projects))
        for t in truth:
            current=[r for r in rows if r["synthetic_project_id"]==t["diagnostic_project_id"] and r["material_code"]==t["material_code"]]
            if t["status"]=="MissingRequiredItem":self.assertEqual(current,[])
            else:self.assertTrue(current);self.assertTrue(all(Decimal(r["quantity"])==0 for r in current))
            self.assertFalse(t["not_applicable"]);self.assertIsNone(t["engineering_anomaly"])
        self.assertTrue(all(Decimal(r["quantity"])>0 for r in self.data["boq"]))

    def test_reproducible_generation_checksums_and_overwrite_guard(self):
        with tempfile.TemporaryDirectory() as tmp:
            a,b=Path(tmp)/"a",Path(tmp)/"b"
            ma,mb=factory.write_dataset(a),factory.write_dataset(b)
            self.assertEqual(ma,mb)
            self.assertFalse(ma["database_imported"]);self.assertFalse(ma["automatic_approval_allowed"])
            self.assertEqual(ma["catalog_material_count"],104)
            self.assertEqual(ma["distinct_material_codes"],len({r["material_code"] for r in self.data["boq"]}))
            for name,sha in ma["files_sha256"].items():
                self.assertEqual(hashlib.sha256((a/name).read_bytes()).hexdigest(),sha)
                self.assertEqual((a/name).read_bytes(),(b/name).read_bytes())
            with self.assertRaises(ValueError):factory.write_dataset(a)
            self.assertEqual(json.loads((a/"manifest.json").read_text(encoding="utf-8")),ma)

    def test_complete_workbook_has_no_intentional_missing_fields(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); source=root/"source";factory.write_dataset(source)
            p=next(p for p in self.data["projects"] if p["structure_type"]=="SC" and p["split"]=="test")
            workbook,truth=root/"factory.xlsx",root/"truth.json"
            excel.export(source,workbook,truth,p["synthetic_project_id"],include_defects=False)
            t=json.loads(truth.read_text(encoding="utf-8"))
            self.assertFalse(t["intentional_defects_enabled"])
            self.assertTrue(all(r["scenario"]=="complete" and r["expected_quantity"] is not None and
                r["expected_unit_price"] is not None and r["expected_amount"] is not None for r in t["cases"]))
            with ZipFile(workbook) as z:self.assertIsNone(z.testzip())

    def test_source_tampering_is_rejected_before_export(self):
        with tempfile.TemporaryDirectory() as tmp:
            source=Path(tmp)/"source";factory.write_dataset(source)
            with (source/"project_boq_rows.csv").open("a",encoding="utf-8") as h:h.write("\n")
            with self.assertRaises(ValueError):excel.read_source(source)

    def test_delivery_contains_train_only_candidates_and_six_complete_workbooks(self):
        with tempfile.TemporaryDirectory() as tmp:
            source=Path(tmp)/"source";factory.write_dataset(source)
            archive=Path(tmp)/"factory.zip";package=delivery.prepare(source,archive)
            self.assertEqual(package["train_projects"],126)
            self.assertEqual(len(package["workbooks"]),6)
            candidates=source/"delivery/import_candidate"
            for filename in ("train_projects.csv","train_project_boq_rows.csv","train_historical_transactions.csv"):
                with (candidates/filename).open(encoding="utf-8-sig") as handle:
                    self.assertTrue(all(row["split"]=="train" for row in csv.DictReader(handle)))
            with ZipFile(archive) as z:self.assertIsNone(z.testzip())
            with self.assertRaises(ValueError):delivery.prepare(source,archive)

if __name__=="__main__":unittest.main()
