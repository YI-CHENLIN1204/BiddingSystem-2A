"""Build report figures and Word document from the offline evidence."""
import csv
import json
import re
import shutil
import zipfile
from pathlib import Path
from xml.sax.saxutils import escape as esc
from PIL import Image, ImageDraw, ImageFont

out=Path("deliverables/factory-fairness-v2")
base=Path("work/BiddingSystem-2A/research/2a_synthetic/output/evening-2a")
def read(path):
    with path.open(encoding="utf-8-sig",newline="") as f:
        return list(csv.DictReader(f))
for name in ("factory_mapping_safety.csv","labor_savings_scenarios.csv","factory_t05_metrics.csv"):
    shutil.copyfile(base/"factory-b1"/name,out/name)
shutil.copyfile(base/"factory-dictionary-67/dictionary_growth.csv",out/"dictionary_growth.csv")
for name in ("factory_a0_a1_pairs.csv","factory_a0_a1_90pct_pairs.csv"):
    shutil.copyfile(out.parent/name,out/name)
for name in ("past_only_factory_a0_a1_pairs.csv","past_only_factory_a0_a1_90pct_pairs.csv"):
    shutil.copyfile(base/"factory-b1"/name,out/name)

def plot(name,title,series,caption,unit):
    width,height=1800,240+75*len(series)
    img=Image.new("RGB",(width,height),"white")
    d=ImageDraw.Draw(img)
    svg=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}"><rect width="100%" height="100%" fill="white"/>']
    def txt(x,y,t,size):
        d.text((x,y),t,font=ImageFont.truetype("C:/Windows/Fonts/msjh.ttc",size),fill="#172d43")
        svg.append(f'<text x="{x}" y="{y+size}" font-size="{size}" font-family="Microsoft JhengHei,sans-serif" fill="#172d43">{esc(t)}</text>')
    lo=min(0,min(v for _,v in series)); hi=max(0,max(v for _,v in series))
    scale=lambda v:510+int((v-lo)/(hi-lo)*1030)
    txt(45,20,title,48);txt(45,83,unit,32)
    for i,(label,v) in enumerate(series):
        y=133+75*i
        txt(45,y,label,32)
        left,right=sorted((scale(0),scale(v)))
        color="#246e73" if v>=0 else "#b45742"
        d.rectangle((left,y,right,y+31),fill=color)
        svg.append(f'<rect x="{left}" y="{y}" width="{right-left}" height="31" fill="{color}"/>')
        txt(right+14,y-6,f"{v:.3f}",30)
    txt(45,height-64,caption,26)
    img.save(out/(name+".png"),dpi=(300,300))
    (out/(name+".svg")).write_text("".join(svg)+"</svg>",encoding="utf8")

source="FACTORY_20261122_V1 合成廠房測試集"
plot("01-e1","E1 同參考案區間分數",[("70% A0",.5302),("70% A1",.5026),("90% A0",.6943),("90% A1",.6664)],
     source+"｜26 案、78 案項｜8 案同權重、樓地板面積","相對區間分數，越低越好")
labels={"SYN-CONCRETE":"混凝土","SYN-REBAR":"鋼筋","structure_price":"結構體金額"}
e2=[r for r in read(out/"equal_sample_summary.csv") if r["split"]=="test" and r["level"]=="0.7"]
for k in ("5","8"):
    plot(f"02-e2-k{k}",f"E2 K{k} 等樣本數主分析",
         [(f'{labels[r["scope"]]} {m.upper()}（n={r["paired"]}）',float(r[m+"_score"]))
          for r in e2 if r["k"]==k for m in ("a0","b1")],
         source+"｜同規格、同單位、樓地板面積｜開工日代理｜70%","共同可估案的相對區間分數")
e3=[r for r in read(out/"e3_summary.csv") if r["level"]=="0.7"]
for metric,title,unit,scale in (("score","E3 完整方法區間分數","相對區間分數",1),("available","E3 完整方法可估性","可估建案數／26",1),("hit","E3 完整方法涵蓋率","百分比",100),("width","E3 完整方法相對寬度","區間寬／真值",1),("ape","E3 完整方法中位數誤差","P50 MAPE 百分比",100)):
    plot("03-e3-"+metric,title,[(f'{labels[r["scope"]]} {m.upper()}（n={26 if metric=="available" else r["paired"]}）',float(r[m+"_"+metric])*scale)
                              for r in e3 for m in ("a0","b1")],
         source+"｜較早開工｜70%｜金額 A0 樓地板面積、B1 基地面積",unit)
