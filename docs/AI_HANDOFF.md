## 2026-11-22 實驗證據索引

完成區間、擾動及字典分層的資料對帳。[報告證據索引](../research/2a_synthetic/kde_comparison/FACTORY_REPORT_EVIDENCE_INDEX_ZH.md)

## 2026-11-22 T05 指標與原計畫覆核

補分層 P50 MAPE、有效樣本數與棄答。正式 API 等價驗證待完成。[原計畫進度](../research/2a_synthetic/kde_comparison/FACTORY_TEST_STATUS_ZH.md)

## 2026-11-22 量價區間壓力測試

固定 70% 區間完成 780 筆擾動結果。隔離 API 測試仍受 Docker 存取權限及本機無 PostgreSQL 工具阻擋。[驗證紀錄](../research/2a_synthetic/kde_comparison/FACTORY_INTERVAL_STRESS_ZH.md)

## 2026-11-22 A0 選案核對

固定候選池新增正式 ProjectSimilarityService.ScoreCandidates 核對。159 案項共 1,272 個參考 ID、順位與權重相符（權重絕對誤差 ≤ 1e-12），477 個分位對照再度通過。候選資格由離線快照提供，排序與權重公式由驗證程式重播；SQL、正式 API 與入庫資料範圍仍待隔離環境驗證。

## 2026-11-22 A0 正式分位核對

159 個案項、477 個 C# 分位對照通過，正式 API 候選池仍待驗證。詳見研究分支 A0_QUANTILE_CONTRACT_ZH.md。

## 2026-11-22 廠房時間順序敏感度

只採日期早於目標的訓練案，三項各可估 20/26，70% 主區間改善 3.1%，未達採用門檻。[驗證紀錄](../research/2a_synthetic/kde_comparison/FACTORY_TEMPORAL_SENSITIVITY_ZH.md)

## 2026-11-22 合成廠房第一版結案

三份 CSV 重跑 SHA256 相符，維持探索性結論。第二版先核對正式 A0，再以獨立資料做時間切分。[結案紀錄](../research/2a_synthetic/kde_comparison/FACTORY_EXPERIMENT_V1_CLOSEOUT_ZH.md)

## 2026-11-22 廠房比較補充紀錄

[比較紀錄](../research/2a_synthetic/kde_comparison/FACTORY_COMPARISON_LOG_ZH.md) 收錄 A0／B1 配對、A0／A1 70% 主分析及 90% 敏感度、三份逐案 CSV 與重跑方式。A0 仍待 API 逐案驗證。

## 2026-11-22 廠房研究結果與重現

事前選值提交 6a46be8，安全規則提交 527cb72，C1 提交 915a3ad。結果提交以 `research: publish factory evening results and report` 識別。說明、命令及限制見 [廠房實驗報告](../research/2a_synthetic/kde_comparison/FACTORY_EVENING_RESULTS_ZH.md)。B1 逐案 CSV 兩次 SHA256 同為 6ae62704923abf6351717acff8c3c7d190748e3341a14d59956da58c7c21e107，直接平均連結比對 126 案分組相符。數量各 9/26 案可估，價格 26/26；重建假設不代表作者實作。材料碼匹配 3386/3386，保守規則放行 207/3386；配對壓力 182/182 轉人工；章節評估僅涵蓋已見名稱。Python 18+21 與既有 Release 解析器 79 項通過。住宅缺原始檔，A0/A1 未評分；下一步是另以獨立可追溯原始資料執行 A0 基線驗證。無生產變更或演算法替換。

## 2026-11-22 外部報告原文核對及公式測試

`external-report-golden-probes-20261122`。比對原 DOCX 第 3.2～3.7 節與規格。A1 是保留 A0 參考案、最終權重及面積的加權 KDE 消融；B1 是依報告文字重建八特徵距離、平均連結分群、最近群及等權 KDE。B1 價格以基地面積，數量以總樓地板面積。

