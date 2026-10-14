# 2A 現行架構

> 更新日期：2026-10-15  
> 正式開發 Branch：`feature/1d-api-integration`

本文件記錄 2A 目前正式開發方向，刻意與舊版 POC Branch 及已淘汰的通用材料估算實驗分開。

## 1. 目前產品範圍

2A 目前有兩個分析層級：

- **大項估算／價格異常：** `結構體工程`
- **細項數量異常偵測：** `鋼筋`、`混凝土`

目前層級：

```text
結構體工程
├─ 鋼筋      → 數量異常偵測
└─ 混凝土    → 數量異常偵測
```

其他結構體工項仍保留於最終標單中作為追溯資料，且可能會計入結構體工程總價，但不進入目前鋼筋／混凝土的數量歷史基準。

對於已定案歷史標單，原始 `Category1 / Category2 / Category3` 必須保留。如果 Parser 無法正確辨識工程大項，審核人員可以另外設定 `major_category_override`。這個人工修正只改變 2A 的適用資格與價格分組，不會破壞原始分類層級。

## 2. 待偵錯資料與歷史資料必須分開

### 待偵錯流程

用於目前正在估價、修改或需要反覆檢查的建案。

```text
自行上傳 ──────────────┐
                      ├─ AnalysisCase
選配 1D 匯入 ──────────┘      │
                             ├─ V1
                             ├─ V2
                             └─ V3
                                  │
                                  └─ AnalysisRun
```

資料表：

- `analysis_case`
- `analysis_bid_version`
- `analysis_bid_item`
- `analysis_run`

待偵錯案件**絕對不能**直接寫入 `historical_transaction`。

### 歷史資料流程

只用於真正已定案、並準備成為模型基準的正式最終標單。

```text
最終標單
  → ProjectMaster（Draft）
  → FinalBidItem
  → 工程大項／材料／單位審核
  → 明確執行 Publish History
  → ProjectMaster（Approved）
       ├─ 結構體工程價格基準保留在 FinalBidItem
       └─ 鋼筋／混凝土數量基準 → HistoricalTransaction
```

因此，匯入一份最終標單後，不會立即被模型信任。必須先經過人工審核，並明確發布為歷史基準。

如果重新開啟審核，該建案會暫時從相似建案／價格基準中排除，相關數量歷史交易也會暫時標記為不可用。

審核頁面會持續重新計算是否可以發布。以下情況會阻止發布：

- 未忽略的工項仍有無法辨識的工程大項
- 結構體工程工項沒有可使用的價格資料
- 已經對應為鋼筋／混凝土的工項，仍有無效的數量或單位

這樣可以避免尚未審核完整的資料默默進入模型。

資料表：

- `project_master`
- `final_bid_item`
- `historical_transaction`

## 3. 1D 為選配

2A 在沒有 1D 的情況下也必須完整可用。

### 專題／獨立環境

```text
上傳結構化待偵錯標單
→ AnalysisCase 快照
→ 多版本 V1 / V2 / V3
→ 數量／價格異常分析
```

### 產學整合環境

若已設定 `OneD:BaseUrl`：

```text
1D 結果
→ 一次性匯入
→ AnalysisCase 快照
→ 後續分析全部在 2A 內執行
```

1D 是待偵錯資料的選配來源，不是歷史資料來源，也不是每一次分析都必須依賴的執行期服務。

## 4. 直接上傳暫定規則

在廠商確認最終介面規格前，直接上傳的待偵錯標單暫時只接受能提供以下資訊的結構化檔案：

- 工程大項
- 工項名稱與／或規格
- 單位
- 數量

若要進行大項價格分析，還需要：

- 單價
- 或金額

完全未分類的原始文件不會默默送去 1D，也不會由 2A 任意猜測欄位與分類。

## 5. 歷史數量資料資格

FinalBidItem 只有在其 Project 已經是 `Approved`，而且該列同時符合以下條件時，才可以進入 `historical_transaction`：

- 工項沒有被忽略
- 已對應標準材料
- 工程大項解析為 `結構體工程`
- 材料為 `鋼筋` 或 `混凝土`
- 數量大於 0
- 原始單位可以轉換成標準單位

數量歷史**不要求單價**。

`historical_transaction` 只負責數量歷史，價格仍保留在 `final_bid_item`。

## 6. 歷史資料審核與不可變更性

匯入的最終標單一開始都進入 `Draft` 審核狀態。

```text
最終標單匯入
→ Draft
→ 審核工程大項／材料對應／數量／單位／價格
→ 發布歷史基準
→ Approved
```

當建案成為 `Approved` 後，以下歷史輸入必須鎖定：

- 建案相似度基本資料
- 工程大項人工覆寫
- 材料對應
- 數量／單位／單價
- 忽略／取消忽略狀態

若要修正已核准歷史基準，使用者必須明確執行 **Reopen History Review**。

重新開啟審核後：

```text
Approved
→ Reopen History Review
→ Draft
→ 修正
→ 再次發布
```

重新審核期間，該建案的有效數量歷史資料會暫時停用，直到再次通過審核並發布。

這樣可以避免已經被模型使用的歷史基準在沒有記錄的情況下被修改。

## 7. 歷史價格基準

目前大項價格估算只支援 Approved 歷史建案，而且只支援：

- `結構體工程`

對每個歷史建案：

1. 加總所有有效的結構體工程工項金額
2. 如果 `Amount` 沒有值，改用 `Qty × UnitPrice`
3. 將總樓地板面積換算成坪
4. 計算結構體工程每坪單價
5. 依相似建案分數排序與加權
6. 套用時間衰減
7. 計算加權 P15／P50／P85

