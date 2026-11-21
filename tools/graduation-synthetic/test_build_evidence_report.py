import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from generate_full_project_dataset import generate_full, PROJECT_FIELDS, HISTORY_FIELDS, BOQ_FIELDS, write_csv
from evaluate_full_project_holdout import evaluate
from build_evidence_report import compute, make_html


class EvidenceReportTests(unittest.TestCase):
    def test_verified_report_contains_comparison_and_category_figures(self):
        with tempfile.TemporaryDirectory() as folder:
            src=Path(folder)
            projects,histories,lines=generate_full(75,20261119)
            extended=PROJECT_FIELDS+["site_area","facade_system","finish_level","foundation_system","site_condition"]
            files=[("projects.csv",extended,projects),("historical_transactions.csv",HISTORY_FIELDS,histories),
                   ("project_boq_rows.csv",BOQ_FIELDS,lines)]
            for name,header,records in files:
                write_csv(src/name,header,records)
            hashes={name:hashlib.sha256((src/name).read_bytes()).hexdigest() for name,_,_ in files}
            (src/"manifest.json").write_text(json.dumps({
                "generator_version":"2.0.0","data_origin":"SYNTHETIC",
                "files_sha256":hashes,"project_count":len(projects),
                "transaction_count":len(histories),"boq_line_count":len(lines)
            }),encoding="utf-8")
            report=evaluate(src)
            path=src/"holdout.json"
            path.write_text(json.dumps(report),encoding="utf-8")
            stats=compute(src,path)
            page=make_html(stats)
            self.assertEqual(stats["projects"],75)
            self.assertEqual(stats["history"],len(histories))
            self.assertEqual(stats["lines"],len(lines))
            self.assertIn("P15～P85",page)
            self.assertIn("合成",page)
            self.assertIn("bar-fill",page)
            self.assertIn("OFFLINE_FULL_BOQ_PROXY_NOT_CSHARP_API",page)
            self.assertGreater(stats["material_codes"],25)
            path.write_text(json.dumps({**report,"origin":"REAL"}),encoding="utf-8")
            with self.assertRaisesRegex(ValueError,"Benchmark"):
                compute(src,path)


if __name__=="__main__":
    unittest.main()