執行 `python research/2a_synthetic/kde_comparison/probe_external_report.py`。得距離 33.95、標準化帶寬 0.24248171、混凝土 349.5368–650.4632 m³、價格 17,476,839.94–32,523,160.06 元，等值樣本棄答。現有 WorkbookParserTests 通過。以上僅公式金標，完整分群、A0/A1/B1 比分未執行；住宅 A0 獨立重播仍缺原始 CSV。未確認的作者參數以 null 保留，數值探針已選值獨立記錄。研究未寫正式環境。原作者實作須以其程式與設定驗證。

## 2026-11-22 外部作者演算法忠實度補充（非實驗結果）

使用者確認「Codex光讀今晚測試計畫是否會理解另一人的演算法？」；核對原始上傳《專題書面報告 (2).docx》3.2～3.7節後，發現既有文件只列高階方法。新增 `docs/EXTERNAL_REPORT_ALGORITHM_SPEC_2026-11-22.md` 轉述可確認公式與原作者未指定的參數。區分A1加權KDE-only（我方消融）與B1八因子分群+群內等權單峰KDE（依報告重建）及C1 TF-IDF工程章節分類，提供D=33.95與混凝土349.54～650.46金標，揭露群數、帶寬網格、分類字典等尚未完整定義。原DOCX仍保留於使用者對話附件，**未上傳 Public repo**，無原作者碼不可聲稱實際系統等價。研究結果、圖表、正式系統均未由此文件更新產生。

## 2026-11-22 字典實驗報告證據

`factory-dictionary-report-evidence-20261122`。已加入廠房精確名稱對應重播程式與繁中報告段落，逐列及彙總結果可由固定來源重建。重跑 6 規模 × 2 分割，數值與前次一致。下步以彙總 CSV 產圖並查核陌生名稱、安全負例；展示頁尚未建立。

## 2026-11-22 廠房字典規模基準

`factory-exact-dictionary-growth-20261122`。廠房精確名稱字典學習曲線已完成。只使用 train，按名稱出現的獨立建案數降序，依標準化名稱及材料碼破同分。validation 3644 列，test 排除 #16 後 3386 列。字典 0／10／30／50／100／193 條，validation 正確命中 0／270／810／1350／2493／3644，test 正確命中 0／260／780／1300／2384／3386，各組錯配 0。名稱皆在 train 出現，未測安全放行或陌生名稱。

下一步凍結配對改寫與安全負例，及定位住宅原 CSV。圖表從結果檔重現，展示介面可另讀同一份資料。本輪未改正式系統。

## 2026-11-22 晚間實驗開始

`evening-experiment-preflight-20261122`：廠房來源 SHA 與分割通過，精確名稱基準完成，已見名稱覆蓋飽和。住宅 237 快照識別確認，原 CSV 待定位，A0 重播與 KDE 尚未執行。詳見晚間計畫末節。

## 2026-11-22 晚間實驗執行規則 v2

`evening-experiment-protocol-v2-20261122` 以晚間計畫為準，凍結字典詞排序、案例排除、工時公式、KDE 採用門檻及棄答規則。建立隔離執行 manifest 後先驗 A0。此提交只修訂文件，尚未評分 KDE 或字典。

## 2026-11-22 案件材料規格試算

提交識別 `case-specification-estimate-20261122`，基線 `bb15708`。案件數量結果可展開既有養護劑／脫模劑規格試算，輸入產品與實際施作面積；鋼筋混凝土納入統計改名以免與鋼骨／模板／土方歷史概算混淆。#16 快照無養護劑或脫模劑明細；本機能力 API 兩項均無正式材料或可比歷史。防水仍欠缺施作面積、材料系統及層數／厚度。

vue-tsc、Vite build 通過；待瀏覽器驗收。

## 2026-11-22 統一由參數設定管理 Top K

提交識別 `centralize-estimation-topk-20261122`，基線 `8d4a5d1`。移除案件分析、獨立價格估算及大宗估算畫面的 Top K 輸入，前端不再傳每次覆寫值，統一沿用既有參數設定。種子與 5264 實際初始設定為 Top K 8、最低樣本 5。後端介面仍相容原有呼叫者。

