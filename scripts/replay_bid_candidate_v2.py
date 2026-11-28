"""Offline family expansion and material-ID shortlist; never approve or publish.
Input: v1 review CSV, optional output CSV. All original columns are preserved.
"""
import argparse,csv,re
from pathlib import Path
from collections import Counter
from replay_bid_candidate import norm,unit

# New families only when v1 has no candidate. Unit is a compatibility filter.
EXTRA=[
('臨時設施及工務所','工務所|臨時廁所|施工便梯|施工走廊|行人安全走廊','式 M 座'),
('臨時水電及設備','抽水電費|抽水設備|工地夜間照明|緊急發電機|臨時供水|臨時供電','式 月'),
('施工機具及運輸服務','施工電梯|塔吊|壓送車|千斤頂預壓','式 次 處'),
('行政檢測及工程服務','消毒費|鄰房鑑定|工程告示牌|污染防治費|設計及取得標章|技師簽證|風雨試驗|美容|現場保護|陽角修補費|配合請照','次 式 M'),
('道路修復','人行道修復|道路AC修復','M M2'),
('擋土及基礎施工','壓梁|擋土柱|擋土牆|中間支柱|中間柱|構台共構柱|切樁|水平支撐|施工構台|護圈|搭接回填','M M2 M3 支 KG T 處 個'),
('灌漿工程','灌漿費|灌漿|無收縮水泥','M3 式'),
('施工架構件','工作平台|延伸架|帆布網|壁拉桿|安全斜籬|三角架|鐵夾板|踢腳板|遮斷板|斜板|樑下滿鋪|安全斜撐|扶手先行|地下室階段上下設備|桁架開口|樓層標示帆布','M M2 支 座 個 件 處'),
('螺栓及植筋','螺栓|植筋','支 處'),
('止水墩','止水墩','M'),
('地坪施工輔具','地坪標高器','M2'),
('裝修收邊及開口','窗簾盒|燈槽|檢修口|暖風機口|燈孔','M 個'),
('清水面處理','拆模不處理','M2'),
('門窗工程','鐵板拉門|固定.*窗|上懸窗|社區側門','樘'),
('停車交通設備','車輪擋|柱角防撞條|柵欄機|管制號誌|響鈴式指示燈|警示燈|車道反射鏡','組 支 個'),
('無障礙及標誌','無障礙標誌|警示磚|無障礙指標|地板指示|店鋪招牌|各戶門牌','處 式 組'),
('浴廁設備','(?:浴廁|廁所).*明鏡|(?:浴廁|廁所).*檯面|淋浴拉門','個 座 組'),
('生活附屬設備','曬衣架|曬被架|回收桶|垃圾集中槽','組 個 式'),
('防雷工程','避雷針','式'),
('臨時牆體','假牆','M2'),
('景觀及戶外設施','戶外大樹區|碎石|路床分隔板|百歲磚|景石|街道座椅|座椅.*鐵件|不織布','M2 M 座 式'),
('消防箱體','消防送水口','座'),
('金屬補強','鐵件補強','處'),
('複合地坪','聚尿.*厚磚','M2'),
]
# Provisional lookup into the inspected active master. A family-level match alone
# cannot assert equivalence of specification, project type, or work scope.
LINKS={
'鋼筋主材':[(2,'KG')],'混凝土主材':[(1,'M3')],'結構鋼骨':[(3,'KG')],
'模板工程':[(8,'M2'),(52,'M2'),(9,'M2')], '圍籬':[(4,'M')],
'土方開挖運棄':[(5,'M3'),(49,'M3')],'回填工程':[(7,'M3'),(50,'M3')],
'防水工程':[(10,'M2'),(11,'M'),(23,'M'),(27,'M2'),(58,'M2'),(59,'M'),(60,'M2')],
'磁磚鋪貼':[(17,'M2')],'油漆塗裝':[(26,'M2'),(67,'M2')],
'粉光泥作':[(35,'M2'),(55,'M2'),(56,'M2')],'天花板':[(30,'M2'),(68,'M2')],
'輕隔間':[(29,'M2'),(54,'M2')],'石材工程':[(28,'M2'),(71,'M2')],
'耐磨地坪':[(41,'M2'),(102,'M2')],'防火門':[(18,'樘'),(64,'樘')],
'門窗工程':[(12,'樘'),(62,'樘'),(63,'樘'),(65,'樘'),(101,'樘')],
'欄杆扶手':[(13,'M')],'金屬板及格柵':[(66,'M2'),(103,'M2')],
'填縫密封':[(59,'M')],'排水設備':[(19,'M'),(75,'M'),(97,'M')],
'交通標線':[(33,'M2'),(100,'M')],'植栽景觀':[(96,'M2')],
'道路修復':[(98,'M2')], '螺栓及植筋':[(53,'處')],
}
def refine(row):
    n,u=norm(row['原始工項']),unit(row['單位'])
    old=row['候選工項族'].split(' / ') if row['候選工項族'] else []
    hits=old or [f for f,p,us in EXTRA if u in us.split() and re.search(p,n)]
    # Prefer a composite work identity, retaining component families for audit.
    # Only explicit relationships, never arbitrary keyword priority.
    h=set(hits); identity=' / '.join(hits); kind='單一工項族' if len(hits)==1 else '多義待審' if hits else '無候選'
    prep={'粉光泥作','輕隔間'}
    if len(h)>1 and '磁磚鋪貼' in h and h<=prep|{'磁磚鋪貼','防水工程'} and re.search('貼|鋪',n):
        identity='磁磚鋪貼複合工項';kind='複合工項候選'
    elif len(h)>1 and '油漆塗裝' in h and h<=prep|{'油漆塗裝','天花板','防水工程'} and re.search('漆|塗裝',n):
        identity='塗裝複合工項';kind='複合工項候選'
    elif len(h)>1 and h=={'安全設施','欄杆扶手'} and re.search('臨時|安全',n):
        identity='施工安全欄杆扶手';kind='複合工項候選'
    elif len(h)>1 and '鷹架及施工架' in h and h<={'鷹架及施工架','安全設施','欄杆扶手','爬梯','純施工工資'}:
        identity='施工架附屬工項';kind='複合工項候選'
    # Preserve ambiguous alternatives (e.g. mixed fill). No automatic master ID.
    ids=sorted({i for f in hits for i,v in LINKS.get(f,[]) if v==u or (v=='KG' and u=='T')})
    if kind=='複合工項候選': ids=[] # a component ID is not the composite identity
    if h & {'鋼筋主材','混凝土主材'} and (len(h)>1 or re.search('工資|運費|切除|不含|組立用|續接器',n) or norm(row['項次']).startswith('拆')): ids=[]
    if h=={'粉光泥作'} and re.fullmatch(r'(?:F\d+(?:-\d+)?)?(?:地坪)?整體粉光',n):
        identity='整體粉光工資';ids=[] # confirmed standalone wording only; no inference for EPOXY bundles
    if '螺栓及植筋' in h and '植筋' not in n: ids=[i for i in ids if i!=53]
    return dict(row,**{'V2候選工項族':' / '.join(hits),'V2建議工項身分':identity,'V2身分狀態':kind,'既有主檔候選ID':' / '.join(map(str,ids)),'主檔狀態':('待核定候選' if ids else '需另建或確認主檔' if hits else '待分類'),'歷史回寫':'禁止：本輪僅候選'})

