# 2A 新對話接手提示（2026-11-11）

> 直接將下面的「交接提示」整段複製到新的 ChatGPT 對話。專案的真正長期紀錄在 GitHub docs，而不是此段文字。**不要只憑提示詞猜測程式內容。**

## 交接提示（可直接複製）

```text
請直接繼續我在雲科大 × 崴正營造的 2A「量價估算與異常偵測」產學合作／專題研究專案。

GitHub：YI-CHENLIN1204/BiddingSystem-2A
正式開發分支：feature/1d-api-integration
最新交接主檔：docs/CURRENT_HANDOFF_2026-11-11.md
研究證據與回溯：docs/RESEARCH_TRACEABILITY_2026-11-11.md
研究操作：research/2a_synthetic/README.md
目前模型審查：docs/2A_MODEL_AUDIT_2026-11-10.md
外部資料：docs/2A_PUBLIC_DATASET_FEASIBILITY_2026-11-10.md
既有設計：README.md、docs/DECISIONS.md、docs/2A_CURRENT_ARCHITECTURE.md、
docs/DATABASE.md、docs/API.md、docs/WORKLOG.md

**請先用 GitHub 工具直接讀最新文件與程式碼**，再開始開發；如果新對話無法讀取 repo，依交接文件請我提供檔案，不要虛構已讀。老 docs/HANDOFF.md、docs/NEXT_CHAT_HANDOFF.md 記錄 10 月狀態，已過時；最新版主交接優先。

兩條線必須完全隔離：
1. 專題：老師允許模擬資料，使用獨立 research/2a_synthetic 程式研究估算算法、機率分布、小樣本、異常與相似度權重。
2. 產學正式系統：只用真正完整、人工審核核准的崴正歷史標單作正式模型基準；模擬、公開研究資料只能獨立參考，絕對不能混入 HistoricalTransaction / FinalBidItem 歷史真值。不能把模擬結果稱為真實工程準確率。

正式 2A 仍為：
- AnalysisCase / AnalysisBidVersion / AnalysisRun 待偵錯快照，V1/V2/V3；可 2A 自行上傳，1D 選配。
- ProjectMaster Draft / FinalBidItem 人工審核 / Publish Approved / HistoricalTransaction 正式歷史。
- 數量偵錯：鋼筋、混凝土；價格異常：結構體工程。
- 相似度 6 因子現有權重 25/20/10/10/0/5，TopK 8，最低名義參考數 5，時間半衰期 3 年、相似度指數 2、加權 P15/P50/P85。**這些尚未經真實資料證明最佳**。
- 先前查 DB：8 Approved 全住宅 RC，7 件名稱疑似模擬，另 2 Draft；與公司原始約 20 件建案不同，請重新查證目前資料狀態。
- ESS 已接入數量／價格結果，不等於信心正確率。

已經實作研究：
- generate.py：6 用途結構組合、鋼筋＋混凝土、4 情境（stable/high_variance/company_shift/outliers）。
- compare.py：中位數／normal 描述區間／加權分位的比較。
- batch.py：多 seed、小樣本 3/5/8、模擬誤差與涵蓋率。
- production_weight_review.py：六因素相似度與 3 組結構 gate／權重方案比較。
- test_research.py：研究回歸測試。
完整路徑都在 research/2a_synthetic/。

使用者完整 PowerShell 實測 batch.py 成功：5760 group runs、96 summary rows；當時 unittest 7/7 OK。
最新 production_weight_review.py、新測試也已回報「沒有錯誤」，但尚未貼出其 summary/detail CSV 或完整測試輸出。不要自行推測哪個模型較好。

下一步，請優先分析本機檔案：
research/2a_synthetic/output/production_weight_summary.csv
research/2a_synthetic/output/production_weight_detail.csv
research/2a_synthetic/output/production_weight_manifest.json
（若尚未提供，請給出準確指令或採相同 seed 重建。）
然後審查 Python 模擬和正式 C# 演算法的差異與研究公平性，補上必要測試、結果報告、交接更新；不要重新實作已完成的 5760 組批次實驗，不要回到過時架構，也不要無限搜索新算法。

合作方式：一次完成有意義的一批修改，推 GitHub，再讓我 Windows PowerShell 拉取與測試；失敗再修。我的偏好是可直接執行的命令、完整而非零碎改動，繁體中文。不要自行合併 main、修改正式資料庫、刪資料或覆蓋本機未提交檔。

每次都要更新 docs/CURRENT_HANDOFF_2026-11-11.md、
docs/RESEARCH_TRACEABILITY_2026-11-11.md 與 docs/WORKLOG.md，寫明最新 SHA、指令、證據等級與未解事項。必要時建立新日期主檔並更新索引，避免舊檔誤導。
```

