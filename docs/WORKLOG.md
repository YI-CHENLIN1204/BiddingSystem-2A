## 2026-11-23 十二項研究最新驗收與群數敏感度

[研究 Run 45118159832](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45118159832)，測試提交 `1ae2f6b2bb695762a738ad4af9249274f32289f5`，三 Job SUCCESS，交接守門 PASS。實際下載 API Artifact 14791959532（ZIP `d7258b9c4909ef9a02feeb1923233f63046e00a09d59a43e3aa0a3809b847771`），成員 5/5 SHA 通過。95 筆 HTTP JSONL，93 通過、0 失敗、2 統計摘要。T07 完整隔離上傳案歷史 0→2→0 與主管稽核讀回通過；T09 四種角色 401／401／403／200，隔離通過。T08 建案與兩列 Summary 各 n=30，p50／p95 為 14.78／17.99 ms 和 24.48／37.39 ms。教授版 Word/PDF Artifact 14792507357 的 11/11 成員雜湊通過，7 頁與 8 圖已檢視。驗證結果與較晚完成的群數敏感度見報告文字補錄。

群數 3／4／5 的較早開工 B1 重建已輸出 468 筆逐案與 18 格摘要。70% 結構金額共同 n=17，分數 1.675／2.090／2.343，可估 19／18／18（母體各 26）。同一批合成測試的探索性分析維持預先凍結的 4 群，任何變更須用獨立資料驗證。T03 分析器正反軟體案例通過，人體實測 0。正式 2A、A0、Supabase 未修改。

## 2026-11-23 T07 完整隔離流程與證據封存

[Run 45117875966](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45117875966) 三 Job SUCCESS，測試提交 `bfd8fd18e772f63100459939279880a294ec93e9`。同一上傳案兩筆人工映射，發布後兩筆有效歷史，重開歸零，主管讀回 ImportFinalBid、兩次 UpdateMapping、PublishHistoryBaseline 與 ReopenHistoryReview。T09 隔離角色矩陣仍通過。下載確認 API ZIP SHA 相符，但內部 server.log 成員雜湊因關機寫入競態不符，修正為等待程序停止再制雜湊。重跑驗證封存。Word/PDF 報告 Artifact 14792546630，11 個成員雜湊通過。T02 補 49,210 筆七種字典規模觀測的 252 組合成結構／場景分層；跨真實來源仍待。

## 2026-11-23 廠房重播與隔離匯入補測

取得實際重播與三輪 RSS。離線 A0 p50／p95 0.298／0.301 秒，峰值 58,932 KiB。A1 含 A0 與 validation 為 0.793／0.821 秒，59,212 KiB。B1/B2 含兩級區間及消融為 11.914／11.999 秒，25,964 KiB。各 n=3，p95 是 nearest rank 最大值，工作量不同。T03 已產出兩張同結構、各 167 列和 58 材料的去重合成練習標單，真人仍 NOT RUN。

[Run 45117656645](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45117656645) 的報告與交接守門通過。隔離匯入實測兩筆手動映射成功，但僅一筆符合發布白名單，原本期待兩筆的斷言失敗。正式範圍以主檔「混凝土」為準，修正隔離 fixture 的名稱後重驗，既有正式邏輯與兩筆判準保持。發布後讀回及重開需以後續 Run 為準。

## 2026-11-23 T00～T11 原始驗收補測

基準 `078f173df2f99e54e36e14aaf95ad2f4733423c2`。取得原始廠房合成包並驗出 180 案、23,980 筆明細／歷史交易、126／27／27，逐檔及明細配對一致。重算廠房字典、規格與單位負例、TF-IDF、A0/A1 70%／90%、B1/B2、K5／K8 與 bootstrap。逐案證據、外部阻礙、重現方式見 [T00～T11 研究驗收](T00_T11_RESEARCH_CLOSEOUT_2026_11_23.md)。T03 完成 AB/BA 執行與分析流程，真人仍 NOT RUN。新增實際匯入案的隔離發布／重開回歸及教授版報告渲染；新 Actions 結果另核實。正式 2A、A0 與 Supabase 未修改。

## 2026-11-23 T00～T11 補測 Run #4 完成（研究隔離，原計畫仍未結案）

