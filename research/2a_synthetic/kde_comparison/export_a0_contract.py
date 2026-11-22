"""Export fixed A0 inputs for the C# quantile contract."""
import json
import runpy
from pathlib import Path

baseline = runpy.run_path(str(Path(__file__).with_name("replay_factory_a0_b1.py")))
area, projects, totals = (baseline[key] for key in ("area", "p", "tot"))
cases = []
for row in baseline["rows"]:
    ids = row["a_refs"].split("|")
    weights = list(map(float, row["a_weights"].split("|")))
    scope = row["scope"]
    cases.append(dict(Project=row["project"], Scope=scope, Area=area(projects[row["project"]]),
                      Values=[totals[i,scope]/area(projects[i]) for i in ids], Weights=weights,
                      Expected=[row["a_low"], row["a_median"], row["a_high"]], Ids=ids,
                      Available=[i for i in baseline["train"] if totals[i,scope]>0]))
metadata = [
    dict(ProjectId=n, ProjectName=i, ProjectType=p["project_type"], City=p["city"],
         StructureType=p["structure_type"], TotalFloorArea=area(p), SiteArea=float(p["site_area"]),
         FloorCount=int(p["floor_count"]), BasementFloorCount=int(p["basement_floor_count"]),
         StartDate=baseline["day"](p).isoformat())
    for n,(i,p) in enumerate(sorted(projects.items()), 1)
]
payload = dict(Projects=metadata, Train=baseline["train"], Cases=cases)
(baseline["out"] / "factory_a0_contract.json").write_text(json.dumps(payload), encoding="utf8")

