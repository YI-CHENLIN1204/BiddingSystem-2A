## 2026-11-23 GitHub Actions 實測與隔離權限修正

[Run 45049455364](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45049455364) 已核實，測試提交為 `b3bceb90b2d8775ba7006ac146bed3d0ecffbc27`。本次修改從 `e0109ade56e34f225efd001022ba3745bcbde838` 接續，提交訊息為 `research: enforce isolated audit roles and reconcile acceptance evidence`。

本機以 .NET 10 合成端點載入相同隔離授權程式，建置 0 警告／0 錯誤。匿名 401、無效憑證 401、Reader 403、Supervisor 200，以及未列入政策的建案 GET 200，五項通過。Python 探測程式與啟動時的覆蓋程式通過語法檢查。實際研究 API 加 PostgreSQL 的修正後 E2E 待新 Run。

- T04 隔離 EF 換算 PASS。T07 隔離 Draft 讀取、人工映射、Publish、Reopen、重複提交與稽核核對 PASS。兩項保留原本完整驗收門檻。
- T08 隔離建案讀取 30 次，p50 19.04 ms、p95 26.33 ms。正式全流程效能仍待測。
- T09 原 Run FAIL，匿名稽核 HTTP 200。研究 runner 編譯時加入隔離授權，門檻為匿名 401／403、無效憑證 401、一般使用者 403、主管 200 且讀回 T07 稽核紀錄。修正後 Actions 結果待核實。正式角色整合仍待驗收。
- Word 渲染 job PASS，產出 8 頁 PDF 與 PNG。逐頁視覺仍待取得可開啟的頁面。渲染狀態及雜湊見[執行查核](RESEARCH_ACTIONS_RUN_VERIFICATION_2026_11_23.md)。
- 本批範圍是 `research/acceptance-ci/` 與研究交接文件。隔離 runner 使用一次性 PostgreSQL，無 Migration。下一步核實新 Run 角色矩陣並完成八頁視覺判讀。研究算法與 A0 維持既有版本。

## 2026-11-23 T04 安全修正與 T07／T08／T09 補測

基準 `cbf7cbeaae27ea3a8b754bc34cbc0f1ada2c6372`。研究 T04 安全副本通過 23/23，原版同一套為 13/23。由基準的 83 個 GitHub 原始碼 blob 乾淨重建 Parser，0 警告／0 錯誤、78 條 PASS。相同 K5 的 A0-K／B1-K 已補離線選案、群聚、帶寬、單案項與批次計時。隔離資料庫 E2E、API p50/p95 和 Word 視覺仍未執行，正式分支及資料庫未修改。證據、各項驗收狀態與正式移植風險見[本輪補測](../research/2a_synthetic/kde_comparison/t00t11-followup-20261123/README.md)。

## 2026-11-23 T00～T11 優先補測

以 `3eef29061e8ab46a12da62c2a4b390f5031bfdba` 為研究基準。T04 服務 fixture 13 案中 10 通過、3 失敗（零或負係數）。T08 K5 共同可估的 30 案項預測計時完成。T07／T09 解析器既有二進位 78 條 PASS，Python 18/18。隔離 Docker daemon 拒絕存取，未執行資料庫寫入流程或角色測試。Word 通過封裝與嵌圖核對，視覺驗收仍未執行。完整狀態、重現命令及缺少的資料欄位見[補測紀錄](../research/2a_synthetic/kde_comparison/t00t11-acceptance-20261123/README.md)。下輪需先取得隔離 PostgreSQL 或可操作的 Word 渲染器，再補資料與人工驗收。

# Codex：從「第一階段報告交付」推進到 T00～T11 全面實驗驗收

研究branch：`research/kde-comparison-20261122`。**先fetch最新版HEAD，檢查 git status，讀 AGENTS/Skill 與最新交接，再開始；不可覆寫另一台電腦未提交檔。**

1. 先讀 `docs/T00_T11_COMPLETION_GATE_2026_11_23.md` 及 `research/2a_synthetic/kde_comparison/LIVE_TRAIN_AUDIT_2026_11_23_ZH.md`，用 `research/2a_synthetic/kde_comparison/sql/live_train_integrity_readonly_2026_11_23.sql` 於正式Supabase **只讀 SELECT**。目前DB現況：126件 synthetic factory 均Approved，16,786筆Auto且material一致，67條TRAIN字典，11類單位中0類允許換算。這是**實際已測的資料完整性**，不要再重做名稱已見字典的100%基準，亦不可聲稱真實辨識100%。舊main交接稱Draft已過時。
2. 第一批**不依賴使用者新資料**直接做：隔離 PostgreSQL 功能E2E（T07）、T09權限與回歸／資料庫角色測試、T04合法與非法換算及工料邊界、T08同工作量A0/B1效能與API p50/p95（若隔離環境可用）、Word用LibreOffice headless PDF逐頁驗收。每項先檢查可用工具；不能執行就標NOT RUN並記錄環境錯誤，不要造數字。
3. 第二批需新資料：住宅原始CSV＋manifest做住宅A0及A1（T00/T05）；跨來源未見真實標單和人工核定材料真值做正式67詞字典及TF-IDF（T01/T02/T10）；有實際決標／發布／入庫日期及標準答案的獨立建案做確認性E1/E2/E3（T05/T11）；工程師異常標籤T06與真人交叉計時T03。**沒有外部資料與人員，不准把這些步驟標完成**；先提交可匿名化的欄位模板和驗收規則。
4. 不必再對同一組已看過的26件廠房test調整演算法換勝負；新研究要先凍結規格、K5/8、70%主、90%輔、train/val/test/日期以及報告判準。原版B1與B1-K研究變體另列，不能混稱。
5. 每組修改打包提交一次，更新交接審查記錄並運行hand-off gate。最後輸出T00～T11每項 `PASS/PARTIAL/FAIL/NOT RUN`、具體數據、驗收證據SHA與**仍需使用者提供的最少資料清單**。正式 `feature/1d-api-integration`、5184/5264、Supabase全程不做寫入、不更換A0、不修改正式參數。