新 [GitHub Actions Run #45115813539](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45115813539)，測試提交 `cf6465ef21e65456765b182ee1d7d902715f10ac`，三個 Job 全部 SUCCESS。原始 `isolated-api` Artifact [#14791179321](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45115813539/artifacts/14791179321) 的 **5/5 SHA256SUMS PASS**、87 筆 HTTP JSONL（85 筆 `passed=true`、0 `passed=false`、2 統計摘要）已實際下載核對。T07 **真實 multipart 合成 Excel 上傳路徑** PASS：雙工作表錯選 400、正確選表上傳 200、兩筆量價工項、比價工作表排除、Draft 不產生歷史交易、重複檔案 SHA 409。T08 新增匯入建案 `/2a-summary` HTTP n=30，p50 **28.33ms**、p95 **34.28ms**；舊 `/api/projects/{id}` 讀取 n=30，p50 **15.69ms**、p95 **19.21ms**。不是完整 KDE 估算 API 和獨立工作負載評估。T04/T09 及 Word 維持前 Run #45057125296 的 PASS。

另經 Supabase 唯讀來源分組，正式資料庫現有 **210 筆住宅 Approved** 皆標記 `SYNTHETIC_V2_PROJECTS.csv`、**126 筆廠房 Approved** 皆標記 `FACTORY_20261122_V1_TRAIN`。Approved 是狀態欄而非真實性證據；這 336 案 **全部是合成來源，不得作為外部真實住宅／廠房盲測**。本輪 Google Drive/Libraries 也沒有取得具獨立最終決標量價、合法授權與人工核定真值的可用原始包，詳見 [資料適格性](T00_T11_SOURCE_ELIGIBILITY_2026_11_23.md) 及 [Run #4 原始驗證](T07_T08_RESEARCH_RUN4_2026_11_23.md)。

**原 T00～T11 仍非全部 PASS**：真實跨來源標單、住宅 frozen manifest、工程師真值、真人 AB/BA、作者 B1 源碼或明確重建聲明、完整版效能驗證仍是阻擋條件；不能把已成功 CI 變造為獨立外部研究驗收。所有修改僅研究程式/交接；**正式 2A、A0、Supabase 未修改**。下一輪請集中取得這些確實缺少的外部輸入，避免重跑已成功的合成 CI。

## 2026-11-23 T07 真實匯入路徑與 T08 2A 摘要效能待驗

為推進原始 T00～T11 全完成、而非重跑現有 PASS，研究分支新增純合成雙工作表 Excel 的隔離 API 回歸：`research/acceptance-ci/Program.cs` 生成測試活頁簿；`research/acceptance-ci/probe-api.py` 以實際 multipart HTTP 測選錯工作表 400、明確選表匯入、兩筆量價有效工項、Draft 不進歷史、隔離回讀、同檔 SHA 重複 409、`/2a-summary` 及 30 次 API 計時。**本次程式變更尚待新的 GitHub Actions Run 執行，不能標 PASS；**既有 Run #45057125296 的 T04/T07 核心/T09/Word PASS 保持不變。只動研究驗收腳本和文件，正式 2A、A0、Supabase 無改動。原始住宅快照、授權獨立最終標單與真值、工程師標註、真人 AB/BA 仍是全項結案必要輸入；現有雲端檔案的唯讀資格盤點詳見 [來源適格性](T00_T11_SOURCE_ELIGIBILITY_2026_11_23.md)，沒有把私人檔案加入公開 GitHub。

## 2026-11-23 Run #3 實測及 Word 八頁目視已核實

[GitHub Actions Run #45057125296](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45057125296) 對應 `da4324596707969ee805a37f68e284a0c0f955d0`，三 Job 均 SUCCESS。隔離 API Artifact `14767619273` SHA256SUMS 4/4、48 JSONL（47 passed=true，30 HTTP 讀取）核對通過：T04 EF PASS、T07 Draft／人工映射／Publish／Reopen／Audit PASS、T09 匿名 401／無效憑證 401／Reader 403／Supervisor 200，主管可讀回三類實際稽核事件，研究隔離 API+DB E2E PASS；T08 讀取 n=30，p50 19.79ms、p95 35.71ms。**實測範圍是隔離 Fixture 與研究授權 Overlay，不等於正式 2A 已部署相同權限**，T07 仍缺上傳建檔，T08 仍缺完整估算效能。

Word Artifact `14768340468` 下載成功，原 DOCX SHA256 `d9fb05c1e4b83422a257f2fb3d3e9ed3d8abbbc275265311b931d511ee2f9ccf`，新 PDF SHA256 `d7349cd99a85b5fe1f86109245ad4bdaa07b28258537091d670775dc04ae25f9`，ZIP 內 10 個 SHA256SUMS 成員全部 PASS。**已逐頁目視 8 張 PNG：表格續頁表頭、孤行、頁碼問題解決，圖 1–8 與中文字完整，版面視覺 PASS**；圖表頁留白較多屬非阻擋改善。詳見 [逐頁](WORD_VISUAL_REVIEW_2026_11_23.md) 和 [驗收查核](RESEARCH_ACTIONS_RUN_VERIFICATION_2026_11_23.md)。此回合沒有修改正式 2A、A0 或正式 Supabase。T00–T11 **全項仍未結案**，下一步需外部工程資料盲測、工程師獨立真值、真人工時，以及剩餘 API／工料整合驗證。

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

## 2026-11-22 — 集中完成合成數值回測、參數對照與兩項警告修復

- 依使用者重新指定的三工作包，直接讀既有source/holdout與原機API，未再seed、上傳13案或跑已完成的來源/敏感度smoke。
- 正式API237個project/scope之Top8與完整池共474次運算全部核對；144組只用validation選參數，test另評，輸出分層coverage/MAE/MAPE與interval score。SRC價格不支持全域換K32/新權重，保留原設定。
- 檢查13份paired truth/hash，確認manual-review標記不提供工程正常/異常真值，Precision/Recall/FPR留null。資料實際是45validation+45test；保留三個曾用於診斷案件的揭露及排除後補充結果。
- 新增長期唯讀live holdout工具與8個安全/統計合約測試；修Unit-null CS8604與Relational版本MSB3277。Windows build 0 warnings/0 errors，Parser/Python11/11/Node PASS。報告與機讀摘要、主交接/新對話入口/研究回溯/審查同步提交；CI與原機部署不可混稱。

---

## 2026-11-21 — 直接開啟工作區副本、完成衝突修復與 Windows 驗證

- 從使用者本機 repo 建立可寫獨立 clone，保留桌面原工作樹及未追蹤測試檔；在 recovery 分支重現並解決最後 a27f850d 審查紀錄衝突，commit ab32e344ef76f67e8c0134f0592ba5b74f136a1f 僅存在此 clone。
- Windows API build、Parser、Python 3/3、Node 語法通過；前端標準 Vite config loader 權限失敗，指定 native loader 後 build 通過。未改套件、CI、模型或資料庫；CS8604、MSB3277 仍存在。
- checker 因 Docker named-pipe 權限失敗；直接讀 case15 HTTP 200，但新版 quantity-scope HTTP 404。已記錄真實阻塞，未把工作區成功說成桌面 cherry-pick／舊 API 重啟或 6/6 成功。
- 同步 CURRENT_HANDOFF、RESEARCH_TRACEABILITY、新對話入口與本日誌，新增審查 JSONL；main 的 AI_HANDOFF 索引另行更新，不合併開發程式。

---

# 2A 開發紀錄

## 2026-10-12 — 正式建立待偵錯／歷史資料分流

完成：

- 新增版本化 `AnalysisCase` 待偵錯流程
- 待偵錯資料表與歷史資料表完全分離
- 新增一次性 1D 快照匯入
- 保留 2A 獨立直接上傳
- 新增 V1／V2／V3 等待偵錯版本
- 新增可保存的 `AnalysisRun`
- 新增重新開啟歷史 Run 結果功能
- 阻止待偵錯案件寫入歷史真值

## 2026-10-12 — 收斂分析範圍

目前正式工作範圍：

- 結構體工程大項估算／價格異常
- 鋼筋數量異常
- 混凝土數量異常

從目前偵錯範圍移除：

- 模板
- 鋼骨

其他工程類別仍保留原始資料，但不強迫進入目前數量模型。

## 2026-10-12 — 資料庫硬清理

開發資料庫完成清理。

移除已淘汰／重複設計，包括：

- 已不使用的舊建案欄位
- 重複保存的歷史來源欄位
- `historical_transaction` 中的歷史價格欄位
- `quantity_rule_base`
- 舊測試／假歷史資料

目前歷史資料責任：

- `final_bid_item`：原始工項與結構體工程價格
- `historical_transaction`：只保存標準化後的鋼筋／混凝土數量

## 2026-10-12 — 基地面積整合

將 `SiteArea` 加入：

- `ProjectMaster`
- `AnalysisCase`
- 最終標單匯入資料
- 待偵錯案件基本資料
- 結構體工程估算表單
- 相似度評分
- 參數設定與 Audit 顯示

新增可設定：

- `SiteAreaWeight`
- `SiteAreaDifferenceCutoffRatio`

## 2026-10-12 — 移除舊版通用材料估算

移除已淘汰架構：

- 通用材料合理區間估算
- 批次材料估算
- 核心材料估算
- 舊版數量規則資料表
- 舊版一次性標單異常 Route
- 舊版通用估算 UI

目前正式服務：

- `MajorCategoryPriceRangeService`
- `DetectionCategoryRangeService`
- `AnalysisCaseService`

## 2026-10-12 — 建立歷史審核／發布流程

最終標單匯入後目前先進：

```text
Draft
```

新增審核：

- 實際工程大項覆寫
- 材料對應
- 數量／單位／單價修正
- 忽略狀態
- 即時發布阻擋條件

新增：

- `Publish History`
- `Reopen History Review`

Approved 建案才會進入模型基準。

Draft 建案不會參與模型。

## 2026-10-12 — Approved 歷史資料鎖定

歷史狀態為 `Approved` 時鎖定：

- 建案相似度基本資料
- 標單工項修改
- 工程大項覆寫
- 材料對應
- 忽略／取消忽略

若要修正，必須先重新開啟歷史審核。

## 2026-10-12 — 進入真實資料驗證階段

下一階段不再繼續擴張架構。

接下來優先：

1. 在本機啟動系統
2. 匯入第一份真正已定案最終標單
3. 檢查審核阻擋項目
4. 驗證工程大項／材料對應／單位修正流程
5. 發布第一個 Approved 歷史建案
6. 驗證 2A 建案摘要
7. 累積足夠真實歷史資料後驗證 P15／P50／P85
8. 建立真實待偵錯案件並比較 V1／V2

## 2026-10-12 — 新對話操作交接文件

新增：

- `docs/NEXT_CHAT_HANDOFF.md`
- 明確的本機「可以開始操作」條件
- 完整 Git Pull／Docker 重建指令
- 第一份真實最終標單驗證順序
- 明確要求下一個對話在本機系統可操作時直接告知使用者

下一階段是用真實最終標單進行實際驗證，不是再重新設計架構。

## 2026-10-13 — 匯入前唯讀數量檢查

在既有 `feature/1d-api-integration` Branch 上新增：

- `POST /api/tender-import/inspect`：必須明確提供選取工作表清單，而且不寫入資料庫。
- 共用 `WorkbookSheetPicker`：讓使用者在輸入建案基本資料前，就可以檢查所有解析候選列，包括原始工作表／列號、來源分類、數量、單位、價格、金額與警告。
- 保守辨識鋼筋與混凝土候選：只有在單位明確時才換算 kg／T 與 m³；鋼筋續接器、鋼筋施工服務、混凝土澆置服務不計入材料數量。
- 無法辨識單位、缺少數量、結構體工程大項不確定、來源列指紋重複等情況都會顯示待審核警告。
- 候選摘要**不是**模型異常、Approved 歷史或正式工程量，不會製造假的 P15／P50／P85。
- 在 `tests/WorkbookParserTests` 加入回歸測試。

下一步：在 UI 手動驗證實際上傳的 `.xls`，將 49 筆解析候選列與 Excel 原始資料逐筆比對，再處理任何漏抓混凝土／鋼筋或錯誤分類列。

不得把預算價或廠商報價直接發布成 Approved 歷史真值。

## 2026-10-15 — 文件／專案交接檢查點

完成：

- 擴充 `README.md`，作為目前專案入口
- 新增 `docs/API.md`，記錄目前 Controller／API 合約
- 新增 `docs/DATABASE.md`，記錄目前資料表責任與開發資料庫 Schema
- 新增 `docs/DEVELOPMENT.md`，記錄 Docker、本機 Build、Parser 測試與修改規則
- 更新 `docs/HANDOFF.md`
- 更新 `docs/NEXT_CHAT_HANDOFF.md`，加入可直接複製的新對話提示詞
- 將文件統一到目前 Draft → 審核 → Publish History → Approved 歷史流程
- 記錄唯讀 Excel 工作表預覽／檢查防護
- 記錄結構體工程價格歷史與鋼筋／混凝土數量歷史的責任分離

目前下一階段仍然是：

1. 拉取並執行最新 Branch
2. 使用真實來源檔驗證工作表預覽／檢查
3. 只匯入真正已定案歷史標單
4. 處理 Draft 審核阻擋
5. 發布 Approved 歷史資料
6. 累積足夠真實歷史後驗證 P15／P50／P85
7. 最後驗證真實待偵錯 V1／V2 分析

不得為了展示而加入假歷史資料。

## 2026-10-15 — 文件索引與 Branch 顯示說明

完成：

- 新增 `docs/README.md` 作為文件入口
- 記錄交接與開發建議閱讀順序
- 明確說明目前正式文件與 2A 實作位於 `feature/1d-api-integration`
- 說明 GitHub 顯示 `main` 時可能看不到 `docs/` 的原因
- 保持「不得自行合併 main」規則不變

目前下一步仍為實際系統操作與真實最終標單驗證。

## 2026-10-15 — 文件中文化

完成：

- 將專案 README 與 `docs/` 主要說明內容統一改為繁體中文
- 保留 API 路徑、Branch 名稱、Class／Table／欄位名稱等實際技術識別字
- 後續新增文件預設使用繁體中文


## 2026-10-15 — 模擬資料端對端分析與清理交接

- 用 20 筆含雜訊的虛構標單測試工作表預覽／唯讀檢查；修正小數項次、逐列工程分類及材料服務工項誤算。
- 建立 AnalysisCase V1、V2，確認版本快照獨立、舊 Run 可保存與重新開啟；修正舊 Run JSON PascalCase 前端相容問題。
- 完成 Run #9 V2 的數量合理區間、Run #6 價格合理區間介面測試。
- 依使用者要求，特別加入 8 個**明確標記**的 SQL 合成 Approved 模擬建案、24 筆價格明細及 16 筆鋼筋／混凝土數量歷史，以驗證 8 案 P15/P50/P85 的端對端執行。
- **此批 Approved 不代表人工審核或真實標單，屬開發展示例外，不得供真實工程決策使用。**
- 詳情見 `docs/DEMO_DATA_20261015.md`，清理腳本見 `scripts/cleanup_demo_2a_20261015.sql`。
- 正式待辦：測歷史 Draft／Publish／Reopen 的完整操作，展示完先清除合成歷史，之後才進行真實歷史資料驗證。

## 2026-10-16 — 資料庫交易穩定性集中修正（待整合驗證）

觀察到的故障：Supabase Session Pooler 5432 間歇性 `NpgsqlException: Exception while reading from stream`、HTTP 500/504；發布批次寫入時連線中斷，Rollback 可能拋出次生例外。另全域 `EnableRetryOnFailure` 與既有顯式交易不相容，已撤回。

本次修改：
- `ProjectManagementService` 中 Publish 以外的明確交易也保護 Rollback，保留原始例外；Publish 原有的保護保持不變。
- `FinalBidService` 五處交易全面保護 Rollback，原始例外寫入後端診斷記錄。
- `FinalBidImportService` 原已保護 Rollback，未重複更動。
- `ProjectDetailView.vue` 發布／重新開啟請求若 HTTP 回應失敗，先重新讀取建案狀態及歷史摘要；結果不明時警告不要連續重試，避免網路逾時誤認為資料未提交。
- 未直接變更資料庫、未合併 main；仍保留 `Pooling=false` 作為暫時診斷，不視為長期效能調校。

**未完成、不可宣稱正式修復**：尚未於使用者的本地 Docker 編譯／執行完整迴歸；Supabase Pooler 中斷的網路根因仍待定位。不可對顯式交易啟用未包裹完整交易的自動重試，避免重複寫入。

整合驗證順序：
1. `git status` / `git pull --ff-only`、`docker compose up -d --build`、`docker compose ps`，確認 dotnet 與 Vite Build 都通過。
2. 讀取 Project #11 並確認 Approved／有效歷史交易 7 筆及總價 12,063,350，作為測試前狀態。
3. 先驗證核准期間修改被拒絕，再做一次 Reopen，核對 Draft／有效交易 0 筆／保留完整 FinalBidItem。
4. 修改非核心 Draft 工項後再 Publish，核對 Approved／有效交易不重複、結構體價格與試體試驗排除邏輯。
5. 用操作紀錄及資料庫查詢比對每次交易，壓力測試連續查詢與短暫斷線；發生 500/504 應先回查資料庫狀態，不要直接重試寫入。

## 2026-10-17 — DEMO #8 Publish/Reopen 寫入整合驗證

執行 `scripts/test-demo-history-cycle.ps1`，DEMO Project #8 成功完成兩輪 Draft → Approved → Draft。Supabase 實際驗證：#8 最終 Draft、核准時間 NULL、有效歷史交易 0、已停用歷史交易 2；當天稽核紀錄 PublishHistoryBaseline 2 筆、ReopenHistoryReview 2 筆。Project #11 保持 Approved 與 7 筆有效歷史交易，沒有更動。此測試通過**正常寫入流程**，尚未驗證注入斷線、併發發布、重試冪等、長時間穩定性，不能稱為完整根因修復。

## 2026-10-17 — Publish／Reopen 併發保護（待本機驗證）

發現同一建案兩個並行請求可能通過狀態檢查。已在 PublishHistoryAsync、ReopenHistoryReviewAsync 的顯式交易起點使用 PostgreSQL pg_advisory_xact_lock(2202, projectId) 序列化相同建案的操作；交易結束或連線失效時自動解鎖。發布後的有效歷史交易計數移至 Commit 前，避免提交成功後再查詢失敗造成成功被誤報。

新增 scripts/test-demo-history-concurrent.ps1，限制 DEMO #8，預期兩個同時 Publish 一個成功、另一個 409，Reopen 亦然；結束時回到 Draft。**尚未執行本機 Build 或併發測試，不能宣稱正式驗證通過**；須核對稽核紀錄與有效歷史交易，若錯誤立即停止，不能自動重試寫入。

## 2026-10-17 — DEMO #8 併發測試完成

使用本機重建後 API 執行 scripts/test-demo-history-concurrent.ps1。雙重 Publish 回應 200/409，雙重 Reopen 回應 409/200；Supabase 核查 Project #8 最終 Draft、有效歷史交易 0、停用歷史交易 2，該測試時段 PublishHistoryBaseline 與 ReopenHistoryReview 稽核紀錄各 1 筆。Project #11 保持 Approved、7 筆有效交易。此結果證明本輪雙請求衝突保護有效，但不代表斷線故障注入、長時間高併發及端到端連線根因已完成驗證。

## 2026-10-17 — 隔離資料庫故障測試（待執行）

新增 scripts/test-isolated-postgres-failure.ps1：以 Docker 啟動獨立 postgres:17 容器（無對外 port、network none），在顯式交易先 INSERT 再強制 statement_timeout，確認失敗後不留部分提交，finally 清除容器。此為 PostgreSQL 原子性基礎測試，**不會測到本專案 Npgsql/EF Core 的連線中斷、Commit 回應遺失或應用程式重試安全性**。尚未在使用者本機執行，不能視為完成故障復原驗收。DEMO #8／Project #11 均不參與此測試。

## 2026-10-17 — 隔離 PostgreSQL 逾時回復測試通過

使用者本機執行 scripts/test-isolated-postgres-failure.ps1：CREATE TABLE 成功、BEGIN/INSERT 成功、強制 statement_timeout 出現 expected error，最後顯示 PASS: transaction was rolled back; no partial row exists。此為獨立 PostgreSQL 的回滾原子性驗證，不涉及 Supabase、DEMO #8、Project #11，也不構成 Npgsql/EF Core 斷線或 Commit 回應遺失驗收。

## 2026-10-17 — 隔離 Npgsql 故障測試程式（待本機執行）

新增 tests/ConnectionFaultHarness（.NET 10 / Npgsql）及 scripts/test-isolated-npgsql-failures.ps1。腳本以 Docker 建立一次性私有網路和 postgres:17，使用 dotnet/sdk:10.0 執行兩種情境：(1) 交易 INSERT 後，透過另一路連線 pg_terminate_backend 中斷提交前的 writer，檢查無殘留資料；(2) Commit 已成功但模擬應用層 HTTP 回應遺失，透過持久操作鍵與 unique constraint 避免重覆插入。finally 清除測試 DB 和網路；絕不使用 Supabase。需於 Windows PowerShell/Docker 本機驗證，**不是實際 ProjectManagementService 的斷線整合測試，也不代表目前 Publish API 已具備完整請求冪等保護**。

## 2026-10-17 — Npgsql 隔離斷線實測通過

使用者本機執行 scripts/test-isolated-npgsql-failures.ps1，測試環境為一次性 PostgreSQL 17 與 .NET 10/Npgsql。實測輸出：Expected writer connection failure: PostgresException；PASS: connection dropped before commit; no partial row；Expected simulated lost response；PASS: lost response can be reconciled by durable operation key；PASS: isolated Npgsql integration harness completed。這證明隔離測試的交易前斷線回滾與持久操作鍵範例可行，不代表正式 PublishHistory／ReopenHistoryReview API 已具備端到端請求識別與冪等。後續優先調查正式 API 丟失 Commit 回應時的狀態回查，以及整合操作鍵或等價的重複提交防護，再於隔離 DB 做故障注入。

## 2026-10-17 — 正式 API 故障處理盤點及前端重入保護

核對 ProjectsApiController、ProjectManagementService 與 ProjectDetailView：正式 Publish/Reopen 已有 PostgreSQL 建案級 advisory transaction lock 與 Approved/Draft 狀態檢查；因此相同建案的同步重複請求已有基於狀態的防護，但**尚未具備持久化 Idempotency-Key**。前端加入 publishingHistory 執行中直接拒絕再次觸發，可避免 UI 雙擊造成並行請求；不等於網路斷線重試的真正冪等保護。此階段未新增資料庫 schema、未更動 Project #8/#11。根因目前只能定位為曾發生連線讀取中斷與多處交易錯誤處理問題，尚無證據可單一定因於 Supabase、Npgsql 或 Nginx。待辦：設計包含唯一操作 ID、持久化狀態與唯一限制的寫入協定，使用獨立應用程式資料庫驗證，再部署至共用開發資料庫；勿在正式資料庫進行故障注入。新增前端變更尚待本機 Build。

## 2026-10-17 — 正式 Publish/Reopen 持久操作收據（待部署/驗證）

此次集中實作（尚未部署）：
- scripts/migrations/20261017_history_operation_receipt.sql：新增 public.history_operation_receipt，operation_id UUID 主鍵、project_id/action_type/result_json/created_at，保留成功操作的持久化紀錄。**尚未在 Supabase 執行**。
- 崴正/Models/HistoryOperationReceipt.cs + ApplicationDbContext：新增 EF Core 對應。
- ProjectManagementService：在同建案 advisory transaction lock 取得後，若 Idempotency-Key 已有同建案同動作收據則回傳原結果；首次成功則將操作收據寫在同一個資料庫交易內，隨工項歷史與稽核一起 Commit。操作 ID 被不同建案/動作複用時回傳 Conflict。
- ProjectsApiController：Publish/Reopen 接受可選 Idempotency-Key UUID header；新增 GET /api/projects/{projectId}/history-operation/{operationId} 提供成功收據查詢。
- ProjectDetailView：使用 sessionStorage 保存尚未確定結果的操作 ID；首次成功清除，連線逾時後先讀成功收據，若查不到則保留識別碼、不斷言提交結果。UI 同時有 busy guard。
- scripts/test-demo-history-idempotency.ps1：使用 DEMO #8 對同一動作以相同 key 重送，驗證結果與收據，再做 Reopen 並回到 Draft；資料庫交易數及稽核需後續人工 SQL 核對。

**部署順序不可顛倒**：先備份/確認環境、在目標開發 Supabase 執行 migration，確認 schema；再更新程式碼和 Docker Build，最後執行 DEMO #8 測試、核對稽核；完成前不得把新前端搭配舊後端或反之視為穩定版本。舊第三方無 header 請求仍使用狀態鎖，但沒有持久鍵回放保障；正式全面冪等需另行決定是否強制要求 header。

**本輪尚未在使用者本機 dotnet build / npm build、未執行 SQL migration、未實測 API；所有功能標註「待驗證」**。切勿在驗證之前宣稱正式修復。失敗時需保留 DB schema 並回滾應用版本，不應直接 drop receipt 表，以免丟失提交證據。

## 2026-10-17 — 開發 Supabase 操作收據 schema 已部署

使用者本機回報 docker compose build backend frontend 皆 Built（尚未 up）。已在 Supabase 開發專案 sbykuwypriudrgurmssb 查證 history_operation_receipt 原不存在，執行 scripts/migrations/20261017_history_operation_receipt.sql 對應的 BEGIN/CREATE TABLE/INDEX/COMMENT/COMMIT，成功建立並核對欄位 operation_id uuid、project_id integer、action_type varchar、result_json jsonb、created_at timestamptz，初始 0 筆。此遷移未改動 project_master、final_bid_item、historical_transaction 既有資料。**新版容器尚未啟動，正式 API 同 key 重送尚未驗證。** 下一步部署本機新版並只以 DEMO #8 測試，確認操作收據及稽核各僅一筆；保留 Project #11 Approved。不要因舊版 API 還能啟動就假定新收據流程可用。

## 2026-10-17 — 正式 API 持久化冪等驗證通過

本機新版容器啟動後，`scripts/test-demo-history-idempotency.ps1` 在 DEMO #8 成功執行：Publish 使用同一 Idempotency-Key 重送成功，Reopen 使用另一相同 key 重送成功，最後 #8 回到 Draft。直接查 Supabase：history_operation_receipt 在 #8 有 2 筆（各一筆 PublishHistory/ReopenHistoryReview）；該測試期間 audit_log PublishHistoryBaseline 1 筆、ReopenHistoryReview 1 筆；#8 historical_transaction 有 2 筆停用、0 筆有效；#11 保留 Approved、7 筆有效。此驗證是正常環境同 key 重送，不代表已完成實際 Commit ACK 遺失的故障注入、長時間負載、跨瀏覽器重送。仍需要測無收據／不同 action 使用同 key、並行同 key 和例外處理、保留收據等測試。

## 2026-10-17 — 收據回查的非破壞性迴歸測試

已檢查正式 API 與前端的持久操作收據回查邏輯，新增 `scripts/test-history-receipts-readonly.ps1`，讀取前次 DEMO #8 成功交易留下的 PublishHistory/ReopenHistoryReview UUID 收據，核對操作種類與建案 ID，另外確認 #8 Draft、#11 Approved。腳本只送 GET，不修改 Supabase，不需要 Docker rebuild。**尚待使用者本機執行**。剩餘風險：尚未注入正式 API 真實網路斷線／Commit 回應遺失；前端目前在無法查到收據時保留原操作 ID 並回報結果不明，不能因此認定交易成功或失敗。既有 API header 為可選，無 key 的非 UI 客戶端仍無跨請求重放保護。

## 2026-10-17 — 操作收據唯讀迴歸已通過

使用者於本機執行 `scripts/test-history-receipts-readonly.ps1`，回傳 `PASS: receipt PublishHistory`、`PASS: receipt ReopenHistoryReview`、`PASS: DEMO #8 Draft; Project #11 Approved. No writes issued.`。這項驗證確定先前提交的兩筆成功收據可由正式 API 再次讀取，並且本輪沒有資料庫寫入；不代表已完成正式 API 的連線斷線故障注入。

程式碼檢查發現 PublishHistoryAsync/ReopenHistoryReviewAsync 在已開始的資料庫交易中有提前 return 的分支（成功收據重播、狀態衝突等），由 using Dispose 進行交易清理；未來可改為顯式早期分支處理並設計獨立故障注入，以降低例外處理和連線生命週期的歧義。尚未執行更動。

## 2026-10-17 — Publish/Reopen 交易生命週期小幅加固（待本機編譯）

重新檢查兩個正式 API，狀態衝突與成功收據重播都可能在交易開啟後提前 return。這些路徑原先透過 using Dispose 清理未提交交易，並非已發現資料損壞。已改成 `await using var transaction`，讓提早結束時非同步釋放交易／連線，避免 request thread 上同步處理 I/O；例外路徑仍保留捕捉原始錯誤並在 Rollback 失敗時記錄次要錯誤的防護。未更動資料庫 schema 或實際建案資料。需於本機重新 Build，並在隔離 DEMO 情境驗證，不宣稱因此解決 Supabase 曾發生的連線中斷根因。

## 2026-10-17 — 正式 API 故障注入隔離環境骨架（尚未執行故障注入）

新增 `compose.fault-test.yaml`：獨立 `bidding-2a-fault` 專案、本機 PostgreSQL 17 tmpfs、DB Host=db，後端僅映射 `127.0.0.1:18080`，Docker internal network 隔離，未引用 Supabase；絕不可與主 `compose.yaml` 合併啟動。新增 `scripts/test-fault-stack-preflight.ps1`，僅靜態檢查隔離邊界及 docker compose config，不啟動服務或觸及資料庫。需要先取得**經審查的 schema-only 與去識別化的獨立 DEMO fixture**，才能真正啟動 ProjectManagementService 執行 Publish/Reopen 故障注入；這階段未準備 fixture，亦未在本機執行 preflight / build / API 測試，因此不宣稱通過。正式開發 Supabase #8 Draft / #11 Approved 不應參與此隔離測試。

## 2026-10-19 — 隔離正式 API 的 Commit 前斷線故障開關（待本機驗證）

新增 ProjectManagementService 的 opt-in `FAULT_TEST_DROP_BEFORE_COMMIT=1` 故障點，僅在操作有 UUID、DbConnection.DataSource=db 且 Database=bidding_fault、projectId=1 與 `[FAULT_FIXTURE_ONLY]` 建案時，於 Publish/Reopen `SaveChanges` 後、Commit 前關閉真正的資料庫連線，再拋出測試例外。預設開關關閉，Supabase 的 DB host 不匹配。新增 `compose.fault-drop.yaml` 作為獨立 backend env override，以及唯讀驗證腳本 `scripts/check-fault-precommit-readonly.ps1`。此機制用於重現 Commit 前連線遺失的資料庫回滾，不包含真實 Commit 成功後 HTTP 回應遺失。尚未執行本機 build，也未故障注入。啟動時務必 `--no-deps`，避免重建含 tmpfs 的 db；故障測試前後保留並比對 audit/receipt/history baseline，完成後必須移除 override 並重新建立 backend。暫不以此測試證明長時間可靠性。

## 2026-10-20 — 立彩真實標單工程大項別名修正（待本機 Build）

檢視立彩原始 Excel 的「標單」工作表，實際存在「一 假設工程」「二 土方工程」「三 基礎工程(擋土柱及安全支撐工程)」「五 結構體工程」「八 平頂裝修工程」「九 牆面裝修工程」等分類標題。2A 原本只接受標準名稱精確匹配，造成部分非結構體項目列為未辨識。TwoAScopePolicy 新增**明確且保守**的工程大項別名：土方／上述基礎 -> 土方及基礎工程、平頂 -> 天花板裝修工程、牆面 -> 內外牆裝修工程。不猜測安全觀測、輕隔間、配合使照等分類，也不擴大目前僅針對結構體的模型範圍。不更改資料表、歷史交易或既有分類 override；**已匯入的 Draft 工項不會自動更新**。尚待本機 dotnet build 與新預覽結果驗證，不聲稱 194 筆會全數解決。下一步重點應針對已存在 Draft 的分類核對與審核效率，避免反覆匯入造成重複建案。

## 2026-10-20 — 立彩 Excel 15 個章節比對與未辨識分類修正（待本機 Build）

直接核對用戶上傳之「3. 立彩_最終標單.xlsx」工作表「標單」A1:H654，章節標題為：假設、土方、基礎(擋土柱及安全支撐)、安全觀測、結構體、輕隔間、地坪裝修、平頂裝修、牆面裝修、外部裝修、防水、門窗、雜項、配合使照、景觀，共 15 種。前次已補土方／基礎／平頂／牆面別名；本次把「安全觀測工程」「輕隔間工程」「配合使照工程」列入已知工程大項，而**非**強行改成結構體或雜項。現行僅結構體工程屬價格／鋼筋混凝土數量模型範圍，新增三類仍不納入模型。已存在 Draft 的人工覆寫不應被覆蓋，計數會由現行 ResolveEffectiveMajorCategory 查詢重新計算；實際減少多少未辨識須待更新 Backend 後驗證，不保證歸零。無 schema migration，無資料庫操作。

## 2026-10-21 — 收斂至建材模型之發布資格（待本機 Build/確認）

使用者明確要求 2A 聚焦有有效數量及成本的建材，不擴張成施工／人工估算系統。本次只做現有發布資格與材料歷史範圍的最小修正：
- Publish 的「結構體無有效價格」改為只審查已對應到鋼筋／混凝土材料的結構體工項；模板、放樣等未對應建材的缺價工項仍完整保留在最終標單，不因缺價阻擋材料基準發布。
- 已確認的鋼筋不足噸數運費補貼不是建材量，明確阻止其進入材料歷史，亦不列入建材缺價與單位資格阻擋。
- **不會自動處理其他模糊或誤對應工項**；未知是否含材料者仍須人工審核。此前 5 筆數量資格問題需重新確認；不保證能直接發布。
- 不增加資料表、新審核狀態、人工成本模型，也不修改既有資料庫記錄。
- **待驗證與後續開發界線**：結構體工程整體價格摘要可能仍包含施工費，不可將它標為純建材成本，也不可拿它與材料量價模型混用。此提交未重設整體工程價格估算。正式部署前需確定目標是保留整體結構體價格估算作獨立指標，還是完全取消該模型。

## 2026-10-21 — 摘要與發布資格一致化（待本機 build）

ProjectTwoASummary 的發布阻擋原本將「結構體全工項無有效價格」計入，與先前 Publish 改為只針對鋼筋／混凝土目標建材價格資格不一致。本次保留 StructuralUnpricedItemCount 作為完整工程價資訊，但將 PublishBlockingIssues 的缺價項改為目標建材工項的價格檢查；不動原始 Draft。這是最小規則同步，**未經本機編譯與實際摘要驗證**。目前 QuantityBlocked 計數仍需審核真正材料項，鋼筋運費補貼等誤配對可能使既有來源件數偏高；不得逕行 Publish。注意完整結構體工程總價不等於建材成本。

## 2026-10-21 — 鋼筋／混凝土資格摘要與 Publish 對齊（待本機編譯）

發現 ProjectTwoASummary 的鋼筋來源／合格件數原本使用不同的計算路徑，且曾將「鋼筋不足噸數運費補貼」等明確非材料費計入分母，造成與 Publish 判斷不一致。改由同一份未忽略、有效結構體工程、材料名稱鋼筋／混凝土且排除明確運費補貼的集合計算來源與合格數；合格資格仍須 CanBecomeHistoricalTransaction，維持既有安全限制。材料尚未對應的模糊工項不會自動計入合格材料，更不會自動發布。未修改資料庫、資料值或模型範圍，應待本機 build 並於建案 #12 查看摘要數字後才能確認實際效果。注意「結構體整體工程價格」仍非純材料成本。

## 2026-10-21 — 精準定位 2A 材料資格阻擋（待本機 Build）

新增唯讀 GET /api/projects/{projectId}/blocked-material-items，列出目前工程大項為結構體、已對應目標材料鋼筋／混凝土但未通過 CanBecomeHistoricalTransaction 的工項，排除明確非材料運費補貼。返回 ID、來源名稱、Qty/Unit/Price/Amount、MappingStatus、材料與原始分類、粗略原因；其列出集合與目前 Publish 資格阻擋邏輯一致。此端點不操作資料庫且不改模型；供開發者定位剩餘四筆異常，嚴禁為了消警示將模糊工項直接改成材料。原因中的「單位無法安全換算或其他限制」僅為待查提示，不是確定診斷。待本機 build 與 API 執行結果確認。

## 2026-10-21 — 建案 #12 四筆鋼筋組立用施工輔助設施排除

唯讀 API /api/projects/12/blocked-material-items 確認 ID 191–194 均屬「室內架／中欄杆／安全母索／鐵爬梯（鋼筋組立用）」，原本因名稱含「鋼筋」自動對應到鋼筋 materialId=1，數量單位 m²、M、M、支不能換算為 kg。TwoAScopePolicy.IsExplicitNonMaterialCharge 加入**狹義、名稱明確**的施工輔助項排除條件；FinalBidService.IsInTwoAHistoryScope 及摘要／Publish 資格檢查皆使用此條件。保留原始資料、不修改已有對應記錄；此提交未修正通用自動對應引擎，因此未來匯入可能仍顯示錯誤對應，後續再視具體情形阻止自動對應。待本機 Build 驗證摘要與 API 不再將四筆視為材料，發布前仍需確認其他有資格材料的價格真實性。不得把結構體總價當成純材料成本。

## 2026-10-21 — 發布前 7 筆建材候選唯讀來源核對（待本機 Build）

新增 GET /api/projects/{projectId}/eligible-material-items，以現有 Publish 可用的量與單位資格判斷列出鋼筋／混凝土合格候選，包含來源 Sheet/Row、Qty/Unit、原始價格、材料對應及對「組立、彎紮、壓送、澆築」的保守人工確認提醒。只是唯讀資訊，**不能把資格通過解釋成已審核實際材料量或純材料單價**。無 schema 或模型擴張；待本機 Build 與建案 #12 七筆查詢確認。後續發布前必須由人確認材料量、重複計量與價格性質；禁止僅因 3/3、4/4 就直接發布。

## 2026-10-26 — 基本建案資料即可分析的量價範圍（待本機建置）

依最新需求，五級設計敏感度只作內部研究規範；目前不新增施工範圍／防水面積／牆面面積／門窗開口等輸入。閱讀現有 AnalysisCaseService：已設雙入口 CreateFromOneD/CreateFromUpload、多版本 AddVersion、RunQuantityAnalysis、RunMajorPriceAnalysis；資料模型具 AnalysisCase、AnalysisBidVersion、AnalysisBidItem、AnalysisRun。不能將這些介面直接等同整條功能已通過驗收。

TwoAQuantityAnalysisService.AnalyzeAsync 現新增執行時 fail-closed：僅 TwoAScopePolicy 核准的工程大項＋偵錯類別才可產生正式數量異常；不符合者回傳 NotApplicable／Gray 並說明原因，即使 DB 另增 DetectionCategory 也不會自行開啟未研究模型。目前核准組合仍為結構體工程之鋼筋、混凝土。保留既有不足樣本 InsufficientData 顯示、單位失敗防護，不重引 Z-score/LSTM。新增模板與鋼骨前必須先用現有基本資料、歷史案件及正確材料量核對可行性；價格比較亦須獨立查核同類工程的價格範圍一致性。沒有資料表遷移；待本機 dotnet build 和一批流程驗證。

## 2026-11-10 — 外部研究資料與崴正歷史基準分層策略（研究決策，尚未啟用）

需求：若企業可比較歷史標單不足，評估可合法使用的公開數量／價格資料集；研究資料不得直接寫入正式 FinalBidItem／HistoricalTransaction，也不得將公開資料稱作崴正的實際決標紀錄。

目前實際執行的 DetectionCategoryRangeService 只從已核准的歷史交易取得候選，對每件計算 (SimilarityScore/100)^SimilarityExponent × 0.5^(AgeYears/TimeHalfLifeYears)。尚無來源可信度權重與獨立外部模型流程，不能宣稱企業歷史已加權優先於公開資料。

預備方案（不改生產判定）：1) 建立研究用外部資料來源清單與版本／授權／工程類型／材料數量涵蓋範圍；2) 區分台灣公共工程價格（適合價格外部對照）、公開 BIM QTO（適合擷取測試，非真實決標）、材料強度區域估算（不應冒充獨立真實建案）；3) 以同一批未參與訓練的真實崴正案進行企業-only、外部-only、分層／收縮三組比較；4) 企業資料是否加重權重，應由留案測試的誤差、區間涵蓋率與穩定性決定，不設定任意固定倍率；5) 權重有效樣本數 ESS = (sum w)^2/sum(w^2)，並追蹤標準工項口徑與完整／部分工程範圍；6) 交付前將結果標示分為「可正式判定／僅供參考／資料不足」，對信心度不宣稱未校準的正確率百分比。

