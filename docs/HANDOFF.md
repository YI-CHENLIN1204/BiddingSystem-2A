# 2A 開發交接

> 更新日期：2026-10-15  
> Repository：`YI-CHENLIN1204/BiddingSystem-2A`  
> 目前正式開發 Branch：`feature/1d-api-integration`

## 1. 先閱讀

不要從舊對話重新拼湊專案，也不要恢復已移除的通用材料估算程式。

請依照以下順序閱讀：

1. `docs/NEXT_CHAT_HANDOFF.md`
2. `README.md`
3. `docs/2A_CURRENT_ARCHITECTURE.md`
4. `docs/DECISIONS.md`
5. `docs/API.md`
6. `docs/DATABASE.md`
7. `docs/DEVELOPMENT.md`
8. `docs/LOCAL_TEST_GUIDE.md`
9. `docs/VENDOR_QUESTIONS.md`
10. `docs/WORKLOG.md`

## 2. 目前正式範圍

目前模型範圍：

- 大項估算／價格異常：**結構體工程**
- 細項數量異常：**鋼筋、混凝土**

目前層級：

```text
結構體工程
├─ 鋼筋      → 數量異常
└─ 混凝土    → 數量異常
```

不要自動擴張到所有工程類別。

## 3. 正式資料分流

### 待偵錯／工作資料

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

### 已確認歷史資料

```text
已定案最終標單候選
→ ProjectMaster Draft
→ FinalBidItem
→ 人工審核
→ Publish History
→ ProjectMaster Approved
   ├─ 結構體工程價格基準 → FinalBidItem
   └─ 鋼筋／混凝土數量基準 → HistoricalTransaction
```

資料表：

- `project_master`
- `final_bid_item`
- `historical_transaction`

關鍵規則：

```text
AnalysisCase != ProjectMaster
```

待偵錯資料不得自動成為歷史模型真值。

## 4. 1D 規則

2A 必須在有 1D 與沒有 1D 的情況下都能運作。

專題／獨立模式：

```text
結構化標單上傳
→ AnalysisCase 快照
→ V1 / V2 / V3
→ 分析
```

產學整合模式：

```text
1D
→ 一次性匯入
→ 2A 本地快照
→ 後續全部在 2A 內分析
```

1D 不可以成為每一次分析都必須呼叫的執行期依賴。

## 5. 歷史資料審核規則

匯入不代表核准。

```text
匯入
→ Draft
→ 人工審核
→ 處理阻擋項目
→ Publish History
→ Approved
```

審核可修正：

- 透過 `major_category_override` 修正實際工程大項
- 材料對應
- 數量
- 單位
- 單價
- 忽略狀態

來源 `Category1 / Category2 / Category3` 必須保留，不得覆寫。

Approved 歷史資料要鎖定。

若要修正：

```text
Approved
→ Reopen History Review
→ Draft
→ 修正
→ 再次發布
```

## 6. Excel 防護規則

目前重要目標之一，是避免錯誤 Excel 解析污染歷史模型資料。

使用：

```text
POST /api/tender-import/preview
POST /api/tender-import/inspect
```

目前規則：

- 多工作表 Excel 必須明確選擇要處理的工作表
- 檢查流程必須唯讀
- 保留原始工作表名稱與列號
- 數量與價格只能來自已明確辨識的欄位
- 不可以從列尾任意數字猜測
- 摘要表、廠商比較表、說明表不得默默被當成工程明細
- 鋼筋／混凝土候選總量只作為檢查輔助
- 工作表名稱出現「預算單」或「自估」不代表商務上已經定案

## 7. 目前相似建案模型

待估案件與歷史建案特徵：

- 建案類型
- 結構類型
- 縣市
- 總樓地板面積
- 基地面積
- 地上樓層
- 地下樓層
- 建案日期／年份

模型行為：

- 加權相似建案評分
- 相似度指數
- 時間衰減
- 加權 P15／P50／P85
- 樣本不足備援

預設最低有效歷史案件：

```text
5 個 Approved 建案
```

歷史不足就必須維持「樣本不足」，不能強制產生結果。

## 8. 價格與數量資料責任

### 結構體工程價格

來源：

`final_bid_item`

每個建案：

```text
Amount
或 Qty × UnitPrice
→ 結構體工程總價
→ 樓地板面積換算坪
→ 每坪單價
```

### 鋼筋／混凝土數量

來源：

`historical_transaction`

目前標準單位：

- 鋼筋 → kg
- 混凝土 → 立方公尺

`historical_transaction` 為純數量歷史資料，刻意不保存單價欄位。

## 9. 目前資料庫狀態

開發資料庫目前刻意保持乾淨。

截至 2026-10-15：

```text
project_master             0
final_bid_item             0
historical_transaction     0

analysis_case              0
analysis_bid_version       0
analysis_bid_item          0
analysis_run               0
```

不要為了讓模型產生合理區間而建立假案例。

## 10. 已移除架構 — 不要恢復

除非有新的明確決策，不要重新加入：

- 通用材料估算
- `RangeEstimationService`
- `BatchEstimationService`
- `CoreMaterialService`
- `quantity_rule_base`
- 舊版 `/api/bid-anomaly`
- 舊版一次性異常偵測頁面
- POC 偵錯顆粒度取捨示範

## 11. 接下來的階段

系統現在進入的是**真實資料驗證**，不是重新設計架構。

下一步順序：

1. 拉取最新 `feature/1d-api-integration`
2. 重新 Build Docker
3. 開啟 `http://localhost:8081`
4. 使用 Excel 工作表預覽
5. 明確選擇真正的工程明細工作表
6. 執行唯讀檢查
7. 將解析結果與原始 Excel 對照
8. 只有在檔案確實為最終定案資料時，才建立歷史建案
9. 處理 Draft 審核阻擋項目
10. 發布第一個 Approved 歷史建案
11. 確認建案 2A 歷史基準摘要
12. 持續加入其他真實歷史標單
13. 有足夠 Approved 歷史資料後，再驗證 P15／P50／P85
14. 建立待偵錯 AnalysisCase 並比較 V1／V2 結果

## 12. 仍待廠商確認

廠商尚未回覆前，繼續使用目前暫定假設：

1. 產學版 2A 是否除了 1D 外仍要保留直接上傳
2. 直接上傳的正式格式
3. `結構體工程 → 鋼筋／混凝土` 是否為接受的層級
4. 1D 是否應只作為一次性快照來源
5. 哪些類別適合進入數量歷史，以及不適合數量偵錯的類別是否仍能做大項價格分析

詳見：

`docs/VENDOR_QUESTIONS.md`

## 13. Branch 規則

持續開發於：

```text
feature/1d-api-integration
```

除非使用者明確要求，否則不要合併到 `main`。