vue-tsc、Vite build 通過；5264 GET /api/settings 已確認目前為 8／5；瀏覽器畫面待驗。 後續用 5184 確認設定說明及案件估算結果。

## 2026-11-22 待偵錯工項字典與大宗實際用量

提交識別 `case-bulk-dictionary-20261122`，基線 `b34b3eb`。一鍵數量估算改從唯讀版本端點使用既有工項字典，依標準材料、工程大項與單位彙總作業區明細，把實際總量傳給歷史區間 API 並顯示於同一張表。無效數量或不相容單位顯示原因。無資料搬遷。

WorkbookParserTests、vue-tsc、git diff --check 通過；5264 仍需更新後驗證 #16 live 結果。

## 2026-11-22 合併數量估算結果表

提交識別 `unified-quantity-results-20261122`，基線 `a92f0b5`。待偵錯案件數量估算的鋼筋、混凝土與其他材料共用同一張表，其他材料的 P15／P50／P85 各自對欄並顯示原因。工項納入範圍移至三個頁籤的內容下方。價格結果照原流程。

vue-tsc、Vite build、git diff --check 通過；瀏覽器待驗。 下一步核對 #16 的畫面與標準材料對應。

## 2026-11-22 廠房歷史基準發布完成與待偵錯驗證

提交識別 `factory-history-publish-20261122`，基線 `dc9e941`。

以 train 名單精確核對 126 案，#256 由 106 筆未分類降至 0，先發布並讀回鋼筋 3／混凝土 3 筆；其餘 125 逐案查 publishReady 後經 API 成功發布，覆核 RC／SC／SRC 各 42 Approved、16,786 決標明細、756 有效歷史交易。獨立測試 #16 V1 未進歷史庫。數量 Run #40 及結構體價格 Run #41 均正常，各項取 8 案。鋼骨、模板、土方的標準材料名稱在 #16 V1 與 options 完全一致的命中均為 0，尚未完成快照工項對應與瀏覽器驗收。

## 2026-11-22 廠房工程大項補齊

提交識別 `factory-major-categories-20261122`，基線 `b7d0920`。

新增 11 個來源工程大項的精確分類，訓練集 21 種大項、16,786 筆全部涵蓋。既有數量及價格模型白名單不變。WorkbookParserTests 通過。待以同一 Supabase 啟動新版 5264 後，重查 #256 及全批並以發布 API 納入合格 train 案，目前未執行發布。

## 2026-11-22 合成廠房獨立測試案件已建立

提交識別 `factory-holdout-case16-20261122`，基線 `1164773`。

使用本機 5264 原有上傳 API，從獨立測試集 FAC-20261122-SC-0052 建立待偵錯案件 #16／V1，讀回 164 筆。第一次未選工作表遭拒，按真值檔指定明細工作表後成功。歷史廠房尚未發布，原始訓練資料含消防、給排水、電氣等現行分類政策未承認的工程大項，#256 仍有 106 筆未辨識。下一步擴充／核對來源工程分類並重新跑發布前檢查，再以既有發布 API 處理符合條件的 train 案。

## 2026-11-22 案件分析一鍵列出工項

提交識別 `case-analysis-one-click-20261122`，基線 `b17e5e4`。案件明細改為兩個按鈕。數量估算列出正式數量偵錯及可按標準名稱確認的歷史概算，缺產品／施工面積者列原因。價格異常執行本版本全部受支援的大項，材料單價項目列出基準缺口。新增歷史數量概算沿用唯讀 API，既有正式 Run 維持保存。

vue-tsc、Vite runner 75 modules、git diff --check 通過；瀏覽器與實際估算待驗。 下一步在 5184 用已知標準工項名稱的案件驗證兩個按鈕。

## 2026-11-22 工項估算整合回原分析區

提交識別 `inline-quantity-options-20261122`，基線 `cc46615`。移除獨立材料能力表與大宗估算卡。原案件分析及試算區以分析項目切換鋼骨／模板／土方歷史數量與養護劑／脫模劑產品規格用量；預設維持既有流程。價格異常沿用大項價格 API，大宗材料單價仍缺一致計價口徑。