正式基準繼續使用企業已核准歷史資料；公開資料若經研究可用，預設只在獨立外部參考通道提供結果，未經明確驗收不得參與正式異常判定。此記錄為設計與研究門檻，非功能完成或資料庫變更。

## 2026-11-11 — 專題模擬研究驗證與完整跨對話交接

- 教授允許專題以明確標記模擬資料做模型研究；產學正式歷史不允許模擬數據混入真實決標標單，仍須真實工程資料驗證。
- `research/2a_synthetic/generate.py` 建立六種用途結構 × 兩材料合成資料；`compare.py` 做基礎方法比較；`batch.py` 多種子四情境（stable/high_variance/company_shift/outliers），小樣本 3/5/8；`production_weight_review.py` 研究現有六因子相似度、結構門檻、基地面積權重調整。所有研究與正式 DB 無連線。
- **完整使用者 PowerShell 證據**（提交節點 `3986920`）：執行 `python research/2a_synthetic/batch.py --first-seed 20261111 --seeds 10 --cases-per-combo 30 --train-sizes 3 5 8`，訊息 `5760 group runs, 96 summary rows`；`python -m unittest discover -s research/2a_synthetic -p "test_*.py" -v` 得 7/7 OK。
- **其後新增第三輪**（提交節點 `68e8e45`）：`production_weight_review.py`、新增單元測試。使用者最新明確回報「沒有錯誤」，**但本輪無詳細測試逐行輸出及 CSV，尚未對演算法誤差做量化解讀**。
- 建立 `docs/CURRENT_HANDOFF_2026-11-11.md`、`docs/RESEARCH_TRACEABILITY_2026-11-11.md`、`docs/NEW_CHAT_START_2026-11-11.md`，並更新 README / docs/README / docs/HANDOFF / docs/NEXT_CHAT_HANDOFF 指向新入口，避免 10/15 過時資料庫 0 筆及工作方向誤導新對話。
- 決策同步追加 `docs/DECISIONS.md`（研究隔離、先審查權重再改生產、可重現交接）。
- 下一步：先讀 `research/2a_synthetic/output/production_weight_summary.csv` 與 `production_weight_detail.csv` 實際結果，再比較 C# 與 Python 評分等價性、公平區間定義、時間前推回測。不要重做已確認的 5760 組模擬或盲目更改正式 DB。
- 注意：本機 `output/` 被 gitignore 排除，**不能靠 GitHub 自動讀到其檔案**。可以透過使用者上傳匿名結果 CSV 或固定 seed 重現。


