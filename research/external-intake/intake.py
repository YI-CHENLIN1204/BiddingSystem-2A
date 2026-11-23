"""Prepare private external research inputs using Python's standard library."""
import argparse
import csv
from datetime import date
from decimal import Decimal, InvalidOperation
import hashlib
import hmac
import json
import os
from pathlib import Path, PurePosixPath

CASES=("case_id","source_id","family_id","kind","structure","area_m2","footprint_m2","floor_count","basement_count","location_code","available_date","estimate_date","split")
LINES=("case_id","line_id","item_code","spec_code","unit_code","quantity","unit_price","currency")
LABELS=("case_id","line_id","material_code","spec_code","classification","reason_code")
TABLES={"cases.csv":CASES,"lines.csv":LINES}
def require(condition, message):
    if not condition:
        raise ValueError(message)

def read(path, fields):
    with path.open(encoding="utf-8-sig",newline="") as stream:
        reader=csv.DictReader(stream)
        require(tuple(reader.fieldnames or ())==fields, f"{path.name}: unexpected header")
        rows=list(reader)
    for i,row in enumerate(rows,2):
        require(None not in row and all(v and v==v.strip() for v in row.values()),
                f"{path.name}:{i}: missing, extra or untrimmed value")
    return rows

def write(path, fields, rows):
    with path.open("x",encoding="utf-8",newline="") as stream:
        writer=csv.DictWriter(stream,fieldnames=fields)
        writer.writeheader()
        writer.writerows(rows)

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def index(rows, fields):
    result={tuple(row[k] for k in fields): row for row in rows}
    require(len(result)==len(rows), f"Duplicate key: {fields}")
    return result

def opaque(value):
    return bool(value) and len(value)<=72 and value.isascii() and all(
        x.isalnum() or x in "_-" for x in value)

def validate(directory):
    cases,lines=(read(directory/name,fields) for name,fields in TABLES.items())
    require(cases and lines,"No test data supplied")
    case_index=index(cases,("case_id",))
    index(lines,("case_id","line_id"))
    by_split={name:[] for name in ("train","validation","blind")}
    sources, families={},{}
    for row in cases:
        split=row["split"]
        require(split in by_split and row["kind"] in {"residential","factory"},"Unknown split or case kind")
        require(row["structure"] in {"RC","SC","SRC"},"Unknown structure")
        require(opaque(row["location_code"]),"Supply reviewed location code")
        for field in ("area_m2","footprint_m2"):
            value=Decimal(row[field])
            require(value.is_finite() and value>0,f"Invalid {field}")
        require(int(row["floor_count"])>0 and int(row["basement_count"])>=0,"Invalid floor counts")
        available, estimate=(date.fromisoformat(row[k]) for k in ("available_date","estimate_date"))
        require(estimate<=available,"Outcome date precedes stated estimate date")
        for field,seen in (("source_id",sources),("family_id",families)):
            value=row[field]
            require(seen.setdefault(value,split)==split,f"Cross-split leakage through {field}")
        by_split[split].append((available,estimate))
    require(all(by_split.values()),"All splits must contain cases")
    for reference_split,target_split in (("train","validation"),("train","blind"),("validation","blind")):
        require(max(a for a,_ in by_split[reference_split])<min(e for _,e in by_split[target_split]),
                f"Evidence from {reference_split} was unavailable before a {target_split} estimate")
    with_lines=set()
    for row in lines:
        require((row["case_id"],) in case_index,"Unknown case key")
        with_lines.add(row["case_id"])
        require(all(opaque(row[k]) for k in ("item_code","unit_code","currency")),"Supply reviewed codes, not free text")
        require(len(row["currency"])==3 and row["currency"].isalpha() and row["currency"].isupper(),"Invalid currency")
        for name in ("quantity","unit_price"):
            try:
                amount=Decimal(row[name])
                require(amount.is_finite() and amount>0,f"Invalid {name}")
            except InvalidOperation:
                raise ValueError(f"Invalid {name}") from None
    require(len(with_lines)==len(cases),"A project has no final bid rows")
    return cases,lines

def assign(directory, output, validation_start, blind_start):
    require(validation_start<blind_start,"Cutoff dates must increase")
    cases=read(directory/"cases.csv",CASES)
    require(all(r["split"]=="auto" for r in cases),"Use auto in the input split column")
    lines=read(directory/"lines.csv",LINES)
    for row in cases:
        stamp=date.fromisoformat(row["estimate_date"])
        row["split"]="blind" if stamp>=blind_start else "validation" if stamp>=validation_start else "train"
    output.mkdir()
    write(output/"cases.csv",CASES,cases)
    write(output/"lines.csv",LINES,lines)
    validate(output)
    print("PASS date split without crossed sources, families or availability")

