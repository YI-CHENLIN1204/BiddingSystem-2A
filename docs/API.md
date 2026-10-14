# 2A API 參考

> 更新日期：2026-10-15  
> Branch：`feature/1d-api-integration`  
> 基底路徑：`/api`

本文件對應目前正式開發 Branch 上的 ASP.NET Core Controller，記錄 2A 正式流程、歷史最終標單審核流程、主檔、參數設定與選配 1D 整合。

## 1. 共通規則

### 回應行為

多數由 Service 驅動的 API 會使用：

- `200 OK`：成功
- `400 Bad Request`：資料驗證錯誤
- `404 Not Found`：找不到指定資料
- `409 Conflict`：目前狀態不允許執行該操作

錯誤內容：

```json
{
  "message": "錯誤說明"
}
```

### 上傳限制

目前 Excel／CSV 上傳 API 的 Request Body 上限為 **100 MB**。

### 重要資料規則

待偵錯 API 與歷史建案 API 是刻意分開的：

```text
待偵錯工作資料：
AnalysisCase → Version → Run

歷史真值：
ProjectMaster → FinalBidItem → HistoricalTransaction
```

待偵錯案件不得自動轉成歷史真值。

---

## 2. 系統

### GET /api/system/health

Backend 基本健康檢查。

範例回應：

```json
{
  "status": "ok"
}
```

### GET /api/system/options

回傳 UI 選項與標準材料。

目前包含：

- 建案類型：住宅／廠房
- 結構類型：RC／SC／SRC
- 臺灣縣市
- 啟用中的標準材料

---

## 3. Excel 工作表預覽／檢查

以下 API 都是**唯讀**，不會建立建案、待偵錯案件、歷史交易或模型樣本。

### POST /api/tender-import/preview

Content Type：`multipart/form-data`

欄位：

- `excelFile`：Excel／CSV

用途：

- 列出 Excel 內所有工作表
- 顯示解析列數與範例工項
- 讓使用者明確判斷哪些工作表才是真正工程明細

注意：

- 多工作表 Excel 不可以默默全部匯入
- 摘要／報價／廠商比較工作表必須人工判斷

### POST /api/tender-import/inspect

Content Type：`multipart/form-data`

欄位：

- `excelFile`：Excel／CSV
- `includedSheetNames`：明確選取工作表名稱的 JSON Array

範例：

```text
includedSheetNames = ["建築工程-預算單"]
```

用途：

- 只解析被選取的工作表
- 回傳原始工作表／列號供追溯
- 保守預覽鋼筋／混凝土候選數量
- 在任何資料庫寫入之前顯示警告

規則：

- 至少要明確選取一個工作表
- 重複或空白工作表名稱會被拒絕
- 候選列超過 5000 筆會被拒絕
- 預覽數量不是 Approved 歷史資料，也不是 P15／P50／P85 結果

---

## 4. 待偵錯案件 — AnalysisCase

基底路徑：

```text
/api/analysis-cases
```

### GET /api/analysis-cases/policy

回傳目前暫定業務規則，包括：

- 可獨立上傳
- 1D 為選配
- 目前大項／細項層級
- 目前估算範圍

### GET /api/analysis-cases

列出待偵錯案件。

主要回傳欄位：

- `analysisCaseId`
- `caseName`
- `projectType`
- `structureType`
- `city`
- `totalFloorArea`
- `siteArea`
- `floorCount`
- `basementFloorCount`
- `projectDate`
- `versionCount`
- `currentVersionId`
- `currentVersionNo`
- `currentSourceType`
- `updatedAt`

### GET /api/analysis-cases/{analysisCaseId}

回傳單一待偵錯案件與版本清單。

### GET /api/analysis-cases/{analysisCaseId}/versions/{versionId}

回傳某一個不可覆寫的待偵錯標單快照與所有工項。

### GET /api/analysis-cases/{analysisCaseId}/runs

回傳已保存的分析執行摘要。

### GET /api/analysis-cases/{analysisCaseId}/runs/{analysisRunId}

回傳一筆儲存在 `analysis_run.result_json` 的歷史分析結果。