## 2026-11-11 — 專案專屬 Handoff Guardian Skill

- 應使用者要求，把跨對話交接與可回溯規範封裝成專案 Skill `waizheng-2a-handoff-guardian`；GitHub 路徑 `.agents/skills/waizheng-2a-handoff-guardian/SKILL.md`。
- 新增 `AGENTS.md`，明確要求每次程式／研究／模型／文件變動後重新審查是否同步更新交接。
- 新增 `scripts/check_handoff_review.py`，以變更前 SHA 比對 Git 檔案與**當輪新增**的 `docs/HANDOFF_CHANGE_REVIEWS.jsonl` 記錄；只有完整覆蓋 changed_paths 且符合更新文件聲明才通過。
- 新增 `.github/workflows/2a-handoff-review.yml`，針對目標 `feature/1d-api-integration` 的 PR 或手動觸發執行檢查；直接向 feature 推送仍需手動按 Skill 檢查。
- 更新主交接／研究證據追溯／新對話入口／決策文件，讓新對話先閱讀 Skill。這只是治理流程的建立，與正式量價模型計算無關。


## 2026-11-11 — 第四輪：六因素模擬權重研究產出報告工具

- 先依 `waizheng-2a-handoff-guardian` Skill 閱讀基線、查最新程式與交接。
- 新增 `research/2a_synthetic/report_weights.py`：依本機第三輪 CSV 生成明確標記「模擬研究」的 Markdown 權重比較，檢查三種設定是否齊全與數值合法；不會自行猜模型勝負。
- `test_research.py` 補上由彙總資料生成報告、缺比較設定拒絕的測試。
- 下一步使用者本機執行後，上傳 `production_weight_report.md` 或 `production_weight_summary.csv`／manifest，以確切指標分析結果。正式 2A 生產模型／DB 未動。
- 交接審查：模型研究流程與下個任務改變，**須更新主交接／研究回溯／研究 README／WORKLOG／新對話提示**；本輪已有對應更新並記錄 JSONL。


