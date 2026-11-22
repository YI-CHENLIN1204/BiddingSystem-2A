import csv,json,sys,hashlib
from pathlib import Path
from datetime import date
from statistics import mean
from collections import defaultdict
src,out=map(Path,sys.argv[1:3])
def read(p):
 with p.open(encoding='utf-8-sig') as f:return list(csv.DictReader(f))
m=src/'manifest.json'
assert hashlib.sha256(m.read_bytes()).hexdigest()=='e04d7fa10e012825561031befff1f9d7f63b6e88272c9e528eb523fdcab6bfd0'
for k,v in json.loads(m.read_text(encoding='utf8'))['files_sha256'].items():assert hashlib.sha256((src/k).read_bytes()).hexdigest()==v
key='synthetic_project_id'
p={r[key]:r for r in read(src/'projects.csv')}
train=sorted(i for i in p if p[i]['split']=='train')
assert len(train)==126
area=lambda x:float(x['total_floor_area'])
day=lambda x:date.fromisoformat(x['start_date'][:10])
tot=defaultdict(float)
for r in read(src/'historical_transactions.csv'):
 if r['material_code'] in ('SYN-CONCRETE','SYN-REBAR'):
  assert r['unit']=={'SYN-CONCRETE':'m3','SYN-REBAR':'kg'}[r['material_code']]
  tot[r[key],r['material_code']]+=float(r['actual_quantity'])
for r in read(src/'project_boq_rows.csv'):
 if r['major_category']=='結構體工程':tot[r[key],'structure_price']+=float(r['amount'])
def score(a,b):
 def rel(k):return max(0,1-abs(float(b[k])/float(a[k])-1))
 return round(25*(a['structure_type']==b['structure_type'])+30*rel('total_floor_area')+10*rel('site_area')+15*max(0,1-.1*abs(int(a['floor_count'])-int(b['floor_count']))-.15*abs(int(a['basement_floor_count'])-int(b['basement_floor_count'])))+10*max(0,1-abs(day(a).year-day(b).year)/20)+10*(a['city']==b['city']),2)
def quant(ref,q):
 target=sum(r[2] for r in ref)*q
 cum=0
 for r in sorted(ref,key=lambda r:r[3]):
  cum+=r[2]
  if cum>=target:return r[3]
 raise ValueError('empty quantile')
rows=[]
past_only = "--past-only" in sys.argv[3:]
for b in read(out/'factory_b1_pairs.csv'):
 if b['method']!='B1' or b['level']!='0.7':continue
 i,k=b['project'],b['scope']
 a=p[i]
 assert i not in train
 y=float(b['truth'])
 assert abs(tot[i,k]-y)<.01
 ref=[]
 for j in train:
  c=p[j]
  if past_only and day(c)>=day(a):continue
  if a['project_type']!=c['project_type'] or (a['structure_type']=='SRC' and c['structure_type']!='SRC') or tot[j,k]<=0:continue
  sim=score(a,c)
  w=sim/100*0.5**(abs((day(a)-day(c)).days)/365.2425/3)
  if w>0:ref.append((j,sim,w,tot[j,k]/area(c)))
 ref=sorted(ref,key=lambda r:(-r[1],-r[2],r[0]))[:8]
 row=dict(b,a_refs='|'.join(r[0] for r in ref),a_sim='|'.join(str(r[1]) for r in ref),a_weights='|'.join(format(r[2],'.17g') for r in ref),a_low=None,a_median=None,a_high=None,a_score=None,a_hit=None)
 if len(ref)>=5:
  lo,med,hi=[round(quant(ref,q)*area(a),2) for q in (.15,.5,.85)]
  row.update(a_low=lo,a_median=med,a_high=hi,a_score=(hi-lo+2/.3*max(lo-y,y-hi,0))/y,a_hit=int(lo<=y<=hi))
 rows.append(row)
with (out/('factory_a0_past_baseline.csv' if past_only else 'factory_a0_b1_pairs.csv')).open('w',encoding='utf8',newline='') as f:
 w=csv.DictWriter(f,fieldnames=rows[0]);w.writeheader();w.writerows(rows)
for k in sorted(set(r['scope'] for r in rows)) if not past_only else ():
 r=[r for r in rows if r['scope']==k and r['split']=='test']
 both=[x for x in r if x['status']=='ok' and x['a_score'] is not None]
 print(k,len(r),sum(x['a_score'] is not None for x in r),sum(x['status']=='ok' for x in r),len(both),sum(x['a_hit'] for x in both),sum(int(x['covered']) for x in both),mean(x['a_score'] for x in both),mean(float(x['relative_score']) for x in both))
