# BiddingSystem-2A

> **2026-11-11 最新交接入口：** [完整交接主檔](docs/CURRENT_HANDOFF_2026-11-11.md) · [研究版本／證據回溯](docs/RESEARCH_TRACEABILITY_2026-11-11.md) · [新對話可複製提示](docs/NEW_CHAT_START_2026-11-11.md)。舊 2026-10-15 文件與零資料庫紀錄為歷史快照，不應當成現況。專題模擬研究請見 [研究操作](research/2a_synthetic/README.md)。


崴正營造 × 雲科大資管產學合作之 **2A 量價估算與異常偵測系統**。

目前正式開發方向已收斂為：

- **大項估算／價格異常：結構體工程**
- **細項數量偵錯：鋼筋、混凝土**
- 待偵錯案件與歷史最終標單完全分流
- 1D 為選配資料來源，2A 必須可獨立運作
- 歷史最終標單必須經人工審核與 `Publish History` 才能成為模型基準
- 相似建案使用建案條件 + 時間權重建立 P15／P50／P85 合理範圍

> 目前正式開發 Branch：`feature/1d-api-integration`

---

## 1. 目前系統概念

```text
                         2A
                          │
            ┌─────────────┴─────────────┐
            │                           │
        待偵錯流程                  歷史資料流程
            │                           │
    ┌───────┴───────┐              Draft 審核
    │               │                  │
自行上傳        選配 1D 匯入          Publish History
    │               │                  │
    └───────┬───────┘               Approved
            │                           │
       AnalysisCase                     │
            │                           │
       V1 / V2 / V3                     │
            │                           │
       AnalysisRun                      │
            │                           │
            └─────────────┬─────────────┘
                          ↓
                 2A 估算／異常偵測
```

關鍵規則：

```text
AnalysisCase != ProjectMaster
```

正在修改中的待偵錯標單絕對不能自動成為歷史模型真值。

---

## 2. 目前分析範圍

### 大項估算

```text
結構體工程
→ 歷史每坪單價
→ 相似建案加權
→ 時間權重
→ 加權 P15／P50／P85
```

### 細項數量異常偵測

```text
結構體工程
├─ 鋼筋      → kg / m²
└─ 混凝土    → m³ / m²
```

系統刻意不對所有工程類別做估算。

---

## 3. 技術架構

### Frontend

- Vue 3
- TypeScript
- Vite
- 正式 Docker Container 內使用 Nginx

### Backend

- ASP.NET Core Web API
- .NET 10
- Entity Framework Core
- Npgsql

### 資料庫

- PostgreSQL
- 目前開發資料庫使用 Supabase

### 部署／本機整合

- Docker Compose
- Frontend 與 Backend 完全分離
- Frontend 只透過 HTTP API 呼叫 Backend

---

## 4. Repository 結構

```text
BiddingSystem-2A/
├─ frontend/                 Vue 3 前端
├─ 崴正/                     ASP.NET Core 後端
├─ database/                 SQL Migration／Schema 異動
├─ docs/                     架構、交接與開發文件
├─ tests/                    Parser 回歸測試
├─ compose.yaml
├─ .env.example
└─ README.md
```

---

## 5. 文件索引

新的開發對話或新成員應先閱讀文件，不要再從舊對話重新拼湊整個專案。

| 文件 | 用途 |
|---|---|
| `docs/README.md` | 文件索引與建議閱讀順序 |
| `docs/NEXT_CHAT_HANDOFF.md` | 下一個 ChatGPT／開發對話的精簡交接 |
| `docs/HANDOFF.md` | 目前開發狀態與下一步 |
| `docs/2A_CURRENT_ARCHITECTURE.md` | 正式系統架構與資料流 |
| `docs/DECISIONS.md` | 不應被默默推翻的架構決策 |
| `docs/API.md` | 目前 Backend API 參考 |
| `docs/DATABASE.md` | 資料表、責任、關聯與模型資格規則 |
| `docs/DEVELOPMENT.md` | 本機開發、Docker、Build、測試與修改規則 |
| `docs/LOCAL_TEST_GUIDE.md` | 本機實際驗證步驟 |
| `docs/VENDOR_QUESTIONS.md` | 仍等待廠商確認的問題 |
| `docs/WORKLOG.md` | 開發紀錄 |

建議閱讀順序：

1. `docs/README.md`
2. `docs/NEXT_CHAT_HANDOFF.md`
3. 本 README
4. `docs/2A_CURRENT_ARCHITECTURE.md`
5. `docs/DECISIONS.md`
6. `docs/API.md`
7. `docs/DATABASE.md`
8. `docs/DEVELOPMENT.md`
9. `docs/LOCAL_TEST_GUIDE.md`
10. `docs/WORKLOG.md`

---

## 6. Docker 快速啟動