## 2026-11-11 — 第三輪結果分析與第五輪配對因子拆解

- 收到使用者上傳 `production_weight_report.md`，解析 24 組情境 MAPE：A 約 17.10%、B 約 17.07%、C 約 16.91%。B 與 C 相對 A 的平均差距分別約 0.03、0.19 百分點；沒有足夠數據證明統計顯著或實務效益。
- 現行配置跨結構混入平均僅 0.18%，硬門檻改善非常有限；C 同時修改多因子，無法直接歸因基地面積權重。詳細證據存於 `docs/2A_SYNTHETIC_WEIGHT_RESULT_REVIEW_2026-11-11.md`。
- 新增 `factorial_weight_review.py` 六配置交叉對照，將結構硬門檻與「直接去掉基地面積」／「將權重挪給總樓地板面積及樓層」分開分析；`production_weight_review.run/predict` 可注入研究設定，原正式服務未變；新增配對完整性單元測試。
- 交接審查判定：**必須更新**主交接、研究回溯、研究 README、新對話指引及 WORKLOG；已同步，且將本輪檔案列入 `docs/HANDOFF_CHANGE_REVIEWS.jsonl`。
- 新腳本需使用者 Windows 執行；不得把程式提交視為測試成功。下一步上傳 `factorial_weight_report.md` 判讀。


