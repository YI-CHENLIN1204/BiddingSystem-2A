## 2026-11-23 研究 Run #3 通過隔離驗收與 Word 重渲染

[Run 45057125296](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45057125296) 對應修正提交 `da4324596707969ee805a37f68e284a0c0f955d0`。三個 Job 均已完成並通過。修正後本機以相同非 Web SDK 與 ASP.NET Core framework reference 編譯稽核驗證類別，0 警告／0 錯誤。實際 runner 成功建置並啟動 PostgreSQL 17 隔離 API。

| 驗收 | Run #3 結果 |
|---|---|
| T04 | EF 換算與無效係數拒絕 PASS。 |
| T07 | Draft、人工映射、Publish、Reopen、重複提交與跨建案鍵拒絕 PASS。 |
| T08 | 隔離建案讀取 30 次，p50 19.79 ms、p95 35.71 ms。 |
| T09 | 匿名 401、無效憑證 401、Reader 403、Supervisor 200。主管讀回同案 `PublishHistoryBaseline`、`ReopenHistoryReview`、`UpdateMapping`，發布與重開各一筆。研究隔離端到端 PASS。 |
| Word | LibreOffice 重渲染 PASS。頁碼依序、舊頁首孤行未重現的 PDF 文字檢查 PASS。DOCX 四個續表表頭設定、有關段落保持完整、八張圖與原內文已核對。新版逐頁目視仍待，當前環境無法取得 PNG 位元組。 |

- [隔離 API Artifact 14767619273](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45057125296/artifacts/14767619273)，ZIP SHA-256 `5fc60c5b755169942be6751e81628b366a164155ccb7706d35a130450892bb19`。內含 `fixture.log`、`http.jsonl`、`server.log` 與 `SHA256SUMS`。
- [Word Artifact 14768340468](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45057125296/artifacts/14768340468)，ZIP SHA-256 `9aa485d44da025fc8fa93bcc9c060cf04e9ce6683dcd8fb5d5663adb3b5d5c8a`。內含 PDF、逐頁 PNG、`page-checks.json`、`source.sha256` 與 `SHA256SUMS`。

原 Run 45049455364 的 T04／T07／T08 成功證據、原 T09 200 失敗與 Run #2 編譯失敗仍保留為歷史。正式部署及資料庫角色另待驗收，完整 T00～T11 不因此結案。下一步以新版 PNG 確認續頁表頭是否實際可見、頁碼位置與各頁版面。正式 2A、A0、Supabase 維持原版本。

## 2026-11-23 研究驗收編譯與 Word 排版修正

以 `addac2f247512c12ece609c8ae35517bc67c1501` 為起點。Run [45055170040](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45055170040) 的隔離 API 在 Fixture 編譯缺少 `ILoggerFactory` 命名空間，本次補入 `Microsoft.Extensions.Logging`。保留 T09 的匿名 401／403、無效憑證 401、Reader 403、Supervisor 200，以及主管讀回隔離 T07 Publish／Reopen 各一筆的門檻，新增明確的稽核事件驗證輸出。原 Run 45049455364 的 T04／T07 PASS 與 T08 30 次 p50 19.04 ms／p95 26.33 ms 仍有效。

研究 Word 四張表皆設定續頁表頭，原跨頁段落設為整段同頁，頁尾加入置中的 PAGE 欄位。本機已驗證 OOXML／ZIP，原內文及八張內嵌圖逐位元組保持一致（內文比較為文字值）。新版 DOCX SHA-256 `d9fb05c1e4b83422a257f2fb3d3e9ed3d8abbbc275265311b931d511ee2f9ccf`。Workflow 檢查 PDF 頁碼依序及舊孤行未重現，新版逐頁目視仍待渲染產物。研究 CI 新增僅用於發布 Run 網址的狀態，不代表驗收結果。

下一驗證點是新版 Workflow 的獨立 API／Word Job、重新渲染的逐頁判讀與產物雜湊。正式 2A、A0、Supabase 及既有算法維持原版本，沒有 Migration。

## 2026-11-23 可追溯 Run #2 與 Word 視覺證據

