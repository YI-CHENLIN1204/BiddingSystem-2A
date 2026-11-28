"""Read-only catalog grouping and quantity review, not an estimator or publisher."""
import argparse,csv,re,json
from collections import Counter,defaultdict
from decimal import Decimal
from pathlib import Path
from replay_bid_candidate import norm,unit,num,read_sheet

TARGETS={'鋼筋主材':('鋼筋','KG'),'混凝土主材':('混凝土','M3')}
SERVICE=r'工資|運費|補貼|租賃|切除|打除|拆除|續接器|組立用|鋼筋計'
AMBIGUOUS=r'回填|或|不含|另計|另列|不計|未含|不包括'
BUNDLE=r'彎紮|綁紮|組立|壓送|澆築|澆置|供料|施工|安裝'

def quantity_review(row,layout):
    n=norm(row['原始工項']); u=unit(row['單位']); qty=num(row['數量']); item=norm(row['項次']); major=row['原始大項線索']
    hits=set(row['V2候選工項族'].split(' / ')); target=hits & TARGETS.keys()
    if not target: return '', '', '', '非目標工項'
    if len(target)!=1 or len(hits)!=1: return '', '', '', '材料身分或組成歧義'
    key,standard=TARGETS[next(iter(target))]
    if u!=standard and not (standard=='KG' and u=='T'): return key,standard,'','單位不符'
    if qty is None or qty<=0: return key,standard,'','非正量或空白'
    if '拆' in item or re.search(SERVICE,n): return key,standard,'','拆項或非材料服務'
    if re.search(AMBIGUOUS,n): return key,standard,'','材料或計量範圍歧義'
    if '增' in item: return key,standard,'','追加待核'
    if layout=='final' and major!='結構體工程': return key,standard,'','目前結構體範圍外'
    if layout=='review' and major!=key+'工程': return key,standard,'','校對標單工程範圍待核'
    if key=='鋼筋' and not re.search('鋼筋',n): return key,standard,'','主材名稱不足'
    if key=='混凝土' and not re.search('預拌混凝土',n): return key,standard,'','主材名稱不足'
    if layout=='review': status='校對資料候選：未核定決標及範圍'
    else: status='工料計量待核' if re.search(BUNDLE,n) else '材料計量待核'
    return key,standard,str(qty*(1000 if u=='T' else 1)),status

def run(source,workbook,layout):
    with open(source,encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
    raw=dict(read_sheet(workbook)); uc,qc=('D','E') if layout=='final' else ('C','D')
    seen=set(); out=[]; dup=defaultdict(list)
    for r in rows:
        rn=int(r['Excel列號']); d=raw[rn]
        if rn in seen: raise ValueError('duplicate source row')
        seen.add(rn)
        if (r['原始工項'],r['單位'],num(r['數量']),r['項次'])!=(d.get('B',''),d.get(uc,''),num(d.get(qc,'')),d.get('A','')): raise ValueError(f'source mismatch row {rn}')
        state=r['V2身分狀態']; family=r['V2建議工項身分'] if state in ('單一工項族','複合工項候選') else ''
        key,standard,q,status=quantity_review(r,layout)
        x=dict(r,**{'目錄草稿工項族':family,'目錄狀態':'待核工項族' if family else '多義或無候選待分類','標準化單位':unit(r['單位']),'原工料編號':d.get('C','') if layout=='final' else '', '原單價':d.get('F','') if layout=='final' else '', '原複價':d.get('G','') if layout=='final' else '', '原備註':d.get('H' if layout=='final' else 'E',''), '估算聚合鍵候選':key,'估算標準單位':standard,'換算候選量':q,'V4數量覆核狀態':status,'V4發布資格':'禁止：未核定'})
        out.append(x)
        if q: dup[(key,norm(r['原始工項']),standard,Decimal(q))].append(x)
    for group in dup.values():
        if len(group)>1:
            for x in group: x['V4數量覆核狀態']='同案重複計量待核';x['換算候選量']=''
    return out

def write(path,rows):
    with open(path,'x',encoding='utf-8-sig',newline='') as f:
        w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)

def catalog(results):
    groups=defaultdict(list)
    for layout,rows in results:
        for r in rows:
            if r['目錄草稿工項族']: groups[r['目錄草稿工項族']].append((layout,r))
    return [{'工項族草稿':k,'明細數':len(v),'來源文件數':len({s for s,r in v}),'單位變體':' / '.join(sorted({r['標準化單位'] for s,r in v})), '主檔策略':'分類索引；不新增估算類別','核定狀態':'待審'} for k,v in sorted(groups.items(),key=lambda x:(-len(x[1]),x[0]))]

def selftest():
    def test(n,u,q='1',i='1',major='結構體工程',family='鋼筋主材',layout='final'):
        return quantity_review({'原始工項':n,'單位':u,'數量':q,'項次':i,'原始大項線索':major,'V2候選工項族':family},layout)
    assert test('鋼筋及彎紮組立SD420','噸')[2:] == ('1000','工料計量待核')
    assert test('鋼筋材料費','kg')[2:] == ('1','材料計量待核')
    assert test('鋼筋綁紮工資','kg')[2]==''
    assert test('鋼筋','m3')[2]==''
    assert test('鋼筋','kg','0')[2]==''
    assert test('鋼筋','kg','')[2]==''
    assert test('鋼筋','kg',i='拆')[2]==''
    assert test('鋼筋','kg',i='增')[2]==''
    assert test('鋼筋','kg',major='基礎工程')[2]==''
    assert test('預拌混凝土壓送澆築','M3',family='混凝土主材')[2:] == ('1','工料計量待核')
    assert test('混凝土或級配料回填','M3',family='混凝土主材 / 回填工程')[2]==''
    assert test('混凝土壓送工資','M3',family='混凝土主材')[2]==''
    assert test('鋼筋材料費','頓',major='鋼筋工程',layout='review')[3].startswith('校對資料候選')
    assert test('鋼筋材料費','kg',major='其他',layout='review')[2]==''
    assert test('模板','M2',family='模板工程')[0]==''
    print('V4 checks: 15 PASS')

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--final',nargs=2,metavar=('V3_CSV','XLSX'));p.add_argument('--review',nargs=2,metavar=('V3_CSV','XLSX'));p.add_argument('--outdir',type=Path);p.add_argument('--selftest',action='store_true');a=p.parse_args()
    if a.selftest:selftest()
    results=[]
    for layout,pair in [('final',a.final),('review',a.review)]:
        if not pair:continue
        rows=run(*pair,layout);results.append((layout,rows)); print(layout,len(rows),dict(Counter(x['V4數量覆核狀態'] for x in rows)))
        totals=defaultdict(Decimal)
        for x in rows:
            if x['換算候選量']:totals[x['估算聚合鍵候選']]+=Decimal(x['換算候選量'])
        print('unapproved preview totals:',{k:str(v) for k,v in totals.items()})
        if a.outdir:write(a.outdir/(layout+'_v4.csv'),rows)
    if results:
        cat=catalog(results);print('catalog',len(cat),'rows',sum(x['明細數'] for x in cat),'shared families',sum(x['來源文件數']>1 for x in cat))
        if a.outdir:write(a.outdir/'catalog_v4.csv',cat)