## 2026-11-11 — 第五輪成功與第六輪稀疏結構歷史研究

- 使用者提供實測 PowerShell 輸出：第五輪 4320 組配置／144 彙總／192 配對，13 項單元測試全部通過，交接 Guardian 已確認 9/9 檔案審查。此前估算 2160 組不正確，文件統一更正。
- 第五輪資料只顯示執行成功，尚未看到 `factorial_weight_report.md` 的實際 ΔMAPE，**模型採用結論仍未作成**。
- 建立 `research/2a_synthetic/sparse_structure_review.py`，模擬相同結構只有 0～3 件，而跨結構參考充裕時，是否有交叉套用風險。將名義最低 5 案 readiness 與演算法能計算分開，保留現有正式樣本不足規則。
- `test_research.py` 補上稀疏度、gate、可重現、非法配置測試；研究 README 與交接已更新。尚待 Windows 本機實跑，研究不連 DB。


## 2026-11-11 — 第五／六輪模擬報告分析收斂

- 收到第五輪因子拆解報告及第六輪 0～3 同結構可比歷史壓力測試報告；詳細表及來源限制存於 `docs/2A_FACTORIAL_SPARSE_RESULTS_2026-11-11.md`。
- 第五輪顯示權重微調的模擬 ΔMAPE 較小，沒有足夠證據改正式參數。
- 第六輪在特意安排其他結構候選充足情境下，無硬門檻皆通過名義五件門檻，但 TopK 跨結構比例高；有硬門檻僅 0～3 同結構時，均達不到最低 5 件。模型可數學計算 ≠ 可正式判定；研究數字不可外推實務準確率。
- 下一件待做工作：跨結構差異大小的敏感度、資料可比性資格診斷與時間前推；此輪僅更新分析與交接，不動任何產學正式模型／資料庫。
- 依 `waizheng-2a-handoff-guardian` Skill 審查：本次研究結論與新對話下一步改變，故更新主交接、研究回溯、WORKLOG、研究 README 與新對話交接。


