"""Compare adjacent B1 group counts on the same frozen case grid."""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
from audit_frozen_factory import read, sha, write

def main(source, baseline, output):
    root = Path(__file__).resolve().parent
    source, baseline, output = source.resolve(), baseline.resolve(), output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    cases = []
    for groups in (3,4,5):
        destination = output/f"groups-{groups}"
        destination.mkdir()
        freeze = json.loads((root/"factory_b1_freeze.json").read_text(encoding="utf-8"))
        freeze["groups"] = groups
        (destination/"configuration.json").write_text(json.dumps(freeze,indent=2)+"\n",encoding="utf-8")
        if groups == 4:
            shutil.copyfile(baseline,destination/"factory_b1_pairs.csv")
        else:
            with tempfile.TemporaryDirectory() as tmp:
                sandbox=Path(tmp)
                for name in ("replay_factory_b1.py","probe_external_report.py"):
                    shutil.copyfile(root/name,sandbox/name)
                shutil.copyfile(destination/"configuration.json",sandbox/"factory_b1_freeze.json")
                with (destination/"run.log").open("wb") as log:
                    subprocess.run([sys.executable,str(sandbox/"replay_factory_b1.py"),str(source),
                                    str(destination),"--past-only"],stdout=log,stderr=subprocess.STDOUT,check=True)
        for row in read(destination/"factory_b1_pairs.csv"):
            if row["split"]=="test" and row["method"]=="B1":
                cases.append(dict(groups=groups,project=row["project"],scope=row["scope"],level=row["level"],
                    status=row["status"],score=row["relative_score"],refs=row["refs"]))
    case_populations = [{(r["project"],r["scope"],r["level"]) for r in cases if r["groups"]==g} for g in (3,4,5)]
    assert case_populations[0]==case_populations[1]==case_populations[2]
    scores={(r["groups"],r["project"],r["scope"],r["level"]):float(r["score"]) for r in cases if r["score"]}
    summary=[]
    for scope, level in sorted({(r["scope"],r["level"]) for r in cases}):
        population=[p for p,k,l in case_populations[0] if (k,l)==(scope,level)]
        shared=[p for p in population if all((g,p,scope,level) in scores for g in (3,4,5))]
        for g in (3,4,5):
            summary.append(dict(scope=scope,level=level,groups=g,population=len(population),
                                estimated=sum((g,p,scope,level) in scores for p in population),
                                common_estimated=len(shared),
                                common_score=sum(scores[g,p,scope,level] for p in shared)/len(shared) if shared else None))
    write(output/"per-case.csv",cases)
    write(output/"summary.csv",summary)
    (output/"manifest.json").write_text(json.dumps(dict(source_manifest_sha256=sha(source/"manifest.json"),
        frozen_k4_pairs_sha256=sha(baseline), date_proxy="start_date strictly earlier",
        policy="Exploratory sensitivity only. Frozen groups=4 retained."),indent=2)+"\n",encoding="utf-8")
    print(f"{len(cases)} observations, {len(summary)} summaries")

if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    for key in ("source","baseline","output"):
        parser.add_argument(key,type=Path)
    args=parser.parse_args()
    main(args.source,args.baseline,args.output)