從 `.env.example` 建立 `.env`。

必要：

```text
DB_CONNECTION_STRING
```

選配：

```text
ONE_D_API_BASE_URL
```

更新本機 Branch：

```powershell
git status
git fetch origin
git switch feature/1d-api-integration
git pull origin feature/1d-api-integration
```

重新 Build 並啟動：

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

---

## 7. 本機 Frontend／Backend 開發

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

Frontend 預設開發網址：

```text
http://127.0.0.1:5174
```

Vite 會將 `/api` 代理到 Backend。

---

## 8. 不同環境的行為

### 專題／獨立模式

保持：

```text
ONE_D_API_BASE_URL=
```

2A 仍可完全透過直接上傳運作。

### 產學整合模式

設定：

```text
ONE_D_API_BASE_URL=<1D API Base URL>
```

之後 UI 會額外開放 1D 匯入。

1D 只用來建立一次性的 2A 本地快照；後續分析都使用本地 `AnalysisBidVersion`。

---

## 9. 歷史資料流程

匯入最終標單後不會立即成為模型真值。

```text
Excel / CSV
→ 預覽工作表
→ 明確選擇工作表
→ 唯讀檢查
→ 匯入
→ ProjectMaster Draft
→ 工項／工程大項／材料／單位／價格審核
→ 處理發布阻擋項目
→ Publish History
→ Approved
```

只有 Approved 歷史資料可以參與估算。

若要修改 Approved 基準：

```text
Reopen History Review
→ Draft
→ 修改
→ 再次發布
```

---

## 10. 待偵錯案件流程

```text
建立 AnalysisCase
→ V1
→ 執行數量／價格分析
→ 上傳修改後標單
→ V2
→ 再次分析
→ 保留 V1 與舊 AnalysisRun
```

舊的待偵錯版本都是不可覆寫快照。

---

## 11. 匯入前唯讀 Excel 檢查

正式匯入前，使用共用工作表預覽／檢查流程。

Backend：

```text
POST /api/tender-import/preview
POST /api/tender-import/inspect
```

檢查流程：

- 不建立 Project
- 不建立 AnalysisCase
- 不寫入 HistoricalTransaction
- 保留來源工作表與列號
- 不猜測任意數字欄位
- 顯示鋼筋／混凝土候選數量與警告

---

## 12. 模型準備度

預設最低有效歷史資料：

```text
5 個 Approved 建案
```

準備度分開計算：

- 結構體工程價格
- 鋼筋數量
- 混凝土數量

歷史資料不足時，正確結果就是：

```text
樣本不足
```

不要為了讓 P15／P50／P85 出現而建立假歷史資料。

---

## 13. Build 驗證

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

Parser 回歸測試：

```powershell
dotnet run --project .\tests\WorkbookParserTests\WorkbookParserTests.csproj -c Release
```

---

## 14. 安全／設定規則

禁止提交：

- PostgreSQL 密碼
- API Key
- `.env`
- 正式環境機密資料

資料庫連線必須透過：

```text
ConnectionStrings__DefaultConnection
```

Docker Compose 對應：

```text
DB_CONNECTION_STRING
→ ConnectionStrings__DefaultConnection
```

---

## 15. 已移除舊架構

沒有新的明確決策前，不要恢復：

- 通用材料估算
- `RangeEstimationService`
- `BatchEstimationService`
- `CoreMaterialService`
- `quantity_rule_base`
- 舊版一次性 `/api/bid-anomaly`
- 舊版一次性異常 UI
- POC 偵錯顆粒度取捨示範

目前正式待偵錯入口：

```text
AnalysisCase → AnalysisBidVersion → AnalysisRun
```

---

## 16. 目前開發階段

目前架構已經足以進入實際操作與真實資料驗證。

立即目標：

1. 在本機執行最新 Branch
2. 將真實 Excel 解析結果與原始列對照
3. 匯入第一份真正已定案歷史標單
4. 處理 Draft 審核阻擋項目
5. 發布第一個 Approved 歷史建案
6. 持續加入其他真實案件
7. 有足夠歷史資料後驗證 P15／P50／P85
8. 建立真實待偵錯案件
9. 比較 V1／V2 結果

在真實資料驗證發現真正問題以前，不要再重新設計整套系統。

---

## 17. Branch 規則

目前正式開發：

```text
feature/1d-api-integration
```

除非使用者明確要求，否則不要合併到 `main`。

---

## 18. GitHub Branch 顯示

目前正式文件與最新 2A 實作都維護在：

```text
feature/1d-api-integration
```

如果 GitHub 當前顯示 `main`，可能看不到最新 `docs/` 與 2A 修改。

請使用 Branch 選擇器切換到 `feature/1d-api-integration`。

除非使用者明確要求，否則不要合併到 `main`。