## 2026-11-12 — 第七輪：歷史參考來源與可比性診斷

- 使用者確認 `cedf7df` 已更新到另一台 Windows 開發機，交接審查腳本 6/6 檔案通過；完成第六輪文件交接的操作驗證。
- 按 `waizheng-2a-handoff-guardian` 規範檢查最新程式／交接，新增 `eligibility_review.py` 研究來源、用途、結構、材料、單位、完整／部分工程、Approved 等條件，不與正式 DB 互動。
- 研究輸出同時區分『假設來源真實時其他條件合格』與『正式歷史來源實際合格』，全部合成資料均不得進入正式基準。新增三項研究回歸測試。
- **此次需更新交接**：新增研究方法、新 CLI 與下一個驗證點，故更新 README、主交接、研究回溯、WORKLOG 及新對話提示；不修改正式參數、C# Service 或 Supabase。


## 2026-11-12 — 第七輪合成來源資格報告回收與資料模型映射

- 收到 `eligibility_report.md`：35 件合成候選，假設來源真實時 1 件符合其餘條件，真正正式來源合格 0 件；所有模擬資料保持隔離。
- 對照最新 C# 模型：用途／結構／材料／Approved 有對應欄位，但來源真實性和工程範圍完整性尚無可靠的直接判定來源；研究示意欄位不等於 C# 正式欄位。
- 新增 `docs/2A_REFERENCE_ELIGIBILITY_MAPPING_2026-11-12.md` 與研究區唯讀 SQL `reference_schema_readonly.sql`；後者只查 metadata，使用者確認開發 DB 後才執行。
- 交接影響判定：研究發現、模型資料欄位差距及下一個工作改變，**需同步**主交接、研究回溯、WORKLOG、研究 README 與新對話提示；已更新並留 JSONL 審查。正式 ASP.NET 與 Supabase 均未修改。


## 2026-11-12 — 真實資料庫 86 欄位核驗

- 使用者已提供 Supabase `information_schema.columns` 查核結果，本次確認六張正式表共 86 欄：project_master 17、final_bid_item 27、historical_transaction 8、material_master 5、unit_master 7、estimation_settings 22。
- 已核對來源檔名/Hash、Draft/Approved、材料/單位、結構/用途等欄位存在，但無來源真偽／工程範圍完整性的專用資格欄位；且部分關鍵 FK 與面積／類型欄位可 NULL。
- 新增 `docs/2A_VERIFIED_DB_SCHEMA_GAPS_2026-11-12.md` 固化真正 DB Schema 證據；新增 `research/2a_synthetic/reference_quality_counts_readonly.sql` 提供唯讀聚合查核（尚未執行）。
- **交接審查：需要更新**主交接、研究回溯、WORKLOG、新對話指示與研究 README，因真實 Schema 的查核狀態和下次工作已變更。本輪不改任何 ASP.NET Core、Supabase Schema 或正式演算法。

## 2026-11-12 — Q4 核准材料獨立建案數真實檢查

- 使用者貼出 Q4 聚合：住宅 RC、material_id=1 有 8 獨立 Approved 建案及 11 筆交易；material_id=2 有 8 獨立 Approved 建案及 10 筆交易；兩組分別關聯 11、10 個工項 ID。不能把交易筆數當工程件數。
- 仍需核查材料 ID 名稱、DEMO 來源、工程範圍與 Q1～Q3 的缺漏/FK 檢查。8 件名義 Approved 不代表 8 件真實商務定案標單。
- 新增 `docs/2A_APPROVED_MATERIAL_COHORT_2026-11-12.md` 保存證據，及 `research/2a_synthetic/reference_material_source_audit_readonly.sql` 供材料 ID、疑似 DEMO 與一案多交易的唯讀聚合查核。
- 研究及接手狀態已有重大進展，依 Guardian 審查**必須更新**主交接、研究回溯、WORKLOG 與新對話文件；正式程式與 DB 未動。


## 2026-11-12 — Q1～Q4 完整真實資料品質盤點

- 使用者已提供四組完整 SQL 聚合：10 件住宅 RC（8 Approved、2 Draft）；8 件 Approved 中 7 件缺少來源檔案 Hash；23 筆材料交易中 2 筆刪除、2 筆關聯非 Approved／已刪除建案，外鍵空值／錯連為零；有效數量歷史材料 ID=1 有 8 件建案／11 交易，ID=2 有 8 件建案／10 交易。
- 兩組 Q3 異常計數可能重疊，不能直接加總；Q4 成功查到 21 筆有效歷史，不表示全數來源可信或 DEMO 已剔除。詳細另存 `docs/2A_HISTORICAL_DATA_QUALITY_Q1_Q4_2026-11-12.md`。
- 已擴充下一份唯讀 `reference_material_source_audit_readonly.sql`，原 Q5～Q7 不變，新增 Q8 交叉檢查刪除與非 Approved 的實際重疊。
- 本輪變更會影響研究事實與接手狀態，依 Skill 更新主交接、研究回溯、WORKLOG、新對話提示與審查 JSONL。正式服務、資料表、權重均未更動。