def selftest():
    def r(n,u,h='',i='1'):
        return refine({'原始工項':n,'單位':u,'候選工項族':h,'項次':i})
    assert r('抽水電費','月')['V2建議工項身分']=='臨時水電及設備'
    assert r('砂漿打底貼磁磚','M2','磁磚鋪貼 / 粉光泥作')['V2身分狀態']=='複合工項候選'
    assert not r('砂漿打底貼磁磚','M2','磁磚鋪貼 / 粉光泥作')['既有主檔候選ID']
    assert r('混凝土或級配回填','M3','混凝土主材 / 回填工程')['V2身分狀態']=='多義待審'
    assert not r('混凝土或級配回填','M3','混凝土主材 / 回填工程')['既有主檔候選ID']
    assert r('鋼筋材料','噸','鋼筋主材')['既有主檔候選ID']=='2'
    assert not r('鋼筋材料','噸','鋼筋主材','拆')['既有主檔候選ID']
    assert not r('模板加工組立','建坪','模板工程')['既有主檔候選ID']
    assert not r('抽水電費','')['V2候選工項族']
    assert r('F11 整體粉光','M2','粉光泥作')['V2建議工項身分']=='整體粉光工資'
    assert not r('F11 整體粉光','M2','粉光泥作')['既有主檔候選ID']
    assert r('無障礙廁所明鏡','個')['V2建議工項身分']=='浴廁設備'
    print('Synthetic V2 checks: 12 PASS')

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('source',nargs='?');p.add_argument('--out');p.add_argument('--selftest',action='store_true');a=p.parse_args()
    if a.selftest:selftest()
    if a.source:
        with open(a.source,encoding='utf-8-sig',newline='') as f: rows=[refine(x) for x in csv.DictReader(f)]
        print('Rows:',len(rows),'identity:',dict(Counter(x['V2身分狀態'] for x in rows)),'ID status:',dict(Counter(x['主檔狀態'] for x in rows)))
        if a.out:
            if Path(a.out).resolve()==Path(a.source).resolve(): p.error('output must differ from source')
            with open(a.out,'w',encoding='utf-8-sig',newline='') as f:
                w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