vue-tsc、Vite runner（75 modules）、git diff --check 通過；瀏覽器待驗。 下一步用 5184 核對各選項與既有偵錯流程。

## 2026-11-22 廠房發布前檢查與歷史基準標籤

提交識別 `history-baseline-label-20261122`，基線 `a8047e4`。建案管理篩選與標籤、建案明細、決標回寫統一顯示「已納入歷史基準」。前端 vue-tsc 通過，瀏覽器待確認。

本機 5264 API 確認合成廠房 RC、SC、SRC 各 42 案，均為 Draft；126 案發布檢查全數成功回應，0 案可發布、126 案有未辨識工程大項。#256 發布前檢查有 106 筆未辨識工程大項。下一步對照 train 原始分類，走既有審核／發布流程，再用未匯入的 test 標單新增待偵錯案件。這次沒有寫入資料庫。

## 2026-11-22 估算入口與建案條件搜尋

- 提交識別 `case-workspace-search-20261122`，基線 `d7f7eb3e`。
- 估算移入「待偵錯案件 → 工程與材料估算」頁籤，側欄與總覽移除獨立入口，舊 /estimation 導向新頁籤。兩份列表共用關鍵字、用途、結構、縣市及清除；案件另有來源，建案管理另有歷史基準狀態。案件列表移除四張計數卡與重複的暫定流程／歷史基準摘要。
- 程式位於 `frontend/src/views/`、`frontend/src/components/ProjectSearch.vue`、`frontend/src/services/projectSearch.ts`、`frontend/src/App.vue` 與 `frontend/src/router/index.ts`。沿用清單 API，於前端套用組合條件。
- vue-tsc 通過；Vite runner 建置通過（75 modules）；9 項條件比對檢查通過。預設 Vite 設定載入受 Windows 沙箱限制。真實瀏覽器、5184 與雲端資料操作待驗。
- 下一步在 5184 驗證合成廠房的建案篩選、舊估算網址轉導、案件內估算及頁籤試算。

前次資料與研究紀錄見 [主交接](CURRENT_HANDOFF_2026-11-11.md)。

## 2026-11-22 KDE 獨立研究與報告圖表規格（只立項，尚未跑）

- 使用者明確要求：正式 5184→5264→Supabase 不動，研究另開 5185 靜態比較頁，使用固定離線合成 CSV/JSON、無 DB/正式 API 寫入，不能改正式 Top K、P15/P50/P85、AnalysisRun 或歷史。
- 研究計畫：docs/ALGORITHM_KDE_EXPERIMENT_PLAN_2026-11-22.md；測試報告模板：docs/ALGORITHM_KDE_EXPERIMENT_REPORT_TEMPLATE_2026-11-22.md；識別 kde-isolated-comparison-plan-20261122。A0 既有加權分位、A1 同 reference IDs/權重只換 KDE、A2 等權 KDE、B1 對方階層分群＋KDE，另列價格面積口徑。70%同名義區間為主，90%另比較。
- 住宅既有210/45/45；廠房126/27/27待離線來源驗證。不得將 test 回流調參、不得以合成模擬宣稱企業準確度。實驗後必須輸出真實逐案資料、中文報告 docx/md、PNG 300 dpi/SVG 圖表與可重現 manifest，先A0/A1再後續B1。**目前未執行KDE、未產生數字與圖表、未開5185、未更動正式系統。**


## 2026-11-22 晚間全量測試研究計畫（尚未執行）

使用者指示今晚以 Codex 優先完成演算法公平比較、材料字典 auto precision/false-auto/覆核效益、字典規模學習曲線、人工時間保守/中性/樂觀情境（無真人計時不得宣稱實測）、單位與工項適用性、異常壓力、Excel/隔離副本回寫、效能、安全回歸、報告圖表。新增 `docs/EVENING_FULL_TEST_PLAN_2026-11-22.md` 與 `docs/CODEX_EVENING_TEST_RUNBOOK_2026-11-22.md`，識別 `2a-evening-test-matrix-20261122`。

