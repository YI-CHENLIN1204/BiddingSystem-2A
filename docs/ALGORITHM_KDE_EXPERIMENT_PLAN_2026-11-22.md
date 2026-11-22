# 2A 演算法對照實驗計畫：現行加權分位數 vs KDE／階層分群

日期：2026-11-22。識別：kde-isolated-comparison-plan-20261122。狀態：**只有計畫，尚未建立比較頁、執行 KDE 或產出任何勝負數據**。

## 目標
回答三個問題：（一）相同參考建案、相同權重、相同名義涵蓋率下，KDE 是否勝過現行加權 P15／P50／P85？（二）去掉時間／相似度權重是否改善？（三）使用報告第 3.4～3.7 節的平均連結階層分群＋單峰高斯 KDE 是否更好？**不可把 KDE、分群、等權、價格面積口徑一起更換後，聲稱是 KDE 單獨造成的進步。**

外部方法依據：使用者提供的《營建公司輔助系統－標單異常偵測專題書面報告》（第 3.2～3.7 節）。該文件提出關鍵字＋字元 TF-IDF 章節分類、8個條件距離、平均連結階層分群、同群等權高斯 KDE、近似最小單峰帶寬、非負條件 CDF、P5～P95、價格按總用地面積與材料數量按總樓地板面積換算。**這是報告方案，尚未證明比正式系統準確。**

## 一、隔離要求（必須先通過）

- 保留現有 Windows 正式整合環境：Vue 5184 → API 5264 → Supabase。**不要修改其程式、環境變數、資料庫內容、估算權重、Top K、任何 AnalysisRun、API 路由或既有歷史**。
- 新研究分支：research/kde-comparison-20261122；只新增研究目錄 research/2a_synthetic/kde_comparison/ 及必要的獨立靜態 UI；不得直接改 崴正/Services、frontend/src 或現有正式資料表／Migration。
- 新介面預定 http://127.0.0.1:5185，為**獨立 Vite 靜態報表頁**：只讀比較器預先產生的 JSON；沒有 /api 代理、沒有正式 DB 連線、沒有 POST/PUT/PATCH/DELETE、沒有分析資料寫入。不能只換 Port 卻接到同一個有寫入權限的後端。
- 演算法比較**離線**讀取附 SHA256、manifest 的固定合成 CSV／既有經核對 API 回應快照；若現有 cache 不足，先解決基準重現，不允許偷偷對 Supabase 寫入或臨時產生「補充訓練歷史」。
- 建立禁止網路寫入與資料庫憑證載入的測試；輸出檔案不可包含密碼、商業機密或真實工程原始資料。
- 與正式系統的隔離驗收：正式 settings、project_master、analysis_case、analysis_run 在實驗前後的唯讀摘要一致；實驗流程不可有副作用。研究介面若只是讀本機 JSON，不需要碰 Supabase。

## 二、來源與分割凍結

- 住宅 SYNTHETIC v2：300 案、210 train／45 validation／45 test；沿用 docs/HOLDOUT_EVALUATION_2026-11-22.md 的真實 manifest、hash、lineage、現有 baseline 重播與 cache。**不要重跑已完成的 144 組全域權重比較**。
- 廠房 FACTORY_20261122_V1：180 案、126 train／27 validation／27 test，RC／SC／SRC 各 42/9/9；獨立來源與 SHA，**資料可取得並驗證後才做第二批比較**。不要把住宅重新改名為廠房，也不能將 train/validation/test 混合。
- train 只作候選歷史，validation 只供合法方法參數選擇，test 用凍結方法最後評分，不得按 test 再調參。住宅 test 曾有3案用於既有診斷，不屬全新盲測，另提供排除這3案的敏感度結果。
- 每案／每估算項目最多一筆有效觀測，不把數萬決標列當成數萬個獨立案件；同用途、結構與原 2A 參考相容條件、材料範圍、單位、完整性、SRC-only 選案、outlier 排除保持一致。
- 沒有工項不等於0：NotApplicable、Missing、ExplicitZero、OutOfScope 不得混為同一種樣本。樣本不足5案則棄答並記錄原因，不產生假的分布或合理區間。
- 本批全部屬合成資料，不能稱真實企業準確率。

## 三、分組對照