如果有效參考建案數低於 `MinimumSampleCount`，系統必須顯示樣本不足，不可以強制產生異常判定。

## 8. 相似建案特徵

目前相似度輸入：

- 建案類型
- 結構類型
- 縣市
- 總樓地板面積
- **基地面積**
- 地上樓層
- 地下樓層
- 建案年份

基地面積目前已正式加入：

- `project_master.site_area`
- `analysis_case.site_area`
- 待偵錯表單
- 最終標單匯入基本資料
- 結構體工程估算輸入
- 相似度評分
- 參數設定介面

目前可調整權重：

- 結構權重
- 總樓地板面積權重
- 基地面積權重
- 樓層權重
- 縣市權重
- 年份權重

基地面積也有獨立的差距截止比例。

## 9. 目前模型準備度規則

預設最低樣本數：**5 個建案**。

數量模型分開追蹤：

- 鋼筋
- 混凝土

價格模型分開追蹤：

- 結構體工程

系統總覽會顯示每一種基準目前有多少有效歷史建案，以及還差多少案才達最低樣本數。

每個歷史建案詳細頁也會顯示該份最終標單對 2A 模型的資格診斷。

最終標單審核清單會直接標示每一列為：

- `結構體價格基準`
- `鋼筋／混凝土數量基準`
- `不納入目前 2A 基準`

讓審核人員可以立即看出每一項修正對模型的影響。

目前診斷項目包括：

- 尚未辨識工程大項的工項
- 已人工修正工程大項的工項
- 結構體工項但尚未建立標準材料對應
- 結構體工項但沒有可用價格資料
- 鋼筋來源列是否成功形成數量歷史交易
- 混凝土來源列是否成功形成數量歷史交易

## 10. 主要 API

### 待偵錯案件

- `GET /api/analysis-cases`
- `GET /api/analysis-cases/{id}`
- `GET /api/analysis-cases/{id}/versions/{versionId}`
- `GET /api/analysis-cases/{id}/runs`
- `GET /api/analysis-cases/{id}/runs/{runId}`
- `POST /api/analysis-cases/from-upload`
- `POST /api/analysis-cases/from-1d/{oneDProjectId}`
- `POST /api/analysis-cases/{id}/versions/upload`
- `POST /api/analysis-cases/{id}/versions/from-1d/{oneDProjectId}`
- `POST /api/analysis-cases/{id}/versions/{versionId}/quantity-analysis`
- `POST /api/analysis-cases/{id}/versions/{versionId}/major-price-analysis`

### 模型準備度

- `GET /api/detection-category/readiness`
- `GET /api/major-category-price/readiness`

### 最終標單審核

- `GET /api/Estimation/mapping-items`：支援依實際工程大項篩選，也可篩出未分類工項
- `POST /api/Estimation/update-major-category`：設定或清除人工工程大項覆寫
- 工程大項異動後會立即重新計算候選資格，並寫入 Audit Log
- 最終標單審核頁會即時顯示 Publish History 阻擋原因，全部通過後可直接發布

### 歷史建案

- `GET /api/projects`
- `POST /api/projects/{projectId}/publish-history`
- `POST /api/projects/{projectId}/reopen-history-review`
- `GET /api/projects/{projectId}`
- `GET /api/projects/{projectId}/historical-data`
- `GET /api/projects/{projectId}/2a-summary`

### 結構體工程估算

- `POST /api/major-category-price/range`

## 11. 已移除舊架構

舊版通用材料估算架構已移除，因為它與目前「大項價格／細項數量」的正式方向衝突。

已移除：

- `RangeEstimationService`
- `BatchEstimationService`
- `CoreMaterialService`
- `EstimationTargetService`
- `RangeEstimationController`
- 舊版 Range／Core Material DTO
- `quantity_rule_base` Model 與資料表
- 核心材料設定 UI
- 舊版一次性 `/api/bid-anomaly` 流程

目前正式待偵錯入口是版本化的 `AnalysisCase` 流程。

## 12. 目前資料庫狀態

開發資料庫已經做過硬清理。

歷史建案目前使用 `project_master.history_status`，狀態為：

- `Draft`
- `Approved`

只有 `Approved` 建案可以參與相似建案模型與結構體工程價格基準。

目前沒有建立任何假歷史建案。

預期資料狀態：

- `project_master`：在真實最終標單匯入前為 0 筆
- `historical_transaction`：在建立真實鋼筋／混凝土歷史前為 0 筆
- `analysis_case`：在建立第一個真實待偵錯案件前為 0 筆

目前偵錯主檔：

- `結構體工程 / 鋼筋 / kg`
- `結構體工程 / 混凝土 / 立方公尺`

標準材料 `鋼筋續接器` 只保留用來避免被錯誤合併到鋼筋重量歷史。

## 13. 仍待廠商確認

目前實作先依以下暫定假設進行：

1. 產學正式版是否仍需保留直接上傳
2. 直接上傳接受的正式資料格式
3. 最終大項／細項層級
4. 1D 與 2A 的正式操作流程
5. 哪些工程類別應排除數量估算，以及被排除數量估算的類別是否仍能做大項價格分析

目前 Target／History 分流架構可以在不重新設計整個系統的前提下調整上述規則。

## 14. 目前限制

目前清理後的開發資料庫尚未有正式歷史建案。

因此 P15／P50／P85 目前還不能產生有實務意義的真實範圍。

在正式最終標單完成匯入、對應、人工審核與發布前，系統必須持續回傳「樣本不足」。
