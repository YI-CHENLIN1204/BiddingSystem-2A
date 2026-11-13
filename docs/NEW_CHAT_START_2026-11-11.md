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


## 2026-11-12：最新第七輪研究接手

新研究程式 `research/2a_synthetic/eligibility_review.py` 已新增，針對來源／用途／結構／材料／單位／工程範圍／核准狀態做研究版資格診斷；`production_eligible` 不得讓任何合成資料通過。下一步在 Windows 本機執行並回報 `eligibility_report.md` 和單元測試結果。該研究未接觸正式資料庫，後續是否能映射至實務資料，仍須先查證真正歷史表欄位，不能預設已完成。


## 2026-11-12：最新第七輪結果與正式欄位查核交接

使用者已提供 `eligibility_report.md`：35 件模擬候選，hypothetical_eligible=1、production_eligible=0。已靜態核對正式 `ProjectMaster`／`FinalBidItem`／`HistoricalTransaction` C# 模型，詳細見 `docs/2A_REFERENCE_ELIGIBILITY_MAPPING_2026-11-12.md`。下一步不是重跑生成器，而是**由使用者在確認正確的開發 DB 後執行** `research/2a_synthetic/reference_schema_readonly.sql`，回傳 `information_schema` 欄位清單以確認來源資格欄位缺口。SQL 唯讀；禁止自行增加 DB migration、改正式模型或推論已經有來源可信認證。


## 2026-11-12：實際 Schema 已確認（新對話最新接手點）

使用者已提供六張表的真實 PostgreSQL Schema，總共 **86 欄**。詳見 `docs/2A_VERIFIED_DB_SCHEMA_GAPS_2026-11-12.md`，不用再向使用者索取相同的 columns 結果。已明確確認主要表缺少專用來源真實性與工程範圍完整性資格欄位，並且 ProjectMaster 若干基礎條件、HistoricalTransaction 外鍵可 NULL。**下一步**為讓使用者在正確 Supabase 開發 DB 執行 `research/2a_synthetic/reference_quality_counts_readonly.sql`，回報**聚合結果**，核對實際 Approved 案件數、用途/結構/材料可用案數、來源欄位缺漏與交易 FK 可追溯性；不得直接建 migration 或修改正式異常判斷。

## 最新 2026-11-12：Q4 聚合已收到

Q4 已由使用者提供：有效 Approved 歷史材料記錄目前只回傳住宅 RC；material_id=1、2 各有 8 個**獨立建案**，對應交易分別 11、10 行。材料 ID 名稱未知，DEMO 資料疑慮未解，不能將 8 筆當成 8 個真實工程模型資格。見 `docs/2A_APPROVED_MATERIAL_COHORT_2026-11-12.md`。下一個對話請優先讀先前 Q1～Q3 結果（若未提供，才請使用者補）與新 `research/2a_synthetic/reference_material_source_audit_readonly.sql` 聚合結果，不要重查已完成的 Q4，也不要擅改正式資料庫。


## 2026-11-12 最新進度：完整 Q1～Q4 已取得

已收到四組真實 DB 聚合！**不用再索取 Q1～Q4**。報告在 `docs/2A_HISTORICAL_DATA_QUALITY_Q1_Q4_2026-11-12.md`。8 Approved、2 Draft 全住宅 RC；7/8 Approved 缺 source_file_hash；23 交易有 2 刪除、2 連非 Approved（交集尚未確定），FK 無空或錯連；有效歷史材料 ID=1／2 各 8 件獨立建案及 11／10 交易。下一步執行 `research/2a_synthetic/reference_material_source_audit_readonly.sql` 的 Q5～Q8，確認材料名稱、疑似 DEMO 來源、同案材料多筆交易和刪除／非 Approved 交集。任何合格率結論都不能跳過來源真實性、標單完整性驗證；產學正式模型未變。


## 2026-11-12 最新：Q1～Q8 都已完成，正式來源資格待設計

查庫八組聚合結果全部收到；請先讀 `docs/2A_HISTORICAL_DATA_QUALITY_Q1_Q4_2026-11-12.md` 與 `docs/2A_HISTORICAL_DATA_QUALITY_Q5_Q8_2026-11-12.md`。10 案全部住宅 RC（8 Approved、2 Draft）；7/8 Approved 缺 Hash；**7/8 Approved 來源名稱疑似展示或測試，剩餘 1 件未核真實**。鋼筋/kg 與混凝土/立方公尺各有 8 名義 Approved 建案、11/10 有效材料交易。刪除的兩筆恰與兩筆非 Approved 連結資料相同。下一步應提出來源/完整性審核、證據與資料模型方案，避免把 Approved 混同 Verified Reference；**不要再向使用者索取相同 Q1～Q8 結果，也不要重跑模擬**。不改正式權重/DB。


## 2026-11-12：Verified Reference 草案已完成，下一步是接入點審查

已完成 Q1～Q8 真實 DB 聚合，8 Approved 裡 7 件**名稱疑似**展示/測試，1 件未核實；不可由此宣稱沒有真實建案。**最新版需求草案**：`docs/2A_VERIFIED_REFERENCE_DESIGN_V1_2026-11-12.md`。規格包含獨立來源狀態、鋼筋／混凝土／結構體價格各自資格、證據與稽核、角色／撤銷、舊資料過渡和 14 項 VR 驗收情境。它**尚未獲業務／老師確認，完全未實作**。下一件工作是讀現行 History Publish/Reopen、權限與估算服務，列出完整接入變更清單；不要直接修改 C#、DB、權重或重跑 Q1～Q8。


## 最新 2026-11-12：Verified Reference 接入位置已靜態查核

請先讀 `docs/2A_VERIFIED_REFERENCE_DESIGN_V1_2026-11-12.md`、`docs/2A_VERIFIED_REFERENCE_INTEGRATION_AUDIT_2026-11-12.md`。實際 C# 已看到 P0：Program 無 AuthN/AuthZ、Projects/Audit API 無 [Authorize]；相似度候選 int overload 無 Approved 限制、DTO overload 有；數量模型已有按 ProjectId 聚合。**Verified Reference 仍是未核定規格／未實作功能**；下一步釐清部署入口、實際身份與角色、前端 UI 位置及業務政策，之後才安全分期實作，禁止跳過核准直接改 DB 或模型候選。


## 最新 2026-11-13：前端／部署靜態核對已完成

Verified Reference 還未實作，業務／老師尚未核定。已完成後端及前端接入盤點，最新：`docs/2A_VERIFIED_REFERENCE_FRONTEND_SECURITY_AUDIT_2026-11-13.md`。Vue router/App/api 沒有已集成的登入及角色守衛，AuditLogs 出現在側欄；Nginx `/api/` 轉發 backend:8080 未見 auth 相關設定；ASP.NET Program 也沒有 AuthN/AuthZ，但**實際主機可能有外部入口限制，不能推斷它公開可達**。下一個需要確認的是部署管理員對 API/前端實際可達性、反向代理/防火牆/VPN、公司身份提供者與主管角色的規範，然後才實作端點授權。舊 Publish/Reopen 的 Idempotency-Key 不得被新機制破壞；不要再重跑已取得的 Q1–Q8。
