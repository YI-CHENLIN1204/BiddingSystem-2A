## 2026-11-23 正式資料庫只讀來源查核（可重現的分類結果）

從唯一已連接 Supabase 專案以 SELECT 聚合 `public.project_master`（不含 deleted）並依 `project_type, history_status, source_file_name` 分組：
- 住宅：210 筆 Approved；來源欄 `SYNTHETIC_V2_PROJECTS.csv`。
- 廠房：126 筆 Approved；來源欄 `FACTORY_20261122_V1_TRAIN`。
- 全部 336 筆是**合成來源**。不能因 `Approved` 就宣稱存在真實最終決標、工程師真值或獨立住宅盲測。沒有更動正式 DB。
- 這是本輪活資料庫來源標記的唯讀查核，不等同來源 CSV 與 manifest 的原始位元組凍結，更不能替代合法獨立工程資料。

## 2026-11-23 T00～T11 補測 Run #4 完成（研究隔離，原計畫仍未結案）

新 [GitHub Actions Run #45115813539](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45115813539)，測試提交 `cf6465ef21e65456765b182ee1d7d902715f10ac`，三個 Job 全部 SUCCESS。原始 `isolated-api` Artifact [#14791179321](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45115813539/artifacts/14791179321) 的 **5/5 SHA256SUMS PASS**、87 筆 HTTP JSONL（85 筆 `passed=true`、0 `passed=false`、2 統計摘要）已實際下載核對。T07 **真實 multipart 合成 Excel 上傳路徑** PASS：雙工作表錯選 400、正確選表上傳 200、兩筆量價工項、比價工作表排除、Draft 不產生歷史交易、重複檔案 SHA 409。T08 新增匯入建案 `/2a-summary` HTTP n=30，p50 **28.33ms**、p95 **34.28ms**；舊 `/api/projects/{id}` 讀取 n=30，p50 **15.69ms**、p95 **19.21ms**。不是完整 KDE 估算 API 和獨立工作負載評估。T04/T09 及 Word 維持前 Run #45057125296 的 PASS。

另經 Supabase 唯讀來源分組，正式資料庫現有 **210 筆住宅 Approved** 皆標記 `SYNTHETIC_V2_PROJECTS.csv`、**126 筆廠房 Approved** 皆標記 `FACTORY_20261122_V1_TRAIN`。Approved 是狀態欄而非真實性證據；這 336 案 **全部是合成來源，不得作為外部真實住宅／廠房盲測**。本輪 Google Drive/Libraries 也沒有取得具獨立最終決標量價、合法授權與人工核定真值的可用原始包，詳見 [資料適格性](T00_T11_SOURCE_ELIGIBILITY_2026_11_23.md) 及 [Run #4 原始驗證](T07_T08_RESEARCH_RUN4_2026_11_23.md)。

**原 T00～T11 仍非全部 PASS**：真實跨來源標單、住宅 frozen manifest、工程師真值、真人 AB/BA、作者 B1 源碼或明確重建聲明、完整版效能驗證仍是阻擋條件；不能把已成功 CI 變造為獨立外部研究驗收。所有修改僅研究程式/交接；**正式 2A、A0、Supabase 未修改**。下一輪請集中取得這些確實缺少的外部輸入，避免重跑已成功的合成 CI。

# T00～T11 獨立資料來源適格性檢查｜2026-11-23

## 目的與保密

為嘗試完成原始 12 項研究驗收，已唯讀檢查研究分支、已連接的檔案庫與 Google Drive 中可能的工程標單。Repository 目前為 Public，**沒有**複製、提交或對外發布任何合作方、企業或雲端硬碟原始檔。資料檢查結果只列結構級事實，不列來源檔全文、個人資訊、價格或客戶識別資訊。此盤點只代表本輪可存取的來源，不能聲稱所有可能的資料儲存位置均不存在。

| 候選 | 唯讀判定 | 可否作為正式獨立標單盲測 |
|---|---|---|
| 雲端工程明細試算表 | 可開啟；主要是工程項目、單位與數量，缺獨立量價真值所需單價欄 | **否**。可另評估非價格解析，但不能冒充最終決標 |
| 同名企業的專案試算表 | 內容實為團隊任務與進度；不是工程標單 | **否** |
| `.xls` 檔案連結 | 下載結果實為存取頁 HTML，不是可解析的 Excel 位元組 | **否**，需由資料保管人直接提供可讀取檔案 |
| GitHub 既有廠房合成 TRAIN／CSV | 具研究重現性與來源快照，但並非獨立真實工程 | **否**，僅可用於合成探索及回歸 |

## 尚未取得的必要輸入

1. 住宅原始來源 CSV、manifest、快照 SHA-256、估算當時真正可得的資料日期及盲測案例分割（T00、T05）。
2. 擁有資料保管人**明確使用授權**的去識別住宅及廠房**完整最終決標標單**；每筆需可核對工料名稱、規格、單位、數量、單價與來源時點；依 train/validation/blind 切分，且同來源不能洩漏跨組（T00、T01、T02、T05、T10、T11）。
3. 工程師在不知道方法輸出的情況下提供材料／規格核定及正常／異常／不確定的專業標註，必要時第二人覆核歧義（T01、T06、T10）。
4. 真人以難度配對的授權標單做 AB／BA 交叉計時、登錄修正／覆核時間及品質（T03、T10）。
5. 完整 B1 作者原始碼及參數（若以「與原法逐行一致」為 T11 驗收要求；若無則只可稱獨立重建）。

## 下一步資料處理原則

- 最小欄位及空白模板以 `research/2a_synthetic/kde_comparison/input_templates/` 與 `research/acceptance-ci/README.md` 為準，不要發明假真值。
- 授權企業資料使用私密入口或受控儲存；先做權限、去識別化及洩漏查核，**不得 commit 到公開 Repository**。所有研究結果僅輸出聚合指標與非識別證據。
- 既有 GitHub Actions Run #45057125296 是隔離合成 API、T04/T07/T09 及 Word 驗收通過，不代表盲測與真人實驗結案。
- 本輪新增 T07 上傳與 T08 摘要效能測試採**完全自行生成的合成 Excel**，尚待新 CI Run 真實執行，與上述外部資料缺口分開列示。
