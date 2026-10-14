# 2A 開發指南

> 更新日期：2026-10-15  
> Repository：`YI-CHENLIN1204/BiddingSystem-2A`  
> 目前正式開發 Branch：`feature/1d-api-integration`

本文件說明如何延續目前開發，同時避免把已經移除的舊架構重新加回來。

## 1. Branch 規則

目前正式開發持續使用：

```text
feature/1d-api-integration
```

除非使用者明確要求，否則不要合併到 `main`。

舊版 POC Branch：

```text
feature/1d-detection-integration
```

不是目前正式產品方向，必須與正式開發分開。

---

## 2. 修改架構前必讀文件

請依照以下順序閱讀：

1. `docs/NEXT_CHAT_HANDOFF.md`
2. `README.md`
3. `docs/2A_CURRENT_ARCHITECTURE.md`
4. `docs/DECISIONS.md`
5. `docs/API.md`
6. `docs/DATABASE.md`
7. `docs/LOCAL_TEST_GUIDE.md`
8. `docs/VENDOR_QUESTIONS.md`
9. `docs/WORKLOG.md`

不要從已刪除 Service 或舊 Branch 猜測目前系統行為。

---

## 3. 本機 Docker 開發流程

在 Repository 根目錄：

```powershell
git status
git fetch origin
git switch feature/1d-api-integration
git pull origin feature/1d-api-integration
```

重新建置並啟動容器：

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

只看 Backend：

```powershell
docker compose logs backend --tail=150
```

只看 Frontend：

```powershell
docker compose logs frontend --tail=150
```

---

## 4. 環境變數

從：

```text
.env.example
```

建立：

```text
.env
```

必要：

```text
DB_CONNECTION_STRING
```

選配：

```text
ONE_D_API_BASE_URL
```

如果 `ONE_D_API_BASE_URL` 為空：

- 2A 仍然必須可以正常使用
- 直接上傳必須保留
- 1D 按鈕應為不可使用／停用狀態
- 待偵錯分析不得依賴 1D

禁止提交密碼、Connection String、API Key 或本機 `.env` 內容。

---

## 5. 不使用 Docker 的本機開發

Backend：

```powershell
$env:ConnectionStrings__DefaultConnection = "<PostgreSQL 連線字串>"
dotnet run --project .\崴正\崴正.csproj
```

Backend 預設開發網址：

```text
http://127.0.0.1:5261
```

Frontend：

```powershell
cd .\frontend
npm install
npm run dev
```

Frontend 預設網址：

```text
http://127.0.0.1:5174
```

Vite 會將 `/api` 代理到 Backend。

---

## 6. Build 驗證

Backend：

```powershell
dotnet restore .\崴正\崴正.csproj
dotnet build .\崴正\崴正.csproj -c Release
```

Frontend：

```powershell
cd .\frontend
npm ci
npm run build
```

只要任一端 Build 失敗，就不能視為修改完成。

---

## 7. Parser 回歸測試

Repository 內有 Excel／CSV Parser 回歸測試。

執行：

```powershell
dotnet run --project .\tests\WorkbookParserTests\WorkbookParserTests.csproj -c Release
```

以下修改完成後建議執行：

- 工作表選擇
- 數量／價格欄位辨識
- Excel／CSV 解析
- 鋼筋／混凝土候選檢查
- 原始列追溯

---

## 8. 目前正式架構

不要偏離以下架構：

```text
待偵錯
AnalysisCase
  → AnalysisBidVersion
      → AnalysisBidItem
      → AnalysisRun

歷史資料
ProjectMaster
  → FinalBidItem
      → HistoricalTransaction
```

待偵錯資料與歷史資料是刻意分開的。

---

## 9. 目前分析範圍

不要自動擴大。

目前大項價格分析範圍：

```text
結構體工程
```

目前數量偵錯範圍：

```text
鋼筋
混凝土
```

除非有新的明確需求，否則不要重新加入：

- 通用材料估算
- 模板估算
- 鋼骨估算
- 門窗數量估算
- 任意全類別估算

---

## 10. 已定案歷史資料開發規則

匯入不代表核准。

