# 2A 合成 Holdout 回測與參數比較（2026-11-22）

## 結論

已直接跑現有 5263 正式 range API，完成 237 組「留出建案 × 適用估算項目」的目前 TopK 與完整參考池回應（474 次唯讀 range POST），並用現行 C# 六因子規則重現參考排序、最低樣本門檻及數量/價格區間。144 組參數只用 validation 比較；選定方案再對 test 評估。**保留正式參數，不整套採用 validation 最佳方案**：部分材料誤差改善，但 SRC 價格變差，沒有證據支持全面替換。

本文件及 JSON 都是合成數值實驗；不是企業工程準確度驗收。JSON 是可機讀摘要；完整 API 回應留在執行工作區的 research/2a_synthetic/output/live-holdout-20261122/responses/，未上傳企業資料。

## 資料與驗證界線

- 既有 source manifest：SYNTHETIC v2、seed 20261119，300 建案、143,700 筆對齊 BOQ/history，三個 CSV SHA256 與逐列 lineage 均驗證。
- 實際 split 是 train=210、validation=45、test=45，不能把 90 案都叫獨立 test。237 個估算組合也不是 237 個獨立建案。
- 未新增/匯入歷史、未重 Seed、未上傳既有 13 份標單、未建立案件或 AnalysisRun、未改 settings；API settings/case snapshot 前後一致。range 路由的 POST 僅運算，不是上傳或發布。
- 每個 API reference 必須對到來源 train ID、面積、日期及交易/大項數值；SRC reference 必須仍是 SRC。這是本次回測的防洩漏契約，不是重跑先前 9/9 source smoke，也不是新的 Docker 連線證明。
- 每個目前模型重現均須符合 API Top8 參考 ID，並以 0.025 總量/總價的數值容許差核對三個區間端點；未通過就停止實驗。237/237 通過後才比較替代參數。
- 現行 C# 使用目標與參考日期絕對差，允許目標日期之後的 train 建案；本次是回溯留案估算，**不稱為按時間先後的預測驗證**。
- SYN-00026/29/30 曾作為既有 paired/UI 診斷案件，test 並非全新盲測；JSON 另提供排除三案後的 42 案群之非注入/非極端子集指標。不以 test 指標再重選參數。

## SRC test 結果：未注入且非極端的來源觀察值

以下「未注入」只代表沒有記錄生成器 shock，不代表工程師認證正常；P15–P85 名義涵蓋度為 70%，不是 100% 合格門檻。MAE/MAPE 以來源實際合成數字和預估 P50 比較，非逐字 OCR 或 Excel 解析準確度。

| 項目 | n | 現行 MAPE | 候選 MAPE | 現行區間涵蓋率 | 候選區間涵蓋率 |
|---|---:|---:|---:|---:|---:|
| 鋼筋數量 | 8 | 17.25% | 13.20% | 75.00% | 75.00% |
| 混凝土數量 | 8 | 9.92% | 7.94% | 75.00% | 87.50% |
| 結構體價格 | 5 | 14.56% | 24.33% | 60.00% | 20.00% |

SRC 全部 10 個 test 案（含 shock/極端）亦在 JSON 單獨列出；不能將混合壓力樣本的 coverage 稱為正常案例誤報率。RC/SC 的各範圍指標及不同物理單位的 MAE 均分組保存，不混算 kg、m³ 和元。SRC 價格此子集只有 5 案，沒有做顯著性或泛化保證。

## 144 組對照與採用決策