### POST /api/analysis-cases/from-upload

由結構化上傳建立新的待偵錯案件。

Content Type：`multipart/form-data`

欄位：

- `excelFile`
- `caseName`
- `projectType`
- `structureType`
- `city`
- `totalFloorArea`
- `siteArea`
- `floorCount`
- `basementFloorCount`
- `projectDate`
- `note`：選填
- `includedSheetNames`：工作表名稱 JSON Array

目前暫定規則：

上傳檔案必須提供足夠結構，讓 2A 能辨識可使用的工程大項／工項／單位／數量資料。任意原始檔不會被系統默默猜測。

### POST /api/analysis-cases/from-1d/{oneDProjectId}

讀取 1D **一次**，建立新的待偵錯案件並保存成 2A 本地快照。

Content Type：`application/json`

Body：

```json
{
  "caseName": "A集合住宅",
  "projectType": "住宅",
  "structureType": "RC",
  "city": "臺中市",
  "totalFloorArea": 15000,
  "siteArea": 1800,
  "floorCount": 15,
  "basementFloorCount": 3,
  "projectDate": "2026-10-01"
}
```

選填 Query Parameter：

- `note`

後續 AnalysisRun 不會再次呼叫 1D。

### POST /api/analysis-cases/{analysisCaseId}/versions/upload

新增一個不可覆寫的待偵錯版本。

Content Type：`multipart/form-data`

欄位：

- `excelFile`
- `note`：選填
- `includedSheetNames`：工作表名稱 JSON Array

上一版必須保持不變。

### POST /api/analysis-cases/{analysisCaseId}/versions/from-1d/{oneDProjectId}

明確重新讀取 1D，建立新版本。

Body：

```json
{
  "note": "重新取得 1D 最新結果"
}
```

只有真的需要重新取得 1D 最新資料時才使用。

### POST /api/analysis-cases/{analysisCaseId}/versions/{versionId}/quantity-analysis

對已保存版本執行目前鋼筋／混凝土數量異常分析。

Body：

```json
{
  "topCount": 8
}
```

`topCount` 選填。

目前細項範圍：

- 鋼筋
- 混凝土

### POST /api/analysis-cases/{analysisCaseId}/versions/{versionId}/major-price-analysis

對已保存版本執行大項價格分析。

Body：

```json
{
  "majorCategory": "結構體工程",
  "topCount": 8
}
```

目前支援大項：

- 結構體工程

---

## 5. 歷史最終標單

### POST /api/Estimation/upload-final-bid

將最終標單候選匯入**歷史審核流程**。

Content Type：`multipart/form-data`

欄位：

- `excelFile`
- `projectName`
- `projectType`
- `structureType`
- `city`
- `totalFloorArea`
- `siteArea`
- `floorCount`
- `basementFloorCount`
- `projectMonth`
- `includedSheetNames`：選填 JSON Array；但多工作表檔案應明確選擇

重要：

匯入後**不代表**已成為可信任歷史模型資料。

新建案狀態：

```text
HistoryStatus = Draft
```

### GET /api/Estimation/mapping-items

Query Parameter：

- `projectId`：選填
- `status`：預設 `Pending`
- `search`：選填
- `issueCode`：選填
- `majorCategory`：選填
- `category`：選填
- `unit`：選填
- `page`：預設 1
- `pageSize`：預設 50

回傳工程大項／材料／單位／資料修正審核列。

### GET /api/Estimation/mapping-summary

Query：

- `projectId`：選填

回傳審核摘要統計。

### GET /api/Estimation/mapping-filter-options

Query：

- `projectId`：選填

回傳審核 UI 使用的篩選選項。

### GET /api/Estimation/material-options

Query：

- `search`：選填

回傳人工材料對應可用的標準材料。

### POST /api/Estimation/update-material-mapping

Content Type：`multipart/form-data`

欄位：

- `finalBidItemId`
- `materialId`

人工指定標準材料。

Approved 歷史資料必須先重新開啟審核才能修改。

### POST /api/Estimation/update-major-category

Content Type：`multipart/form-data`

欄位：

- `finalBidItemId`
- `majorCategory`：選填

