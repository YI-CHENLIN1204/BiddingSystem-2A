# 2A 晚間全面測試計畫 — 2026-11-22

**狀態：計畫已制定，尚未執行本輪測試；不得偽稱已有演算法勝負、節省工時或實測圖表。**

## 目標與不變條件
以 Codex 在今晚完成盡可能多的**可重現、可放入專題書面報告**的測試；先求具真實數據、清楚限制的比較報告，再求實驗展示頁。正式系統 Vue 5184 → ASP.NET 5264 → Supabase **不變**，不得更新正式估算參數、資料字典、歷史、案件、Run、Schema、RLS、正式 API/前端。新測試應於專用 Git worktree 的獨立研究目錄完成，另規劃 5185 **只讀靜態結果頁**（不可 proxy 正式 API），不必以修改正式頁面方式展示研究。

- 開始前讀 `AGENTS.md`、`.agents/skills/waizheng-2a-handoff-guardian/SKILL.md`、`README.md`、`docs/CURRENT_HANDOFF_2026-11-11.md`、`docs/RESEARCH_TRACEABILITY_2026-11-11.md`、`docs/HOLDOUT_EVALUATION_2026-11-22.md`、`docs/ALGORITHM_KDE_EXPERIMENT_PLAN_2026-11-22.md`，以及 feature 最新 HEAD／`git status`。實際程式碼高於較早交接。別覆蓋 untracked 測試檔。
- 住宅 SYNTHETIC v2 **210 train/45 validation/45 test**；廠房 FACTORY_20261122_V1 **126/27/27**；來源需 SHA256 與 lineage 驗證，不能重生替代。只有 train 能當歷史或字典來源，validation 選方法，test 凍結評估。住宅已用3個test案做過診斷，不能聲稱全新盲測。
- 2026-11-22 當輪已連接 Supabase **純 SELECT 快照**：336 Approved合成建案、67個 `item_dictionary`、104種標準材料、11單位、16案件、47 Run、117952 final bid rows。這不是永遠固定的數字；67條字典的訓練來源未核實，需分開評估 live-current 與 train-only，不得用已看過測試答案的字典冒稱無洩漏。
- 核對實際 `崴正/Services/FinalBidImportService.cs` 是 `ItemName/Spec.Contains(Keyword)`、最長Keyword優先，命中合法 `MaterialId` 即標Auto；`MaterialMappingService.cs` 人工對應會更新同Keyword所有字典行。Auto != 正確；單位/數量問題不應被自動覆核掩蓋。

## 今晚測試項目／優先級

| 代號 | 優先 | 主題 | 必做測試與指標 |
|---|---|---|---|
| T00 | P0 | 安全與來源 | git/來源 SHA、split與 train/test 防洩漏；A0 baseline重現現有C#參考IDs、分位、樣本不足；DB前後只讀計數/設定一致；禁DB寫入、禁研究頁代理5264 |
| T01 | P0 | **材料字典自動對應** | train-only建立字典後在獨立val/test比較「零字典／全名精確／現有最長Contains／加安全門檻的字典」；單位、規格、章節、材料vs施工、含工帶料及跨用途分開報 `auto_coverage`、`auto_precision`、`wrong_auto_count`、`pending_rate`、`safe_auto_rate`、歧義數；現有67條要先唯讀匯出及證明來源，否則live-67為NOT RUN |
| T02 | P0 | **字典規模學習曲線** | 10/30/50/100條 train-derived 字典及可用時的67條現況，固定相同val/test；比較每新增字典正確自動對應率、錯自動率、待審比例、跨廠房/住宅泛化；不能把 test 工項名稱或材料ID提前灌字典 |
| T03 | P0 | **人工工時與成本** | 不只算自動化率，要扣人工快速檢查、Pending手動、錯誤偵測修正及建/維護字典成本。若無真人計時，只能用清楚標記的**保守/中性/樂觀情境估計**；100/500/1000筆標單列的節省分鐘/工時與敏感度。若可真人計時，使用兩份不同但難度匹配的標單交叉測試並記錄實測人數／中位秒數 |
| T04 | P0 | **單位與工項安全** | kg↔噸、m↔公尺、m²↔㎡、m³↔立方公尺，僅測試設定完整同類換算；現有SYN不可換算單位須拒絕猜測；m²↔m³拒絕；0/負數/空值、運費／純施工／試驗、材料／含工帶料重複計量、可估/不可估/不適用/未知缺漏與明確0的邊界測試 |
| T05 | P0 | **KDE vs 現行估算** | A0固定TopK 8／最低5／相似×時間權重 P15/50/85；A1用**同reference IDs與同最終權重**改加權KDE；名義70%公平比較+雙方90%敏感度；validation先調帶寬、test只一次；報相對Interval Score、P50 MAPE、Coverage、Width、棄答率／ESS；不重跑已完成144組權重搜尋 |
| T06 | P1 | 異常壓力與漏判 | 相同案件分別施加數量、價格、混合、規格變異、缺漏、極端值、不同結構與日期後的結果，紀錄高低待審/資料不足；未經工程師核定的正常/異常真值，不得填Precision/Recall/FPR |
| T07 | P1 | Excel與決標回寫 | 解析多Sheet、重名、總價/數量、錯列/表頭/無效格式；Draft→人工對應→單位→Publish→Reopen→Audit **只可使用可丟棄的獨立 DB 副本**，沒有就改為mock／現有Parser回歸、標記E2E Write=NOT RUN，禁止寫Supabase |
| T08 | P1 | 效能及穩定性 | 合成1k/10k/50k工項，字典0/10/67(具快照)/100/500條；測解析、查字典、KDE p50/p95耗時與記憶體；固定環境、暖機、3輪，不能以CI compilation稱性能合格 |
| T09 | P1 | 測試證據／權限／回歸 | SHA一致性、重現性、錯誤案例、資料洩漏拒絕、研究頁只能讀JSON、輸出不含密碼、現有build/tests/hand-off gate及正式DB快照前後只讀核對 |
| T10 | P2 | TF-IDF候選分類 | char ngram 1-3 TF-IDF、top1/top3 recall、人工覆核負擔；僅候選建議，不自動改材料，不能把章節分類準確率稱材料對應準確率 |
| T11 | P2 | 階層分群＋KDE | 按既有 KDE計畫 B1/B2 跑平均連結、群內等權、價格基地面積單獨消融；不能把完整組變化當作只有KDE效果 |

