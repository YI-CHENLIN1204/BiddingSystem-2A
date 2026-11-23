from datetime import datetime
import csv
import json
import os
from pathlib import Path
import tempfile
import unittest
from openpyxl import Workbook
from import_awards import prepare
from intake import CASES, LINES, digest, freeze, verify, write
from engineer_review import GOLD, PRED, reconcile, score
from check_crossover_events import EVENTS, verify as verify_events

class Operations(unittest.TestCase):
    def test_final_award_to_blind(self):
        with tempfile.TemporaryDirectory() as temp:
            folder=Path(temp)
            spec=json.loads(Path(__file__).with_name("templates").joinpath("final-award-map.json").read_text(encoding="utf-8"))
            spec.update(units={"KG":"kg"},item_specs={"Rebar":{"D16":{"item_code":"RB","spec_code":"D16","unit_code":"kg"}}})
            cases=[]
            header=list(spec["columns"].values())
            for i,split in enumerate(("train","validation","blind"),1):
                case=dict(zip(CASES,(f"C{i}",f"S{i}",f"F{i}","residential","RC","1000","250","5","1","L1",
                      f"202{2+i}-02-01",f"202{2+i}-01-01",split)))
                cases.append(case)
                values=["1","Rebar","D16","KG",str(i),"10",str(10*i),"已決標"]
                if i==1:
                    file=folder/"original.xlsx"
                    book=Workbook()
                    book.active.title="DetailA"
                    book.active.append(header)
                    book.active.append(values)
                    page=book.create_sheet("DetailB")
                    page.append(header)
                    page.append(["2",*values[1:]])
                    book.create_sheet("Ignore").append(["ignored"])
                    book.save(file)
                    selectors=["DetailA","DetailB"]
                else:
                    file=folder/f"original-{i}.csv"
                    with file.open("x",encoding="utf-8",newline="") as stream:
                        csv.writer(stream).writerows([header,values])
                    selectors=["csv"]
                spec["awards"].append(dict(case_id=case["case_id"],path=file.name,sha256=digest(file),
                    version="approved-1",first_available_date=case["available_date"],authorization_ref="TEST_ONLY",
                    currency="TWD",sheets=selectors))
            write(folder/"cases.csv",CASES,cases)
            def mapping():
                path=folder/"mapping.json"
                path.write_text(json.dumps(spec,ensure_ascii=False),encoding="utf-8")
                return path
            mapped=folder/"mapped"
            prepare(folder,mapping(),mapped)
            self.assertEqual(json.loads((mapped/"import-receipt.json").read_text())["line_count"],4)
            self.assertEqual(len((mapped/"PRIVATE_line_provenance.csv").read_text().splitlines()),5)
            os.environ["RESEARCH_ANON_KEY"]="test-only-key-not-for-real-research"
            frozen=folder/"frozen"
            freeze(mapped,frozen)
            verify(frozen)
            self.assertFalse((frozen/"model"/"blind_final_bid.csv").exists())
            self.assertNotIn("unit_price",(frozen/"model"/"targets.csv").read_text())
            entry=spec["awards"][-1]
            file=folder/entry["path"]
            for j,(field,value) in enumerate(((3,"lb"),(2,"D99"),(4,"0"),(5,"-1"),(6,"999"),(7,"pending"))):
                invalid=["1","Rebar","D16","KG","3","10","30","已決標"]
                invalid[field]=value
                with file.open("w",encoding="utf-8",newline="") as stream:
                    csv.writer(stream).writerows([header,invalid])
                entry["sha256"]=digest(file)
                with self.assertRaises(ValueError):
                    prepare(folder,mapping(),folder/f"rejected-{j}")
                self.assertFalse((folder/f"rejected-{j}").exists())
            with file.open("w",encoding="utf-8",newline="") as stream:
                csv.writer(stream).writerows([header,["1","Rebar","D16","KG","2","10","20","已決標"]])
            entry["sha256"]=digest(file)
            with self.assertRaisesRegex(ValueError,"repeated workbook|Duplicate normalized"):
                prepare(folder,mapping(),folder/"duplicate")

    def test_engineering_seal_and_crossover(self):
        with tempfile.TemporaryDirectory() as temp:
            f=Path(temp)
            write(f/"blind.csv",LINES,[dict(zip(LINES,("C","1","RB","D16","kg","2","3","TWD")))])
            g=dict(zip(GOLD,("C","1","yes","RB","D16","kg","normal","anomaly","no","TEST")))
            write(f/"left.csv",GOLD,[g])
            write(f/"right.csv",GOLD,[dict(g,price_status="normal")])
            reconcile(f/"blind.csv",f/"left.csv",f/"right.csv",f/"dispute",None)
            self.assertFalse((f/"dispute"/"adjudicated.csv").exists())
            reconcile(f/"blind.csv",f/"left.csv",f/"right.csv",f/"gold",f/"left.csv")
            write(f/"pred.csv",PRED,[dict(zip(PRED,("C","1","RB","OTHER|RB","normal","anomaly","no","10","1")))])
            with self.assertRaises(ValueError):
                score(f/"gold"/"adjudicated.csv",f/"pred.csv","0"*64,f/"rejected")
            score(f/"gold"/"adjudicated.csv",f/"pred.csv",digest(f/"pred.csv"),f/"scored")
            stats=json.loads((f/"scored"/"metrics.json").read_text())["metrics"]
            self.assertEqual((stats["top1"]["rate"],stats["top3"]["rate"],stats["price_status"]["precision"]),(0,1,1))
            timings=("setup","mapping","review","error_detection","error_correction","maintenance")
            from datetime import timedelta, timezone
            start=datetime(2024,1,1,tzinfo=timezone.utc)
            events=[dict(zip(EVENTS,("O","1","mapping",evt,(start+timedelta(seconds=seconds)).isoformat(),"NA")))
                    for evt,seconds in (("start",0),("pause",5),("resume",20),("end",25))]
            write(f/"events.csv",EVENTS,events)
            fields=EVENTS[:2]+tuple(k+"_seconds" for k in timings)
            write(f/"obs.csv",fields,[dict(zip(fields,("O","1",0,10,0,0,0,0)))])
            verify_events(f/"events.csv",f/"obs.csv")

if __name__=="__main__":
    unittest.main()
