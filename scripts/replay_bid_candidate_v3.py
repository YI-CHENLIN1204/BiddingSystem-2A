"""Conservative shortlist refinement for V2 review CSV. No approvals or DB access."""
import argparse,csv,re
from collections import Counter
from pathlib import Path
from replay_bid_candidate import norm,unit

# Each cue is an explicit positive feature, not proof of equivalent work scope.
CUES={
  1:r'混凝土',2:r'鋼筋',3:r'鋼柱工程|結構鋼骨',
  8:r'普通模板.*柱梁',52:r'柱梁版.*普通模板',9:r'鋁模.*租賃|樓版支撐.*租賃',
  10:r'外牆.*(?:PU|聚氨酯).*防水|外牆.*防水.*(?:PU|聚氨酯)',
  27:r'屋頂.*防水.*(?:保護層|隔熱材)',58:r'外牆.*防水.*(?:兩道|二道)',60:r'(?:浴廁|廁所).*防水.*試水',
  11:r'連續壁.*止水',23:r'(?:筏基|底版).*止水帶',59:r'窗框.*(?:填縫|封膠).*防水|窗框.*防水.*(?:填縫|封膠)',
  12:r'鋁窗框.*玻璃.*安裝',62:r'推射鋁窗|鋁合金推射窗',63:r'氣密.*固定.*玻璃窗',65:r'梯廳.*不[鏽銹]鋼門框',101:r'工業捲門',
  18:r'防火門.*(?:框扇|含五金)',64:r'(?:雙開|雙扇)防火門',
  5:r'基坑.*土方.*運棄',49:r'基礎.*開挖.*土方.*外運',
  13:r'不[鏽銹]鋼欄杆',17:r'60[×X*]60.*磁磚.*打底|打底.*60[×X*]60.*磁磚',
  28:r'外牆.*乾[掛卦].*石材',71:r'大廳.*石材.*地坪',29:r'輕隔間.*(?:C型|雙面封板)',54:r'輕質隔間磚.*砌築',
  26:r'樓梯間.*油漆.*批土',67:r'乳膠漆.*二底二度',35:r'混凝土.*(?:表面修補).*粉光',55:r'外牆.*粉刷.*打底',56:r'室內.*地坪.*水泥砂漿.*找平',
  30:r'公共區域.*天花板.*輕鋼架',68:r'石膏板.*輕鋼架.*天花',
  19:r'屋頂.*排水.*泛水',75:r'雨水立管.*落水頭',97:r'戶外.*排水側溝.*鑄鐵蓋',
  41:r'地下室.*地坪.*耐磨塗裝',102:r'廠房.*耐磨地坪',66:r'外牆.*鋁板.*骨架',103:r'廠房.*金屬外牆板',98:r'室外.*道路鋪面.*路緣石',96:r'庭園.*植栽.*客土',
}
def refine(row):
    n=norm(row['原始工項']); old=[int(x) for x in row['既有主檔候選ID'].split(' / ') if x]
    strong=[i for i in old if i in CUES and re.search(CUES[i],n)]
    if re.search('推射鋁門|推射鋁窗.*固定|固定.*推射鋁窗',n): strong=[i for i in strong if i!=62]
    # A cue only prioritizes review. The old shortlist remains visible.
    state='特徵支持唯一候選' if len(strong)==1 else '特徵支持多候選' if strong else '近鄰不足以核定' if old else '需建立或確認標準工項' if row['V2候選工項族'] else '待分類'
    return dict(row,**{'V3特徵候選ID':' / '.join(map(str,strong)),'V3主檔覆核狀態':state,'V3工項目錄待建群組':(row['V2建議工項身分']+' / '+unit(row['單位'])) if not strong and row['V2候選工項族'] else '', 'V3回寫資格':'禁止：待人工核定'})

def selftest():
    def r(n,ids):return refine({'原始工項':n,'既有主檔候選ID':ids,'V2候選工項族':'測試族','V2建議工項身分':'測試族','單位':'樘'})['V3特徵候選ID']
    cases=[('木纖門','12 / 62 / 63 / 65 / 101',''),('推射鋁窗','12 / 62 / 63 / 65 / 101','62'),('推射鋁窗+固定鋁窗','12 / 62 / 63',''),('固定鋁百葉窗','12 / 62 / 63',''),('雙開防火門','18 / 64','64'),('防火門','18 / 64',''),('清水模板','8 / 9 / 52',''),('中欄杆','13',''),('實木扶手','13',''),('外牆乾掛石材','28 / 71','28'),('鋼筋材料費','2','2'),('抽水電費','','')]
    for n,ids,expected in cases: assert r(n,ids)==expected,(n,r(n,ids),expected)
    print('V3 synthetic checks:',len(cases),'PASS')
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('source',nargs='?');p.add_argument('--out');p.add_argument('--selftest',action='store_true');a=p.parse_args()
    if a.selftest:selftest()
    if a.source:
        with open(a.source,encoding='utf-8-sig',newline='') as f: rows=[refine(x) for x in csv.DictReader(f)]
        print('Rows',len(rows),dict(Counter(x['V3主檔覆核狀態'] for x in rows)))
        if a.out:
            if Path(a.out).resolve()==Path(a.source).resolve():p.error('output must differ from source')
            with open(a.out,'w',encoding='utf-8-sig',newline='') as f:
                w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
