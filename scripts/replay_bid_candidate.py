"""Read-only candidate-family experiment. No API, database, or workbook writes.
Usage: python replay_bid_candidate.py workbook.xlsx --layout final --out local.csv
Layouts refer to the two explicitly inspected worksheet header formats, not all Excel files.
All results are candidates, not approved mappings or published quantities.
"""
import argparse,csv,re,unicodedata,zipfile,xml.etree.ElementTree as ET
from collections import Counter
from decimal import Decimal,InvalidOperation
NS={'m':'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
def norm(s): return re.sub(r'\s+','',unicodedata.normalize('NFKC',s or '')).upper()
def unit(s):
    s=norm(s); return {'噸':'T','頓':'T','公斤':'KG','公噸':'T','立方公尺':'M3','平方公尺':'M2','公尺':'M'}.get(s,s)
def num(s):
    try:
        v=Decimal(s.replace(',','')); return v if v.is_finite() else None
    except (InvalidOperation,AttributeError): return None
# A family is not an existing material ID. Unit restrictions deliberately differ per family.
RULES=[
('鋼筋主材',r'鋼筋(?!計|續接器|組立用)|\bSD(?:280|420W?|490W?)\b','KG T'),
('混凝土主材',r'混凝土','M3'),
('結構鋼骨',r'鋼柱工程|結構鋼骨|鋼骨','KG T'),
('模板工程',r'模板|板模','M2 建坪'),
('續接器',r'續接器','組 個 支'),
('土方開挖運棄',r'土方|棄土|棄方|挖土','M3'),
('回填工程',r'回填|級配','M3'),
('鷹架及施工架',r'鷹架|室內架|施工架|排架|滿堂架|拆架','M M2 M3 支 座 工 式'),
('安全設施',r'安全[欄攔]杆|臨時扶手|防墜網|安全母索|安全防護網|安全活動柵門|防塵網','M M2 支 處 件 組'),
('圍籬',r'圍籬|綠籬','M M2 座 式'),
('測量及監測',r'放樣|測量|傾度管|鋼筋計|傾斜計|應變計|觀測|水壓計|土壓計','建坪 處 組 點 式 式月'),
('防水工程',r'防水|止漏|止水帶|止水條','M M2 式'),
('磁磚鋪貼',r'磁磚|地磚|壁磚|石英磚|車道磚|丁掛磚|拋光磚|隔熱磚','M2'),
('油漆塗裝',r'刷漆|水泥漆|晴雨漆|洋灰漆|乳膠漆|外牆塗料|塗裝|烤漆','M M2'),
('粉光泥作',r'粉光|水泥砂漿|批土|粉刷|打底','M M2'),
('天花板',r'天花|矽酸鈣板','M2'),
('輕隔間',r'輕隔間|輕質隔間|石膏磚','M2'),
('石材工程',r'石材|大理石|人造石|抿石','M2 M 支 組'),
('木地板',r'木地板','M2'),
('耐磨地坪',r'EPOXY|耐磨地坪','M2'),
('防火門',r'防火門','樘'),
('門窗工程',r'鋁窗|百葉窗|固定窗|鋁門|木纖門|木作拉門|塑鋼門|玻璃門|自動門|鋼板門|捲門|防水閘門','樘'),
('玻璃工程',r'玻璃','才 M2'),
('欄杆扶手',r'欄杆|扶手','M 組'),
('金屬板及格柵',r'金屬板|金屬件|包板|包版|格柵|隔柵','M2 M 組'),
('填縫密封',r'填縫|崁縫|嵌縫|矽利康','M 樘'),
('排水設備',r'排水溝|截水溝|集水槽|排水板|雨水回收|污水處理|汙水處理','M M2 組 式'),
('電梯設備',r'升降梯|無障礙電梯','部'),
('清潔清運',r'清潔|清運|廢棄物','M2 坪 M3 月 式'),
('交通標線',r'劃線|畫線|標線','位 M2 式'),
('植栽景觀',r'植栽|沃土|植草磚','M2 M3 式 株'),
('爬梯',r'爬梯','支 座'),
('人孔蓋',r'人孔蓋','個 組'),
('運輸費',r'運費|運輸|運棄','式 M3 次'),
('純施工工資',r'工資|點工|操作手','KG T M M2 M3 支 式 工 月'),
]
def classify(name,raw_unit,item='',qty=None):
    n,u=norm(name),unit(raw_unit)
    hits=[f for f,p,us in RULES if u in us.split() and re.search(p,n)]
    # Identity is separate from quantity eligibility. Never discard a service from identity review.
    if '續接器' in n: hits=[h for h in hits if h!='鋼筋主材']
    if '鋼筋組立用' in n: hits=[h for h in hits if h!='鋼筋主材']
    if '工資' in n or '運費補貼' in n: hits=[h for h in hits if h not in ('鋼筋主材','混凝土主材')]
    status='唯一候選' if len(hits)==1 else '多候選' if hits else '未對應'
    q='非本輪鋼筋混凝土估算目標'
    if '鋼筋主材' in hits or '混凝土主材' in hits:
        if qty is None or qty<=0: q='待審：非正量或空白'
        elif norm(item).startswith('拆') or re.search('切除|拆除|打除|工資|運費|租賃|補貼',n): q='待審：拆除或服務'
        elif len(hits)!=1 or re.search('回填|或|不含|另計|組立用|續接器',n): q='待審：材料或計量範圍歧義'
        elif re.search('彎紮|綁紮|組立|壓送|澆築|澆置|供料|施工',n): q='待審：工料合併計量'
        else: q='候選：材料計量仍須核定'
    return status,' / '.join(hits),q

def read_sheet(path,sheet='標單'):
    with zipfile.ZipFile(path) as z:
        w=ET.fromstring(z.read('xl/workbook.xml')); rel=ET.fromstring(z.read('xl/_rels/workbook.xml.rels')); links={x.get('Id'):x.get('Target') for x in rel}
        ss=[''.join(t.text or '' for t in x.findall('.//m:t',NS)) for x in ET.fromstring(z.read('xl/sharedStrings.xml'))] if 'xl/sharedStrings.xml' in z.namelist() else []
        sh=next(s for s in w.find('m:sheets',NS) if s.get('name')==sheet); t=links[sh.get('{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id')]; t=t.lstrip('/') if t.startswith('/') else 'xl/'+t
        for r in ET.fromstring(z.read(t)).findall('.//m:sheetData/m:row',NS):
            d={}
            for c in r:
                v=c.find('m:v',NS); val=v.text if v is not None else ''.join(t.text or '' for t in c.findall('.//m:is//m:t',NS))
                if c.get('t')=='s' and val: val=ss[int(val)]
                if val: d[re.match(r'[A-Z]+',c.get('r')).group()]=val.strip()
            yield int(r.get('r')),d

def run(path,layout):
    uc,qc,first=('D','E',4) if layout=='final' else ('C','D',5)
    out=[]; major=''
    for rn,d in read_sheet(path):
        if rn<=first: continue
        name=d.get('B',''); u=d.get(uc,''); qty=num(d.get(qc,'')); item=d.get('A','')
        if name and not u and qty is None and re.fullmatch(r'[（(]?[一二三四五六七八九十百]+[）).、]?',item):
            major=name
            continue
        if not name or re.search('小計|合計|總計',name): continue
        if not u and qty is None and not re.search('增|拆',item): continue
        status,hits,q=classify(name,u,item,qty)
        if not u: status,hits,q='缺單位待審','','待審：缺單位'
        elif qty is None: q='待審：缺數量'
        elif qty<=0: q='待審：非正量'
        elif q.startswith(('候選：','待審：工料合併')) and layout=='final' and major!='結構體工程':
            q='待審：超出目前結構體估算範圍' if q.startswith('候選：') else '待審：工料合併及工程範圍'
        # Local output retains the raw name only for auditing; do not publish it.
        out.append([rn,major,item,name,u,str(qty) if qty is not None else '',status,hits,q])
    return out

def selftest():
    cases=[('鋼筋材料費','kg','1','鋼筋主材','候選：材料計量仍須核定'),('鋼筋及彎紮組立','T','1','鋼筋主材','待審：工料合併計量'),('鋼筋綁紮工資','kg','1','純施工工資','非本輪鋼筋混凝土估算目標'),('混凝土壓送工資','m3','1','純施工工資','非本輪鋼筋混凝土估算目標'),('預拌混凝土壓送澆築','m3','拆','混凝土主材','待審：拆除或服務'),('鋼筋續接器','組','1','續接器','非本輪鋼筋混凝土估算目標'),('混凝土或級配料回填','M3','1','混凝土主材 / 回填工程','待審：材料或計量範圍歧義'),('鋼柱工程','kg','1','結構鋼骨','非本輪鋼筋混凝土估算目標'),('鋼筋','M3','1','','非本輪鋼筋混凝土估算目標'),('模板加工組立及拆除','建坪','1','模板工程','非本輪鋼筋混凝土估算目標')]
    for n,u,i,h,q in cases:
        actual=classify(n,u,i,Decimal(1)); assert actual[1:]==(h,q),(n,actual)
    print('Synthetic rule checks:',len(cases),'PASS')
if __name__=='__main__':
    p=argparse.ArgumentParser(); p.add_argument('workbook',nargs='?');p.add_argument('--layout',choices=['final','review'],default='final');p.add_argument('--out');p.add_argument('--selftest',action='store_true');a=p.parse_args()
    if a.selftest: selftest()
    if a.workbook:
        rows=run(a.workbook,a.layout);print('rows',len(rows),'status',dict(Counter(x[6] for x in rows)),'quantity',dict(Counter(x[8] for x in rows)))
        print('rows with factory suffix:',sum('－廠房作業區' in x[3] for x in rows))
        if a.out:
            with open(a.out,'w',encoding='utf-8-sig',newline='') as f:
                w=csv.writer(f);w.writerow(['Excel列號','原始大項線索','項次','原始工項','單位','數量','對應候選狀態','候選工項族','數量資格']);w.writerows(rows)
