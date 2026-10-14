# 2A 下一個對話交接

> 更新日期：2026-10-15  
> Repository：`YI-CHENLIN1204/BiddingSystem-2A`  
> 目前正式 Branch：`feature/1d-api-integration`

## 給下一個對話的指示

直接延續目前既有專案。

**不要**從頭重新設計系統。  
**不要**恢復已移除的通用材料估算程式。  
除非使用者明確要求，否則**不要**合併到 `main`。

請先閱讀：

1. `README.md`
2. `docs/HANDOFF.md`
3. `docs/2A_CURRENT_ARCHITECTURE.md`
4. `docs/DECISIONS.md`
5. `docs/API.md`
6. `docs/DATABASE.md`
7. `docs/DEVELOPMENT.md`
8. `docs/LOCAL_TEST_GUIDE.md`
9. `docs/VENDOR_QUESTIONS.md`
10. `docs/WORKLOG.md`

之後直接從目前的真實資料驗證階段繼續。

---

## 目前正式範圍

```text
大項估算／價格異常：
結構體工程

細項數量異常：
鋼筋
混凝土
```

目前層級：

```text
結構體工程
├─ 鋼筋
└─ 混凝土
```

不要自動重新加入模板、鋼骨、門窗、景觀或全類別估算。

---

## 關鍵架構

### 待偵錯／工作資料流程

```text
直接上傳 ─────────────┐
                     ├─ AnalysisCase
選配 1D 匯入 ─────────┘      │
                            ├─ V1
                            ├─ V2
                            └─ V3
                                 │
                                 └─ AnalysisRun
```

待偵錯資料表：

- `analysis_case`
- `analysis_bid_version`
- `analysis_bid_item`
- `analysis_run`

### 歷史資料流程

```text
已定案標單候選
→ ProjectMaster（Draft）
→ FinalBidItem
→ 人工審核
→ Publish History
→ ProjectMaster（Approved）
   ├─ 結構體工程價格基準來自 FinalBidItem
   └─ 鋼筋／混凝土數量基準寫入 HistoricalTransaction
```

歷史資料表：

- `project_master`
- `final_bid_item`
- `historical_transaction`

永遠不要把 AnalysisCase 直接視為歷史真值。

---

## 1D 規則

1D 為選配。

專題／獨立環境：

```text
直接上傳結構化標單
→ 2A 快照
→ 分析
```

產學環境：

```text
1D
→ 一次性匯入
→ 2A 本地快照
→ 後續分析都留在 2A
```

不要讓 1D 變成每一次偵錯都必須呼叫的執行期依賴。

---

## 歷史資料審核規則

匯入不等於核准。

```text
匯入
→ Draft
→ 檢查原始列／工程大項／材料／數量／單位／價格
→ 處理阻擋項目
→ Publish History
→ Approved
```

Approved 資料必須鎖定。

若要修正：

```text
Reopen History Review
→ Draft
→ 修正
→ 再次發布
```

來源 `Category1 / Category2 / Category3` 必須保留追溯。  
人工修正實際工程大項使用 `major_category_override`。

---

## 已實作 Excel 防護

歷史匯入與待偵錯上傳共用工作表預覽／檢查概念。

唯讀 API：

```text
POST /api/tender-import/preview
POST /api/tender-import/inspect
```

規則：

- 必須明確選擇要納入的工作表
- 保留原始工作表名稱與列號
- 不可以默默匯入所有工作表
- 不可以從每列尾端任意數字猜測數量欄
- 不可以把摘要、報價、廠商比較等列當作工程明細
- 鋼筋／混凝土候選總量只作檢查輔助
- 檢查流程不得寫入模型歷史

工作表名稱包含「預算單」或「自估」，不代表該價格已經正式定案。

---

## 相似建案特徵

目前正式輸入：

- 建案類型
- 結構類型
- 縣市
- 總樓地板面積
- 基地面積
- 地上樓層
- 地下樓層
- 建案年份

模型使用：

- 相似度權重
- 相似度指數
- 時間衰減
- 加權 P15／P50／P85
- 歷史不足備援

預設最低樣本數：

```text
5 個 Approved 建案
```

絕對不要建立假歷史資料來湊足門檻。

