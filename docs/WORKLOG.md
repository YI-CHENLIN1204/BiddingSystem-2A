# 2A 開發紀錄

## 2026-10-12 — 正式建立待偵錯／歷史資料分流

完成：

- 新增版本化 `AnalysisCase` 待偵錯流程
- 待偵錯資料表與歷史資料表完全分離
- 新增一次性 1D 快照匯入
- 保留 2A 獨立直接上傳
- 新增 V1／V2／V3 等待偵錯版本
- 新增可保存的 `AnalysisRun`
- 新增重新開啟歷史 Run 結果功能
- 阻止待偵錯案件寫入歷史真值

## 2026-10-12 — 收斂分析範圍

目前正式工作範圍：

- 結構體工程大項估算／價格異常
- 鋼筋數量異常
- 混凝土數量異常

從目前偵錯範圍移除：

- 模板
- 鋼骨

其他工程類別仍保留原始資料，但不強迫進入目前數量模型。

## 2026-10-12 — 資料庫硬清理

開發資料庫完成清理。

移除已淘汰／重複設計，包括：

- 已不使用的舊建案欄位
- 重複保存的歷史來源欄位
- `historical_transaction` 中的歷史價格欄位
- `quantity_rule_base`
- 舊測試／假歷史資料

目前歷史資料責任：

- `final_bid_item`：原始工項與結構體工程價格
- `historical_transaction`：只保存標準化後的鋼筋／混凝土數量

## 2026-10-12 — 基地面積整合

將 `SiteArea` 加入：

- `ProjectMaster`
- `AnalysisCase`
- 最終標單匯入資料
- 待偵錯案件基本資料
- 結構體工程估算表單
- 相似度評分
- 參數設定與 Audit 顯示

新增可設定：

- `SiteAreaWeight`
- `SiteAreaDifferenceCutoffRatio`

## 2026-10-12 — 移除舊版通用材料估算

移除已淘汰架構：

- 通用材料合理區間估算
- 批次材料估算
- 核心材料估算
- 舊版數量規則資料表
- 舊版一次性標單異常 Route
- 舊版通用估算 UI

目前正式服務：

- `MajorCategoryPriceRangeService`
- `DetectionCategoryRangeService`
- `AnalysisCaseService`

## 2026-10-12 — 建立歷史審核／發布流程

最終標單匯入後目前先進：

```text
Draft
```

新增審核：

- 實際工程大項覆寫
- 材料對應
- 數量／單位／單價修正
- 忽略狀態
- 即時發布阻擋條件

新增：

- `Publish History`
- `Reopen History Review`

Approved 建案才會進入模型基準。

Draft 建案不會參與模型。

## 2026-10-12 — Approved 歷史資料鎖定

歷史狀態為 `Approved` 時鎖定：

- 建案相似度基本資料
- 標單工項修改
- 工程大項覆寫
- 材料對應
- 忽略／取消忽略

若要修正，必須先重新開啟歷史審核。

## 2026-10-12 — 進入真實資料驗證階段

下一階段不再繼續擴張架構。

接下來優先：

1. 在本機啟動系統
2. 匯入第一份真正已定案最終標單
3. 檢查審核阻擋項目
4. 驗證工程大項／材料對應／單位修正流程
5. 發布第一個 Approved 歷史建案
6. 驗證 2A 建案摘要
7. 累積足夠真實歷史資料後驗證 P15／P50／P85
8. 建立真實待偵錯案件並比較 V1／V2

## 2026-10-12 — 新對話操作交接文件

新增：

- `docs/NEXT_CHAT_HANDOFF.md`
- 明確的本機「可以開始操作」條件
- 完整 Git Pull／Docker 重建指令
- 第一份真實最終標單驗證順序
- 明確要求下一個對話在本機系統可操作時直接告知使用者

下一階段是用真實最終標單進行實際驗證，不是再重新設計架構。

## 2026-10-13 — 匯入前唯讀數量檢查

在既有 `feature/1d-api-integration` Branch 上新增：