設定或清除 `major_category_override`。

來源 `Category1 / Category2 / Category3` 保持不變。

### POST /api/Estimation/update-bid-item-data

Content Type：`multipart/form-data`

欄位：

- `finalBidItemId`
- `itemName`
- `qty`
- `unitPrice`
- `unit`

用於 Draft 審核期間修正 Parser 解析資料。

### POST /api/Estimation/ignore-bid-item

欄位：

- `finalBidItemId`

將工項排除於歷史基準審核。

### POST /api/Estimation/unignore-bid-item

欄位：

- `finalBidItemId`

恢復已忽略工項。

---

## 6. 歷史建案管理

基底路徑：

```text
/api/projects
```

### GET /api/projects

Query：

- `search`：選填

列出歷史建案候選與目前審核狀態。

### GET /api/projects/{projectId}

回傳單一歷史建案。

### PUT /api/projects/{projectId}

Body：

```json
{
  "projectName": "A集合住宅",
  "projectType": "住宅",
  "structureType": "RC",
  "city": "臺中市",
  "totalFloorArea": 15000,
  "siteArea": 1800,
  "floorCount": 15,
  "basementFloorCount": 3,
  "projectMonth": "2026-10"
}
```

Approved 歷史資料會鎖定。修改模型輸入前必須重新開啟審核。

### POST /api/projects/{projectId}/publish-history

明確將已審核 Draft 建案發布為歷史基準。

預期狀態轉換：

```text
Draft → Approved
```

仍有未解決問題時必須阻止發布。

目前重要阻擋項目：

- 未忽略工項仍無法辨識工程大項
- 結構體工程工項沒有可用價格資料
- 鋼筋／混凝土工項有無效數量／單位
- 建案摘要回報的其他審核條件

### POST /api/projects/{projectId}/reopen-history-review

狀態轉換：

```text
Approved → Draft
```

重新開啟後，該建案會暫時從有效模型基準中排除，並重新允許修改。

### DELETE /api/projects/{projectId}

依目前 Service 規則刪除／軟刪除建案。

### GET /api/projects/{projectId}/final-bid-items

Query：

- `search`：選填
- `page`：預設 1
- `pageSize`：預設 50

回傳最終標單來源工項。

### GET /api/projects/{projectId}/2a-summary

回傳 2A 資格診斷，包括：

- 歷史狀態
- 結構體工程工項數／有價工項數／無價工項數
- 未分類工項
- 人工工程大項修正數
- 結構體工程未對應材料數
- 鋼筋／混凝土來源工項／候選工項／排除工項數
- 是否可發布與阻擋原因
- 結構體工程總價
- 結構體工程每坪單價
- 鋼筋／混凝土歷史數量與交易筆數

### GET /api/projects/{projectId}/historical-data

回傳該建案標準化後、目前有效的數量歷史資料。

目前數量歷史範圍：

- 鋼筋
- 混凝土

---

## 7. 結構體工程價格估算

### GET /api/major-category-price/readiness

回傳 `結構體工程` 目前價格歷史基準準備度。

準備度只計算有效的 **Approved** 歷史建案。

### POST /api/major-category-price/range

Body：

```json
{
  "target": {
    "projectType": "住宅",
    "structureType": "RC",
    "city": "臺中市",
    "totalFloorArea": 15000,
    "siteArea": 1800,
    "floorCount": 15,
    "basementFloorCount": 3,
    "projectDate": "2026-10-01"
  },
  "majorCategory": "結構體工程",
  "observedAmount": null,
  "topCount": 8
}
```

主要回傳：

- 候選／可用／實際參考建案數
- `hasEnoughSamples`
- P15／P50／P85 每坪單價
- P15／P50／P85 總價
- 選填的觀察金額／觀察每坪單價
- 分析狀態／交通燈
- 加權歷史參考案件

如果有效樣本低於 `MinimumSampleCount`，正確結果是樣本不足，而不是強制產生區間。

---

## 8. 偵錯細項 — 鋼筋／混凝土數量

基底路徑：

```text
/api/detection-category
```

### GET /api/detection-category