[研究 Run #45055170040](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45055170040) 已核實，對應 `89faf4f6ad1ff5eccb71ecad15c2c00f8ad23b44`（Push、Completed、Failure）。`isolated-api` 在 `research/acceptance-ci/ResearchAuditAuthentication.cs(10,5)` 遇到 `CS0246 ILoggerFactory` 編譯錯誤，因此 **T09 修正後隔離 API+DB E2E NOT RUN**。檔案缺 `using Microsoft.Extensions.Logging;`，需修正後取得新 Runner 證據，不得降低匿名 401/403、Reader 403、Supervisor 200 的驗收標準。原 Run #45049455364 的 T04 EF PASS、T07 Draft／人工映射／Publish／Reopen／Audit PASS、T08 30 次 HTTP 讀取 p50=19.04ms、p95=26.33ms 均保留已執行證據。

同 Run `word` Job SUCCESS，Artifact [`14767261344`](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45055170040/artifacts/14767261344) 已取得 8 頁 PDF + PNG，全部 ZIP SHA256SUMS 通過。PDF 原始 SHA-256 `7fe896141e88d08d915f5397ea45edd3ea9855b5b1f6f95a1485a4c47a7e98cf`。真正逐頁目視：8 圖及繁體中文完整、無明顯裁切或亂碼；第 2→3 頁跨頁表格未重複表頭、第 3→4 頁首孤行、全篇無頁碼、圖表頁留白過大。因此 **Word 渲染 PASS、目視已執行、最終排版 PARTIAL**。詳細見 [Word 八頁覆核](WORD_VISUAL_REVIEW_2026_11_23.md)。T00～T11 全項尚未完成，正式 2A/A0/Supabase 均未修改。

## 2026-11-23 GitHub Actions 實測與隔離權限修正

[Run 45049455364](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45049455364) 已核實，測試提交為 `b3bceb90b2d8775ba7006ac146bed3d0ecffbc27`。本次修改從 `e0109ade56e34f225efd001022ba3745bcbde838` 接續，提交訊息為 `research: enforce isolated audit roles and reconcile acceptance evidence`。

本機以 .NET 10 合成端點載入相同隔離授權程式，建置 0 警告／0 錯誤。匿名 401、無效憑證 401、Reader 403、Supervisor 200，以及未列入政策的建案 GET 200，五項通過。Python 探測程式與啟動時的覆蓋程式通過語法檢查。實際研究 API 加 PostgreSQL 的修正後 E2E 待新 Run。

- T04 隔離 EF 換算 PASS。T07 隔離 Draft 讀取、人工映射、Publish、Reopen、重複提交與稽核核對 PASS。兩項保留原本完整驗收門檻。
- T08 隔離建案讀取 30 次，p50 19.04 ms、p95 26.33 ms。正式全流程效能仍待測。
- T09 原 Run FAIL，匿名稽核 HTTP 200。研究 runner 編譯時加入隔離授權，門檻為匿名 401／403、無效憑證 401、一般使用者 403、主管 200 且讀回 T07 稽核紀錄。修正後 Actions 結果待核實。正式角色整合仍待驗收。
- Word 渲染 job PASS，產出 8 頁 PDF 與 PNG。逐頁視覺仍待取得可開啟的頁面。渲染狀態及雜湊見[執行查核](RESEARCH_ACTIONS_RUN_VERIFICATION_2026_11_23.md)。
- 本批範圍是 `research/acceptance-ci/` 與研究交接文件。隔離 runner 使用一次性 PostgreSQL，無 Migration。下一步核實新 Run 角色矩陣並完成八頁視覺判讀。研究算法與 A0 維持既有版本。

## 2026-11-23 GitHub runner 研究驗收

新增獨立研究 workflow，包含一次性 PostgreSQL fixture、EF 換算、HTTP 歷史基準流程與 Word 渲染。實驗成果依 Actions 實際紀錄及人工頁面覆核判定。既有 T04 研究版、Parser 和離線 K5 不重跑。外部資料需求及作業界線見[研究驗收 CI](../research/acceptance-ci/README.md)。

## 2026-11-23 T04 安全修正與 T07／T08／T09 補測

基準 `cbf7cbeaae27ea3a8b754bc34cbc0f1ada2c6372`。研究 T04 安全副本通過 23/23，原版同一套為 13/23。由基準的 83 個 GitHub 原始碼 blob 乾淨重建 Parser，0 警告／0 錯誤、78 條 PASS。相同 K5 的 A0-K／B1-K 已補離線選案、群聚、帶寬、單案項與批次計時。隔離資料庫 E2E、API p50/p95 和 Word 視覺仍未執行，正式分支及資料庫未修改。證據、各項驗收狀態與正式移植風險見[本輪補測](../research/2a_synthetic/kde_comparison/t00t11-followup-20261123/README.md)。

## 2026-11-23 T00～T11 優先補測

以 `3eef29061e8ab46a12da62c2a4b390f5031bfdba` 為研究基準。T04 服務 fixture 13 案中 10 通過、3 失敗（零或負係數）。T08 K5 共同可估的 30 案項預測計時完成。T07／T09 解析器既有二進位 78 條 PASS，Python 18/18。隔離 Docker daemon 拒絕存取，未執行資料庫寫入流程或角色測試。Word 通過封裝與嵌圖核對，視覺驗收仍未執行。完整狀態、重現命令及缺少的資料欄位見[補測紀錄](../research/2a_synthetic/kde_comparison/t00t11-acceptance-20261123/README.md)。下輪需先取得隔離 PostgreSQL 或可操作的 Word 渲染器，再補資料與人工驗收。

## 2026-11-23 T00～T11全完成推進：正式合成TRAIN／字典／單位唯讀實測（不等於全完成）

使用者要求**完整做完原計畫全部T00～T11**，不是只完成V2報告。研究branch基線 `b38ea147ecd5d7209608e6f344385a99e6cec5ed`。本輪經連接Supabase實際**只讀 SELECT**驗證：合成廠房126案**全部Approved**（早期main文件說Draft已過時）、16,786筆Auto決標列全有material且對應現況字典，matched_keyword不在字典0、material衝突0、量價非正0、金額與qty×unit_price相差>0.02為0。現行67個字典詞**全為合成廠房TRAIN詞**；11個unit_category中允許換算0個、unit_master 11，沒有錯誤係數、重複或缺基準單位。這些是 TRAIN資料完整性PASS，**不是**實務工項字典辨識率或端到端換算。已依T00～T11分開列未驗證、需要獨立DB／住宅CSV／盲測材料／工程師／真人時間。詳見 [`docs/T00_T11_COMPLETION_GATE_2026_11_23.md`](T00_T11_COMPLETION_GATE_2026_11_23.md)、[`research/2a_synthetic/kde_comparison/LIVE_TRAIN_AUDIT_2026_11_23_ZH.md`](../research/2a_synthetic/kde_comparison/LIVE_TRAIN_AUDIT_2026_11_23_ZH.md)、[`docs/CODEX_T00_T11_FINISH_RUNBOOK_2026_11_23.md`](CODEX_T00_T11_FINISH_RUNBOOK_2026_11_23.md)。本輪僅研究檔案與交接更新，**無正式DB寫入、無正式程式/演算法變更**，全計畫**尚未完成**。

## 2026-11-23 Factory Fairness V2 報告及證據包交付

研究基準 `ce0c90d97bd731bc00aff13c048aa8a1bd763995`。本輪沿用 E1、E2、E3 封存結果。Word 補入 8 張核心圖，附圖號、繁體中文圖說及樣本數。完整證據包含 Word、Markdown、14 組 300 dpi PNG／SVG、封存逐案與彙總資料、重現說明及逐檔 SHA256。

- [Word](../research/2a_synthetic/output/fairness-v2/final-delivery/Factory-Fairness-Report-ZH.docx)
- [完整證據包](../research/2a_synthetic/output/fairness-v2/final-delivery/Factory-Fairness-V2-Evidence.zip)
- [驗收紀錄](../research/2a_synthetic/output/fairness-v2/final-delivery/README.md)

合成結果僅供探索性引用。E2 B1-K5／K8 屬等樣本控制，E3 全群 B1 是研究重建。正式 A0 的採用結論維持。Word 版面逐頁驗收尚未完成，Word COM 無法啟動，未取得可供目視核對的渲染頁。

## 2026-11-22 Fairness V2 交付檔案結構驗收（研究數字未改）

識別 `factory-fairness-v2-deliverable-qa-20261122`。使用者帶回Codex  `d2a71c9` E2/K5/K8等樣本結果及教授版報告。本輪GitHub核對：`fairness-v2` 直接有14張SVG、沒有獨立PNG，雖manifest列出PNG SHA；`Factory-Fairness-V2-Raw.zip` 僅含 `equal_sample_per_case.csv`、`e3_per_case.csv`、`dictionary_material_per_case.csv` 三份原始CSV；GitHub Word DOCX 只有主XML等3個OOXML檔、沒有`word/media/`或嵌圖。使用者提到的 Evidence.zip 在該GitHub資料夾未找到（可能另在Codex本機）。報告實際版面未render目視，正式API/DB未驗證，原E2試驗未由本輪重跑。具體補圖、PNG/證據包、逐頁QA與結論引用要求見 [`docs/FACTORY_FAIRNESS_V2_DELIVERABLE_QA_2026-11-22.md`](FACTORY_FAIRNESS_V2_DELIVERABLE_QA_2026-11-22.md)。E2結構金額K5/K8結果可引用為合成探索性研究，但K8鋼筋混凝土各只有1個共同可估；正式A0保持不變，不是實務勝負已驗證。

## 2026-11-22 第二版等樣本實驗與報告

研究基準 ce925c63a602885fad55f95cb261926931267c03，正式分支 2d574e866aa27d18ec58a0196d5fbda9a7fe600a。E2 凍結共同口徑與日期規則，K5 共同可估 6／6／18，K8 為 1／1／15。70% 分數點估計偏向 A0-K，K8 數量僅單案。E3 獨立呈現共同可估的品質及全體可估率。新增材料 ID 候選及安全面板，同名且可觀察欄位相同的異材合成負例會突破保守規則。工時仍是情境推估。報告、重跑程式、原始資料包與圖表位於研究目錄 fairness-v2。正式 API／回寫、真正可用日期、獨立工程、正式字典及真人計時仍待驗證。維持 A0。

## 2026-11-22 獨立研究嚴謹性稽核（原始碼＋CSV；未重跑完整模型）

識別 `research-quality-independent-audit-20261122`。對照研究分支實際Python、固定合成來源生成器、`factory_a0_a1_pairs.csv`、`factory_a0_b1_pairs.csv`，並獨立以78筆數據重算九層主指標（A0 score 0.5302134664、A1 0.5025979998，Coverage 0.5833333／0.6481481，Width 0.3622876／0.3881268）相符；A0/A1每筆同8個參考ID。舊A0/B1同目標但**有效歷史n不同且數量scope/規格篩選不同**，無嚴格等N勝負；研究原始生成器每材料在單案只有一規格，不能推到真實多規格工程。Codex來源稽核：699/1272筆A0與4528/9264筆B1/B2參考使用紀錄來自同日或較晚開工案例，屬回溯而非真實as-of。193詞字典100%僅已見合成名稱、67詞是train-derived與現行67條不同；工時7.7分鐘是情境推估、章節TF-IDF非材料映射、單位與解析效能僅特定契約/微基準。完整稽核及修法見 [`docs/EXPERIMENT_QUALITY_AUDIT_2026-11-22.md`](EXPERIMENT_QUALITY_AUDIT_2026-11-22.md)。既有研究數據未修改，**沒有新演算法結果**；正式系統/Supabase未變。下一步嚴格同scope/等K與as-of比較、真實標單/真人時間和報告圖說修正。

## 2026-11-22 演算法等樣本數公平性更正（研究規格，尚未重跑）

使用者要求比較兩套算法時同測試案、每案每scope同樣**有效歷史樣本筆數**；已核對 A0/A1 是同26件test×3項、每項同8個參考IDs，為KDE-only消融；舊A0/B1為Top8對B1變動群組樣本量，**不是嚴格等N的完整方案比較**。因此撤回「已證明完整B1不如A0」的表述。正式A0繼續運作只為風險控制，而非已證實勝出。新增 docs/EQUAL_SAMPLE_ALGORITHM_FAIRNESS_PROTOCOL_2026-11-22.md：K5主與K8敏感度；兩法共同可比test案、同scope/真值/單位/面積、相同有效參考K；原B1與B1-K研究變體分開，缺K者報不可比，不借用test或造假。**新版測試NOT RUN**；沒有修改正式DB、程式或歷史結果。

## 2026-11-22 外部作者演算法忠實度補充（非實驗結果）

使用者確認「Codex光讀今晚測試計畫是否會理解另一人的演算法？」；核對原始上傳《專題書面報告 (2).docx》3.2～3.7節後，發現既有文件只列高階方法。新增 `docs/EXTERNAL_REPORT_ALGORITHM_SPEC_2026-11-22.md` 轉述可確認公式與原作者未指定的參數。區分A1加權KDE-only（我方消融）與B1八因子分群+群內等權單峰KDE（依報告重建）及C1 TF-IDF工程章節分類，提供D=33.95與混凝土349.54～650.46金標，揭露群數、帶寬網格、分類字典等尚未完整定義。原DOCX仍保留於使用者對話附件，**未上傳 Public repo**，無原作者碼不可聲稱實際系統等價。研究結果、圖表、正式系統均未由此文件更新產生。

## 2026-11-22 晚間全量測試研究計畫（尚未執行）

使用者指示今晚以 Codex 優先完成演算法公平比較、材料字典 auto precision/false-auto/覆核效益、字典規模學習曲線、人工時間保守/中性/樂觀情境（無真人計時不得宣稱實測）、單位與工項適用性、異常壓力、Excel/隔離副本回寫、效能、安全回歸、報告圖表。新增 `docs/EVENING_FULL_TEST_PLAN_2026-11-22.md` 與 `docs/CODEX_EVENING_TEST_RUNBOOK_2026-11-22.md`，識別 `2a-evening-test-matrix-20261122`。

本輪已連接Supabase唯讀查到時點數字：336 Approved合成建案、67字典、104材料、11單位、16案件、47 Run、117952決標列；故先前字典0已過時。現有字典來源與跨測試洩漏資格待驗；不得宣稱自動減少工時已有實證。研究5185僅靜態JSON、正式5184/5264/Supabase不改；**目前尚無任何新實驗結果、研究頁、Word圖表或工時實測**。先做P0並誠實標FAIL/NOT RUN，不把合成工程指標當真實企業準確度。

## 2026-11-22 KDE 獨立研究與報告圖表規格（只立項，尚未跑）

- 使用者明確要求：正式 5184→5264→Supabase 不動，研究另開 5185 靜態比較頁，使用固定離線合成 CSV/JSON、無 DB/正式 API 寫入，不能改正式 Top K、P15/P50/P85、AnalysisRun 或歷史。
- 研究計畫：docs/ALGORITHM_KDE_EXPERIMENT_PLAN_2026-11-22.md；測試報告模板：docs/ALGORITHM_KDE_EXPERIMENT_REPORT_TEMPLATE_2026-11-22.md；識別 kde-isolated-comparison-plan-20261122。A0 既有加權分位、A1 同 reference IDs/權重只換 KDE、A2 等權 KDE、B1 對方階層分群＋KDE，另列價格面積口徑。70%同名義區間為主，90%另比較。
- 住宅既有210/45/45；廠房126/27/27待離線來源驗證。不得將 test 回流調參、不得以合成模擬宣稱企業準確度。實驗後必須輸出真實逐案資料、中文報告 docx/md、PNG 300 dpi/SVG 圖表與可重現 manifest，先A0/A1再後續B1。**目前未執行KDE、未產生數字與圖表、未開5185、未更動正式系統。**

## 2026-11-22 廠房獨立資料已生成（尚未匯入）

識別 `factory-dataset-v1-20261122`；本批僅離線工具、測試與交接，沒有更改應用程式服務、Schema、模型權重或雲端資料。

- 廠房 180 案：RC／SC／SRC 各 60；每結構 42 train／9 validation／9 test，合計 126／27／27。獨立 FAC 前綴 ID，原住宅 300 案及 210 雲端住宅未改；原三份 CSV SHA 與既有 manifest 相符。
- 最終 23,980 筆正量、有單價與金額的基準明細，歷史 CSV 一對一；每案 39–59 種工項。型錄 104 種、實際基準用到 67 種，四種新廠房模板為鋼板屋面、金屬牆板、工業捲門、耐磨地坪，尚未建立雲端主檔。
- 適用性 sidecar 區分 ApplicablePresent、NotApplicableAssumed、OutOfScope；沒有工項不補零。包含有／無辦公區及不同屋面、地下層數等假設；6 個 test 配對缺項／明確零量變體獨立於基準／歷史，不算 6 個獨立建案。
- 新生成器與封裝器都拒絕覆寫既有輸出。Excel exporter 新增 `--complete` 保留所有數量／單位／價格，舊預設缺陷測試仍保留。交付含 6 份完整 Excel、來源／真值／假設／雜湊與僅含 126 train／16,786 列的匯入候選。
- 實測：Python 21/21（新增廠房10項）、API／Parser 專案 Release 0 warnings/errors、6 份 RC/SC/SRC train/test Excel 全部逐列數量／價格／金額／分類讀回一致、ZIP CRC與所有封裝檔雜湊通過。
- 明確限制：數量與價格是未校準情境假設，非真實工程／完整結構合約／已驗證異常真值。未匯入、未核准；雲端實查仍住宅210、廠房0。只可在確認的隔離合成測試環境審核 train 候選，不得把 validation/test/配對變體發布為歷史。
- 資料包 `factory-synthetic-v1-20261122.zip` 已交付對話。詳見 [`FACTORY_DATASET_2026-11-22.md`](FACTORY_DATASET_2026-11-22.md) 與同名 JSON。下一步是確認目標環境、四種新材料對應及匯入／回滾方案，再驗新版 DB/UI；準確度仍最後。
- 初版有辦公區套用過廣及型錄／實際材料數混淆，已在本批發布前修正並重跑；中間輸出不是交付版，以報告最終雜湊為準。GitHub CI 提交後另查。

## 2026-11-22 資料用途與可選工項盤點（廠房尚缺）

本輪只讀 Supabase 與既有 CSV，沒有修改程式／Schema／雲端資料。識別 `dataset-applicability-audit-20261122`。

- 已核對雲端現有 210 個 Approved 合成案全為住宅：RC 78、SC 55、SRC 77，廠房 0。既有來源 CSV 300 案也全為住宅（210 train／45 validation／45 test）。不能把 SC 結構誤稱為廠房用途；廠房估算資料覆蓋尚未完成。
- 鋼骨、模板、土方在住宅決標資料中已存在；啟用新估算不需要重建或覆寫住宅資料。養護劑／脫模劑材料主檔匹配 0，目前只有指定產品規格試算，沒有這兩種歷史基準。
- 100 種標準材料並非每案全有：每案不同材料 63–92 種。這只證明工項有變化，不證明缺項皆合理或工程範圍完整。
- 使用者確認的原則：工項可以依建案適用性不存在；**不適用／確定未施作、未知缺漏、明確數量 0 必須區分**。不得為湊完整度強塞每種材料、把缺項補 0、補隨機正量，或因未出現就判偏低。現有 CSV／DB 沒有完整逐案逐工項適用性真值，缺項仍不能自動認定不適用。
- 廠房應作為獨立用途資料新增，維持獨立 ID／批次／train-validation-test；不能只改住宅案名／project_type 就當廠房，也不覆寫現有住宅與 Holdout。新材料若要歷史模型，另補適用案例及對照面積／產品工法，不重生整批住宅。
- 數字是資料存在案數，尚未套完整單位、來源、範圍及可比性門檻，不等於有效估算樣本或工程真值。此輪沒有生成或匯入廠房，不能宣稱已補齊。
- 詳細實查及材料存在數：[`DATASET_APPLICABILITY_AUDIT_2026-11-22.md`](DATASET_APPLICABILITY_AUDIT_2026-11-22.md)，同名 JSON 保留盤點摘要。後續維持先完成工項／資料範圍，再 UI、回歸整合，最後準確度比較。

## 2026-11-22 大宗工項已接入歷史概算

識別 `bulk-material-range-20261122`；遠端起點 `db40e874`，以含此識別的 Git commit 回查本批。

- 鋼骨（SC／SRC 分開）、模板、土方開挖已有實際歷史區間與待比總量入口，工程估算頁及案件明細皆已接入；不再只有候選狀態。一次明確選一個標準材料，保留來源及排除原因。
- 新估算唯讀採用 Approved、已對應的決標工項；已有歷史交易時交叉核對，沒有舊交易時直接讀決標並標示來源，已刪除／離群不會透過回退重新啟用。使用 RepeatableRead，無 seed／DB 寫入／Migration。
- 同用途、同結構；鋼骨與模板按樓地板面積概算，土方按基地面積並要求同地下層數。這是受設計與工法影響的歷史概算，不是通用工程係數。超出只人工複核、樣本不足不出界限；未開啟新正式紅燈或大宗單價比較。
- 原歷史發布／Run 的鋼筋混凝土白名單不變；新流程不要求重新回寫或重匯資料。權重／半衰期／Top K 設定未改；既有純計分方法公開 static 供重播重用，公式未改。
- 已通過：後端與測試專案 0 警告建置、78 個 Parser 檢核（新增 36）、前端 72 modules 建置、4 種 Npgsql SQL 轉譯、6 個 HTTP 無效請求 400、5 組合成來源重播各 8 案產出區間。這些不是準確度或瀏覽器驗收。
- **未測**：新服務 DB 整合／options（此程序缺隔離 DB 環境），真正瀏覽器操作（無控制工具）。原桌面及 :5263／:5174 未部署。GitHub CI 提交後另核對。
- 下一步先驗新服務 DB 整合及 UI；仍是工項擴充 → UI 驗收 → 修正回歸整合 → 最後 Holdout。不要重生／匯入合成資料或再跑已完成 SRC smoke。
- 詳細 API／修改檔／資料責任／失敗修正／回滾：[`BULK_MATERIAL_ESTIMATION_2026-11-22.md`](BULK_MATERIAL_ESTIMATION_2026-11-22.md)；同名 JSON 保留實測摘要。

## 2026-11-22 最新指示與工項擴充第一批

本節優先於下方較舊的工作順序。識別 `material-capability-phase1-20261122`（起點 `ab565eaa`；以含此識別的 Git commit 回查提交）。

- 最新四階段：**工項擴充 → 真正瀏覽器 UI 驗收 → 集中修正／回歸／整合 → 最後 Holdout 準確度與演算法比較**。既有回測報告為歷史基線，本輪未重跑準確度。
- 能力矩陣新增 8 類工項，檢查材料候選、正式對應、單位、獨立 Approved 建案及缺少工程條件。新候選未經工程條件模型驗證不啟用正式紅燈。
- 新增養護劑及脫模劑之 3 組指定產品規格試算，使用實際施作面積；是廠商理論範圍，不是 P15/P85。超出只提示人工確認，不修改原統計白名單。
- 工程估算／案件明細已加 UI。無 schema／seed／DB 寫入或模型參數變更。另修正文檔誤植權重為實際 `25/30/10/15/10/10`。
- 已通過：後端 0 警告建置、Parser 全部（含 11 新檢核）、前端建置（初次 option 標籤錯誤修復後通過）、Python 11/11、HTTP 規格契約 7/7。
- **未驗證**：DB 能力端點（隔離 DB 環境密碼未提供至本工作程序）、真正瀏覽器全流程（本工作階段無瀏覽器控制工具）。原 :5263／:5174 未部署此批。不可將編譯或 HTTP 通過記為 UI 驗收完成。
- 下一步先啟動本批於隔離 DB 環境核對能力矩陣，再進入完整 UI 流程；不要重匯合成資料、重跑既有 SRC smoke／敏感度。
- 詳細檔案、API、廠商來源、合成候選數及回滾：[`MATERIAL_CAPABILITY_PHASE1_2026-11-22.md`](MATERIAL_CAPABILITY_PHASE1_2026-11-22.md)。

## 2026-11-22：正式range API合成留案實測與參數取捨

以GitHub feature e644edb0為本輪基線，核心模型與a27f850d相同。來源SYNTHETIC v2既有300案、143700行，逐檔SHA與逐列BOQ/history lineage核對；train210/validation45/test45。正式API237組Top8/完整池配對通過（474 range POST），重現參考選擇與區間後，validation比較144組，test只評現行/validation-selected方案。完整設定、hash、分層MAE/MAPE/coverage/relative interval score見HOLDOUT_EVALUATION_2026-11-22.md/json。

正式參數不改：全域validation最佳K32/結構權重組在SRC價格test變差；不以test挑參數。13個paired檔案hash通過、只有3個獨立案且無工程正常/異常核定，Precision/Recall/FPR不可識別。另排除已用於診斷3案提供42案補充觀察。此為回溯合成數值實驗，非企業工程/時間順序預測驗收。

Windows API build 0 warning/0 error、Parser PASS、Python11/11及Node checks PASS。CS8604和MSB3277本機已修；0新增cases/runs/history/settings。原機6/6/部署與完整系統整合仍待驗。

---

## 最新證據補記（2026-11-21）：Windows 工作區修復驗證

- 開發基線 a27f850d；桌面 HEAD dadc7aa，最後審查紀錄仍衝突。獨立 clone 的修復 commit ab32e344ef76f67e8c0134f0592ba5b74f136a1f，不等同桌面已提交或 GitHub 已有該修復 commit。
- 本輪直接執行：Windows API Release build、WorkbookParserTests、Python 3/3、兩個 Node syntax checks 均 PASS。前端標準 build 在 config loader 被 OS 目錄權限擋住；沿用整合副本 node_modules，指定 --configLoader native 後 vue-tsc/Vite PASS。非乾淨 npm ci 證據；保留 CS8604 與 MSB3277 警告。
- HTTP 實測：5263 case15 GET=200、currentVersionId=15；相同版本 quantity-scope GET=404。Docker checker 因 named-pipe 權限失敗，6/6 未完成。全程零 DB 寫入；不是實際工程或模型 accuracy 證據。
- 詳見 CURRENT_HANDOFF 頂部最新狀態；下一驗證仍為使用者權限下啟動新版 5263 並跑 6/6。

---

# 2A 研究證據、版本回溯與實驗追蹤（2026-11-11）

> 目的：新對話不靠模糊記憶，而能用**檔案、命令、Git commit、數據來源、驗證狀態**追回每一步。真實工程的準確率與模擬結果分開報告。

## A. 各證據等級（每次回報必須分類）

- **E1：有使用者完整終端輸出／可重現的結果 CSV／CI 日誌**，可確認程式執行及本輪測試情況。
- **E2：使用者口頭回報「沒有錯誤」**，視為執行未發現錯誤，但不代表結果數值、邊界情況或驗收全部通過。
- **E3：GitHub commit 和程式碼可查**，只確認檔案已提交，不代表執行或效果。
- **E4：研究假設／設計提案／待實作**，不能寫為「完成」。
- DB 查詢皆有「查詢時間」限制，不可假設永久一致；`Approved` 代表系統狀態，不代表可證明材料數量真實、完整、已經過現場驗收。

## B. 研究時間線與可回溯 commit（來源：先前會話操作＋當前分支檔案）

| 時點 | SHA 縮寫 | 交付／目的 | 實際證據 |
|---|---|---|---|
| 2026-10-12～15 | 詳 `docs/WORKLOG.md` | 待偵錯案件、V1/V2/V3、已核准歷史分流、1D 選配、鋼筋／混凝土、結構體價格 | 既有程式與 docs |
| 2026-10-15 | `DEMO_2A_20261015`（資料批次 ID，**不是 SHA**） | 8 件示範歷史案、示範分析，`docs/DEMO_DATA_20261015.md` | 開發演示，不是工程驗證 |
| 2026-11 初 | `36e94c0` | UI 調整「數量偵錯／價格異常」通用名稱 | GitHub 實作 |
| 2026-11-10 | `85bc279`／`318503f` | 常態分布適用性診斷（描述性 CV、偏態；不更動判定）及前端顯示 | 提交，未校準為正式常態模型 |
| 2026-11-10 | `b5b1c5f` | 外部資料研究／公司歷史優先決策記錄 | docs/WORKLOG.md |
| 2026-11-10 | `80fa288` | 全面模型審查（來源、結構、權重、價格基期、分位、回測） | docs/2A_MODEL_AUDIT_2026-11-10.md |
| 2026-11-10 | `053bf749`／`4b7ebc9`／`39df16a`／`a37f800`／`eeeb1e4`／`986065e` | ESS／最大單案權重占比，數量與價格服務及 UI | 程式已提交 |
| 2026-11-10 | `40ff682` | 審查文件補 Run ESS 示例 | DB Run #18/#19 查詢 |
| 2026-11-10 | `7f559e1` | 公開資料集可行性／資料隔離 | docs/2A_PUBLIC_DATASET_FEASIBILITY_2026-11-10.md |
| 2026-11-11 | `94267fb` | `generate.py` 固定 seed 合成六組類型 × 兩材料 | 程式提交 |
| 2026-11-11 | `fd71391` | `compare.py` 小樣本對照 | 程式提交 |
| 2026-11-11 | `e70c73a` | `test_research.py` 初版自動化測試 | 程式提交 |
| 2026-11-11 | `56eab92` | 同建案兩材料共用城市等專案欄位 | 程式修正 |
| 2026-11-11 | `40d169f`／`eac75b8` | 分位數精確索引 bug 修正及回歸測試 | 程式提交 |
| 2026-11-11 | `474fc03` | 模擬研究第一輪結束，.gitignore 排除輸出 | 已拉取／可查 |
| 2026-11-11 | `75bf7d5` | 參考案抽樣改確定性 SHA256，避免固定前幾筆偏差 | 程式提交 |
| 2026-11-11 | `8e6b7b2` | `batch.py` 多種子／情境／樣本數／模型 | 程式提交 |
| 2026-11-11 | `7f37a5e` | 批次穩定性測試 | 程式提交 |
| 2026-11-11 | `3986920` | 批次 README 及第二輪整體提交節點 | **使用者實測** `5760 group runs, 96 summary rows` + 原有 **7 tests OK** |
| 2026-11-11 | `ee81ea0` | `production_weight_review.py` 重現 6 因子、結構 gate 對照 | 程式提交 |
| 2026-11-11 | `98e1757` | 結構 gate、分數、批次可重現等新增測試 | 程式提交 |
| 2026-11-11 | `68e8e45` | 第三輪模擬權重比較文件 | 使用者之後**回報「沒有錯誤」**（未附 CSV 結果） |
| 2026-11-11 | `aa0ec994` | 最新主交接檔 | 本交接工作 |

如 GitHub SHA 檢索與上表摘要有差異，以實際 `git show` 的變更為準，不要猜 commit 內容。GitHub 個別檔案可追歷史與 Blame，未儲存的本機輸出不會出現在 commit。

## C. 已有實驗的重跑與資料位置

先 `git status`，確認目前分支與修改狀態。研究使用 Python 標準函式庫（無需安裝 NumPy）。

```powershell
# 原第二輪，已在 2026-11-11 由使用者完整確認成功：
python research/2a_synthetic/batch.py --first-seed 20261111 --seeds 10 --cases-per-combo 30 --train-sizes 3 5 8
# 原輸出：Simulation-only benchmark completed: 5760 group runs, 96 summary rows

# 第三輪，使用者口頭確認「沒有錯誤」，CSV 尚未分析：
python research/2a_synthetic/production_weight_review.py --first-seed 20261111 --seeds 5 --cases-per-combo 30 --train-sizes 3 5 8

# 測試（原 7/7 有完整成功記錄；新增測試的總數要以最新輸出為準）
python -m unittest discover -s research/2a_synthetic -p "test_*.py" -v
```

本機 `research/2a_synthetic/output/` 是 **gitignore 排除的產生檔**。要新對話精準銜接，請保留或傳入：`batch_summary.csv`、`batch_detail.csv`、`batch_manifest.json`、`production_weight_summary.csv`、`production_weight_detail.csv`、`production_weight_manifest.json`。

**重跑不等於新貢獻**，同 seed 相同程式理應重現結果；只有模型或資料修改才需要重新做全面回歸。

## D. 最重要的錯誤與風險登記（追蹤而不是暗改）

| ID | 風險 | 狀態 | 何時可以結案 |
|---|---|---|---|
| R-01 | DB 裡 Approved 仍含示範案（7 件名稱指向 DEMO） | **未結案** | 正式來源欄位和資料隔離、清理方案經明確驗收 |
| R-02 | 歷史 RC／SC／SRC 只扣相似分數、未強制相容 | **研究中** | 模擬 + 真實不同材料／不同用途可比性審核 |
| R-03 | 權重 25/20/10/10/0/5、TopK 8、半衰期 3 年主觀 | **研究中** | 有留案誤差／敏感度對照與清楚採用理由 |
| R-04 | 資料不足，只有 20 件原始企業案，分類後更少 | **未解** | 至少確認每案每材料完整性與可比性；資料不足時保留人工審核 |
| R-05 | P15～P85 不保證工程合理上下限／校準涵蓋率 | **未解** | 同目標覆蓋率公平比較 + 真實留案檢驗 |
| R-06 | 歷史價額未統一價格基期 | **未解** | 同口徑價格資料與適用指數驗證 |
| R-07 | 模擬數量／虛構企業差異可能自我證實研究結論 | **已標注、持續注意** | 使用多情境／真實資料交叉比較，不把模擬結果當實務準確率 |
| R-08 | 演算法回測可能使用未來年份資料（對稱 abs） | **未解** | 新增時間前推、不看未來案例之 holdout |
| R-09 | 公開資料集真實用途與授權未完成實檔驗收 | **未解** | 下載實檔、欄位／口徑／授權／年份核實 |
| R-10 | 研究 Python 與 .NET 正式公式可能不完全等價 | **待驗證** | 建立相同輸入的等價測試，記錄差異 |
| R-11 | 研究區間定義不同（normal ±1SD vs weighted P15/P85） | **待修** | 同等目標涵蓋率／相同實驗樣本公平比 |
| R-12 | 產學公司資料加權尚未實作 | **待研究** | 企業-only、external-only、部分池化對照，不能任意設倍率 |

## E. 跨對話的可追溯協議

每一輪要在 `docs/WORKLOG.md` 或獨立研究紀錄記錄：
1. 日期、任務目的、欲挑戰的假設與前一輪根據。
2. 變更路徑、commit SHA、精確執行命令、環境／seed。
3. 輸入來源與權利（real-company / external-public / synthetic），含版本／授權、樣本數與條件。
4. 實際輸出檔及 SHA256／成功或失敗（只貼 console 無 CSV，不可自行推斷 MAPE）。
5. 結果（同時記優勢與失敗、區間涵蓋、ESS），與資料生成假設的限制。
6. 接受／拒絕／待驗證決策及可回退 commit，不得只保留最成功的一組。
7. 下一個具體驗證點，避免新對話重做既有的實驗。

若研究報告想宣稱**跨真實工程準確率**，一定要有真實 holdout 和資料完整性審查；模擬實驗再多仍不能替代。

## F. 新對話與環境安全

- 分支固定 `feature/1d-api-integration`；勿擅自合 main。
- 不使用 `git reset --hard`、`git clean -fd` 等破壞性命令，可能有個別未提交修改或本機輸出。
- PostgreSQL／Supabase 查詢需先確認目標專案及 DB；執行研究無需讀取或修改 DB。
- `output/` 有完整可重建資料才可要求使用者重跑；若要復原原始輸出，使用精確 seed+commit。


## G. 2026-11-11 新增 Handoff Guardian 追蹤制度

- Skill：`.agents/skills/waizheng-2a-handoff-guardian/SKILL.md`，識別名 `waizheng-2a-handoff-guardian`。
- 操作指令：`AGENTS.md`；機械檢查器：`scripts/check_handoff_review.py`；審查記錄：`docs/HANDOFF_CHANGE_REVIEWS.jsonl`；GitHub PR gate：`.github/workflows/2a-handoff-review.yml`。
- 每輪必記：起始 SHA、修改路徑、是否更新交接、真實更新文件、原因、實際驗證。GitHub 的每檔提交 SHA 可透過 change_id 與前後版本重建變動。
- **審查紀錄不會自動證明結論正確**；Python script 只檢查 changed_paths 和審查 JSON 新增行的一致性，人工仍要判定交接內容是否準確。PR CI 只在符合事件條件時執行，不涵蓋所有直接 push。
- 當次建立治理文件屬 E3（GitHub 程式已提交）；若未見本機命令或 GitHub Actions log，不得宣稱 E1 gate 已通過。


## H. 2026-11-11 第四輪：本機第三輪權重 CSV 實證報告

- 新增 `research/2a_synthetic/report_weights.py`；新增 `test_research.py` 的「實際彙總轉報告」及「不完整配置拒絕」測試。
- 資料來源是本機 `production_weight_summary.csv`，並要求 manifest 標明 `SIMULATED_RESEARCH_ONLY`；沒有可讀結果檔就不能推論模型表現。
- 明確輸出 `production_weight_report.md`（本機忽略 Git）；可透過固定亂數種子與同一 Git 版本回溯，或由使用者上傳報告／資料。
- 狀態：GitHub 已提交研究工具（E3）；**第三輪實際數值尚未取得／分析，新增測試待本機輸出確認**。不能把這份腳本提交當作真實工程統計結果。


## I. 2026-11-11 第三輪結果收到與第五輪因子拆解

- 使用者正式上傳本機 `production_weight_report.md`（第三輪、72 筆三設定比較）：以報表 24 個已四捨五入的 MAPE 行平均 A 17.10%、B 17.07%、C 16.91%。改善約 0.03／0.19 個百分點；跨結構參考 A 平均 0.18%，B/C 0%；報告記載 B 18/4/2、C 20/4/0 改善／惡化／持平。**這是 E1 的輸出結果文件，不是原始 detail CSV，也不是實際建案工程驗證。**
- 審查報告 `docs/2A_SYNTHETIC_WEIGHT_RESULT_REVIEW_2026-11-11.md` 記錄計算方式、三方案的混雜因素以及因為第三輪同結構樣本充裕而未測出風險。
- 新增 `research/2a_synthetic/factorial_weight_review.py`，擴充 `production_weight_review.py` 的呼叫端可注入六方案設定，不改原 A/B/C CLI；`test_research.py` 新增對照設計與配置完整性測試。
- 新的 2×3 配置能分離硬門檻效果與兩種基地面積權重調整方式，但仍依賴人為合成工程，且同結構資料尚未稀疏化。下一輪要再測少樣本及時間前推。
- **目前驗證狀態**：上輪使用者 PowerShell 11/11 OK，新增第五輪腳本／測試尚未得到用戶本機輸出；不能將 Git commit 當成已實驗通過。


## J. 2026-11-11 第五輪已驗證、第六輪稀疏樣本壓力實驗

- **第五輪（E1、使用者終端完整證據）**：`factorial_weight_review.py` 執行成功 4320 group runs、144 summaries、192 paired rows；`python -m unittest discover -s research/2a_synthetic -p "test_*.py" -v` 實際 13/13 OK；`python scripts/check_handoff_review.py --base 2acf10d` 顯示 9 files / 1 new audit record、handoff updated。原先 2160 的前瞻估算已修正為 4320。
- 尚未取得 `factorial_weight_report.md` 和配對原始 CSV，因此此時**不能**斷言在控制因素後哪種方法誤差較低。
- **第六輪**：新增 `sparse_structure_review.py`，以 0～3 件同結構資料、不同結構候選多的反差場景，評估無 gate 與有 gate 的名義最低 5 件門檻、技術上可算性、跨結構混入、模擬誤差。新增單元測試。此為刻意壓力情境，其他結構參考數量**不是**崴正歷史樣本實況。
- 目前 E3：提交程式與文件；第六輪實際通過與輸出內容待使用者本機證據。不可聲稱樣本稀少時安全放行任何正式異常燈號。


## K. 2026-11-11：第五／六輪本機結果已收到

- 使用者上傳 `factorial_weight_report.md` 與 `sparse_structure_report.md`，屬 E1：可檢視**模擬研究輸出報告**，不是原始 detail CSV，更不是企業實務工程驗證。
- 原始報告要點及限制已固化 `docs/2A_FACTORIAL_SPARSE_RESULTS_2026-11-11.md`。第五輪部分配對改善約 0.1～0.18 MAPE 百分點，但不保證真實增益；第六輪少同結構樣本時雖然無硬門檻顯示 100% 達五筆名義門檻，參考案最高 100% 不同結構，顯示數量門檻與可比性門檻需分開審查。
- **不得宣稱** 73.82%／47.90% 代表崴正真實工程錯誤率；模擬生成器設置結構不同用量基準，研究設計還特意提供大量其他結構候選；報表不含各 seed 完整差異。
- 此次僅完成分析、證據與交接文檔（E3 GitHub commit），沒有變更 C# 生產服務、正式設定或 DB。下一研究方向為可比性／資料資格檢查和跨結構差異敏感度的獨立實驗。


## L. 2026-11-12：可比性／歷史資料來源資格研究

- 使用者提供 2026-11-12 PowerShell 完整輸出，`git merge 705efe3..cedf7df` 成功，`check_handoff_review.py --base 705efe3` 顯示 **Reviewed 6 files with 1 new audit record**；屬 E1 交接機械完整性驗證。
- 新增 `research/2a_synthetic/eligibility_review.py`（研究-only），把來源真實性、同用途、同結構、材料、標準單位、工程範圍與 Approved 審核分開列拒絕原因。
- 所有生成資料都帶 `source_kind=synthetic`；`hypothetical_eligible` 只用於隔離其他規則的研究影響，**不會授予任何假資料正式可用資格**。
- 新增三項測試：全 synthetic 都不得成為生產基準；同案、自身與多錯誤防護；結構限制調整不會繞過來源驗證。**本輪新增的 Python 程式與測試尚未在使用者本機執行；不得記成 E1。**
- 這些研究欄位並不證明真實 DB 有工程範圍或驗證來源欄位；日後若要對接生產，需先唯讀檢查資料表結構、資料品質及使用者授權。


## M. 2026-11-12 第七輪結果與正式資料模型缺口

- **E1 合成研究輸出**：使用者上傳 `eligibility_report.md`，35 筆候選、hypothetical_eligible=1、production_eligible=0，25 筆 `wrong_structure`、35 筆 `unverified_source`；不是資料庫實際來源查核。
- **E3 靜態欄位對照**：查閱現行分支 `崴正/Models/ProjectMaster.cs`、`FinalBidItem.cs`、`HistoricalTransaction.cs`、`MaterialMaster.cs`、`UnitMaster.cs`、`崴正/Data/ApplicationDbContext.cs`；結果在 `docs/2A_REFERENCE_ELIGIBILITY_MAPPING_2026-11-12.md`。
- `source_kind` 與 `scope_quality` 是**模擬研究欄位**；在已讀實際 ProjectMaster 等模型中未見等價的可靠資格標記，不能斷言 DB 絕無其他欄位。
- 新增 `research/2a_synthetic/reference_schema_readonly.sql` 供唯讀檢查資料庫 schema；**尚未實際連線執行，不可稱為 DB 驗證通過**。本輪無 C#、DB、正式參數變更。


## N. 2026-11-12：真實六張表 86 欄 Schema 證據

- **E1（使用者本機 SQL editor 輸出）**：上傳 `information_schema.columns` JSON，逐筆包含 table_name、column_name、data_type、is_nullable；實際 86 列，涵蓋 `project_master` 17、`final_bid_item` 27、`historical_transaction` 8、`material_master` 5、`unit_master` 7、`estimation_settings` 22。這是 Schema 存在性驗證，**不是資料列或資料來源的證實**。
- **特別缺口**：六張表內沒有正式來源真實性證明／工程範圍完整性的專用欄位；`ProjectMaster.HistoryStatus=Approved` 不代表完成實體標單鑑真。ProjectMaster 類型、結構、樓地板面積 nullable；HistoricalTransaction 對 ProjectMaster/FinalBidItem 的參考可 null。
- 建立 `docs/2A_VERIFIED_DB_SCHEMA_GAPS_2026-11-12.md` 保存經核對的資料表與限制；建立 `reference_quality_counts_readonly.sql` 作為下一步**未執行**的聚合式歷史品質查核。查詢不得讀取個別建案、檔名、hash、標單內容。
- 過去文件中的 2026-10-15「0 案」為當時快照；**本輪沒有 COUNT 查核，不得沿用成今日結論**。此輪只修改 research/docs，不涉及正式資料庫 DML/DDL。

## O. 2026-11-12：Q4 獨立 Approved 材料建案數（使用者實際 DB 聚合）

E1：使用者貼出 Q4 兩列：住宅 RC、material_id=1：8 獨立建案／11 交易／11 linked bid items；material_id=2：8 獨立建案／10 交易／10 linked bid items。SQL 限定 Approved、未刪除、非 Outlier、正數量。**未能確認來源真實、各工項外鍵品質或兩個 material ID 的名稱**；其他用途/結構未出現在這份有效歷史材料 Q4 聚合。Q1～Q3 結果尚未收到，不能當成已檢查。

新建 `docs/2A_APPROVED_MATERIAL_COHORT_2026-11-12.md` 與只查聚合、不揭露檔名和建案名稱的 `research/2a_synthetic/reference_material_source_audit_readonly.sql`。只提交未執行；先前 DEMO Approved 風險仍未結案。


## P. 2026-11-12：Q1–Q4 真實資料品質完整證據

E1（使用者直接貼出 SQL 四組聚合）：Q1 10 件住宅 RC（8 Approved、2 Draft，未刪除皆 0）；Q2 8 Approved 中 **7 件缺 source_file_hash**，其他被查欄位缺漏均為 0；Q3 23 歷史交易，標記刪除 2、連到非 Approved／已刪除建案 2，全部 FK 有值且有效、工項不跨案；Q4 材料 ID=1／2 各涵蓋 8 個獨立住宅 RC Approved 專案，分別 11／10 筆有效交易。**Q3 兩組 2 件可能重疊，尚未證實交集。**

歸檔 `docs/2A_HISTORICAL_DATA_QUALITY_Q1_Q4_2026-11-12.md`；補強 `reference_material_source_audit_readonly.sql` Q8，供同一 SQL Editor 唯讀交叉檢查。仍未取得 Q5～Q8 輸出、material_id 對應名、正式來源證明、工程範圍認證、實際可用真實公司歷史案數。未動 C# 或 DB。不得把 DB Approved 數推論成真實材料資料集。


## Q. 2026-11-12：Q5～Q8 使用者真實 DB 聚合完成

E1：Q5 確認材料 1=鋼筋 kg、2=混凝土 立方公尺；Q6 8 Approved 中 7 件名稱疑似展示／測試，1 件未核實；2 Draft 中 1 疑似、1 未核實；Q7 鋼筋 7×1+1×4=11 行、混凝土 7×1+1×3=10 行，與 Q4 一致；Q8 刪除的 2 行恰好都是連非 Approved/刪除建案，**不存在四筆不同異常**。Q2 7件缺hash與Q6 7件疑似Demo交集仍未知。詳細數據固化於 `docs/2A_HISTORICAL_DATA_QUALITY_Q5_Q8_2026-11-12.md`。

**來源認證現況**：此輪未取得原始完整定案文件、真實工程範圍核證，故「目前已證實真實完整的來源數」為 0 件，但不能推論只有 0 件真案；單憑檔名不能確認是假案。Q1～Q8 數據盤點已完成，下一步是來源驗證流程設計，不再盲目微調權重。


## R. 2026-11-12：Verified Reference 規格草案（設計 E3，非實作）

- 使用者已確認上輪 GitHub 更新／Guardian 命令「沒有錯誤」；這是操作回報，並未提供另外完整終端截圖，不能當做真實 DB 來源人工審核證明。
- `docs/2A_VERIFIED_REFERENCE_DESIGN_V1_2026-11-12.md` 新增獨立來源資格與 per-purpose（鋼筋／混凝土／結構體價格）審核設計，Draft/Approved 業務發布流程不變，舊 Approved 暫不取得 Verified Reference。狀態、角色、稽核、重開、撤銷、API、資料模型與 VR-01～VR-14 測試均屬**提案**。
- 依據：Q1～Q8 真實 DB 聚合，8 Approved 但 7 名稱疑似展示、1 來源尚未核實；不得由此斷言已有 0 件真案，只能說已提供證據中尚無完成鑑真的真實完整標單。不能據此擅改生產查詢。
- 研究下一步：對照最新 C# Published History／權限／模型查詢實際實作，建立變更影響清單並由業務窗口確認；這批只改文件，無程式／DB／模型驗證。


## S. 2026-11-12：Verified Reference 接入路徑審查（E3 靜態證據）

已直接閱讀目前分支後端程式碼，詳細文件 `docs/2A_VERIFIED_REFERENCE_INTEGRATION_AUDIT_2026-11-12.md`。有兩個需先處理的 P0：`Program.cs` 無內建 Authentication/Authorization 中介設定且 Projects/Audit 等敏感 Controller 無 `[Authorize]`；ProjectSimilarityService 以 int ProjectId 取候選的 overload 缺 Approved 限制，而 DTO overload 有 Approved。需辨別一般展示與正式模型使用，不能盲目改邏輯。

另外證實 Publish/Reopen 已採交易與冪等識別，數量模型依 ProjectId 聚合合格交易，價格模型依 FinalBidItem 工程大項彙總，正式最少樣本依 EstimationSettings 判定。**本輪靜態查核，不等於實際部署安全驗證、執行測試或新 Verified 功能上線**。下一步先完成部署身份機制與前端接入點清單、業務審核政策核准。


## T. 2026-11-13：前端與 Nginx 靜態入口查核

E3：從 GitHub 開發分支讀取 `frontend/src/services/api.ts`、`frontend/src/router/index.ts`、`frontend/src/App.vue`、`ProjectDetailView.vue`、`ProjectsView.vue`、`AuditLogView.vue`、`AnalysisCaseDetailView.vue`、`frontend/nginx.conf`、`frontend/Dockerfile`、`崴正/Program.cs`。分析已收錄 `docs/2A_VERIFIED_REFERENCE_FRONTEND_SECURITY_AUDIT_2026-11-13.md`。

核對到：Vue 無角色/登入 route guard，側欄直接可見 AuditLogs；`api.ts` 未設 token，Nginx 配置只有 proxy_pass 而無 auth_request／allow/deny；ProjectDetailView 使用 Idempotency-Key + receipt 防失敗重送；分析頁已做 DEMO 名稱警告。部署主機、外部 Gateway/VPN/ACL 不在 GitHub 範圍，**本輪沒有做實際對外 API 測試、登入測試或建置測試**，不得推論正式服務一定對公眾可達。Verified Reference 仍為未核定設計，未做 API/DB 變更。


## U. 2026-11-21：UI 資料鏈相依修復與測試可信度界線

- **E1 使用者實測：** 7/7 5263／5174 API 預檢 PASS，4/4 Docker/SQL/API 唯讀比對 PASS，14 個案件、14 個版本、28 筆 Run、210 個歷史建案；只證實可查與讀回，未證實新 UI 上傳、模型結果準確或真實公司資料資格。
- **E1 失敗紀錄：** `prepare_ui_case.py` 執行遇到 `ModuleNotFoundError: No module named 'export_full_project_excel'`；後續 `ENOENT` 為沒有產生 XLSX 的結果。保留失敗證據，不應算 parser 缺陷。
- **E3 程式修正：** 本分支加入 main 已有來源驗證與 test-only XLSX 產生器及相依的 OOXML 產生器（`97558ce`、`3235696`），並建立 `test_prepare_ui_case.py`（`0aca507`）驗證 XLSX/JSON 產出、來源完整性、test-only 及拒絕覆寫。
- **E3 CI 治理：** 更新 `2a-build.yml`（`9f68b3a`），刪除重複的 `graduation-ui-build.yml`（`71738d9`），不修改量價模型或 DB。**CI 結果須查 Actions 實際狀態，不得只憑提交宣稱通過。**
- **未測：** 本地重新產出新 Excel、透過 UI 逐筆上傳／生成 AnalysisRun、正式歷史回寫與 Audit；不得把 holdout 加入 Approved 當新訓練歷史。後續準確度仍待獨立真值。


## V. 2026-11-21：UI 測試標單來源缺失的正式消除方案

- **E1 失敗證據**：使用者 Windows `test_prepare_ui_case.py` 2/2 PASS，產生器因 `graduation-src-extreme-v3/manifest.json` 不存在而失敗；預檢的 `ENOENT` 為連鎖錯誤，Excel 真正預覽未執行。
- **E3 提交修正**：`prepare_ui_case.py` 新增省略 `--source` 的獨立 UI smoke 工作簿，完全不需要大型來源 CSV/manifest，並保留原 SHA/test split 模式。資料包含示意鋼筋、混凝土、模板、鋼骨與基礎工項，數量／單價只是操作測試值，**不參與模型訓練，也不是獨立 holdout 的準確度真值**。
- **E3 永久回歸**：`test_prepare_ui_case.py` 新增真正 subprocess 執行 CLI 的測試和 XLSX SHA/ZIP、逐列 amount 及 metadata 安全條款；利用既有 `2a-build.yml`，不再新增臨時 CI。
- **未驗證**：使用者本機修正版匯出 Excel／HTTP preview+inspect、瀏覽器建立案件與 Run、決標回寫／Audit。


## W. 2026-11-21：Windows Python 子程序 CP950 與 UI 實際 Excel 9/9

- **E1 使用者實測 PASS**：已建 `graduation-ui-smoke-1121-02.xlsx`＋truth JSON，SRC 3 sheets、15 工項。5174/5263 唯讀 API 實際 9/9 PASS，Excel preview 3 sheets (2 detail)、inspect 15 rows/4 warnings；原有 4/4 隔離 DB 14 案／28 runs 已另有實際證據。**不可**把 9/9 當作 UI 真實上傳、審核或模型準確率。
- **E1 使用者實測 FAIL**：Python 3.12 Windows `test_offline_standalone_ui_fixture_without_source_dataset`，子程序使用 CP950 而父用 UTF-8 解碼，發生 `UnicodeDecodeError` 及 `TypeError`，3 項測試中 2 PASS / 1 ERROR。錯誤與已成功生成的 Excel/API 無因果。
- **E3 代碼修正**：`test_prepare_ui_case.py` 對 subprocess child 顯式使用 `PYTHONIOENCODING=utf-8` commit `79af4b822`，父 decoder UTF-8 不變；`verify-ui-state.mjs` 擴充 `--case-id` 嚴格 synthetic case + same-version Quantity/MajorPrice Runs 與 payload GET，commit `d6b3fb146`。兩者僅修改永久驗證工具，零 DB schema、model、授權/標準範圍變更。
- **仍待**：Windows Python 回歸 PASS、瀏覽器實際自行上傳並保存 2 Runs、跨刷新成功、決標審核與稽核 E2E。4 warning rows 必須讀實際警告內容，不臆測工程缺陷。


## X. 2026-11-21：Case #15 UI 成功與工項排除統計對照
- **E1 使用者截圖**：17? 不採猜測。正式證據為 `/analysis-cases/15` 的 V1 自行上傳 **15 條**、價格分析 `Run #31` 已顯示保存成功，實際大項總價 **29,268,000 元**／每坪 **8,062.81**，相對人工合成歷史區間顯示 Low；數量分析顯示鋼筋 **38,000kg**／混凝土 **3,650m³** 並列出各 8 個參考案。尚未用 `verify-ui-state --case-id 15` 對照 DB 保存 payload，故「API 重新讀回 5/5」**未驗證**。
- **E3 原始 fixture／分類程式核對**：結構體原始鋼筋共 130,000kg，混凝土共 5,450m³；`ClassifyQuantityCandidate` 將前 3 條鋼筋加工、組立、綁紮／工資與第一條預拌混凝土澆置標為 `Excluded`，合計被排除 92,000kg、1,800m³，符合 preflight 的 4 警示；量估納入 38,000kg/3,650m³。價估計所有結構體 10 行共 29,268,000 元，與畫面一致。只是關鍵字分類策略，**不代表已證實複合項目無材料，也不代表工程數量/價錢推估有外部真實有效性**。
- 下個驗證點：對已存在 #15，執行永久 script `node scripts/verify-ui-state.mjs --case-id 15`（read-only），再按歷次偵錯查看刷新保存；分類邊界／排除理由 UI 顯示列為待查，不改模型閾值或資料庫。