本輪已連接Supabase唯讀查到時點數字：336 Approved合成建案、67字典、104材料、11單位、16案件、47 Run、117952決標列；故先前字典0已過時。現有字典來源與跨測試洩漏資格待驗；不得宣稱自動減少工時已有實證。研究5185僅靜態JSON、正式5184/5264/Supabase不改；**目前尚無任何新實驗結果、研究頁、Word圖表或工時實測**。先做P0並誠實標FAIL/NOT RUN，不把合成工程指標當真實企業準確度。


## 2026-11-22 補充驗證

T06 已完成四組同案量價混合擾動。T08 已完成 Python KDE 批次微基準，完整解析效能及正式 API 尚待驗證。

## 2026-11-22 Excel 解析效能

T08 已補三份完整測試標單的 C# 解析量測，163–167 列，每份 20 輪。大標單與完整 API 負載尚待驗證。詳見 `FACTORY_PARSER_BENCHMARK_ZH.md`。

## 2026-11-22 T08 大規模解析

1,000／10,000／50,000 列各 20 次解析完成，列數一致。詳見 `FACTORY_PARSER_BENCHMARK_ZH.md`。完整 API 負載仍待隔離環境。

## 2026-11-22 B1／B2 金額分母

已補 26 案配對、結構分層 bootstrap。詳見 `FACTORY_DENOMINATOR_BOOTSTRAP_ZH.md`。

## 2026-11-22 T10 未見名稱補測

781 列合成留出測試，Top-1 262、Top-3 553。詳見 `FACTORY_C1_UNSEEN_ZH.md`。

## T05 等權基準

70%／90% 各 78 筆已完成。見 `FACTORY_EQUAL_WEIGHT_ZH.md`。

## T04 單位驗證

正式換算服務的 18 項合成主檔案例全通過。詳見 `FACTORY_UNIT_CONTRACT_ZH.md`。

## T08 字典規模補測

已完成 12 組查找測試，500 詞為合成壓力配置，正式 67 詞快照仍待驗證。詳見 `FACTORY_DICTIONARY_SCALE_ZH.md`。

## T10 覆核負擔補測

已記錄前三候選順位及另查比例。詳見 `FACTORY_C1_UNSEEN_ZH.md`。

## T02 訓練字典 67 詞補測

合成測試正確 1742／3386，正式字典來源待核。詳見 `FACTORY_DICTIONARY_REPORT_ZH.md`。

## T01／T02 建案宏平均補測

每案先算正確列／明細列，再等權平均。驗證 27 案、測試 26 案，按建案重抽 2000 次，種子 20261122。測試 50／67／100 詞為 39.86%／53.41%／72.52%，67 詞的 95% 區間為 49.49–57.30%。只涵蓋凍結的合成已見名稱。從研究目錄執行 `python factory_dictionary_macro.py <mapping_per_item.csv> <output.csv>`。機讀結果位於 `evening_results/dictionary_macro.csv`。

## T02 字典擴充同案配對

50→67 與 67→100 詞在 27 驗證案與 26 測試案均逐案改善。測試宏平均分別 +13.55 與 +19.11 個百分點，案級配對 bootstrap 95% 區間分別為 [12.56,14.54]、[17.81,20.48] 個百分點（2,000 次，種子 20261122）。沿用同一凍結資料及巢狀精確名稱字典，結果只適用於已見名稱；未納入正式字典。既有 `factory_dictionary_macro.py` 指令同時產生 `dictionary_paired.csv`。

## T01 材料留出對照

沿用 C1 的訓練材料碼留出與正規化名稱剔除規則。精確與最長包含詞兩法均將 850 驗證列、781 測試列全部轉待審，無自動命中或錯配。自動精確率因零自動命中而無分母。此情境同時留出材料碼與名稱，只支持合成留出下的結果。同材料的新別名與陌生廠商真實資料仍待驗證。

重跑 `python factory_dictionary_unseen.py <source> <output.json>`，機讀結果為 `evening_results/factory_dictionary_unseen.json`。

## T01 同材料新名稱合成測試

