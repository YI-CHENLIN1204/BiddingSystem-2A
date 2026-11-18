import csv
import hashlib
import json
import tempfile
import unittest
from pathlib import Path
from generate import main

class GeneratorTests(unittest.TestCase):
    def test_determinism_and_integrity(self):
        with tempfile.TemporaryDirectory() as folder:
            a,b=Path(folder)/"a",Path(folder)/"b"
            main(["--count","60","--seed","42","--output",str(a)])
            main(["--count","60","--seed","42","--output",str(b)])
            for name in ["projects.csv","historical_transactions.csv","manifest.json"]:
                self.assertEqual((a/name).read_bytes(),(b/name).read_bytes())
            with (a/"projects.csv").open(encoding="utf-8-sig",newline="") as f: projects=list(csv.DictReader(f))
            with (a/"historical_transactions.csv").open(encoding="utf-8-sig",newline="") as f: tx=list(csv.DictReader(f))
            self.assertEqual(len(projects),60)
            self.assertEqual(len(tx),120)
            pids={p["synthetic_project_id"] for p in projects}
            self.assertEqual(len(pids),60)
            splits={p["synthetic_project_id"]:p["split"] for p in projects}
            self.assertTrue(all(t["synthetic_project_id"] in pids and splits[t["synthetic_project_id"]]==t["split"] for t in tx))
            self.assertTrue(all(float(t["actual_quantity"])>0 and t["data_origin"]=="SYNTHETIC" for t in tx))
            self.assertTrue(all(p["project_name"].startswith("合成住宅建案") and p["data_origin"]=="SYNTHETIC" for p in projects))
            self.assertEqual({p["split"] for p in projects},{"train","validation","test"})
            manifest=json.loads((a/"manifest.json").read_text(encoding="utf-8"))
            for filename,digest in manifest["files_sha256"].items():
                self.assertEqual(hashlib.sha256((a/filename).read_bytes()).hexdigest(),digest)
    def test_refuse_overwrite(self):
        with tempfile.TemporaryDirectory() as folder:
            path=Path(folder)
            (path/"existing.txt").write_text("do not overwrite")
            with self.assertRaises(SystemExit): main(["--count","20","--output",folder])
            self.assertEqual((path/"existing.txt").read_text(),"do not overwrite")

if __name__=="__main__": unittest.main()