| 組別 | 歷史參考 | 權重 | 區間演算法 | 說明 |
|---|---|---|---|---|
| A0 正式 Baseline | 六因子相似度、同 scope 原 Top K 8，最低5案 | 正式相似指數1 × 3年半衰期 | 加權 P15/P50/P85 | 絕不改設定 |
| A1 KDE-only（主實驗） | **完全相同 A0 的 reference IDs** | **完全相同 A0 最終權重** | 加權高斯 KDE 的 P15/P50/P85 | 唯一變數是區間方法 |
| A2 KDE-Equal | 相同 A0 reference IDs | 等權 | 高斯 KDE 同分位 | 單獨測加權效益 |
| B1 報告完整方法 | 報告8條件距離 → 平均連結階層分群 → 最近群 | 群內等權 | 報告單峰帶寬、非負CDF KDE | 另列完整管線，不歸因為純 KDE |
| B2 價格面積消融 | 固定 B1 參考群 | 固定 | 將結構體金額改用基地面積 | 單列不同價格分母影響 |

**主比較統一 70% 名義區間 P15/P85。** 另以所有演算法都採 P5/P95（90%）做敏感度。不可用對方90%涵蓋率對比正式70%並宣稱勝出。P50 為中心點預測。

現行價格以每坪樓地板金額推算，對方報告用總用地／基地面積；兩種價格基準的效果必須獨立拆開。數量先比既有鋼筋 kg、混凝土 m³及結構體總金額；新鋼骨、模板、土方可在有足夠乾淨來源及對齊作用範圍時另列，不得強行納入所有估算指標。

KDE 實作須明示高斯核加權 CDF、非負截斷、二分反解分位數、帶寬搜尋容差及單峰判定網格；極端小帶寬、所有值相同、單峰過度平滑、多峰真實群、非正數、NaN、稀疏與高度集中權重都要測試。TF-IDF 是**另一條工程章節分類研究**，不能混入本次數值區間比較；也不能以章節分類分數當材料對應正確率。

## 四、執行階段及完成門檻

### Phase 0：凍結
記錄 source manifest／SHA256／Git HEAD／輸出目錄／版本與日期；檢查 train/validation/test 清單及來源完整性；驗證 no DB / no HTTP write。不得覆蓋已有 outputs。

### Phase 1：A0 基線重現
沿用 tools/graduation-synthetic/evaluate_live_holdout.py 的可重現結構與既有快照，逐案核對正式參考建案ID、相似度、下限／中位／上限及樣本不足；重現失敗即終止，不能開始比較 KDE。

### Phase 2：A1 與 A2
固定相同歷史池與參考ID／目標面積基準，實作兩種 KDE；先在 validation 選帶寬搜尋設定並凍結，再算 test 一次；執行數學單元測試、資料洩漏測試、資料極端情境與決定性測試。

### Phase 3：B1/B2（第二優先）
實作距離對稱化、平均連結群集與最近群選取，固定群數及 tie-break；群內可用樣本低於5案即棄答。價格面積口徑另列；不得把缺樣本或未執行隱藏。

### Phase 4：圖表、測試報告、研究網站
輸出逐案CSV、分組summary CSV、comparison.json、run manifest；生成繁中可引用的 PNG 300 dpi 與 SVG；研究頁 5185 只讀 JSON。生成最終 comparison_report.md 與 comparison_report.docx，附結果、圖、限制、決策建議。

**11/25 近期交件優先順序**：先有可重現的 A0 vs A1＋至少三張真實結果圖＋簡版報告；A2／廠房分組次之；階層分群 B1/B2 不能擠掉既定正式 UI 驗收。沒有來源檔或前置 PASS 就明記阻塞，不得杜撰。

## 五、指標、公平性與是否換演算法

正式執行以[晚間計畫 v2 的凍結規則](EVENING_FULL_TEST_PLAN_2026-11-22.md)為準，包含成對樣本、棄答及採用門檻。A1 使用標準加權高斯混合分布在 x≥0 上做整體條件化，避免把每個核個別截斷後仍沿用原權重而改變目標分布。固定帶寬候選與數值容差後才跑驗證集。

