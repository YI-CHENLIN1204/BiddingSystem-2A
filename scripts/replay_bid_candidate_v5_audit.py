"""Frozen V1-V4 audit on a multi-sheet XLSX. No approval or DB access."""
import argparse,csv,zipfile,xml.etree.ElementTree as ET
from collections import Counter
from replay_bid_candidate import NS,read_sheet,norm,num,classify
from replay_bid_candidate_v2 import refine as refine2
from replay_bid_candidate_v3 import refine as refine3
from replay_bid_candidate_v4 import quantity_review

HEADERS={'name':('項目名稱','工項名稱','品名','項目及說明','名稱與說明'),'unit':('單位',),'quantity':('數量','工程數量'),'spec':('規格','材料規格'),'major':('工程大項',),'item':('項次','序號','編號'),'price':('單價',),'amount':('金額','複價','總價')}

def columns(d):
    found={k:[c for c,v in d.items() if norm(v) in aliases] for k,aliases in HEADERS.items()}
    return {k:v[0] for k,v in found.items()} if all(len(v)==1 for v in found.values()) else None

def evaluate(name,u,q,item,major,spec=''):
    s,h,qs=classify(name,u,item,num(q))
    r=refine3(refine2({'原始工項':name,'單位':u,'數量':q,'項次':item,'原始大項線索':major,'候選工項族':h,'對應候選狀態':s,'數量資格':qs}))
    key,std,qty,status=quantity_review(r,'final')
    return dict(r,**{'規格':spec,'估算聚合鍵':key,'換算候選量':qty,'數量狀態':status,'發布資格':'禁止'})

def run(path):
    with zipfile.ZipFile(path) as z:
        sheets=[s.get('name') for s in ET.fromstring(z.read('xl/workbook.xml')).find('m:sheets',NS)]
    out=[]
    for sheet in sheets:
        rows=list(read_sheet(path,sheet)); headers=[(rn,columns(d)) for rn,d in rows if columns(d)]
        if len(headers)!=1: raise ValueError(f'{sheet}: missing or ambiguous header')
        header,c=headers[0]; skipped=0; start=len(out)
        for rn,d in rows:
            if rn<=header: continue
            v={k:d.get(col,'') for k,col in c.items()}
            if not v['name'] or not v['unit'] or num(v['quantity']) is None:
                if v['name'] and not v['unit'] and not v['quantity'] and v['item']=='壹' and v['name']==sheet: skipped+=1;continue
                raise ValueError(f'{sheet}:{rn}: unsupported detail/heading')
            r=evaluate(v['name'],v['unit'],v['quantity'],v['item'],v['major'],v['spec'])
            out.append(dict(r,**{'工作表':sheet,'Excel列號':rn,'原單價':v['price'],'原複價':v['amount']}))
        print(sheet,'details',len(out)-start,'heading',skipped)
    print('identity',dict(Counter(r['V2身分狀態'] for r in out)))
    print('quantity',dict(Counter(r['數量狀態'] for r in out)))
    return out

def stress():
    # Desired review-preview behavior; an unexpected preview is not a published record.
    cases=[('鋼筋材料費','kg','1','1','',True),('鋼筋及彎紮組立','噸','1','1','',True),('鋼筋綁紮工資','kg','1','1','',False),('混凝土壓送工資','m3','1','1','',False),('鋼筋續接器','組','1','1','',False),('鋼筋','m3','1','1','',False),('鋼筋','kg','0','1','',False),('鋼筋','kg','1','拆','',False),('混凝土或級配回填','m3','1','1','',False),('預拌混凝土壓送澆築','m3','1','1','',True),('混凝土','m3','1','1','',True),('鋼筋','kg','1','1','純施工工資，不含材料',False),('鋼筋加工費','kg','1','1','',False),('預拌混凝土','m3','1','1','僅壓送，不含材料',False)]
    passed=0
    for n,u,q,i,s,want in cases:
        r=evaluate(n,u,q,i,'結構體工程',s); actual=bool(r['換算候選量']); passed+=actual==want
        if actual!=want: print('REVIEW GAP',n,s,'expected preview',want,'actual',actual)
    print('stress',passed,'/',len(cases),'pass')

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('workbook');p.add_argument('--out');a=p.parse_args();rows=run(a.workbook);stress()
    if a.out:
        with open(a.out,'x',encoding='utf-8-sig',newline='') as f:
            w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