- `POST /api/tender-import/inspect`：必須明確提供選取工作表清單，而且不寫入資料庫。
- 共用 `WorkbookSheetPicker`：讓使用者在輸入建案基本資料前，就可以檢查所有解析候選列，包括原始工作表／列號、來源分類、數量、單位、價格、金額與警告。
- 保守辨識鋼筋與混凝土候選：只有在單位明確時才換算 kg／T 與 m³；鋼筋續接器、鋼筋施工服務、混凝土澆置服務不計入材料數量。
- 無法辨識單位、缺少數量、結構體工程大項不確定、來源列指紋重複等情況都會顯示待審核警告。
- 候選摘要**不是**模型異常、Approved 歷史或正式工程量，不會製造假的 P15／P50／P85。
- 在 `tests/WorkbookParserTests` 加入回歸測試。

下一步：在 UI 手動驗證實際上傳的 `.xls`，將 49 筆解析候選列與 Excel 原始資料逐筆比對，再處理任何漏抓混凝土／鋼筋或錯誤分類列。

不得把預算價或廠商報價直接發布成 Approved 歷史真值。

## 2026-10-15 — 文件／專案交接檢查點

完成：

- 擴充 `README.md`，作為目前專案入口
- 新增 `docs/API.md`，記錄目前 Controller／API 合約
- 新增 `docs/DATABASE.md`，記錄目前資料表責任與開發資料庫 Schema
- 新增 `docs/DEVELOPMENT.md`，記錄 Docker、本機 Build、Parser 測試與修改規則
- 更新 `docs/HANDOFF.md`
- 更新 `docs/NEXT_CHAT_HANDOFF.md`，加入可直接複製的新對話提示詞
- 將文件統一到目前 Draft → 審核 → Publish History → Approved 歷史流程
- 記錄唯讀 Excel 工作表預覽／檢查防護
- 記錄結構體工程價格歷史與鋼筋／混凝土數量歷史的責任分離

目前下一階段仍然是：

1. 拉取並執行最新 Branch
2. 使用真實來源檔驗證工作表預覽／檢查
3. 只匯入真正已定案歷史標單
4. 處理 Draft 審核阻擋
5. 發布 Approved 歷史資料
6. 累積足夠真實歷史後驗證 P15／P50／P85
7. 最後驗證真實待偵錯 V1／V2 分析

不得為了展示而加入假歷史資料。

## 2026-10-15 — 文件索引與 Branch 顯示說明

完成：

- 新增 `docs/README.md` 作為文件入口
- 記錄交接與開發建議閱讀順序
- 明確說明目前正式文件與 2A 實作位於 `feature/1d-api-integration`
- 說明 GitHub 顯示 `main` 時可能看不到 `docs/` 的原因
- 保持「不得自行合併 main」規則不變

目前下一步仍為實際系統操作與真實最終標單驗證。

## 2026-10-15 — 文件中文化

完成：

- 將專案 README 與 `docs/` 主要說明內容統一改為繁體中文
- 保留 API 路徑、Branch 名稱、Class／Table／欄位名稱等實際技術識別字
- 後續新增文件預設使用繁體中文


## 2026-10-15 — 模擬資料端對端分析與清理交接

- 用 20 筆含雜訊的虛構標單測試工作表預覽／唯讀檢查；修正小數項次、逐列工程分類及材料服務工項誤算。
- 建立 AnalysisCase V1、V2，確認版本快照獨立、舊 Run 可保存與重新開啟；修正舊 Run JSON PascalCase 前端相容問題。
- 完成 Run #9 V2 的數量合理區間、Run #6 價格合理區間介面測試。
- 依使用者要求，特別加入 8 個**明確標記**的 SQL 合成 Approved 模擬建案、24 筆價格明細及 16 筆鋼筋／混凝土數量歷史，以驗證 8 案 P15/P50/P85 的端對端執行。
- **此批 Approved 不代表人工審核或真實標單，屬開發展示例外，不得供真實工程決策使用。**
- 詳情見 `docs/DEMO_DATA_20261015.md`，清理腳本見 `scripts/cleanup_demo_2a_20261015.sql`。
- 正式待辦：測歷史 Draft／Publish／Reopen 的完整操作，展示完先清除合成歷史，之後才進行真實歷史資料驗證。

## 2026-10-16 — 資料庫交易穩定性集中修正（待整合驗證）

觀察到的故障：Supabase Session Pooler 5432 間歇性 `NpgsqlException: Exception while reading from stream`、HTTP 500/504；發布批次寫入時連線中斷，Rollback 可能拋出次生例外。另全域 `EnableRetryOnFailure` 與既有顯式交易不相容，已撤回。

