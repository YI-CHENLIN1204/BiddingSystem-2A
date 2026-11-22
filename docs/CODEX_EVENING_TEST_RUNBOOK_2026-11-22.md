**公平比較重要更正：A0對B1的舊報告使用不同有效歷史筆數，不能宣稱完整方法的等N勝負。請先讀 docs/EQUAL_SAMPLE_ALGORITHM_FAIRNESS_PROTOCOL_2026-11-22.md，並實際完成K5/K8等有效歷史樣本控制研究。原A0/A1同8筆參考ID結果屬KDE-only消融。**

# Codex 今晚執行指令（整段可貼）

請使用繁體中文，在 `YI-CHENLIN1204/BiddingSystem-2A` 的**獨立研究工作樹**完成完整測試，最後一次整合提交到 `research/kde-comparison-20261122`。開始先 fetch 最新 `feature/1d-api-integration` 與 research HEAD，檢查 `git status`，不得覆蓋現有工作樹／未提交檔案。**先讀** AGENTS.md、.agents/skills/waizheng-2a-handoff-guardian/SKILL.md、README.md、docs/CURRENT_HANDOFF_2026-11-11.md、docs/RESEARCH_TRACEABILITY_2026-11-11.md、docs/HOLDOUT_EVALUATION_2026-11-22.md、docs/ALGORITHM_KDE_EXPERIMENT_PLAN_2026-11-22.md、**docs/EVENING_FULL_TEST_PLAN_2026-11-22.md**。

目標：用既有合成住宅300案（210 train/45 validation/45 test）與可驗證時的廠房180案（126/27/27），今晚盡力完成完整測試、報告圖表。依新矩陣執行 **P0：T00來源/隔離、T01字典正確Auto/錯Auto、T02字典規模曲線、T03節省人工情境、T04單位及可估工項安全、T05 KDE公平比較**；接著 P1 T06異常、T07 Parser／隔離副本回寫、T08效能、T09回歸；P2 T10 TF-IDF、T11階層分群＋KDE 只在時間充裕時做。

**絕對禁止改**正式 Vue5184/API5264、現有Supabase任何資料／設定／Run／RLS／字典，禁止正式匯入、重seed、發布，沒有隔離資料庫副本就不跑寫入型E2E。離線測試不得連接正式 API；獨立結果網站若建立用5185，讀本地JSON無/proxy。只讀雲端時點發現67條現有字典，但若未取得經認證來源的**唯讀本地快照**就標記 live-67=NOT RUN，另用train-only字典完成公平實驗；材料真值以穩定 material_code/型錄為準，不能從val/test取得字典規則。自動對應率不等於正確率。對每條錯Auto留trace。人工節省工時扣審核、修錯、維護字典；沒有真人計時就只能輸出保守／中性／樂觀「情境推估」，不可編造實測節省工時。

KDE A0/A1固定同一參考ID/相同權重；validation選帶寬、test不回調；比較70%（雙方P15/P85）與90%、相對Interval Score、P50 MAPE、Coverage、Width；不重做已完成144組權重搜尋。異常Precision/Recall/FPR缺工程核准真值不得填。來源或工具缺失就以NOT RUN記理由，繼續其他工作，不得偽造結果。

最後必產 **Markdown＋Word 測試報告**、逐案與彙總CSV/JSON、run_manifest、test_status、**至少6張以真實輸出數據製成的300dpi PNG＋SVG**（字典成長、Auto錯誤、安全節省工時、KDE score、Coverage/Width、MAPE）。報告每圖要 n、單位、合成來源、圖說和文字分析；不能只有漂亮示意。結果不能自動改正式演算法。照守門Skill同步 docs/HANDOFF_CHANGE_REVIEWS.jsonl、主交接、WORKLOG、研究追溯，執行 `python scripts/check_handoff_review.py --base <start-SHA>` 與可用測試。整批提交後回覆我：每項PASS/FAIL/NOT RUN、真實數據、報告與圖檔路徑、Git SHA、正式資料是否完全沒變。今晚時間是規劃配額，不能犧牲安全或虛構驗證。


**務必先讀 `docs/EXTERNAL_REPORT_ALGORITHM_SPEC_2026-11-22.md`**；A1 是我方加權 KDE 消融，不是外部作者整套算法。B1 才是依外部報告第3.2～3.7節重建的分群＋等權 KDE；如群數、網格等原文缺值，先明列假設並在看test前凍結，不可宣稱exact reproduction。