- **主要指標**：同名義涵蓋率的**相對 Interval Score**（對區間寬度及漏覆蓋共同懲罰；正量 y 作相對分母），每個用途 × RC/SC/SRC × rebar/concrete/price 群獨立報告，總結以無量綱宏平均。
- **輔助指標**：P50 MAE/MAPE（不同物理單位不能直接平均MAE）；實際 coverage、平均相對區間寬度、可預測率／棄答率、獨立建案 n、ESS、最大單案權重、執行時間。組別樣本太少時標「描述性」，不聲稱顯著。
- 每一 case/scope 必須 paired 比較 A0 與 A1；paired bootstrap 以**建案**為抽樣單位，固定 seed、報不確定性；拒絕用工項列數假裝樣本量。不同適用範圍要註明被排除者，不能只報成功預測案例。
- 無可信工程正常／異常真值，不能給出 Precision、Recall、FPR。生成器注入 shock 不等於工程異常認證。
- **預先決策規則（研究門檻非自然法則）**：A1 主要 interval score 在 validation 不惡化、test 預先凍結後改善 ≥5%，同時 coverage／寬度／棄答率沒有實質惡化；按項目列明勝負及不確定性。不能 test 後挑有利材料或重設門檻。若只在單一項目勝出，僅建議另案研究材料分流，**不自動改正式系統**。
- 正式採用須新的獨立決策與回歸、可回滾部署及更可信的外部／企業案例，不可由研究頁自行切換正式演算法。

## 六、報告書要交付的圖表（有結果後產生，不得先畫假圖）

1. 圖1：每項目/結構 **相對 Interval Score** 分組長條（n及70%／90%）。
2. 圖2：**Coverage vs 相對區間寬度** 散佈／雙指標圖，加名義70%／90%參考線。
3. 圖3：鋼筋、混凝土、結構價格 **P50 MAPE** 分組圖（MAE另分單位）。
4. 圖4：**真值、P50、上下限** 的逐案區間圖（必須包括輸贏案例）。
5. 圖5：固定同一參考池的 **加權經驗 CDF vs KDE 的密度／CDF**，圖註帶寬與選案來源。
6. 圖6：**可預測獨立案件數／不足樣本率** 用途×結構×項目熱圖。
7. 圖7（選作）：每案 **paired Interval Score 差值** 條圖，明示改善與惡化。

每圖輸出 PNG 300 dpi＋SVG，中文標題、單位、n、來源／split／名義涵蓋比例與「合成資料」標記，提供可直接貼到 Word 的圖說、方法與結果分析段落；不能把沒有實測的示意圖放成研究結果。

## 七、結果資料結構與 Codex 守門

建議輸出到 research/2a_synthetic/output/kde-comparison/<run-id>/（獨立run、不覆寫）：
- manifest.json（資料 hash、研究程式 Git SHA、參數、環境、seed、分割及方法）
- predictions.csv（逐案/組別/參考IDs／真值／P15/P50/P85／90%區間／棄答／ESS）
- summary.csv、comparison.json、comparison_report.md、comparison_report.docx
- figures/*.png 與 figures/*.svg（PNG 300 dpi，真正從 predictions.csv 產出）
- 5185 的研究網站需展示同一 JSON、可篩選用途／結構／項目並提供匯出，不得有資料庫連線。

Codex 開始時必讀 AGENTS.md、.agents/skills/waizheng-2a-handoff-guardian/SKILL.md、docs/CURRENT_HANDOFF_2026-11-11.md、docs/RESEARCH_TRACEABILITY_2026-11-11.md、docs/HOLDOUT_EVALUATION_2026-11-22.md、README.md，先檢查工作樹與現有144組比較。每批程式變更依 guardian 同步 docs/HANDOFF_CHANGE_REVIEWS.jsonl 與交接。**此計畫沒有修改現行演算法、DB、正式 API、UI 或正式估算設定，也尚無任何KDE結果。**


## 外部報告演算法來源鎖定（重要）

Codex 要比較的**外部作者原法不是 A1 加權KDE**。必須先讀 [外部專題報告演算法重建規格](EXTERNAL_REPORT_ALGORITHM_SPEC_2026-11-22.md)，其中含原報告第3.2～3.7節八因子 30/15/7/11/7/5/20/5 權重、非對稱距離與對稱化、平均連結階層分群、群內等權 Gaussian KDE、臨界單峰帶寬、非負 CDF、價格用地面積/數量樓地板面積、TF-IDF與容許偏差公式、報告金標數值與尚未指定的參數。A0/A1 是公平 KDE-only 消融；B1 才是外部報告的完整**依文字重建**方案。沒有作者程式/群數等設定就不可稱「忠實重現原始實作」。本次不直接上傳原作者 DOCX 到 Public GitHub。