正式流程：

```text
上傳最終標單候選
→ Draft
→ 人工審核
→ 處理阻擋項目
→ Publish History
→ Approved
```

Approved 模型輸入在重新執行：

```text
Reopen History Review
```

之前必須保持不可修改。

不要為了快速建立模型樣本而繞過發布審核。

---

## 11. 待偵錯版本開發規則

待偵錯標單有修改時，要建立新版本。

正確：

```text
AnalysisCase
├─ V1
├─ V2
└─ V3
```

錯誤：

```text
一直覆寫 V1
```

已儲存的 AnalysisRun 必須永遠綁定當時產生結果的版本。

---

## 12. Excel 匯入規則

待偵錯上傳與歷史最終標單匯入，在需要時都必須明確選擇工作表。

必要防護：

- 先預覽 Excel 內工作表
- 由使用者明確選擇要納入的工作表
- 保留工作表名稱與原始列號
- 不可以從列尾任意數字猜測數量／價格
- 不可以把摘要／說明資料當成工程明細
- 不可以只因工作表名稱含「預算單」或「自估」就自動信任
- 檢查流程必須唯讀

相關 API：

```text
POST /api/tender-import/preview
POST /api/tender-import/inspect
```

---

## 13. 資料庫異動規則

目前仍在開發階段，若設計已明確淘汰，可以直接硬清理開發資料與 Schema。

修改 Schema 時依序：

1. 修改 C# Model
2. 修改 EF Mapping
3. 在 `database/` 新增 SQL
4. 套用到開發資料庫
5. 驗證實際 Schema
6. 移除已淘汰程式
7. 更新 `docs/DATABASE.md`
8. 如果資料責任／架構改變，更新 `docs/DECISIONS.md`

沒有明確理由時，不要保留死欄位或相容性欄位。

---

## 14. 文件維護規則

完成有意義的開發後，至少更新以下其中一份：

- `docs/WORKLOG.md`
- `docs/HANDOFF.md`
- `docs/NEXT_CHAT_HANDOFF.md`

如果 API 改變：

- 更新 `docs/API.md`

如果 Schema／資料責任改變：

- 更新 `docs/DATABASE.md`

如果架構改變：

- 更新 `docs/2A_CURRENT_ARCHITECTURE.md`
- 更新 `docs/DECISIONS.md`

README 應維持入口文件角色，不要塞入所有內部設計細節。

---

## 15. Git 開發流程

修改前：

```powershell
git status
git pull origin feature/1d-api-integration
```

修改完成後，先驗證 Build／測試，再 Commit。

建議 Commit 格式：

```text
feat: ...
fix: ...
refactor: ...
db: ...
docs: ...
test: ...
```

除非使用者明確要求，不要為每一個小修改另外建立 Branch。

---

## 16. 目前真實資料驗證順序

現在優先目標是實際操作，不是繼續擴張架構。

建議順序：

1. 更新本機 Branch
2. 重新 Build Docker
3. 開啟 `http://localhost:8081`
4. 預覽真實 Excel
5. 明確選擇真正的工程明細工作表
6. 執行唯讀檢查
7. 將解析列與 Excel 原始資料比對
8. 確定是真正已定案資料後，才建立 Draft 歷史建案
9. 檢查工程大項／材料／單位／價格阻擋項目
10. 驗證完成後才發布
11. 持續加入真實歷史建案
12. 有足夠歷史資料後，建立真實 AnalysisCase
13. 比較 V1／V2 分析結果

---

## 17. 「可以開始操作」的判定

當以下條件都成立時，本機系統即可開始實際操作測試：

- Branch 已更新到最新
- Working Tree 乾淨
- Backend Container 為 Up
- Frontend Container 為 Up
- `http://localhost:8081` 可以開啟
- 系統總覽正常載入
- 待偵錯案件頁正常載入
- 建案／歷史資料頁正常載入
- 最終標單匯入頁正常載入
- 結構體工程估算頁正常載入
- 參數設定頁正常載入
- 操作紀錄頁正常載入

達成後就進入真實資料驗證，不要再建立人工假歷史資料。
