# SRC 歷史資料庫與安全分支整合 — 2026-11-21

> 稽核完成的是遠端 GitHub 版本與既有 Windows logs；**尚未操作使用者本機 Git 工作樹／資料庫或執行新編譯**。

## 關鍵分辨：SRC 原先有兩個不同問題

1. **歷史樣本是否存在**：舊合作方 Supabase 不能憑歷史討論假設已具 SRC 核准標單。新隔離畢業專題 PostgreSQL `127.0.0.1:55433/bidding2a_graduation_src` 已由 Windows train-only seed 與 E2E 證據證明可執行 SRC 基準標單，但須透過本次唯讀 SQL **重新確認結構型式、歷史案件數、交易列數、核准狀態及外鍵鏈結**。此庫不是正式崴正資料庫，亦不等於合作方真實 SRC 資料已匯入。
2. **SRC 引用錯誤結構參考樣本的演算法問題**：遠端 feature `86c068b` 修改 `崴正/Services/ProjectSimilarityService.cs`，對 SRC 目標強制只選 SRC 歷史 cohort；數量不足須回不足而不能借 RC/SC 出紅燈。使用者本機執行之舊 `97f7cfd` **尚未包含此修正**，因此先前 13 案 E2E 及 3 baseline 正常/紅燈不可視為已驗證新的 SRC 嚴格候選政策。不能把它當成單純 DB Migration 問題。

## 本機風險（使用者 2026-11-21 輸出）

- `feature/1d-api-integration` HEAD `97f7cfdcd04cd6d3b2eb489bf3512ed24323535f`，remote ahead 13 commits / 12 個差異檔。remote 頭 `86c068b`。
- 本機 `compose.fault-test.yaml` 有隔離故障測試 DB 初始 schema volume、bridge network 修改，應保留。由於它使 `isolated` 加上 `api_access` 網路，安全/故障隔離模型應獨立驗證，不能直接推正式環境。
- 本機 `frontend/src/components/WorkbookSheetPicker.vue` 的 diff 在 button 處插入了不完整的 `<button>` 和 `<span>`，無開始標籤內文/對應關閉結構，**高度疑似 Vue template 語法錯誤、尚待 npm build**。遠端 `7e27271` 已有「0 筆工作表允許人工選取與警示」的完整版本。先隔離原檔、在獨立新 worktree 用遠端版建置，避免硬覆蓋本機編輯。
- 未追蹤 `tools/`、`tests/fault-fixture/` 與 `graduation-*` 均不可清理；會包含已驗證腳本/測試報告及可能的敏感測試內容。禁止 `git add .` 無選擇性提交。

## 新增唯讀資料庫查核

`tools/graduation-synthetic/check_src_db_readonly.sql`：`BEGIN TRANSACTION READ ONLY`，僅讀 `current_database/current_user`、依結構的 `project_master` history status/有效面積/案件數、`historical_transaction` 的結構/案件/交易列數與缺標單鏈結、SRC 已核准非刪除案數。沒有修改或重新匯入。**SQL 尚未在使用者 Windows 執行**；欄位/表名依目前 C# 模型與既有 DB 文件，必須以實際 schema 驗證。

執行前先在本機確認 55433 是否仍是隔離 SRC 庫，對照其 Compose `tools/graduation-synthetic/compose.graduation-src-db.yaml`；在配有 `GRADUATION_SRC_DB_PASSWORD` 的 PowerShell，可以在老工作目錄以管線將唯讀 SQL 送到該隔離服務：

```powershell
git fetch --no-tags --refmap= origin main
git show FETCH_HEAD:tools/graduation-synthetic/check_src_db_readonly.sql |
  docker compose -f tools/graduation-synthetic/compose.graduation-src-db.yaml exec -T graduation-src-postgres psql -v ON_ERROR_STOP=1 -U graduation -d bidding2a_graduation_src
```

禁止誤送到正式 DB 或 55432。若 SQL 出現不存在的欄位/表，停止並核實當前 migration/schema，**不得**臨時補建表或執行資料修正。

## 安全整合的首個步驟

遠端 13 commits 僅位於最新 feature；其中 **`WorkbookSheetPicker.vue` 同時被本機修改**。而新 `tools/` 在舊 worktree 為 untracked，遠端 feature 已含 `SimilarityContract/`；直接 fast-forward/pull 有覆蓋風險。

先在不動原 Windows 工作目錄的情況下建立**第二個 worktree**，以剛 fetch 的 feature commit 作基礎。必須在 PowerShell 先取最新遠端 feature，因 FETCH_HEAD 在 fetch main 後會改變：

```powershell
git fetch --no-tags --refmap= origin feature/1d-api-integration
git worktree add -b integration/src-closure ../BiddingSystem-2A-integration FETCH_HEAD
```

上述操作會建立新工作目錄與本地新 branch，**不修改原工作樹**，但需新檢出 tracked 檔案；先確定目標目錄與同名 branch 不存在。新 worktree 的遠端版 Vue/後端可以單獨執行 `npm ci && npm run build`、`dotnet build` 與 `dotnet run -c Release --project tools/graduation-synthetic/SimilarityContract/SimilarityContract.csproj`。此時不修改 55433，不執行相同 13 件 --execute。

要整合 main 工具與 feature 仍需獨立規劃 cherry-pick/小批次合併與 conflict review，**不能假定只靠建立 worktree 就完成 merge**。更不能在沒有備份、驗證 migration 的情況下把所有 SQL 批量施於現有 PostgreSQL。

## 下一驗收

1. 取得 SRC 隔離庫的唯讀結構與歷史樣本計數；若 SRC 0 件且程式仍提供 SRC 紅燈，視為阻擋交付。
2. 在新 worktree 建置遠端最新 feature (.NET/Vue/SimilarityContract)，記錄實際 PASS/FAIL。
3. 驗證 **SRC 僅引用 SRC**（現存的歷史交易與 ProjectMaster 型別）和不足樣本不冒出紅燈；原 E2E 結果需要區分前後不同程式碼版本，不應重複將舊報告標 PASS。
4. 審視原本 0 筆工作表人工勾選操作，確認前端修正與 parser 契約一致。
5. 完成後，再決定對 main 做最小整合 PR，並確保保留完整交接與既有 SRC 研究報告。