## 什麼能最大限度讓新對話「真正銜接」

GitHub 已保存程式／決策／變更與交接檔案，但**不能存取你目前 PC 上未 push 的研究輸出**。`research/2a_synthetic/output/` 受 gitignore 管理；要讓新對話直接判讀實驗，請將 `production_weight_summary.csv`、`production_weight_detail.csv` 和 manifest 附上（或由新對話按固定 seed 重跑）。

完整 100% 文字上下文在不同對話中無法保證；本專案改用 Git SHA、固定 seed、研究輸出與文件來建立**可檢查／可重現的交接**。


## 新對話：先啟用專案交接守門規則

**請在貼上上面的交接提示後，明確附加這句：**

```text
在任何程式／研究／文件變更前，請先讀 GitHub 中的 AGENTS.md 與
.agents/skills/waizheng-2a-handoff-guardian/SKILL.md，
並在每輪修改後重新審查是否需更新交接，
向 docs/HANDOFF_CHANGE_REVIEWS.jsonl 記錄更新或免更新理由，
執行 python scripts/check_handoff_review.py --base <變更前 SHA>。
這是本專案每輪都必做的開發規則；不要只更新程式卻忘記交接。
```


## 2026-11-11 第四輪接手補充

最新加入 `research/2a_synthetic/report_weights.py`，它從本機第三輪權重比較 CSV 真實產出 `research/2a_synthetic/output/production_weight_report.md`，用以比較三組六因素權重。**尚未收到 CSV，也沒有正式模擬 MAPE 勝負結論**。新對話優先讀報告（若無就提供 `python research/2a_synthetic/report_weights.py` 指令），再決定下一輪研究。不把報告當實際建案驗證。


## 最新 2026-11-11：第三輪結果已分析

已收到第三輪 `production_weight_report.md`，證據與數字見 `docs/2A_SYNTHETIC_WEIGHT_RESULT_REVIEW_2026-11-11.md`：A 17.10%、B 17.07%、C 16.91%，差距很小且被多因子同時變更干擾。已建立下一輪 `research/2a_synthetic/factorial_weight_review.py` 供結構門檻 × 面積權重獨立比較。新對話下一件事：取得本機 `research/2a_synthetic/output/factorial_weight_report.md`，解析結果並依交接 Skill 更新文件；不要重做第三輪或擅改正式權重。


## 最新第五輪實測與第六輪接手（2026-11-11）

使用者已證明 `factorial_weight_review.py` 跑完 **4320／144／192**，原有 **13/13** 測試通過、Guardian 9/9 交接審查通過；但尚未上傳 `factorial_weight_report.md`，不能判定新權重實驗勝負。第六輪新增 `sparse_structure_review.py`，模擬每個結構只有 0～3 件可用歷史資料，觀察結構限制與名義 5 件門檻的影響；下一步須檢查報告與單元測試。先閱讀專案 Skill 及最新 README，勿修改正式產學估算與 DB。


## 第五／六輪結果已收到：新對話接手修正（2026-11-11）

先閱讀 `docs/2A_FACTORIAL_SPARSE_RESULTS_2026-11-11.md`。第五輪六配置拆解與第六輪 0～3 同結構樣本壓力測試已經有本機 Markdown 報告，**不需要再次索取同樣報告**。主要結論：權重改動在合成環境平均效益很小；同結構資料不足卻混用大量其他結構時，名義筆數可能達五件但可比性嚴重不足。後續請研究「來源／用途／結構／工程範圍的資格診斷」與跨結構假設敏感度；不得直接改正式 2A 權重或填入假歷史資料。