固定訓練字典及材料碼，驗證 162、測試 156 筆混凝土／鋼筋來源列。原名稱兩法全對。人工加前綴保留原詞時，最長包含詞全對、精確比對全待審。以預拌砼／鋼質補強筋替換名稱後，兩法全待審。兩類改寫皆為合成，同材料真實廠商別名仍缺。研究目錄執行 `python factory_alias_stress.py <source> <output.csv>`；逐列證據見 `evening_results/factory_alias_stress.csv`。

## T01 字典分層比較

以固定廠房快照重跑零字典、精確名稱、最長包含詞與保守規則，按單位、規格、章節與工料範圍輸出分母、正確、錯配、待審數。測試集純材料 207 列均通過保守規則，含工帶料 3179 列均轉人工。精確及包含詞兩法皆正確對應全部 3386 列材料碼。此結果限於已見合成名稱及指定規則。未驗證正式字典或跨用途。沿用 replay_factory_safety.py 原重跑指令，新增輸出 factory_mapping_strata.csv，已收錄於研究目錄 evening_results。

## T05 上下界漏覆蓋

固定 26 案 × 3 工項。依序列出低於下界、區間內、高於上界的案項數。70% A0 為 9／45／24，A1 為 6／50／22。90% A0 為 2／66／10，A1 為 0／68／10。計數逐筆與原命中欄位一致。此為合成資料逐案項描述，三工項可能同案相依。90% 涵蓋增加全部來自低於下界的案例減少，高於上界仍有 10 筆。

重跑 `python factory_interval_tails.py evening_results/factory_a0_a1_pairs.csv evening_results/factory_a0_a1_90pct_pairs.csv`。

## T05 逐案涵蓋轉換

26 合成廠房、78 案項。70% 區間兩法皆命中 43，僅 A1 命中 7，僅 A0 命中 2，兩法皆漏 26。90% 對應為 65／3／1／9。已有的預測與門檻沿用，此為描述性補充，案項在案內相依。結構體金額 90% 雖兩法各命中 21，仍有一進一出。機讀分項見 `evening_results/factory_coverage_transitions.csv`。重跑 `python factory_coverage_transitions.py evening_results/factory_a0_a1_pairs.csv evening_results/factory_a0_a1_90pct_pairs.csv`。

## T05 涵蓋率與寬度的配對不確定性

26 案、78 案項，三結構各以整案重抽 2,000 次（seed 20261122），結構與工項等權。70% 與 90% 的涵蓋率差各為 +6.48 與 +2.78 個百分點，95% 重抽區間均跨零；相對寬度差各為 +0.02584 與 +0.05939，兩區間均高於零。相對寬度為預測區間寬除以真值。此為同一合成資料的描述性補充。[重抽結果](../research/2a_synthetic/kde_comparison/evening_results/factory_coverage_uncertainty.csv)

## T05 逐案留一敏感度

固定 70% 主分析預測，輪流移除 26 件測試案各一件並重算九層等權分數。KDE 的相對改善落在 2.20%–6.94%，26 次均為正，16 次達 5% 點估計門檻。這是已檢視資料的影響度診斷，仍以原定 bootstrap 與採用門檻判斷，維持 A0。研究目錄執行 `python factory_leave_one_out.py evening_results/factory_a0_a1_pairs.csv`。輸出封存於 `evening_results/factory_leave_one_out.csv`。

## A0／B1 成對區間分數重抽樣

名義 70%，按結構分層，以共同可估建案重抽 2,000 次。B1 減 A0 的區間分數差在混凝土、鋼筋、結構金額各為 +0.228、+0.697、+1.337，95% 區間各為 [0.121,0.320]、[0.592,0.832]、[0.833,2.004]。共同可估案數為 9、9、26；兩個數量結果僅適用於共同可估子集。分數越低越好。這是已檢視合成資料的探索性補充，B1 依報告重建。金額涵蓋 B1 17/26、A0 13/26，採用判斷仍保留 A0。

重跑 `python factory_a0_b1_uncertainty.py evening_results/factory_a0_b1_pairs.csv`。結果存於 `evening_results/factory_a0_b1_uncertainty.csv`。
