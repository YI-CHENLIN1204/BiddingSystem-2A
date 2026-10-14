# 2A 本機測試指南

> Branch：`feature/1d-api-integration`

## 1. 更新本機程式碼

在 Repository 根目錄：

```powershell
git status
git fetch origin
git switch feature/1d-api-integration
git pull origin feature/1d-api-integration
```

預期狀態：

```text
On branch feature/1d-api-integration
Your branch is up to date with 'origin/feature/1d-api-integration'.

nothing to commit, working tree clean
```

以上為 Git 原生訊息，保留英文即可。

## 2. 重新 Build Docker

```powershell
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

## 3. 頁面基本測試

確認以下頁面都能正常開啟：

- 系統總覽
- 待偵錯案件
- 建案管理
- 決標回寫／最終標單匯入
- 結構體工程估算
- 參數管理
- 操作紀錄

另外確認：

- 舊版通用材料估算頁已不存在
- 舊版一次性異常偵測頁已不存在
- 即使未設定 1D，獨立模式仍能正常運作

## 4. 第一個真實歷史標單測試

使用真正已定案的最終標單。

預期流程：

```text
上傳最終標單
→ 建立 ProjectMaster，狀態為 Draft
→ 建立 FinalBidItem
→ 人工審核
→ 處理阻擋項目
→ Publish History
→ Approved
```

### 審核項目

對每一筆相關工項檢查：

- 實際工程大項
- 原始 `Category1 / Category2 / Category3`
- 是否有人工工程大項覆寫
- 材料對應
- 數量
- 單位
- 單價／金額
- 忽略狀態
- 對目前 2A 模型的影響

預期模型影響標示：

- 結構體工程價格基準
- 鋼筋／混凝土數量基準
- 不納入目前 2A 基準

## 5. Publish History 測試

如果仍有未處理問題，系統必須阻止發布。

重要阻擋條件包括：

- 未忽略工項仍無法辨識工程大項
- 結構體工程工項沒有可使用價格資料
- 已對應鋼筋／混凝土的工項仍有無效數量／單位

全部處理完成後：

```text
Publish History
→ HistoryStatus = Approved
```

確認：

- 該建案開始計入模型準備度
- Approved 歷史資料被鎖定
- 數量歷史只建立鋼筋／混凝土
- 結構體工程價格仍由 `FinalBidItem` 推導

## 6. Reopen History Review 測試

對一個 Approved 建案執行：

```text
Reopen History Review
```

預期：

- 狀態回到 Draft
- 暫時從有效模型基準中排除
- 歷史數量資料暫停使用
- 可以重新修改
- 修正後必須重新 Publish

## 7. 待偵錯案件測試

建立待偵錯案件，來源可以是：

- 直接上傳結構化標單
- 或在已設定 1D 時從 1D 匯入

預期：

```text
AnalysisCase
└─ V1
```

執行：

- 鋼筋／混凝土數量分析
- 結構體工程價格分析

如果歷史資料不足，正確結果應該是：

```text
樣本不足
```

不要把樣本不足視為系統錯誤。

## 8. 版本測試

上傳修改後的待偵錯標單：

```text
V1 → V2
```

確認：

- V1 保持不變
- V2 成為目前版本
- 每個版本都可以有自己的 AnalysisRun
- 舊的 Run 可以從歷史紀錄重新開啟

## 9. 1D 測試

若已設定 `OneD:BaseUrl`：

- 1D 匯入入口應可使用
- 匯入只讀取 1D 一次
- 結果存成 2A 本地快照
- 後續分析不可以再次呼叫 1D

若沒有設定 1D：

- 直接上傳仍必須完整可用
- 1D 選項應顯示不可使用，但不能讓整個頁面失效

## 10. 目前預期限制

清理後的開發資料庫可能少於 5 個 Approved 歷史建案。

因此：

- 結構體工程價格準備度可能低於 5
- 鋼筋準備度可能低於 5
- 混凝土準備度可能低於 5

在有效歷史資料足夠前，不可以強制產生 P15／P50／P85。

## 11. 匯入前唯讀數量檢查

在 `/final-bid` 與 `/analysis-cases` 都要測：

1. 上傳包含多個工作表的 Excel。
2. **只選擇**真正工程明細工作表，不選摘要／報價／比價工作表。
3. 點選「**檢查選取工作表的全部候選明細**」。
4. 對照原始檔，確認畫面上的工作表、原始列號、工項／規格、數量、單位、單價與金額。
5. 使用鋼筋、混凝土與待檢查篩選。確認單位轉換：T → kg（×1000）、M3 → m³（×1）；續接器、施工服務與無法換算單位不得進入候選總量。
6. 移除或更改工作表選擇後，舊的檢查結果必須清除。
7. 在尚未填寫建案資料前也可以檢查；預覽流程**不得**建立 Project、AnalysisCase、模型歷史或假樣本。

真實 Excel 第一個檢查點：`最終標單.xls`，選擇 `建築工程-預算單`。先前預覽回報 49 筆候選列。請核對來源範例值：214 M3、單價 2350、金額 502900。

不要因為工作表名稱叫「預算單」就推論它已經定案；在取得真正最終價格證據與確認建案基本資料前，不得發布成 Approved 歷史資料。

自動化 Parser 回歸測試：

```powershell
dotnet run --project ./tests/WorkbookParserTests/WorkbookParserTests.csproj -c Release
```

CI 也會建置 Backend 與 Frontend。