- 六因子順序：結構、樓地板面積、基地面積、樓層、年份、城市。現行 25/30/10/15/10/10；幾何組 20/40/15/15/5/5；結構組 40/25/10/15/5/5。
- TopK=5/8/16/32；半衰期=1/3/6 年/無衰減；相似度指數=0/1/2。保留 minimum samples=5、P15/P50/P85、目前可用候選及 SRC-only 規則。
- 只以 validation 未注入且非極端子集的各「項目/結構」群之相對 interval score 宏平均挑選；同時懲罰區間太寬及漏覆蓋，拒絕靠不出預測來美化分數。
- validation 選出 structure-k32-h3-e1（40/25/10/15/5/5，K=32，半衰期3年，相似指數1）。test 的 SRC 鋼筋及混凝土 MAPE 下降，但 SRC 價格 MAPE 14.56%→24.33%、涵蓋率60%→20%。**本輪不更新正式權重或 TopK**。如後續設計分目的參數，必須另行預註冊評估，不能從本次 test 挑最好看的配置。
- 替代組是在真 API 完整 eligible reference pools 上、以已核對的 C# 規則重現的 counterfactual；不是把 144 組參數逐一寫入 API/DB，也不冒稱已部署替代模型。

## Precision / Recall / FPR 為何仍不可填數字

已唯讀驗證既有 13 份 XLSX 的 SHA256、test split 與對應 truth，僅 3 個獨立原始建案。10 個 variant 的 cause_kind 是 DESIGN_VARIATION_POSSIBLY_JUSTIFIED、UNIT_PRICE_STRESS_NOT_PROOF_OF_OVERPRICING 或 MIXED_DESIGN_AND_PRICE_STRESS，全部 manual_review_required=true。既有 provenance 報告 readyForFalsePositiveRate=false。

所以這批標記提供「哪裡被乘上係數」及方向壓力測試，沒有工程正常/異常認證。無注入基準也可能有其他生成器 shock 或合理差異；不能把它直接當真陰性。Precision/Recall/FPR 保留 null/不可識別，既有 paired report 不重新上傳或重算。若要驗收異常分類，需一套事先定義並完整核定正負標籤的測試標準，或另以工程人員核定的資料驗證；這是資料定義缺口，不靠增加 API 次數解決。

## 同批警告修復與回歸

- TenderImportInspectionService.cs：InspectionRow 的 Unit 對 null 顯式轉空字串，保留原有缺單位警示及不推測換算；修復 CS8604。
- 崴正.csproj：明確引用 Microsoft.EntityFrameworkCore.Relational 10.0.5，與既有 EFCore 10.0.5 對齊，避免測試 project transitive resolve 落到10.0.4；修復 MSB3277。沒有 Migration/schema 變更。
- Windows API Release build：0 warnings / 0 errors；WorkbookParserTests 全部通過；Python 全套11/11通過；兩個永久 Node 工具語法通過。既有 CI 將在本輪提交後另核對，不新建臨時 workflow。
- 原桌面工作樹仍為 integration/src-closure，先前最後衝突已於5c0ae5c完成。此輪程式修改在可寫 clone，尚未套回桌面或重啟原5263；quantity-scope先前為404，Docker權限阻塞及新版6/6仍未完成。

## 重現命令與結案範圍

```powershell
python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v
python tools/graduation-synthetic/evaluate_live_holdout.py --source C:\Users\User\Desktop\BiddingSystem-2A\graduation-src-extreme-v3 --output-dir research/2a_synthetic/output/live-holdout-new-run --workers 3
```

工具只接受既有 localhost:5263 與來源 hash；拒絕覆寫已完成報告。中斷後同目錄可讀原 cache；settings/source變更會拒絕混用。該命令是需要重現時的入口，本輪已完成，不要求使用者重跑。

工作包1的合成數值回測及工作包2的對照已產生證據；異常指標受真值定義阻塞。工作包3的兩項編譯警告已修復並本機驗證，原機部署/6-of-6、既定整合回歸、PR #2 合併與最終版本仍未完成；不把 source smoke、CI 或本報告擴張成全系統/企業驗收通過。

交接檢查首跑因Git quotePath對中文檔名轉義而FAIL，已改用NUL分隔路徑並增加真實temp Git repo回歸；保留此失敗紀錄，不將工具誤判當成漏寫交接。