curve=[r for r in read(out/"dictionary_growth.csv") if r["split"]=="test"]
plot("04-dictionary","字典規模與材料碼對應",[(r["k"]+" 詞",100*int(r["correct"])/int(r["total"])) for r in curve],
     source+"｜3,386 列、26 案｜訓練字典與已見名稱","正確對應率（%）")
plot("05-dictionary-guard","材料對應後的放行與待審",[("保守放行",207),("待人工",3179),("錯誤放行",0)],
     source+"｜3,386 列｜按工料範圍、單位及規格套用規則","明細列數")
labor=read(out/"labor_savings_scenarios.csv")
for n in ("100","500","1000"):
    selected=[r for r in labor if r["split"]=="test" and r["rows"]==n]
    names={"conservative":"保守","middle":"中性","optimistic":"樂觀"}
    plot("06-labor-"+n,f"{n} 列標單覆核工時情境",[(names[r["scenario"]]+"／"+r["amortize"]+" 份",float(r["saving_minutes"])) for r in selected],
         source+"｜3,386 列為放行比例基礎｜情境推估","估計每份節省分鐘")
plot("07-time","日期限制前後的 E1 區間分數",
     [("回溯 A0（78 案項）",.5302),("回溯 A1（78 案項）",.5026),("較早開工 A0（60 案項）",.7501),("較早開工 A1（60 案項）",.7266)],
     source+"｜70%｜樓地板面積｜各條件的共同可估集合","相對區間分數")

md=(out/"Factory-Fairness-Report-ZH.md").read_text(encoding="utf8")
W='http://schemas.openxmlformats.org/wordprocessingml/2006/main'
def par(t,size=22,bold=False):
    bold_xml="<w:b/>" if bold else ""
    return f'<w:p><w:pPr><w:spacing w:after="110"/></w:pPr><w:r><w:rPr><w:rFonts w:ascii="Calibri" w:eastAsia="Microsoft JhengHei"/><w:sz w:val="{size}"/>{bold_xml}</w:rPr><w:t xml:space="preserve">{esc(t)}</w:t></w:r></w:p>'
body=[]
in_table=False
for line in md.splitlines():
    if line.startswith("|"):
        if line.startswith("|---"):
            continue
        values=[v.strip() for v in line.strip("|").split("|")]
        if not in_table:
            body.append('<w:tbl><w:tblPr><w:tblW w:w="5000" w:type="pct"/><w:tblBorders><w:bottom w:val="single" w:sz="4"/><w:insideH w:val="single" w:sz="4" w:color="DCE5EA"/></w:tblBorders></w:tblPr><w:tblGrid>'+
                        ''.join(f'<w:gridCol w:w="{9600//len(values)}"/>' for _ in values)+'</w:tblGrid>')
            in_table=True
        body.append('<w:tr><w:trPr><w:cantSplit/></w:trPr>'+''.join('<w:tc>'+par(v,19)+'</w:tc>' for v in values)+'</w:tr>')
        continue
    if in_table:
        body.append('</w:tbl>')
        in_table=False
    if line:
        text=re.sub(r'\[([^]]+)\]\([^)]+\)',r'\1',line).replace("**","").replace("`","")
        body.append(par(text.lstrip("# "),38 if line.startswith("# ") else 29 if line.startswith("## ") else 22,line.startswith("#")))
if in_table:
    body.append('</w:tbl>')
with zipfile.ZipFile(out/"Factory-Fairness-Report-ZH.docx","w",zipfile.ZIP_DEFLATED) as z:
    z.writestr("[Content_Types].xml",'<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/></Types>')
    z.writestr("_rels/.rels",'<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="r1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>')
    z.writestr("word/document.xml",f'<w:document xmlns:w="{W}"><w:body>'+''.join(body)+'<w:sectPr><w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="980" w:bottom="980" w:left="1000" w:right="1000"/></w:sectPr></w:body></w:document>')
