# 2A 分支整合風險稽核（2026-11-21）

> 狀態：僅依 GitHub 比較資料稽核，**未合併、未建立 PR、未執行 build / migration / DB 操作**。

## GitHub compare 實測

- `main...feature/1d-api-integration`：`status=diverged`，feature ahead 619 commits，behind main 167 commits，共 171 個差異檔案。
- `feature/1d-api-integration...main`：`status=diverged`，main ahead feature 167 commits，behind feature 619 commits，共 41 個差異檔案。
- 共同 merge base：`b18e99dacb44476a60dd13101695e643c52ecc38`。
- 上述左右的「差異檔案」是不同方向的 compare 結果，不能直接相加當成需要手動修改的檔案數，也**不能當作已確認的 merge 衝突數**。

## 需要重點檢查的變更群組

| 群組 | feature 相對 main 的差異 | main 相對 feature 的差異 | 整合驗證 |
| --- | --- | --- | --- |
| 2A 案件與兩種偵測 API | 新增 `崴正/Controllers/AnalysisCaseController.cs`、`DetectionCategoryController.cs`、`MajorCategoryPriceController.cs` 及相關 services/models | main 不含以上 feature API；只有新工具交接 | 在 feature 中保留已成功執行的 13 案 API，確認路由/範圍後再 PR |
| Vue UI | `frontend/src/views/AnalysisCaseDetailView.vue`、`AnalysisCasesView.vue`，其他 view/router/types 多檔差異 | 無同等驗證證據 | UI 操作完整鏈須在測試庫重做 smoke test |
| DB | feature 新增 `database/20261010_add_2a_target_analysis_pipeline.sql`、`database/20261012_add_site_area_similarity.sql` 等多個 SQL | main 有實驗資料生成/ seed 工具 | 按部署庫實際 schema 與 migration history 比對，不對 55432/55433 重跑破壞性 seed 或 clean SQL |
| 文件/驗收 | feature 有 `docs/API.md`、`DATABASE.md`、`DEVELOPMENT.md` 等文檔 | main 有 `docs/AI_HANDOFF.md`、`docs/PROJECT_CLOSURE_PLAN.md`、SRC 證據工具與 README | 不以文檔較新推斷程式碼較新；合併要保留兩側有效來源證據 |
| 測試及 CI | feature 包含 `.github/workflows/2a-build.yml`、`tests/WorkbookParserTests` | main 包含 `.github/workflows/graduation-synthetic.yml`，Python 30 tests 的新工具 | 整合後分別跑 .NET、Vue、Python 和 API/E2E，並檢查 workflow 路徑正確 |

## 已成功執行的收斂驗證（使用者 Windows，未重新執行）

- Python unittest **30/30 PASS** (19.065s)，包含最新 4 個 baseline 診斷測試；過去 23/26 的結果以這次 30 項為準。
- `diagnose_baseline_low.py` 唯讀 `http://127.0.0.1:5262` 已存在的三個 baseline case/run：`{"status":"READ_ONLY_DIAGNOSTIC_NOT_ACCURACY","baselines":3,"createdRuns":0}`。
- 觀察到 Low 距下限：RC SYN-00029 鋼筋 12.504%（generator shocked 0）；SRC SYN-00030 鋼筋 10.443%、結構體總價 6.274%（shock 都為 0）；SC SYN-00026 結構體總價 45.949%（shock 4）。
- 使用者分享的 `CrossStructureReferences` 欄位全空白，只表示**沒有已辨識出跨結構來源**，還需檢查完整報告中 `unresolvedReferenceIds` 是否非空。這項診斷不足以證明模型正確或錯誤。
- Baseline 紅燈沒有正式工程正常標註，不可當 false positive，`readyForFalsePositiveRate=false`。

## 合併前的阻擋條件

1. **先凍結兩側 branch head SHA 與本機未提交檔案清單**；避免再誤用 `FETCH_HEAD` 覆寫 feature 分支已有工作。使用 `git status -sb`、`git rev-parse HEAD`、`git fetch --no-tags --refmap= origin main`、`git rev-parse FETCH_HEAD` 記錄。
2. **不要用 `git reset --hard`、`git clean -fd`、`docker compose down -v`**，也不批次重跑 seed/clean SQL；有 migration 差異時先列出部署庫 schema、備份與回滾。
3. 在獨立整合工作分支合併（不是目前已驗證 feature branch 原地硬合）並逐檔解衝突；優先保留功能已驗證的 feature API/Vue/EF code 與 main 中已驗證的 synthetic tooling；`README.md` 等共同變更要人工調和。
4. Build/驗收門檻：`dotnet build`、`npm ci && npm run build`、Python 30 項與必要的 API route smoke test、UI 手動流程；逐項記錄 PASS/FAIL/NOT RUN。涉及 DB 的操作先對備份還原的隔離庫操作。
5. 完成最小驗證後才提 PR，並由使用者確認接收；**目前禁止直接建立 619-commit 的巨型 merge PR 或把 feature 強制推至 main**。

## 下一個可操作驗證點

先做只讀本機 Git 檢查，取得：
```powershell
git status -sb
git rev-parse --abbrev-ref HEAD
git rev-parse HEAD
git diff --name-status main...HEAD
```
最後一條是本機追蹤相對差異概況；若本機 main 落後，須先用 fetch 到的正確 ref 才能比較。不得因遠端比對顯示 619 commits 就把它當成合併 619 個衝突。
