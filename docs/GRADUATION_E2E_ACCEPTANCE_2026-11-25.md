# 2A 畢專 11/25 端到端資料與模型驗收

> 目的：不是單純「產資料測精準度」，而是讓畢專版實際走通歷史建案 → PostgreSQL → 估算 → 待估標單 → 偵錯 → 可視化證據的**完整流程**。
> 本文件標記的完成狀態以執行證據為準；沒有證據的一律為「待驗收」。

## 一、嚴格隔離三種資料

1. **TRAIN / 歷史基準**：依建案切分的 v2 `train` 案，歷史資料僅由其 `project_boq_rows.csv` 衍生。不可把 `test` 案寫入估算參考庫。
2. **VALIDATION / 調參**：獨立 `validation` 案，僅供比較 TopK / 門檻／模型選型，不回填歷史訓練庫。
3. **TEST / 待估標單**：獨立 `test` 案輸出 Excel（例如 `graduation-v2-project.xlsx`），應經 `AnalysisCaseController.CreateFromUpload` 的工作表選取與解析路徑，**不得先將該案的歷史資料匯入**。

不可將預算標單、決標標單、結算數量混成單一真值；來源分級記錄於 `docs/GRADUATION_DELIVERY_2026-11-25.md`。

## 二、實際系統的資料庫依賴（已對照 feature 分支）

- 後端：`崴正/Data/ApplicationDbContext.cs`：`project_master`、`material_master`、`material_category`、`unit_category`、`unit_master`、`historical_transaction`、`detection_category`、`detection_category_material_map`，以及 AnalysisCase 系列表。
- `崴正/Services/FinalBidImportService.cs`：匯入最終標單先建 **Draft**，**不會立即將工項寫進歷史交易**；正式資料需經人工覆核及歷史基準核准流程。
- `崴正/Services/DetectionCategoryRangeService.cs`：只納入同偵錯細項啟用的 `DetectionCategoryMaterialMap` 對應材料，單位可換算、未刪除且非 outlier 的歷史交易；有 TopK / 最小有效樣本門檻。
- `崴正/Services/TwoAQuantityAnalysisService.cs`：只有政策允許的偵錯細項可正式判定數量異常；**完整標單可存 ≠ 所有工項已可用基本建案條件推估**。
- `崴正/Controllers/AnalysisCaseController.cs` POST `api/analysis-case/from-upload`：接受待估算標單及目標建案條件，需指定有效工作表後進行分析。

### 為什麼不能直接把 CSV 塞進產學 Supabase？

目前 v2 CSV 以 `SYN-*` 當材料識別，不是 DB 真正的 `MaterialId`，單位與類別主檔也未對齊。直接 insert 會讓 FK/單位換算、歷史狀態及偵錯範圍失真，更會汙染企業資料。因此**目前沒有執行任何 PostgreSQL 寫入**。

## 三、11/25 必須取得的端到端證據

| 驗證關卡 | 驗收條件 | 目前狀態 |
|---|---|---|
| 多類別資料生成 | 每案工項數與工程組成變動，RC/SC、地下室、樓層條件可驗；各條目保留合成來源 | 已提交 v2；尚待 Windows/CI 實測 |
| 資料完整性 | `project_boq_rows.csv` 和 `historical_transactions.csv` 的交易 ID、數量、單價、split 逐列對齊 | 已寫測試；尚待實測 |
| 歷史資料進獨立 DB | 使用**全新隔離 PostgreSQL 實例／資料庫**，建立正式必要主檔映射，僅匯入 train 案 | **未完成；禁止寫企業 DB** |
| 待估標單 | 未參與 train 的 test 建案 Excel 可被選取工作表、解析並建立 AnalysisCase | 已有 v2 匯出器；ASP.NET E2E 待驗收 |
| 正式估算執行 | `TwoAQuantityAnalysisService` 經實際 DB 回傳 P15/P50/P85 和狀態 | **未測** |
| 模型比較 | 相同 train/test 及相同材料量綱的加權分位數 vs 加權常態基準，完整保留 coverage、MAPE、interval score | 已有離線代理工具，尚不等於正式 API |
| 視覺化 | 有來源標籤的交互比較長條圖、工項類別分布、建案組成與樣本覆蓋；圖表可列印存 PDF | 已提交離線 HTML 工具；待生成驗收 |
| 真實世界精度 | 至少有可追溯且合法使用的獨立真實標單與對應歷史真值 | **沒有證據，不得填百分比** |

## 四、用戶端安全執行指令

在目前 `feature/1d-api-integration` 分支，只同步工具，不切 main、不碰尚未提交的前端檔案：

```powershell
git fetch origin main
git restore --source=origin/main --worktree -- tools/graduation-synthetic
python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v
python tools/graduation-synthetic/generate_full_project_dataset.py --count 300 --seed 20261119 --output .\graduation-data-v2
python tools/graduation-synthetic/export_full_project_excel.py --source .\graduation-data-v2 --output .\graduation-v2-project.xlsx --truth .\graduation-v2-project-truth.json
python tools/graduation-synthetic/evaluate_full_project_holdout.py --source .\graduation-data-v2 --output .\graduation-v2-holdout.json --top-k 8 --min-sample 5
python tools/graduation-synthetic/build_evidence_report.py --source .\graduation-data-v2 --benchmark .\graduation-v2-holdout.json --output .\graduation-v2-evidence.html
Invoke-Item .\graduation-v2-evidence.html
```

新工具輸出皆拒絕覆寫。若過去生成過相同檔名，用新名稱，不刪除現存測試資料。

## 五、下一組開發（不是已完成）

1. 在乾淨的、與產學環境物理隔離的畢專 PostgreSQL，確認 ApplicationDbContext 所需資料表、預設設定及認證，先完成唯讀 schema preflight，再設計可回滾的材料碼→正式 MaterialId／UnitId／偵錯細項對照。
2. 僅匯入 train 基準案，確認 300 案中 train/validation/test 隔離與有效樣本數；不可繞過現有人工審核規則直接核准真實來源。
3. 從 test 選出多案建案，依實際 AnalysisCase API 上傳、人工選工作表、解析、估算；逐案與 truth JSON 比較，記錄未辨識／錯單位／未知工項。
4. 用真實來源標單校準多類別工項、不同工程規模、數量與單價的分布。正式真實精度和合成模型對照報告分開呈現。

安全禁則：不能用假資料冒充已決標工程；不能將本合成 CSV 宣稱為公開工程資料；不得將完整使用者/企業來源資料或連線密碼提交 GitHub。