---

## 價格與數量資料責任

結構體工程歷史價格：

```text
FinalBidItem.Amount
或 Qty × UnitPrice
→ 建案結構體工程總價
→ 每坪單價
```

鋼筋／混凝土歷史數量：

```text
HistoricalTransaction
```

標準單位：

- 鋼筋 = kg
- 混凝土 = 立方公尺

`historical_transaction` 刻意不保存價格。

---

## 開發資料庫狀態

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

這個乾淨狀態是刻意保留的。

---

## 本機啟動

在 Repository 根目錄：

```powershell
git status
git fetch origin
git switch feature/1d-api-integration
git pull origin feature/1d-api-integration

docker compose down
docker compose up -d --build
docker compose ps
```

開啟：

```text
http://localhost:8081
```

若啟動失敗：

```powershell
docker compose logs --tail=150
```

當 Backend、Frontend 容器都正常執行，而且 UI 沒有 API 錯誤時，就可以開始實際操作。

---

## 接下來要做什麼

下一個任務是**真實資料驗證**。

請依照以下順序：

1. 在本機啟動最新版系統
2. 開啟最終標單匯入頁面
3. 上傳真實 Excel
4. 預覽所有工作表
5. 只選擇真正的工程明細工作表
6. 執行唯讀檢查
7. 將解析出的數量／價格／原始列與 Excel 來源逐筆核對
8. 在商務定案狀態與建案資料尚未確認前，不得建立 Approved 歷史資料
9. 如果確定是真正最終標單，先建立 Draft 歷史建案
10. 處理審核阻擋項目
11. Publish History
12. 確認 2A 建案歷史摘要
13. 持續加入其他真實歷史建案
14. 有足夠 Approved 歷史案件後，測試結構體工程 P15／P50／P85
15. 建立真實待偵錯 AnalysisCase
16. 比較 V1／V2 的數量與價格分析結果

下一個對話應優先測試實際系統，不要再發明另一套架構。

---

## 不得恢復的已移除程式

不要恢復：

- `RangeEstimationService`
- `BatchEstimationService`
- `CoreMaterialService`
- `quantity_rule_base`
- 舊版 `/api/bid-anomaly`
- 舊版通用材料估算頁面
- 舊版一次性異常偵測頁面
- POC 偵錯顆粒度取捨示範

---

## 仍待廠商確認

正式回覆前維持目前暫定實作：

1. 產學版是否保留直接上傳，或 1D 為唯一入口
2. 直接上傳的正式資料格式
3. 接受的大項／細項層級
4. 1D 一次性快照流程是否正確
5. 哪些工程類別應參與數量／價格分析

詳見：

`docs/VENDOR_QUESTIONS.md`

---

## 給下一個對話的精簡提示詞

開啟新對話後可直接貼上：

```text
請直接接手 GitHub 專案 YI-CHENLIN1204/BiddingSystem-2A。

目前正式開發 branch 是 feature/1d-api-integration，請不要重新設計整套系統，也不要恢復已移除的通用材料估算。

請先閱讀：
README.md
docs/NEXT_CHAT_HANDOFF.md
docs/HANDOFF.md
docs/2A_CURRENT_ARCHITECTURE.md
docs/DECISIONS.md
docs/API.md
docs/DATABASE.md
docs/DEVELOPMENT.md
docs/LOCAL_TEST_GUIDE.md
docs/VENDOR_QUESTIONS.md
docs/WORKLOG.md

目前核心：
- 結構體工程 = 大項估算／價格異常
- 鋼筋、混凝土 = 細項數量偵錯
- AnalysisCase／Version／Run 是待偵錯工作資料
- ProjectMaster／FinalBidItem／HistoricalTransaction 是已定案歷史資料
- 1D 是選配，沒有 1D 時 2A 必須能獨立上傳使用
- 歷史標單匯入後先 Draft，人工審核後才能 Publish History → Approved
- Approved 歷史要修改必須 Reopen History Review
- HistoricalTransaction 只存鋼筋／混凝土數量，不存價格
- 相似建案包含 SiteArea
- 樣本不足就顯示不足，不准塞假資料

現在不要再做架構重整，直接進入本機啟動與真實標單資料驗證。
```