回傳目前設定的偵錯細項。

目前正式偵錯細項：

- 結構體工程／鋼筋／kg
- 結構體工程／混凝土／立方公尺

### GET /api/detection-category/readiness

分別回傳每個數量偵錯細項的歷史樣本準備度。

### GET /api/detection-category/mappings

Query：

- `detectionCategoryId`：選填

回傳標準材料對應。

### POST /api/detection-category/create

Form 欄位：

- `majorCategoryName`
- `detectionCategoryName`
- `unitId`

### POST /api/detection-category/assign-material

Form 欄位：

- `detectionCategoryId`
- `materialId`

### POST /api/detection-category/unassign-material

Form 欄位：

- `materialId`

### POST /api/detection-category/{detectionCategoryId}/range

Body：

```json
{
  "target": {
    "projectType": "住宅",
    "structureType": "RC",
    "city": "臺中市",
    "totalFloorArea": 15000,
    "siteArea": 1800,
    "floorCount": 15,
    "basementFloorCount": 3,
    "projectDate": "2026-10-01"
  },
  "topCount": 8
}
```

回傳加權後的數量／單位面積合理範圍與歷史參考案件。

---

## 9. 選配 1D 整合

基底路徑：

```text
/api/one-d
```

### GET /api/one-d/status

回傳：

- `configured`
- 可讀訊息

即使沒有設定 1D，2A 直接上傳仍必須可用。

### GET /api/one-d/projects/{projectId}/result

只用於整合檢查／預覽。

正式流程應使用：

```text
POST /api/analysis-cases/from-1d/{projectId}
```

建立本地待偵錯快照。

預期 1D 工項欄位：

- category
- chapter
- sequence
- code
- name
- unit
- quantity
- confidence

目前 1D 沒有提供結構體工程價格分析需要的金額欄位。

---

## 10. 參數設定

### GET /api/settings

回傳目前估算參數。

### PUT /api/settings

Body 欄位：

- `topK`
- `minimumSampleCount`
- `lowerQuantile`
- `medianQuantile`
- `upperQuantile`
- `similarityExponent`
- `timeHalfLifeYears`
- `minimumSimilarityScore`
- `structureWeight`
- `areaWeight`
- `siteAreaWeight`
- `floorWeight`
- `yearWeight`
- `cityWeight`
- `areaDifferenceCutoffRatio`
- `siteAreaDifferenceCutoffRatio`
- `groundFloorPenaltyRatio`
- `basementFloorPenaltyRatio`
- `yearDifferenceCutoffYears`

---

## 11. 操作紀錄

### GET /api/audit-logs

Query Parameter：

- `search`
- `module`
- `actionType`
- `startDate`
- `endDate`
- `page`
- `pageSize`

回傳分頁 Audit Log。

---

## 12. 材料分類主檔

基底路徑：

```text
/api/material-category
```

API：

- `GET /`
- `POST /create`
- `POST /update`
- `POST /set-active`

新增／修改使用 Form Data。

---

## 13. 標準材料主檔

基底路徑：

```text
/api/standard-material
```

API：

- `GET /`
- `GET /filter-options`
- `POST /create`
- `POST /update`
- `POST /set-active`

主要材料欄位：

- 材料名稱
- 材料分類
- 標準單位
- 啟用狀態

---

## 14. 單位類別主檔

基底路徑：

```text
/api/unit-category
```

API：

- `GET /`
- `POST /create`

新增欄位：

- `categoryName`
- `allowConversion`
- `baseUnitName`

可換算類別必須有基準單位。

---

## 15. 單位主檔

基底路徑：

```text
/api/unit-master
```

API：

- `GET /`
- `POST /create`
- `POST /update`
- `POST /set-active`

主要欄位：

- `unitName`
- `unitCategoryId`
- `toBaseFactor`

---

## 16. 目前 API 邊界

除非未來有新的架構決策，否則不要重新加入以下已移除舊 API：

- `/api/bid-anomaly`
- 通用材料合理區間估算 API
- 核心材料估算 API
- 舊數量規則 API

目前正式待偵錯分析入口是版本化 `AnalysisCase` 流程。