## 2026-11-12 — Q5～Q8 聚合查核結果收到：DEMO 資料資格為當前核心風險

- Q5 正式材料主檔：1=鋼筋/kg；2=混凝土/立方公尺。
- Q6 8 Approved 中 7 件名稱或來源檔名疑似 DEMO／TEST；剩餘 1 件只是沒有被字串篩查命中，來源仍未核實。2 Draft 另有 1 件疑似展示、1 件來源不明。
- Q7 鋼筋 7 案 1 筆、1 案 4 筆；混凝土 7 案 1 筆、1 案 3 筆，對應 Q4 的 11／10 行，並非 11／10 件工程。
- Q8 證實 2 筆刪除交易與 2 筆關聯非 Approved／刪除建案交易完全重疊；其餘 21 筆未刪除交易與 Approved 活躍建案對應。
- 存檔 `docs/2A_HISTORICAL_DATA_QUALITY_Q5_Q8_2026-11-12.md`，作為來源認證／工程完整性流程下一步設計依據。只做文件更新、無正式 DB 或 C# 改動，交接狀態須同步。


## 2026-11-12 — 第八階段：Verified Reference 設計規格草案

- 使用者確認上輪同步／交接檢查無錯，開始將歷史資料 Q1～Q8 調查結論轉為可實作之需求提案。
- 建立 `docs/2A_VERIFIED_REFERENCE_DESIGN_V1_2026-11-12.md`，詳細設計來源與估算目的分層審核、證據可追溯、既有 Approved 保留、DEMO 不自動刪除／誤判、權限矩陣、資料表／API 提案、重審撤銷、舊資料預設未核實與 14 項驗收情境。
- 明確標記**尚未取得業務與老師確認，沒有程式、Migration 或 Supabase 變更**。下一步先檢查 ASP.NET History Publish/Reopen/模型參考查詢及授權實作，釐清切入點和安全遷移方案。
- 因下一個對話接手任務、資料治理策略及驗收準則都發生變動，依 Guardian 同步主交接、研究回溯、WORKLOG、新對話入口、決策中「草案非核定」欄位及完整審查記錄。


## 2026-11-12 — Verified Reference 後端接入位置／安全風險查核

- 讀取最新 C# Program、ProjectsApiController、ProjectManagementService、ProjectSimilarityService、DetectionCategoryRangeService、MajorCategoryPriceRangeService、AnalysisCaseService、AuditLogsApiController 等。
- 發現應優先釐清權限保障：Program 目前無 ASP.NET Core AuthN/AuthZ 註冊與管線；敏感歷史發布、重開、刪除、操作記錄 GET 無 [Authorize]。外部部署是否有有效入口限制未知。
- 發現 ProjectSimilarityService int overload 沒有 Approved 限制，DTO overload 則有 Approved；目前無 Verified 條件。數量模型已將同案同材料多筆交易按 ProjectId 聚合，不能重造此功能。
- 固化完整程式路徑、風險、分期方案及回歸測試於 `docs/2A_VERIFIED_REFERENCE_INTEGRATION_AUDIT_2026-11-12.md`。此輪只改文件，不改正式程式、DB、權重，不宣稱實測或企業核准。


## 2026-11-13 — Verified Reference 前端與部署設定靜態審查

- 使用者回報 GitHub 同步／Guardian 無錯，要求繼續。檢查 Vue router/App/api/Projects/ProjectDetail/AuditLog/AnalysisCaseDetail 與 frontend/nginx.conf、Dockerfile、ASP.NET Program。
- 前端路由無身份／角色守衛，操作紀錄在側欄無條件顯示；Nginx 僅 reverse proxy /api/ 到 backend:8080，未見內建 Auth；後端同樣未見 AuthN/AuthZ 設置。這不代表實際部署無外部入口限制，需請管理員確認。
- ProjectDetailView 已有 sessionStorage Idempotency-Key／receipt 對帳；舊 Approved 文案不應混同新 Verified，AnalysisCaseDetailView 已有 DEMO 名稱疑似標記可延續。
- 新建 `docs/2A_VERIFIED_REFERENCE_FRONTEND_SECURITY_AUDIT_2026-11-13.md`，包含權限分級、前端接口、部署核對、必要測試與分期方案。僅文件變更，未修改程式／資料庫。


## 2026-11-21 — 修正 UI 合成 Excel 缺少相依檔、整合永久 CI

- 使用者完成唯讀 7/7 API 預檢及 4/4 Docker/DB/API 來源核對：5263 與 55433 的 14 案指紋、14 版本、28 Runs 及 210 Approved 均可對照；但 UI 寫入尚未驗收。
- 使用者首次產生測試工作簿時遇到 `ModuleNotFoundError`，確認 feature 分支缺少 main 既有的 `export_full_project_excel.py`，而 exporter 依賴 `generate_irregular_excel.py`。已同步兩者到 feature，新增 `test_prepare_ui_case.py` 作可長期運行的**真實匯出契約測試**；後續 `ENOENT` 是 XLSX 不存在，不能當成 API 解析失敗。
- 使用者要求不要建立臨時 CI/CD，故保留長期 `.github/workflows/2a-build.yml` 作正式 build/test 入口、保留 handoff guard，移除重複的 `graduation-ui-build.yml`；不清除使用者本機未追蹤檔或 DB。
- 下一步同批 cherry-pick 五個 commits 並跑 Windows 本機 Excel 產生器和唯讀解析；真實 UI 上傳與資料持久化、決標回寫、異常準確度仍尚未完成。


## 2026-11-21 — UI smoke fixture 不再依賴本機未存在的大型來源資料夾

- Windows 對完整相依模組跑 Python 2/2 PASS，但指定 `graduation-src-extreme-v3` 缺 manifest，產生 Excel 與後續唯讀解析並未執行成功。
- 修正 `prepare_ui_case.py`：不帶 `--source` 時內建製作獨立合成多工作表 XLSX + truth JSON（不使用歷史、DB、API）；保留指定來源時 strict SHA 驗證；絕不可把 UI fixture 當 holdout 或決標真值。
- 在既有永久 Python 合成產生器測試加入真正 CLI 測試；不新增或刪除任何其他 CI。接下來只需同步本批兩 commit 並重跑無 `--source` 的指令。


## 2026-11-21 — UI 9/9 預檢通過；修正 Windows 測試子程序 CP950

- 使用者本機最新 7 commits cherry-pick 成功，`graduation-ui-smoke-1121-02.xlsx` 已真實生成（15 筆，3 sheets），`verify-ui-preflight --excel` **9/9 PASS**（15 候選列、4 警示），不必重新產檔。
- 本機 Python 永久回歸 3 項中 2 PASS／1 ERROR；Windows 預設 CP950 stdout 和父程序 UTF-8 decoder 衝突，與 Excel 匯出/API 成功無關。改用 `PYTHONIOENCODING=utf-8` 限定測試子程序，不變動全機環境。
- 擴充既有 `verify-ui-state.mjs --case-id` 為正式 UI 後續保存驗收：核對 current version 的 Quantity／MajorPrice:結構體工程 同一版兩筆 Run 及完整結果讀回；仍全部 GET/SELECT，不產生新 Run 或歷史資料。永久 `2a-build.yml` 不變、不建臨時 CI。
- 下個唯一未驗證工作：Windows Python 測試修正實測；UI 上傳與保存 Run／刷新驗證，再來決標草稿及權限稽核；準確度最後。


## 2026-11-21 — UI #15 價格與數量畫面首次真實成功，排除明細解釋
- 使用者瀏覽器截圖顯示 V1 15 工項、數量偵錯與結構體工程大項價格偵錯均有回應；價格 Run #31 顯示已保存、總價 29,268,000 元。這是 UI 操作成功，不代表跨刷新查詢或準確度 PASS。
- 檢查後端兩個既存服務：預檢的四筆 warning 對應含加工/組立/綁紮/工資的三筆鋼筋、含澆置的一筆混凝土；在材料數量模型排除但不從大項價格剔除。因此鋼筋量 38,000kg 與混凝土 3,650m³ 相對於原始 130,000kg/5,450m³ 是分類範圍差異，不是算術漏加；複合項目是否需材料部分仍要人工審核。
- 保留現有資料與 Run，先跑永久 `verify-ui-state --case-id 15`，再驗證跨重新整理。只更新文件，不改正式估算 scope/DB/模型參數。
