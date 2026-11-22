# Codex：從「第一階段報告交付」推進到 T00～T11 全面實驗驗收

研究branch：`research/kde-comparison-20261122`。**先fetch最新版HEAD，檢查 git status，讀 AGENTS/Skill 與最新交接，再開始；不可覆寫另一台電腦未提交檔。**

1. 先讀 `docs/T00_T11_COMPLETION_GATE_2026_11_23.md` 及 `research/2a_synthetic/kde_comparison/LIVE_TRAIN_AUDIT_2026_11_23_ZH.md`，用 `research/2a_synthetic/kde_comparison/sql/live_train_integrity_readonly_2026_11_23.sql` 於正式Supabase **只讀 SELECT**。目前DB現況：126件 synthetic factory 均Approved，16,786筆Auto且material一致，67條TRAIN字典，11類單位中0類允許換算。這是**實際已測的資料完整性**，不要再重做名稱已見字典的100%基準，亦不可聲稱真實辨識100%。舊main交接稱Draft已過時。
2. 第一批**不依賴使用者新資料**直接做：隔離 PostgreSQL 功能E2E（T07）、T09權限與回歸／資料庫角色測試、T04合法與非法換算及工料邊界、T08同工作量A0/B1效能與API p50/p95（若隔離環境可用）、Word用LibreOffice headless PDF逐頁驗收。每項先檢查可用工具；不能執行就標NOT RUN並記錄環境錯誤，不要造數字。
3. 第二批需新資料：住宅原始CSV＋manifest做住宅A0及A1（T00/T05）；跨來源未見真實標單和人工核定材料真值做正式67詞字典及TF-IDF（T01/T02/T10）；有實際決標／發布／入庫日期及標準答案的獨立建案做確認性E1/E2/E3（T05/T11）；工程師異常標籤T06與真人交叉計時T03。**沒有外部資料與人員，不准把這些步驟標完成**；先提交可匿名化的欄位模板和驗收規則。
4. 不必再對同一組已看過的26件廠房test調整演算法換勝負；新研究要先凍結規格、K5/8、70%主、90%輔、train/val/test/日期以及報告判準。原版B1與B1-K研究變體另列，不能混稱。
5. 每組修改打包提交一次，更新交接審查記錄並運行hand-off gate。最後輸出T00～T11每項 `PASS/PARTIAL/FAIL/NOT RUN`、具體數據、驗收證據SHA與**仍需使用者提供的最少資料清單**。正式 `feature/1d-api-integration`、5184/5264、Supabase全程不做寫入、不更換A0、不修改正式參數。
