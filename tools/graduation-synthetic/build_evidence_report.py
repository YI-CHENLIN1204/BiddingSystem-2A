#!/usr/bin/env python3
"""Make a self-contained, no-network evidence HTML from v2 synthetic benchmark.

All charts prominently identify synthetic proxy validation, not real accuracy.
"""
import argparse
import csv
import hashlib
import html
import json
from collections import Counter, defaultdict
from pathlib import Path


def read_verified(source):
    manifest=json.loads((source/"manifest.json").read_text(encoding="utf-8"))
    if manifest.get("generator_version")!="2.0.0" or manifest.get("data_origin")!="SYNTHETIC":
        raise ValueError("Requires v2 SYNTHETIC source")
    outputs={}
    for filename in ("projects.csv","historical_transactions.csv","project_boq_rows.csv"):
        if hashlib.sha256((source/filename).read_bytes()).hexdigest()!=manifest["files_sha256"].get(filename):
            raise ValueError("SHA256 mismatch: "+filename)
        with (source/filename).open(encoding="utf-8-sig",newline="") as f:
            outputs[filename]=list(csv.DictReader(f))
    return manifest,outputs


def compute(source,benchmark):
    manifest,data=read_verified(source)
    report=json.loads(benchmark.read_text(encoding="utf-8"))
    if report.get("origin")!="SYNTHETIC" or report.get("benchmark")!="OFFLINE_FULL_BOQ_PROXY_NOT_CSHARP_API":
        raise ValueError("Benchmark must be v2 SYNTHETIC offline proxy")
    if report["boq_line_count"]!=manifest["boq_line_count"] or report["historical_line_count"]!=manifest["transaction_count"]:
        raise ValueError("Benchmark counts mismatch")
    projects=data["projects.csv"]
    lines=data["project_boq_rows.csv"]
    history=data["historical_transactions.csv"]
    counts=Counter(x["synthetic_project_id"] for x in lines)
    categories=Counter(x["major_category"] for x in lines)
    struct=Counter(x["structure_type"] for x in projects)
    splits=Counter(x["split"] for x in projects)
    methods=report["method_comparison"]
    mape={k:v.get("overall_mape_percent") for k,v in methods.items()}
    coverage={k:v.get("overall_coverage_percent") for k,v in methods.items()}
    if any(v is None for v in list(mape.values())+list(coverage.values())):
        raise ValueError("benchmark missing estimated metrics")
    return {"projects":len(projects),"lines":len(lines),"history":len(history),
            "material_codes":len({x["material_code"] for x in lines}),
            "min_lines":min(counts.values()),"max_lines":max(counts.values()),
            "distinct_line_counts":len(set(counts.values())),
            "categories":categories,"structures":struct,"splits":splits,
            "mape":mape,"coverage":coverage,"benchmark":report}


def bar(label,value,max_value,color="#2563eb",suffix=""):
    width=max(1,min(100,round(100*value/max_value))) if max_value else 1
    return (f'<div class="bar-row"><span class="bar-label">{html.escape(str(label))}</span>'
            f'<div class="bar-track"><div class="bar-fill" style="width:{width}%;background:{color}"></div></div>'
            f'<strong>{value:,.2f}{suffix}</strong></div>')