def freeze(directory, output):
    require(not output.exists(),"Freeze destination exists")
    originals={name:digest(directory/name) for name in TABLES}
    cases,lines=validate(directory)
    require(originals=={name:digest(directory/name) for name in TABLES},"Inputs changed during validation")
    secret=os.environ.get("RESEARCH_ANON_KEY","")
    require(len(secret)>=32,"Use an authorized private key of at least 32 characters")
    def token(name,value):
        return hmac.new(secret.encode(),(name+"\0"+value).encode(),hashlib.sha256).hexdigest()
    clean_cases=[{k:(token(k,v) if k in ("case_id","source_id","family_id","location_code") else v)
                  for k,v in row.items()} for row in cases]
    clean_lines=[{k:(token(k,v) if k=="case_id" else token(k,row["case_id"]+"\0"+v) if k=="line_id" else v)
                  for k,v in row.items()} for row in lines]
    blind={r["case_id"] for r in clean_cases if r["split"]=="blind"}
    output.mkdir()
    for child in ("model","private"):
        (output/child).mkdir()
    model=output/"model"
    public_targets=tuple(k for k in CASES if k!="available_date")
    write(model/"targets.csv",public_targets,[{k:r[k] for k in public_targets} for r in clean_cases if r["case_id"] in blind])
    write(model/"reference_cases.csv",CASES,[r for r in clean_cases if r["case_id"] not in blind])
    write(model/"reference_lines.csv",LINES,[r for r in clean_lines if r["case_id"] not in blind])
    private=output/"private"
    write(private/"blind_final_bid.csv",LINES,[r for r in clean_lines if r["case_id"] in blind])
    write(private/"blind_provenance.csv",CASES,[r for r in clean_cases if r["case_id"] in blind])
    files={str(p.relative_to(output).as_posix()):digest(p) for p in output.rglob("*.csv")}
    (output/"manifest.json").write_text(json.dumps(dict(schema=1,source_sha256=originals,
        file_sha256=files,case_count=len(cases),line_count=len(lines)),indent=2)+"\n",encoding="utf-8")

def verify_source(directory, manifest):
    data=json.loads(manifest.read_text(encoding="utf-8-sig"))
    hashes=data.get("file_sha256") or data.get("files")
    require(isinstance(hashes,dict) and hashes,"Manifest requires a non-empty file_sha256 mapping")
    for name,expected in hashes.items():
        rel=PurePosixPath(name)
        require(not rel.is_absolute() and ".." not in rel.parts and "\\" not in name and ":" not in name,
                "Unsafe manifest path")
        path=(directory/name).resolve()
        require(path.is_relative_to(directory.resolve()) and isinstance(expected,str) and digest(path)==expected.lower(),
                f"Original SHA mismatch: {name}")
    print(f"PASS original SHA-256 {len(hashes)} files")

def verify(directory):
    data=json.loads((directory/"manifest.json").read_text(encoding="utf-8"))
    require(data["schema"]==1 and data["file_sha256"],"Unexpected manifest")
    paths={str(p.relative_to(directory).as_posix()):p for p in directory.rglob("*.csv")}
    require(set(paths)==set(data["file_sha256"]),"Frozen file membership mismatch")
    require(all(digest(p)==data["file_sha256"][name] for name,p in paths.items()),"Frozen SHA mismatch")
    print(f"PASS frozen SHA-256 {len(paths)} files")

def reconcile(left,right,output,decision=None):
    a,b=(index(read(p,LABELS),LABELS[:2]) for p in (left,right))
    require(a and a.keys()==b.keys(),"Reviewers must cover the same non-empty blind lines")
    adjudicated=index(read(decision,LABELS),LABELS[:2]) if decision else {}
    disputed=[key for key in a if any(a[key][k]!=b[key][k] for k in LABELS[2:5])]
    require(adjudicated.keys()<=set(disputed),"Adjudication must be restricted to disputes")
    for row in [*a.values(),*b.values(),*adjudicated.values()]:
        require(all(opaque(v) for v in row.values()),"Review values must be reviewed codes")
        require(row["classification"] in {"normal","anomaly","inconclusive"},"Invalid class")
    output.mkdir()
    for name, group in (("left",a),("right",b)):
        write(output/f"disputes-{name}.csv",LABELS,[group[k] for k in disputed])
    pending=set(disputed)-adjudicated.keys()
    if not pending:
        write(output/"adjudicated.csv",LABELS,[adjudicated.get(k,row) for k,row in a.items()])
    print(json.dumps(dict(cases=len(a),disputes=len(disputed),pending=len(pending))))

if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    sub=parser.add_subparsers(dest="action",required=True)
    for action,fields in (("freeze",("source","output")),("verify",("directory",)),("verify-source",("directory","manifest")),("reconcile",("left","right","output"))):
        p=sub.add_parser(action)
        for field in fields:
            p.add_argument(field,type=Path)
        if action=="reconcile":
            p.add_argument("--decisions",type=Path)
    p=sub.add_parser("assign")
    p.add_argument("source",type=Path)
    p.add_argument("output",type=Path)
    p.add_argument("validation_start",type=date.fromisoformat)
    p.add_argument("blind_start",type=date.fromisoformat)
    args=parser.parse_args()
    if args.action=="freeze":
        freeze(args.source,args.output)
        verify(args.output)
    elif args.action=="verify":
        verify(args.directory)
    elif args.action=="assign":
        assign(args.source,args.output,args.validation_start,args.blind_start)
    elif args.action=="verify-source":
        verify_source(args.directory,args.manifest)
    else:
        reconcile(args.left,args.right,args.output,args.decisions)