本次修改：
- `ProjectManagementService` 中 Publish 以外的明確交易也保護 Rollback，保留原始例外；Publish 原有的保護保持不變。
- `FinalBidService` 五處交易全面保護 Rollback，原始例外寫入後端診斷記錄。
- `FinalBidImportService` 原已保護 Rollback，未重複更動。
- `ProjectDetailView.vue` 發布／重新開啟請求若 HTTP 回應失敗，先重新讀取建案狀態及歷史摘要；結果不明時警告不要連續重試，避免網路逾時誤認為資料未提交。
- 未直接變更資料庫、未合併 main；仍保留 `Pooling=false` 作為暫時診斷，不視為長期效能調校。

**未完成、不可宣稱正式修復**：尚未於使用者的本地 Docker 編譯／執行完整迴歸；Supabase Pooler 中斷的網路根因仍待定位。不可對顯式交易啟用未包裹完整交易的自動重試，避免重複寫入。

整合驗證順序：
1. `git status` / `git pull --ff-only`、`docker compose up -d --build`、`docker compose ps`，確認 dotnet 與 Vite Build 都通過。
2. 讀取 Project #11 並確認 Approved／有效歷史交易 7 筆及總價 12,063,350，作為測試前狀態。
3. 先驗證核准期間修改被拒絕，再做一次 Reopen，核對 Draft／有效交易 0 筆／保留完整 FinalBidItem。
4. 修改非核心 Draft 工項後再 Publish，核對 Approved／有效交易不重複、結構體價格與試體試驗排除邏輯。
5. 用操作紀錄及資料庫查詢比對每次交易，壓力測試連續查詢與短暫斷線；發生 500/504 應先回查資料庫狀態，不要直接重試寫入。

## 2026-10-17 — DEMO #8 Publish/Reopen 寫入整合驗證

執行 `scripts/test-demo-history-cycle.ps1`，DEMO Project #8 成功完成兩輪 Draft → Approved → Draft。Supabase 實際驗證：#8 最終 Draft、核准時間 NULL、有效歷史交易 0、已停用歷史交易 2；當天稽核紀錄 PublishHistoryBaseline 2 筆、ReopenHistoryReview 2 筆。Project #11 保持 Approved 與 7 筆有效歷史交易，沒有更動。此測試通過**正常寫入流程**，尚未驗證注入斷線、併發發布、重試冪等、長時間穩定性，不能稱為完整根因修復。

## 2026-10-17 — Publish／Reopen 併發保護（待本機驗證）

發現同一建案兩個並行請求可能通過狀態檢查。已在 PublishHistoryAsync、ReopenHistoryReviewAsync 的顯式交易起點使用 PostgreSQL pg_advisory_xact_lock(2202, projectId) 序列化相同建案的操作；交易結束或連線失效時自動解鎖。發布後的有效歷史交易計數移至 Commit 前，避免提交成功後再查詢失敗造成成功被誤報。

新增 scripts/test-demo-history-concurrent.ps1，限制 DEMO #8，預期兩個同時 Publish 一個成功、另一個 409，Reopen 亦然；結束時回到 Draft。**尚未執行本機 Build 或併發測試，不能宣稱正式驗證通過**；須核對稽核紀錄與有效歷史交易，若錯誤立即停止，不能自動重試寫入。

## 2026-10-17 — DEMO #8 併發測試完成

使用本機重建後 API 執行 scripts/test-demo-history-concurrent.ps1。雙重 Publish 回應 200/409，雙重 Reopen 回應 409/200；Supabase 核查 Project #8 最終 Draft、有效歷史交易 0、停用歷史交易 2，該測試時段 PublishHistoryBaseline 與 ReopenHistoryReview 稽核紀錄各 1 筆。Project #11 保持 Approved、7 筆有效交易。此結果證明本輪雙請求衝突保護有效，但不代表斷線故障注入、長時間高併發及端到端連線根因已完成驗證。

## 2026-10-17 — 隔離資料庫故障測試（待執行）