def make_html(d):
    method_labels={"weighted_quantile_proxy":"相似案例加權分位數（離線代理）",
                   "weighted_normal_proxy":"加權常態分布（離線代理）"}
    mape="".join(bar(method_labels.get(k,k),v,35,"#0f766e","%") for k,v in d["mape"].items())
    coverage="".join(bar(method_labels.get(k,k),v,100,"#4f46e5","%") for k,v in d["coverage"].items())
    cats="".join(bar(k,v,max(d["categories"].values()),"#b45309"," 筆") for k,v in d["categories"].most_common())
    structs="".join(bar(k,v,max(d["structures"].values()),"#0284c7"," 案") for k,v in d["structures"].items())
    info=html.escape(json.dumps({"projects":d["projects"],"lines":d["lines"],"historical":d["history"],
         "material_codes":d["material_codes"],"splits":dict(d["splits"])},ensure_ascii=False,indent=2))
    return f'''<!doctype html><html lang="zh-Hant"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>2A 畢專｜合成資料與估算模型驗證</title><style>
:root{{--ink:#1e293b;--muted:#64748b;--line:#dce3ed;--bg:#f5f7fb;}}
*{{box-sizing:border-box}}body{{font:15px/1.65 "Microsoft JhengHei",system-ui,sans-serif;margin:0;background:var(--bg);color:var(--ink)}}
main{{max-width:1120px;margin:auto;padding:32px 22px}}header{{background:#12243c;color:white;padding:28px;border-radius:14px}}
h1{{font-size:26px;margin:0 0 10px}}h2{{font-size:19px;margin:0 0 17px}}p{{margin:6px 0}}.notice{{margin-top:17px;padding:13px 17px;border:1px solid #eab308;background:#fff8d5;border-radius:8px;color:#693e0c;font-weight:600}}
.cards{{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px;margin:18px 0}}.card,.panel{{background:white;border:1px solid var(--line);border-radius:11px;padding:18px}}
.card strong{{font-size:25px;display:block}}.card span{{color:var(--muted)}}.grid{{display:grid;grid-template-columns:1fr 1fr;gap:18px;margin-top:18px}}
.bar-row{{display:grid;grid-template-columns:175px minmax(80px,1fr) 86px;align-items:center;gap:10px;margin:10px 0}}
.bar-label{{overflow-wrap:anywhere;font-size:12px}}.bar-track{{height:17px;background:#e5eaf0;border-radius:6px;overflow:hidden}}.bar-fill{{height:100%;border-radius:6px}}strong{{font-variant-numeric:tabular-nums}}
small,footer{{color:var(--muted)}}pre{{white-space:pre-wrap;overflow-wrap:anywhere;background:#f1f5f9;padding:14px;border-radius:8px}}
@media(max-width:850px){{.cards,.grid{{grid-template-columns:1fr 1fr}}.bar-row{{grid-template-columns:130px 1fr 65px}}}}
@media(max-width:550px){{.cards,.grid{{grid-template-columns:1fr}}}}
@media print{{body{{background:white}}main{{max-width:100%;padding:10px}}.panel,.card,header{{break-inside:avoid}}}}
</style></head><body><main><header><h1>2A 畢專：合成標單與模型驗證</h1>
<p>展示內容：建案多樣性、工程分類、歷史資料對應與兩種估算方法的離線基準比較</p>
<p>所有數值均來自 SYNTHETIC v2.0，尚未執行正式 ASP.NET 系統 E2E。</p></header>
<div class="notice">這是合成資料的測試證據，不是施工規範、企業實測或真實建案估算準確率。圖中誤差越低越好；P15～P85 目標涵蓋率為 70%。</div>
<section class="cards"><div class="card"><strong>{d["projects"]:,}</strong><span>模擬建案數</span></div>
<div class="card"><strong>{d["history"]:,}</strong><span>同源歷史工程交易</span></div>
<div class="card"><strong>{d["material_codes"]:,}</strong><span>工項材料碼種類</span></div>
<div class="card"><strong>{d["min_lines"]}～{d["max_lines"]}</strong><span>每建案工項筆數範圍</span></div></section>
<div class="grid"><section class="panel"><h2>方法比較：P50 絕對百分比誤差（MAPE）</h2>{mape}<small>越小越好。只評估可取得足夠相似歷史樣本的合成案件。</small></section>
<section class="panel"><h2>區間品質：P15～P85 實際涵蓋率</h2>{coverage}<small>名義目標為 70%；不能只看涵蓋率，還須搭配區間寬度及區間評分。</small></section></div>
<div class="grid"><section class="panel"><h2>各工程類別明細規模</h2>{cats}</section>
<section class="panel"><h2>建案結構分布</h2>{structs}
<p><small>train / validation / test 按建案切分；不能將同一案不同樓層工項同時放進訓練與測試。</small></p></section></div>
<section class="panel" style="margin-top:18px"><h2>資料完整性與限制</h2>
<p>每一筆歷史交易均與同一筆工程明細以 synthetic_transaction_id 對應。預測評估時以建案 × 材料碼彙總，避免重複樓層造成虛增樣本。</p>
<p>限制作法：工項模板、工法假設、數量係數、單價與施工日期均為合成；需以來源可核對的企業或公開工程資料進行外部驗證，才能陳述真實精度。</p>
<details><summary>輸出摘要與版本證據</summary><pre>{info}</pre></details></section>
<footer>OFFLINE_FULL_BOQ_PROXY_NOT_CSHARP_API ｜ SYNTHETIC ｜ 禁止作為實際營造工程估價或正常值門檻。</footer></main></body></html>'''


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source",type=Path,required=True)
    parser.add_argument("--benchmark",type=Path,required=True)
    parser.add_argument("--output",type=Path,required=True)
    args=parser.parse_args()
    if args.output.exists():parser.error("refusing overwrite")
    try:
        report=compute(args.source,args.benchmark)
        result=make_html(report)
        args.output.parent.mkdir(parents=True,exist_ok=True)
        args.output.write_text(result,encoding="utf-8")
    except (ValueError,KeyError,OSError,json.JSONDecodeError) as error:
        parser.error(str(error))
    print(json.dumps({"output":str(args.output),"projects":report["projects"],
                      "historical_lines":report["history"],"material_codes":report["material_codes"]},ensure_ascii=False))


if __name__=="__main__":
    main()