### T01～T03：工時量測公式
- `eligible_n`、`auto_n`、`correct_auto_n`、`wrong_auto_n`、`pending_n`、`excluded_n` 分開記；沒有真值的列不得默默刪掉。
- `auto_precision = correct_auto_n / auto_n`，`auto_coverage = auto_n / eligible_n`，`safe_auto_rate = correct_and_safe_auto_n / eligible_n`。
- `manual_seconds = N * manual_time_per_row`。
- `assisted_seconds = safe_auto_n * quick_review_seconds + pending_or_unsafe_n * manual_time_per_row + wrong_auto_n * (detection_seconds + correction_seconds) + dictionary_setup_seconds + maintenance_seconds`。
- `net_saved_percent = (manual_seconds-assisted_seconds)/manual_seconds*100`。為避免重複計量，wrong_auto與pending_or_unsafe採互斥集合；不要讓錯誤自動對應在公式中免計錯誤偵測成本。情境：每筆人工15/30/60秒、快速確認3/6/10秒、修錯30/60/120秒；都是**假設待測**而非已發生的時間。若沒有真人計時，報「情境推估節省工時」和上下界，不可報「實測省了xx%」。
- 包含安全負例：SD420/SD490、不同PSI、跨工種、單位錯置、材料/施工/運費、同名異材料、長關鍵字覆蓋較短關鍵字、陌生標單格式、住宅vs廠房不同命名、低信心候選。測到高覆蓋但高錯自動率，必須判不適合自動核准。

## 今晚工作時段（2026-11-22 台灣時間；**時間配額非完成保證**）
- 16:30–17:15：T00 安全/資料/版本/既有API快照重現。
- 17:15–18:45：T01～T03 字典安全、自動對應、學習曲線、人工成本情境，先拿結果及3張圖。
- 18:45–20:15：T05 A0 vs A1 KDE公平比較，完成2～3張圖。
- 20:15–21:15：T04單位/工程範圍＋T06異常壓力。
- 21:15–22:15：T07 Parser + 可安全測試的E2E、T08離線效能、T09回歸。
- 22:15–23:15：彙總CSV/JSON、中文Word+Markdown報告、圖表；有時間再做5185靜態頁。
- 23:15–24:00：檢核完整性、handoff reviewer、最後集中提交GitHub；T10/T11若阻塞則NOT RUN，不能擠掉P0與報告。

## 交付格式（即便有FAIL也須交報告）
本地 `research/2a_synthetic/output/evening-2a/<run-id>/`：`run_manifest.json`（SHA、seed、split、資料來源、正式系統快照）、`mapping_per_item.csv`、`mapping_summary.csv`、`dictionary_growth.csv`、`labor_savings_scenarios.csv`、`kde_comparison.csv`、`safety_cases.csv`、`performance.csv`、`test_status.json`、`report/2A_測試報告.md` 與 `.docx`。
**至少6張有真實基礎的圖**，每張有繁中圖名、n、資料分割、單位、合成資料標示、圖說、PNG 300dpi + SVG：
1. 字典規模 vs 正確自動對應率；2. Auto coverage vs 錯誤自動對應率；3. 人工工時節省保守/中性/樂觀；4. A0/A1相對Interval Score；5. 區間Coverage vs Width；6. P50 MAPE；進階7.工項排除原因；8.解析/字典效能p95；9.KDE經驗CDF對照。
研究網頁 `5185` **只讀本地測試JSON**、可篩選/下載圖，沒有正式API。輸出大檔、資料快照與密碼不得推送Public GitHub。

每個測試必須有 `PASS / FAIL / NOT RUN`、實測數字或失敗原因、可重現命令；研究結論區分實測模型效果、情境推估人工節省、不可識別工程異常正確率。不能直接部署新演算法。
