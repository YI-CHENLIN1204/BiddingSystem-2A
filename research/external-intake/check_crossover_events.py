"""Check AB/BA timestamp logs against the existing analyzer input."""
import argparse
import csv
from collections import defaultdict
from datetime import datetime
from pathlib import Path
from analyze_human_crossover import PHASES
from intake import index, read, require

EVENTS=("anonymous_operator_id","period","phase","event","timestamp","notes")
def verify(events, observations):
    with observations.open(encoding="utf-8-sig",newline="") as f:
        entries=index(list(csv.DictReader(f)),EVENTS[:2])
    elapsed=defaultdict(float)
    live={}
    previous={}
    for row in read(events,EVENTS):
        key=tuple(row[k] for k in EVENTS[:2])
        require(key in entries and row["phase"] in PHASES,"Unknown session or phase")
        stamp=datetime.fromisoformat(row["timestamp"])
        require(stamp.utcoffset() is not None,"Timestamp requires UTC offset")
        require(key not in previous or stamp>=previous[key],"Events out of sequence")
        previous[key]=stamp
        phase=row["phase"]
        if row["event"] in {"start","resume"}:
            require(key not in live,"Overlapping intervals")
            live[key]=(phase,stamp)
        else:
            require(row["event"] in {"pause","end"} and key in live and live[key][0]==phase,"Unmatched closing event")
            _, start=live.pop(key)
            elapsed[key,phase]+=(stamp-start).total_seconds()
    require(entries and not live and set(entries)==set(previous),"Missing, empty or unfinished sessions")
    for key,row in entries.items():
        for phase in PHASES:
            require(abs(elapsed[key,phase]-float(row[phase+"_seconds"]))<1,"Duration differs from observation")
    print(f"PASS reconciled {len(entries)} timed sessions")
if __name__=="__main__":
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("events",type=Path)
    p.add_argument("observations",type=Path)
    a=p.parse_args()
    verify(a.events,a.observations)