新增 scripts/test-isolated-postgres-failure.ps1：以 Docker 啟動獨立 postgres:17 容器（無對外 port、network none），在顯式交易先 INSERT 再強制 statement_timeout，確認失敗後不留部分提交，finally 清除容器。此為 PostgreSQL 原子性基礎測試，**不會測到本專案 Npgsql/EF Core 的連線中斷、Commit 回應遺失或應用程式重試安全性**。尚未在使用者本機執行，不能視為完成故障復原驗收。DEMO #8／Project #11 均不參與此測試。

## 2026-10-17 — 隔離 PostgreSQL 逾時回復測試通過

使用者本機執行 scripts/test-isolated-postgres-failure.ps1：CREATE TABLE 成功、BEGIN/INSERT 成功、強制 statement_timeout 出現 expected error，最後顯示 PASS: transaction was rolled back; no partial row exists。此為獨立 PostgreSQL 的回滾原子性驗證，不涉及 Supabase、DEMO #8、Project #11，也不構成 Npgsql/EF Core 斷線或 Commit 回應遺失驗收。

## 2026-10-17 — 隔離 Npgsql 故障測試程式（待本機執行）

新增 tests/ConnectionFaultHarness（.NET 10 / Npgsql）及 scripts/test-isolated-npgsql-failures.ps1。腳本以 Docker 建立一次性私有網路和 postgres:17，使用 dotnet/sdk:10.0 執行兩種情境：(1) 交易 INSERT 後，透過另一路連線 pg_terminate_backend 中斷提交前的 writer，檢查無殘留資料；(2) Commit 已成功但模擬應用層 HTTP 回應遺失，透過持久操作鍵與 unique constraint 避免重覆插入。finally 清除測試 DB 和網路；絕不使用 Supabase。需於 Windows PowerShell/Docker 本機驗證，**不是實際 ProjectManagementService 的斷線整合測試，也不代表目前 Publish API 已具備完整請求冪等保護**。

## 2026-10-17 — Npgsql 隔離斷線實測通過

使用者本機執行 scripts/test-isolated-npgsql-failures.ps1，測試環境為一次性 PostgreSQL 17 與 .NET 10/Npgsql。實測輸出：Expected writer connection failure: PostgresException；PASS: connection dropped before commit; no partial row；Expected simulated lost response；PASS: lost response can be reconciled by durable operation key；PASS: isolated Npgsql integration harness completed。這證明隔離測試的交易前斷線回滾與持久操作鍵範例可行，不代表正式 PublishHistory／ReopenHistoryReview API 已具備端到端請求識別與冪等。後續優先調查正式 API 丟失 Commit 回應時的狀態回查，以及整合操作鍵或等價的重複提交防護，再於隔離 DB 做故障注入。

## 2026-10-17 — 正式 API 故障處理盤點及前端重入保護

核對 ProjectsApiController、ProjectManagementService 與 ProjectDetailView：正式 Publish/Reopen 已有 PostgreSQL 建案級 advisory transaction lock 與 Approved/Draft 狀態檢查；因此相同建案的同步重複請求已有基於狀態的防護，但**尚未具備持久化 Idempotency-Key**。前端加入 publishingHistory 執行中直接拒絕再次觸發，可避免 UI 雙擊造成並行請求；不等於網路斷線重試的真正冪等保護。此階段未新增資料庫 schema、未更動 Project #8/#11。根因目前只能定位為曾發生連線讀取中斷與多處交易錯誤處理問題，尚無證據可單一定因於 Supabase、Npgsql 或 Nginx。待辦：設計包含唯一操作 ID、持久化狀態與唯一限制的寫入協定，使用獨立應用程式資料庫驗證，再部署至共用開發資料庫；勿在正式資料庫進行故障注入。新增前端變更尚待本機 Build。

## 2026-10-17 — 正式 Publish/Reopen 持久操作收據（待部署/驗證）

