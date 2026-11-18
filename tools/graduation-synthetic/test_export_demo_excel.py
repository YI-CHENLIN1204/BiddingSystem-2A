import csv
import hashlib
import json
import tempfile
import unittest
from pathlib import Path
from xml.etree import ElementTree
from zipfile import ZipFile
from generate import generate, write_csv, PROJECT_FIELDS, HISTORY_FIELDS
from export_demo_excel import export, BASE

class ExcelExportTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.source = self.root / "data"
        self.source.mkdir()
        projects, histories = generate(30, 1234)
        write_csv(self.source / "projects.csv", PROJECT_FIELDS, projects)
        write_csv(self.source / "historical_transactions.csv", HISTORY_FIELDS, histories)
        hashes = {name: hashlib.sha256((self.source / name).read_bytes()).hexdigest()
                  for name in ("projects.csv", "historical_transactions.csv")}
        (self.source / "manifest.json").write_text(json.dumps({
            "data_origin": "SYNTHETIC", "project_count": 30,
            "transaction_count": 60, "files_sha256": hashes}), encoding="utf-8")

    def test_export_is_valid_ooxml_and_deterministic(self):
        a = self.root / "a.xlsx"
        b = self.root / "b.xlsx"
        first = export(self.source, a)
        second = export(self.source, b)
        self.assertEqual(first["project_id"], second["project_id"])
        self.assertEqual(a.read_bytes(), b.read_bytes())
        self.assertEqual(first["item_count"], 2)
        with ZipFile(a) as zipped:
            self.assertIsNone(zipped.testzip())
            sheet = ElementTree.fromstring(zipped.read("xl/worksheets/sheet1.xml"))
            rows = sheet.findall(f".//{{{BASE}}}row")
            self.assertEqual(len(rows), 3)
            strings = "".join(t.text or "" for t in rows[0].iter(f"{{{BASE}}}t"))
            for name in ("項次", "項目名稱", "單位", "數量", "單價", "金額"):
                self.assertIn(name, strings)
            self.assertEqual(len(rows[1].findall(f"{{{BASE}}}c")), 7)

    def test_prevent_overwrite(self):
        target = self.root / "keep.xlsx"
        target.write_bytes(b"SAFE")
        with self.assertRaisesRegex(ValueError, "overwrite"):
            export(self.source, target)
        self.assertEqual(target.read_bytes(), b"SAFE")

    def test_reject_tampering(self):
        source_csv = self.source / "historical_transactions.csv"
        source_csv.write_bytes(source_csv.read_bytes() + b"\n")
        with self.assertRaisesRegex(ValueError, "SHA256"):
            export(self.source, self.root / "bad.xlsx")

    def test_reject_unknown_project(self):
        with self.assertRaisesRegex(ValueError, "not found"):
            export(self.source, self.root / "bad.xlsx", "SYN-99999")

if __name__ == "__main__":
    unittest.main()
