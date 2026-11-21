import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from generate_full_project_dataset import generate_full, BOQ_FIELDS, PROJECT_FIELDS, HISTORY_FIELDS, write_csv
from prepare_training_seed import prepare


class TrainingSeedTests(unittest.TestCase):
    def test_seed_contains_training_data_only_and_exact_lineage(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp)
            projects,history,boq=generate_full(45,20261119)
            fields=PROJECT_FIELDS+["site_area","facade_system","finish_level","foundation_system","site_condition"]
            sets=(("projects.csv",fields,projects),
                  ("historical_transactions.csv",HISTORY_FIELDS,history),
                  ("project_boq_rows.csv",BOQ_FIELDS,boq))
            digests={}
            for name,headers,rows in sets:
                write_csv(root/name,headers,rows)
                digests[name]=hashlib.sha256((root/name).read_bytes()).hexdigest()
            (root/"manifest.json").write_text(json.dumps({
                "generator_version":"2.0.0","data_origin":"SYNTHETIC",
                "files_sha256":digests,"project_count":len(projects),
                "transaction_count":len(history),"boq_line_count":len(boq)
            }),encoding="utf-8")
            result=prepare(root)
            self.assertEqual(result["target_database"],"bidding2a_graduation")
            self.assertGreater(result["train_project_count"],0)
            self.assertEqual(result["train_transaction_count"],len(result["transaction_rows"]))
            self.assertEqual(result["train_transaction_count"],len(result["boq_rows"]))
            self.assertTrue(all(p["split"]=="train" for p in result["project_rows"]))
            self.assertTrue(all(p["split"]=="train" for p in result["boq_rows"]))
            self.assertGreater(result["excluded_holdout_projects"],0)
            self.assertGreater(len(result["material_rows"]),25)
            self.assertTrue(all(float(p["site_area"])>0 for p in result["project_rows"]))
            project_ids={p["synthetic_project_id"] for p in result["project_rows"]}
            self.assertTrue(all(tx["synthetic_project_id"] in project_ids for tx in result["transaction_rows"]))
            pairs={t["synthetic_transaction_id"]:t for t in result["transaction_rows"]}
            self.assertEqual({t["synthetic_transaction_id"] for t in result["boq_rows"]},set(pairs))
            for item in result["boq_rows"]:
                tx=pairs[item["synthetic_transaction_id"]]
                self.assertEqual(item["quantity"],tx["quantity"])
            (root/"project_boq_rows.csv").write_bytes((root/"project_boq_rows.csv").read_bytes()+b"\n")
            with self.assertRaisesRegex(ValueError,"SHA256"):
                prepare(root)


if __name__=="__main__":
    unittest.main()