此次集中實作（尚未部署）：
- scripts/migrations/20261017_history_operation_receipt.sql：新增 public.history_operation_receipt，operation_id UUID 主鍵、project_id/action_type/result_json/created_at，保留成功操作的持久化紀錄。**尚未在 Supabase 執行**。
- 崴正/Models/HistoryOperationReceipt.cs + ApplicationDbContext：新增 EF Core 對應。
- ProjectManagementService：在同建案 advisory transaction lock 取得後，若 Idempotency-Key 已有同建案同動作收據則回傳原結果；首次成功則將操作收據寫在同一個資料庫交易內，隨工項歷史與稽核一起 Commit。操作 ID 被不同建案/動作複用時回傳 Conflict。
- ProjectsApiController：Publish/Reopen 接受可選 Idempotency-Key UUID header；新增 GET /api/projects/{projectId}/history-operation/{operationId} 提供成功收據查詢。
- ProjectDetailView：使用 sessionStorage 保存尚未確定結果的操作 ID；首次成功清除，連線逾時後先讀成功收據，若查不到則保留識別碼、不斷言提交結果。UI 同時有 busy guard。
- scripts/test-demo-history-idempotency.ps1：使用 DEMO #8 對同一動作以相同 key 重送，驗證結果與收據，再做 Reopen 並回到 Draft；資料庫交易數及稽核需後續人工 SQL 核對。

**部署順序不可顛倒**：先備份/確認環境、在目標開發 Supabase 執行 migration，確認 schema；再更新程式碼和 Docker Build，最後執行 DEMO #8 測試、核對稽核；完成前不得把新前端搭配舊後端或反之視為穩定版本。舊第三方無 header 請求仍使用狀態鎖，但沒有持久鍵回放保障；正式全面冪等需另行決定是否強制要求 header。

**本輪尚未在使用者本機 dotnet build / npm build、未執行 SQL migration、未實測 API；所有功能標註「待驗證」**。切勿在驗證之前宣稱正式修復。失敗時需保留 DB schema 並回滾應用版本，不應直接 drop receipt 表，以免丟失提交證據。

## 2026-10-17 — 開發 Supabase 操作收據 schema 已部署

使用者本機回報 docker compose build backend frontend 皆 Built（尚未 up）。已在 Supabase 開發專案 sbykuwypriudrgurmssb 查證 history_operation_receipt 原不存在，執行 scripts/migrations/20261017_history_operation_receipt.sql 對應的 BEGIN/CREATE TABLE/INDEX/COMMENT/COMMIT，成功建立並核對欄位 operation_id uuid、project_id integer、action_type varchar、result_json jsonb、created_at timestamptz，初始 0 筆。此遷移未改動 project_master、final_bid_item、historical_transaction 既有資料。**新版容器尚未啟動，正式 API 同 key 重送尚未驗證。** 下一步部署本機新版並只以 DEMO #8 測試，確認操作收據及稽核各僅一筆；保留 Project #11 Approved。不要因舊版 API 還能啟動就假定新收據流程可用。

## 2026-10-17 — 正式 API 持久化冪等驗證通過

本機新版容器啟動後，`scripts/test-demo-history-idempotency.ps1` 在 DEMO #8 成功執行：Publish 使用同一 Idempotency-Key 重送成功，Reopen 使用另一相同 key 重送成功，最後 #8 回到 Draft。直接查 Supabase：history_operation_receipt 在 #8 有 2 筆（各一筆 PublishHistory/ReopenHistoryReview）；該測試期間 audit_log PublishHistoryBaseline 1 筆、ReopenHistoryReview 1 筆；#8 historical_transaction 有 2 筆停用、0 筆有效；#11 保留 Approved、7 筆有效。此驗證是正常環境同 key 重送，不代表已完成實際 Commit ACK 遺失的故障注入、長時間負載、跨瀏覽器重送。仍需要測無收據／不同 action 使用同 key、並行同 key 和例外處理、保留收據等測試。

## 2026-10-17 — 收據回查的非破壞性迴歸測試

已檢查正式 API 與前端的持久操作收據回查邏輯，新增 `scripts/test-history-receipts-readonly.ps1`，讀取前次 DEMO #8 成功交易留下的 PublishHistory/ReopenHistoryReview UUID 收據，核對操作種類與建案 ID，另外確認 #8 Draft、#11 Approved。腳本只送 GET，不修改 Supabase，不需要 Docker rebuild。**尚待使用者本機執行**。剩餘風險：尚未注入正式 API 真實網路斷線／Commit 回應遺失；前端目前在無法查到收據時保留原操作 ID 並回報結果不明，不能因此認定交易成功或失敗。既有 API header 為可選，無 key 的非 UI 客戶端仍無跨請求重放保護。
