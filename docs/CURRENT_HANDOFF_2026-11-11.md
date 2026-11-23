## 2026-11-23 T08 初次 runner 結果

Run 45120741485 首次編譯在隔離 fixture 的可空建案日期寫入非空交易日期失敗，估算 HTTP 未執行。已於 fixture 明確拒絕空日期後重跑。補上 [外部資料最小驗收需求](T00_T11_EXTERNAL_INPUTS_2026_11_23.md)。

## 2026-11-23 研究證據封存及 T08 真正估算路徑補測

以 `60d442f629f428712ae34d4430d9c0f072564dba` 為基線。已驗證凍結廠房來源與逐案 T05 指標、等 K 參考筆數、群數共用試驗鍵。研究 ZIP 包含 T00～T11 索引、原始逐案、歷史隔離產物及清冊。新增獨立 T08 workflow，僅用隔離 PostgreSQL 測版本讀取與數量、價格估算 POST，32 案合成歷史，2／20／100 筆待分析工項。保留既有 T07／T09 PASS。新 workflow 尚待執行證據。

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

## 2026-11-23 Run #3：研究隔離 E2E 與 Word 八頁人工目視最終核對

GitHub Actions [Run #45057125296](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45057125296) 實際三 Job SUCCESS；head `da4324596707969ee805a37f68e284a0c0f955d0`。下載原始 [isolated-api Artifact #14767619273](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45057125296/artifacts/14767619273)，核對 ZIP 內 4 份 SHA256SUMS、48 筆 JSONL（47 筆 `passed=true`、30 筆 benchmark、0 筆 `passed=false`）。隔離 PostgreSQL EF T04 PASS；T07 Draft→人工映射→Publish→Reopen、狀態回讀、重複操作與跨案金鑰 409 PASS（上傳建檔未測）；T09 匿名 401、錯誤 token 401、Reader 403、Supervisor 200，實際讀回 UpdateMapping／PublishHistoryBaseline／ReopenHistoryReview，**研究隔離 API+DB 權限 E2E PASS**；T08 30 次建案讀取 HTTP p50=19.7865 ms、p95=35.7117 ms，非完整估算 API 效能。

下載原始 [word-rendered-pages Artifact #14768340468](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45057125296/artifacts/14768340468)，PDF SHA256 `d7349cd99a85b5fe1f86109245ad4bdaa07b28258537091d670775dc04ae25f9`，DOCX SHA256 `d9fb05c1e4b83422a257f2fb3d3e9ed3d8abbbc275265311b931d511ee2f9ccf`；8 頁 PDF + 8 張 PNG 均與 SHA256SUMS 完全一致。已**人工逐頁目視 1–8 頁**：第 3 頁續表表頭重複可見，第 4 頁不再有舊頁首孤行，頁碼 1–8 全部可見；圖 1–8、圖說、中文字無明顯裁切、溢出與亂碼。**Word 渲染 PASS、人工視覺版面 PASS**；5–8 頁留白仍多屬非阻擋性美觀建議。原報告為歷史研究快照，內文舊的 PostgreSQL NOT RUN 不能代表最新 CI 狀態；圖表數值未於本次重算。

上述 PASS 只涵蓋明確實測子項，T00–T11 原始整體驗收仍 PARTIAL（需住宅原始快照、獨立標單、工程師真值、真人 AB/BA、完整 B1/API 負載等）。正式 2A、A0、正式 Supabase 未更動。後續研究請先處理缺失的獨立工程資料與人工驗收，或另行專門排程剩餘整合測試，勿重新做已通過的 Run #3。詳情見 [Word 逐頁](WORD_VISUAL_REVIEW_2026_11_23.md) 及 [Actions 驗證](RESEARCH_ACTIONS_RUN_VERIFICATION_2026_11_23.md)。

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

## 最新收尾進度（2026-11-22）：合成留案回測與144組參數對照已實測

使用者本輪重新指定先完成「準確度回測、演算法比較、整合回歸」三個工作包；本節優先於先前把 accuracy 永遠排最後的舊工作順序。已完成的SRC來源3條API、9組smoke與6組敏感度不重做，不重建或匯入合成歷史。

- 報告：`docs/HOLDOUT_EVALUATION_2026-11-22.md`；機讀摘要同名 `.json`。既有300案資料實際為210 train / 45 validation / 45 test，非90個獨立test。原機5263正式range API已完成237個project/scope配對（474次唯讀運算）；每組核對train lineage、SRC-only、來源數值、目前Top8參考ID及區間重現。settings/case API快照前後不變，沒有新案件、Run、歷史或設定寫入。
- 144組權重/TopK/時間半衰期/相似指數只以validation挑選，test只比較現行與該選定組。validation最佳為40/25/10/15/5/5、K32、half-life3、exponent1；但SRC價格的未注入非極端test子集(n5) MAPE14.56%→24.33%、coverage60%→20%，因此**保留正式25/30/10/15/10/10、K8與原設定，不整套替換**。SRC鋼筋(n8)現行MAPE17.25%/coverage75%，混凝土(n8)9.92%/75%；候選分別13.20%/75%、7.94%/87.5%。不能只挑改善的項目宣布模型升級。
- 13份既有paired XLSX/truth已驗hash；只來自3案，所有variant仍manual_review_required。無注入不等於工程正常；現有真值不能識別Precision/Recall/FPR，保留null並列資料定義阻塞，不為產出百分比而重傳/重算。3個曾被診斷的test案另排除提供42案補充結果。實驗不是時間順序預測，也不是企業真實工程accuracy。
- 新增永久`tools/graduation-synthetic/evaluate_live_holdout.py`與8項測試；只允許2類range POST及GET settings/cases，拒絕history/upload/settings寫入；來源hash/case-level split、baseline replay、settings/cache一致性與拒覆寫已實作。
- 同批修復`TenderImportInspectionService.cs`的Unit null輸出(CS8604)與`崴正.csproj`顯式Relational10.0.5對齊(MSB3277)，不改分類、模型、schema/Migration。Windows API Release build **0 warnings/0 errors**、WorkbookParser全通過、Python11/11、兩個Node syntax PASS；GitHub既有永久CI提交後另核實，沒有新workflow。
- **未完成**：原桌面仍為5c0ae5c，這輪程式只在可寫clone/GitHub，未套回原機或重啟5263；quantity-scope 404、新版6/6與Docker權限、既定系統整合/PR#2合併/最終版本仍待處理。不可宣稱整個專案或企業模型驗收完成。

- 交接gate首跑另發現Git預設quotePath將中文檔名轉義、導致假缺漏；已改讀NUL分隔路徑，加入真實temp Git repo/core.quotePath=true中文路徑測試。首跑FAIL保留，修正後完整Python11/11 PASS，交接gate於最終commit再核對。

---

## 最新接手狀態（2026-11-21）：工作區修復與 Windows 驗證

- GitHub 開發基線為 `a27f850d356aa46b0f201dc2e672af3341e9014c`。桌面 `integration/src-closure` HEAD 為 `dadc7aa31920638c3a151eca9da67d684263ea73`，仍停在最後一筆 `a27f850d` 的 `docs/HANDOFF_CHANGE_REVIEWS.jsonl` 衝突；不得把遠端提交或工作區修復說成桌面原工作樹已修復。
- 已在可寫工作區建立獨立 clone，分支 `integration/src-closure-recovery` 重現並完成上述 cherry-pick，修復 commit `ab32e344ef76f67e8c0134f0592ba5b74f136a1f`（僅該 clone 的本機 commit）。完整保留雙方 19 筆 JSON 紀錄，無重複 change_id；工作樹無衝突。逐檔比對 `崴正/`、`tests/`、`scripts/`、`tools/`、`frontend/src/` 與 GitHub a27f850d 無差異。整合副本保留自己的前端 package/package-lock 版本，不冒稱與 feature 依賴鎖檔相同。
- **本輪 Windows 實測 PASS**：.NET SDK 10.0.301，使用現有離線 NuGet 快取還原後，API Release build 0 errors；`dotnet run --project tests/WorkbookParserTests/WorkbookParserTests.csproj -c Release --no-restore` 全部檢查通過，含 case15 15 列、4 筆材料相關排除、38,000kg／3,650m³、價格 29,268,000；Python 3.12 `python -m unittest discover -s tools/graduation-synthetic -p 'test_*.py' -v` **3/3 PASS**，CP950 子程序回歸已在本輪 Windows 驗證；兩個 Node 唯讀工具 `node --check` PASS。
- 前端沿用桌面整合副本已安裝的 node_modules（未執行 npm ci）：標準 build 的 vue-tsc 已通過，但 Vite 預設 esbuild config loader 因本執行環境無權枚舉上層目錄而失敗；改以 `npm.cmd run build --prefix frontend -- --configLoader native` **PASS**（vue-tsc + Vite 7.3.8，66 modules）。這是此次工作區的啟動參數，不修改正式 Vite 設定、套件或 CI。
- **現存警告**：`TenderImportInspectionService.cs:135 CS8604`；Parser 測試建置另有 EFCore.Relational 10.0.4/10.0.5 的 MSB3277。測試雖通過，不能隱藏警告或宣稱已消除。
- **實測阻塞／未驗收**：`node scripts/verify-ui-state.mjs --case-id 15` 在 Docker named pipe 回 `permission denied`，Docker config 亦不可讀；未完成任何 DB 身分或 6/6 檢查。獨立 HTTP GET `/api/analysis-cases/15` 為 **200**，取得 currentVersionId=15；GET `/api/analysis-cases/15/versions/15/quantity-scope` 為 **404**。因此現有 5263 尚未提供新版端點；不得寫新版 UI/6/6 已 PASS，也不能僅由 404 斷言具體原因。
- **下一步**：在有桌面寫權及 Docker 權限的使用者環境完成最後 cherry-pick，停止原 5263，使用原已確認的 55433 隔離連線重啟新版 API；先確認 quantity-scope HTTP 200，再重跑永久唯讀 checker 6/6。不要重 seed、重傳 V1、重跑既存 Run 或發布 smoke 為歷史。決標／稽核／準確度仍未完成。
- 本輪只更新交接證據，沒有改演算法、API 實作、DB schema、Migration、既有 DB 或 CI；未將開發程式合併 main。

---

# 崴正營造 2A｜完整開發交接主檔（2026-11-11）

> **此文件是新對話的 CURRENT SOURCE OF TRUTH（目前狀態入口）**。歷史 docs/HANDOFF.md、docs/NEXT_CHAT_HANDOFF.md 的 2026-10-15 快照是歷史紀錄，**不可蓋過本檔**。
>
> 原則：GitHub 最新程式碼／當前 DB 唯讀查核／使用者明確測試回報，優先於歷史文字；對未查證內容明確標注，不得宣稱「百分百完成／正式準確率」。

## 0. 30 秒摘要

- 專案：雲科大 × 崴正營造 2A「量價估算與異常偵測」。GitHub `YI-CHENLIN1204/BiddingSystem-2A`；**主要分支 `feature/1d-api-integration`，不得自行合併 main**。
- **正式功能範圍**：鋼筋／混凝土「數量偵錯」、結構體工程大項「價格異常」。UI 標題用通用名稱，適用範圍另外描述。不得恢復 Z-score、LSTM 作正式模型或已淘汰通用估算服務。
- 系統兩條路：`AnalysisCase → AnalysisBidVersion (V1/V2/V3) → AnalysisRun` 為待偵錯工作／快照；`ProjectMaster Draft → FinalBidItem 人工審核 → Publish History Approved → HistoricalTransaction` 才是歷史基準。**AnalysisCase ≠ ProjectMaster**。1D 匯入可選，2A 可自行上傳。
- 現行正式演算法：六因子建案相似度、相似度平方、時間半衰期、Top K、P15/P50/P85；正式數量按每 m² 用量再乘本案面積；正式價格按結構體工程每坪金額；詳見第 3 節。
- **2026-11-10 讀取 DB 快照**：10 件未刪除 ProjectMaster（8 Approved 全是住宅 RC，2 Draft）；其中 7 件 Approved 名稱辨識為 DEMO／模擬／測試。廠商表示另有約 20 件原始建案（住宅／廠房 × RC／SC／SRC），**不能當作已匯入的 20 件核准歷史樣本**。資料庫即時狀態須重新 SELECT 查證。
- 教授同意**專題報告使用模擬資料**；產學版模擬資料只供研究，**不可混入正式歷史基準**。資料生成器故意使用假設數值與虛構企業偏差，絕非真實施工規範。
- 研究目前已實作 `research/2a_synthetic/`，包含 `generate.py`、`compare.py`、`batch.py`、`production_weight_review.py`、`test_research.py`。
- **已由使用者貼出實測**：`batch.py --seeds 10 --cases-per-combo 30 --train-sizes 3 5 8` 正常完成 **5760 組模擬方法測試／96 列彙總**，原有 **7/7 unittest 通過**，曾合併到 GitHub commit `3986920`。
- **最後一次使用者回報（本次交接）**：「沒有錯誤」：針對其後新增的 `production_weight_review.py` + 新增測試，**使用者口頭表示通過；未收到完整新執行輸出及實際 CSV**，因此結果數值尚未供本對話分析；不可自行捏造哪組模型獲勝。
- **下個開發任務：先讀取／檢查 `production_weight_summary.csv` 和 `detail.csv`，再強化真正公平的模型比較與回測，不先改正式產學演算法。**

## 1. 文件、程式優先閱讀順序

1. `docs/CURRENT_HANDOFF_2026-11-11.md`（本檔，最新）
2. `docs/RESEARCH_TRACEABILITY_2026-11-11.md`（證據來源／版本／實驗可重現／未驗證）
3. `research/2a_synthetic/README.md`
4. `research/2a_synthetic/generate.py`、`compare.py`、`batch.py`、`production_weight_review.py`、`test_research.py`
5. `docs/2A_MODEL_AUDIT_2026-11-10.md`
6. `docs/2A_PUBLIC_DATASET_FEASIBILITY_2026-11-10.md`
7. `README.md`、`docs/2A_CURRENT_ARCHITECTURE.md`、`docs/DECISIONS.md`、`docs/API.md`、`docs/DATABASE.md`
8. `docs/DEVELOPMENT.md`、`docs/LOCAL_TEST_GUIDE.md`、`docs/VENDOR_QUESTIONS.md`、`docs/WORKLOG.md`
9. 舊的 `docs/HANDOFF.md` 和 `docs/NEXT_CHAT_HANDOFF.md` 為**歷史狀態**，只供查舊決策與舊操作；遇到衝突本檔優先。

## 2. 產品架構、功能與限制

### 技術
- Backend：ASP.NET Core Web API、.NET 10、EF Core、Npgsql；`崴正/`。
- Frontend：Vue 3、TypeScript、Vite、Bootstrap；`frontend/`；Nginx Docker 正式入口。
- DB：PostgreSQL（Supabase）。使用 Docker Compose `compose.yaml`，frontend 暴露 `http://localhost:8081`，backend 容器內 8080。
- 必要 `.env`：`DB_CONNECTION_STRING`（映射 `ConnectionStrings__DefaultConnection`）；選配 `ONE_D_API_BASE_URL`；**不得放密碼、TOKEN 在對話／GitHub 文件**。
- 本機 Windows PowerShell，工作目錄例如 `C:\Users\lyich\OneDrive\桌面\BiddingSystem-2A`。不能假設新裝置同路徑。
- GitHub 最新內容在 `feature/1d-api-integration`；GitHub 預設 main 不等於此分支。

### 人的操作流程
**待偵錯案**：建立案件（1D 選配匯入／2A 上傳 Excel、工作表手動選擇）→ 填建案基本條件 → V1/V2/V3 版本快照 → 選擇數量偵錯或價格異常 → 儲存分析 Run → 可查歷史結果與參考來源。此路線不得直接寫入歷史交易。

**正式歷史案**：上傳**真正商務定案的最終標單** → 唯讀預覽與選工作表 → 建 `ProjectMaster` Draft + `FinalBidItem` → 核對來源列號、工項、材料與單位、數量／價額、完整性 → 人工 Publish → Approved → 正式 `HistoricalTransaction`。Approved 修改需 Reopen Review。原始分類 Category1/2/3 保留，人工覆寫 `major_category_override`。

### 分析範圍／呈現
- 數量偵錯只支援鋼筋（kg）與混凝土（m³）細項；依歷史 **單位樓地板面積用量** 計算區間，再乘本案面積為總量。不要錯把 kg/m² 顯示成 kg。
- 價格異常目前只支援「結構體工程」大項。歷史價格用 `FinalBidItem.Amount` 或 `Qty×UnitPrice` 加總，以每坪價計算。與 `HistoricalTransaction` 的數量職責分開；**跨年行情未校正**。
- 模型結果 P15～P85 是參考歷史範圍，不等於工程法定上下限、準確率 70%、信心度或正式預測保證。
- 已有 ESS 與單案權重占比於新結果顯示；舊 Run 快照不會自動補欄位。
- 先前展示 V3 模擬 Run #18 數量（鋼筋偏高、混凝土正常）、Run #19 價格（結構體偏高）；這是**模擬環境功能測試，不是工程有效性證明**。
- 2026-11-10 唯讀 SQL 由 Run #18、#19 計算 8 筆參考案 ESS 7.34、最大單案權重 18.30%；這反映權重集中度，不證明來源真實。
- `docs/DEMO_DATA_20261015.md`、`scripts/cleanup_demo_2a_20261015.sql` 有演示樣本紀錄及清理腳本；**不要未確認依賴與權限就自動刪資料**。

## 3. 正式演算法／參數（來自 2026-11-10 的 DB 唯讀快照，不代表現在必然一樣）

六因子線性加權相似度：

```text
structure 25; floor area 20; site area 10; ground/basement floors 10; project year 0; city 5
Total = 70, S = 100 × sum(partial scores)/70
面積差距及基地面積差距 >= 50% 時該子分數為 0；
地上每差一層扣該樓層權重的 3%，地下每差一層扣該權重的 10%；
縣市相同加 5 分，不同加 0；結構相同加 25，不同加 0。
```

候選先按建案類型一致選取（住宅／廠房），`MinimumSimilarityScore=0`，**RC／SC／SRC 不同只有扣分，並非硬排除**。由個別材料可用的歷史案按相似度排序、再按最終權重排序，取 TopK=8；最低有效案名義數 5。

```text
similarity_weight = (similarity_score / 100) ^ 2
time_weight = 0.5 ^ (abs(project_date - history_date) / (3 years))
final_weight = similarity_weight × time_weight
P15/P50/P85 = weighted empirical quantile over historical usage/m² (or price/ping)
ESS = (sum w)^2 / sum(w^2)
largest_reference_share = max(w)/sum(w)
```

**注意**：相似度的取值、權重與 TopK 不是經真實資料校準的最優設定。結構異質、基礎面積雙重計數、歷史價格基期、日期 `abs` 在時間回測引發未來資訊洩漏、只有計數門檻無完整來源品質門檻，是未解問題。正式生產現行程式中 ESS 只顯示，不影響異常燈號。

## 4. 數據現實與研究倫理

廠商據稱提供約 20 件原始歷史建案，分類為住宅／廠房 × RC／SC／SRC，各組很可能只有 1～3 件。不能拿數百筆工項當數百件獨立建案。

三類資料：

| 類別 | 保存位置／用途 | 是否正式基準 |
|---|---|---|
| 崴正真實完整核准歷史標單 | 正式 ProjectMaster/FinalBidItem/HistoricalTransaction，明確 Publish | 只有來源、範圍、核准合格時才是 |
| 外部公開／研究資料（台灣建研所、FutuRaM 等） | 另建來源追蹤、獨立研究資料集；授權／口徑需核實 | **否**，只供研究與另行驗證後外部參考 |
| 自行生成模擬資料（專題允許） | `research/2a_synthetic/output/`，專題情境評測、算法驗證 | **否**；不能匯入生產歷史表 |

RMS 台灣建案統計平台需購買／登入，沒有帳號，**已排除立即可用資料集**。其他公開來源並非已驗證可直接取得完整台灣 RC/SC/SRC 住宅／廠房最終決標標單，詳見資料集可行性文件。

模擬資料能讓專題演示、研究運作不受「只有 20 件」限制，**不能**解除真實工程可靠度驗證所需的資料限制。不可拿模擬結果聲稱「實務準確率 93%」。

## 5. 模擬研究：腳本、輸出、參數、用途

所有腳本使用 Python 3.10+ **標準函式庫**，與正式 DB 無連線：

- `generate.py`：六組（住宅／廠房 × RC／SC／SRC），鋼筋／混凝土雙材料列；固定 seed 可重現。四情境 `stable`、`high_variance`、`company_shift`、`outliers`。虛構公司 12% 差異是研究者假設，基準數量與單價也是研究假設，不是實際工程參考值。每組 30 案預設 180 獨立合成建案／360 材料行。
- `compare.py`：中位數、描述性常態平均±1 SD、加權百分位、面積-only 加權，目標案 holdout，確定性 SHA256 抽選參考資料，不是時間前推驗證。
- `batch.py`：跨四情境、10 seeds、每組 30 案、可用參考數 3/5/8、四方法。**使用者實測完成** 5760 group runs / 96 summary rows；現有 7 unittest 全過（貼出完整 PowerShell 輸出）。
- `production_weight_review.py`：第三輪，對應正式六項因子公式的研究版、時間權重、TopK、加權 P15/P50/P85；三設定 `current_no_structure_gate`、`current_with_structure_gate`、`no_site_area_with_gate`，包含 ESS、跨結構引用率。**使用者回報「沒有錯誤」**，但尚未提供詳細 console log 或實驗產出的數字。不能說其模型比較結果已完成分析。
- `test_research.py`：先前 7 測試；後續增加正式權重重現／結構門檻與批次重現性測試。當前**新增測試實際通過數**未由完整輸出確認。

執行（從 repo 根目錄）：

```powershell
git status
git fetch origin
git merge --ff-only origin/feature/1d-api-integration

python research/2a_synthetic/batch.py --first-seed 20261111 --seeds 10 --cases-per-combo 30 --train-sizes 3 5 8
python research/2a_synthetic/production_weight_review.py --first-seed 20261111 --seeds 5 --cases-per-combo 30 --train-sizes 3 5 8
python -m unittest discover -s research/2a_synthetic -p "test_*.py" -v
```

資料輸出在 `research/2a_synthetic/output/`（`.gitignore` 排除 Git）；`batch_detail.csv`、`batch_summary.csv`、`batch_manifest.json`；`production_weight_detail.csv`、`production_weight_summary.csv`、`production_weight_manifest.json`。**GitHub 不含本機研究輸出**，若要評估實際 MAPE 優劣，必須請使用者提供 CSV／將匿名研究彙總檔上傳。

模型與區間公平性限制：中位數插值分位、常態平均±1 SD、加權階梯分位**不是相同的預測涵蓋準則**，不得只比較涵蓋率就說哪個較好；下一步需相同目標覆蓋率／更好的真實回測。模擬資料的結構用量差異是預設值，硬門檻模型若在此情境較佳可能是自我設定使然。

## 6. 已驗證／尚未驗證清單

| 項目 | 證據 | 認定 |
|---|---|---|
| GitHub 功能提交 | commits 見追溯文件 | 已提交（非等於部署） |
| `batch.py` 5760 組與 96 彙總 | 使用者完整終端日誌，2026-11-11 | **已實際執行通過** |
| 當時 `unittest` 7/7 | 使用者完整終端日誌 | **已實際執行通過** |
| 第三輪正式權重研究腳本 | 程式已提交，使用者回報無錯誤 | 已回報通過；**詳細輸出未附** |
| GitHub 主系統分析 UI + ESS 程式 | commits `053bf749` ～ `986065e` 等 | 已提交；本對話尚無當前 Docker UI 完整驗收日誌 |
| 2026-11-10 DB 條件與 Run #18/#19 ESS | 唯讀查庫 | 只限查詢當時，不代表永久不變 |
| 真實工程估算準確率／信心度 | 沒有足夠可比真實 holdout | **未完成** |
| 公開資料與正式資料整合 | 尚未實作 | **未完成** |
| 模擬資料與正式表的隔離 | 目前 Python 研究工具根本不連資料庫 | 已在研究工具隔離；**正式 DB 仍留早期 DEMO 測試案，需另處理** |
| 產學正式交付驗收 | 尚缺真實資料代表性與結果驗證 | **未完成** |

## 7. 下一步（按次序，避免重複／無止境優化）

**P0 第一件**：取得 `production_weight_summary.csv`、`production_weight_detail.csv`，先回答模擬的三組權重差在哪、跨結構參考比例多少、ESS 是否穩定、在哪些情境會失敗。不以只跑通作為研究已完成。
**P0 第二件**：全面審查 Python 研究版與 C# 正式演算法是否等價（6 因素、缺漏欄位、TopK 排序、時間、P15/P85、參考來源、數值精度），寫「差異列表＋單元測試」，不要直接改正式服務。
**P1**：公平比較相同目標涵蓋率的預測區間，加入樣本數與權重敏感度。跨結構納入是否允許應按鋼筋／混凝土分別決策。
**P1**：20 件廠商原始真實建案建立用途 × 結構 × 工項可用性矩陣，核對是否完整最終標單、單位與價格範圍。不能宣稱目前已完成匯入。
**P1**：實際下載／核實台灣建研所 PDF 附錄與 FutuRaM Excel 真正的欄位、授權與口徑。RMS 付費平台先排除。
**P2**：研究公司差異的分層／收縮估計；企業優先權重不能任意固定乘倍率。研究停止條件是簡單方法已達成事先約定驗收，而不是必須寫完 LSTM 或所有模型。
**交付**：專題可展示模擬研究結果（標示假設與限制）；產學版必須隔離資料源、確保真實歷史、樣本不足時僅提供參考／人工複核，未校準不顯示信心百分率。

**不要重做**：已通過的 5760 批次和原 7 個測試，除非比較方法、輸入或關鍵程式碼有變；下一輪應集中分析結果、測未測差異。不要動 `main`，不要刪 Supabase 或示範資料來「修正」成功率。

## 8. 安全、Git 和可回溯

- Git commit 是不可變的程式快照；保留 commits 的 SHA 與對應文件。要回溯先 `git log --oneline --max-count=30`、`git show <SHA>`，不要使用 `reset --hard` 破壞本機資料。
- 新對話不能只憑摘要假設使用者工作目錄乾淨；先 `git status`。先看最新 `docs/CURRENT_HANDOFF_2026-11-11.md`，再看程式與 docs/WORKLOG。
- 本機研究 CSV 未 push 且被 gitignore 排除；若要跨對話保存實驗結果，上傳 CSV，或在新對話同種子重跑。
- `git merge --ff-only origin/feature/1d-api-integration` 是避免非快轉覆蓋的優先方式；如有衝突先回報，勿覆蓋。
- 不接觸正式 DB 寫入，不自動刪除示範資料；變更生產公式要先有實驗與使用者明確同意。
- 不分享或記錄資料庫密碼、密鑰或個人資料。

## 9. 新對話可以直接貼的指示（完整版本在 docs/NEW_CHAT_START_2026-11-11.md）

```text
請直接接手 GitHub YI-CHENLIN1204/BiddingSystem-2A 的 feature/1d-api-integration。
先讀 docs/CURRENT_HANDOFF_2026-11-11.md、docs/RESEARCH_TRACEABILITY_2026-11-11.md、
research/2a_synthetic/README.md、docs/2A_MODEL_AUDIT_2026-11-10.md，
並以最新程式碼核對，勿沿用 10/15 的舊交接假設。
最近已完成批次模擬 5760 組／96 彙總、7/7 舊測試通過；
第三輪 production_weight_review.py 與新增測試使用者回報沒有錯誤。
請先分析本機 production_weight_summary.csv 與 detail.csv（若未提供，先執行固定命令產生）
及程式對照差異，不要重新設計系統或重做已完成研究。
專題模擬資料與產學真實歷史必須分離；不改正式 DB／權重，不合併 main。
```


## 10. 2026-11-11 新增專案 Skill：變更後必審交接

- 唯一 Skill 識別：`waizheng-2a-handoff-guardian`；檔案 `.agents/skills/waizheng-2a-handoff-guardian/SKILL.md`；全 repo 代理入口 `AGENTS.md`。
- **每次修改**（程式、模型、研究、設定、資料庫、文件）完成後，必須重新評估交接影響，並在 `docs/HANDOFF_CHANGE_REVIEWS.jsonl` 新增 JSON 審查紀錄。若改動使架構／現況／決策／測試與下一步不同，應實際更新本主交接、`docs/RESEARCH_TRACEABILITY_2026-11-11.md`、`docs/WORKLOG.md`、`docs/DECISIONS.md` 或其他受影響文件；即使不必更新也需記原因。
- 參考可執行檢查：`python scripts/check_handoff_review.py --base <該輪變更前 SHA>`，確認本輪所有檔案都被新增審查記錄涵蓋。GitHub PR（目標為 feature 分支）有 `.github/workflows/2a-handoff-review.yml` 自動 gate，另可手動觸發；直接 push 不會被 PR gate 自動攔截，操作方仍必須遵循 Skill。
- Skill 是版本化的 repo 規則，不可誇稱已自動安裝至所有 ChatGPT 對話。新對話必須明確讀取 `AGENTS.md` 與 Skill 檔案。
- 此項治理機制不改任何正式量價計算或研究數據；已提交程式與 CI 規則仍需由 GitHub Actions／本機驗證執行情況，不等於已由 CI 完整測過。


## 11. 2026-11-11 第四輪工具：第三輪權重結果自動成文

- 新增 `research/2a_synthetic/report_weights.py`，讀取本機 `production_weight_summary.csv` 與 `production_weight_manifest.json`，輸出 `production_weight_report.md`。
- 報告必須以**實際 CSV** 為根據，按情境、樣本數、鋼筋／混凝土比較三組六因素權重，統計 MAPE 改善／惡化組數、ESS、混入不同結構的參考比例。缺少資料、資料不是明確標記模擬或某組設定遺漏，拒絕產生報告。
- **尚未取得使用者本機的第三輪 CSV**，所以目前無權重優勝者、實際 MAPE 數值的研究結論；此輪只交付研究分析工具與單元測試程式。
- 下一步：`python research/2a_synthetic/report_weights.py`，上傳 `research/2a_synthetic/output/production_weight_report.md`，再由新對話判讀結果並確認 Python／C# 模型等價性。正式 DB／生產模型權重不變。


## 12. 2026-11-11 最新研究結論與下一個驗證點

- **已收到使用者上傳的 `production_weight_report.md`**：24 組情境 × 三方案（72 筆彙總比較），A 17.10%、B 17.07%、C 16.91%（24 個已四捨五入 MAPE 的簡單平均）；平均差約 0.03 與 0.19 個百分點。正式審查：`docs/2A_SYNTHETIC_WEIGHT_RESULT_REVIEW_2026-11-11.md`。
- A 模型跨結構參考比例平均僅 0.18%，所以本次無法有效評估跨結構混用；且 C 同時改變 3 個權重與硬門檻，不能推論基地面積「不重要」。
- **最新研究新增** `research/2a_synthetic/factorial_weight_review.py`（六方案拆因子配對），擴充 `production_weight_review.py` 可由研究呼叫端注入設定，保留原本 A/B/C 舊比較。請以最新 `research/2a_synthetic/README.md` 執行新命令，等待本機跑出的 `factorial_weight_report.md` 分析。
- 最近使用者曾貼出 **11／11** 研究測試通過，交接守門審查亦通過。這個證據**發生在本輪新增因子拆解腳本之前**，所以不能宣稱新增測試已通過。
- 下一優先：產生並分析因子拆解配對報告；其後才設計同結構只有 0～3 案的壓力測試、真正時間前推回測及公平區間對比。**不調整正式產學模型、資料庫及主分支**。


## 13. 2026-11-11 第五輪使用者實測／第六輪研究新階段

- 使用者在本機 `C:\Users\User\Desktop\BiddingSystem-2A` 執行第五輪：`factorial_weight_review.py --first-seed 20261111 --seeds 5 --cases-per-combo 30 --train-sizes 3 5 8`，終端顯示 **4320 group runs、144 summaries、192 pairwise rows**；13/13 tests OK；交接審查 9 files / 1 review OK。這些是**程式執行驗證**，不是對實驗配對 MAPE 勝負的判定，因 `factorial_weight_report.md` 仍未提供。
- 新增第六輪 `research/2a_synthetic/sparse_structure_review.py`，刻意測試每個相同結構只有 0/1/2/3 件可比較歷史的情形。區分數學上能輸出結果、達到名義最低 5 件門檻、跨結構混用比例、模擬 MAPE。新工具／新增測試已提交，**尚未取得用戶實測輸出**。
- 下一輪請先保留並上傳 `output/factorial_weight_report.md`，及新 `output/sparse_structure_report.md`；完整指令見研究 README。交接資料與研究結果只用於專題，正式 .NET 判定與 Supabase 均未修改。


## 14. 2026-11-11：第五／六輪報告已收到，正式結論轉向「可比性」

使用者已上傳 `factorial_weight_report.md` 與 `sparse_structure_report.md`，詳細數據及限制**已永久記錄**在 `docs/2A_FACTORIAL_SPARSE_RESULTS_2026-11-11.md`。

- 第五輪：權重重新分配（固定其他條件）在模擬中平均改善約 0.165～0.180 MAPE 百分點；基地面積直接去權重約改善 0.102～0.131 百分點；結構硬門檻在同結構資源充裕情況僅改善約 0.029 百分點。均不足直接修改產學正式權重。
- 第六輪：同結構歷史僅 0～3 件時，現行無硬門檻在假設其他結構案例充足的模擬條件下，仍 100% 通過名義最低五案，但跨結構參考占比分別 100%、87.5%、75%、62.5%。以模擬情境四組簡單平均，無硬門檻 MAPE 約 73.82%、59.67%、47.90%、35.03%；硬門檻 0/1 件無可計算，2/3 件僅技術可算，**沒有一組達到五件門檻**。
- 工程實務不可直接使用上述 MAPE；生成器先人為設定不同結構有不同材料強度，且其他結構每類有 20 件虛構候選。結論是「名義樣本數可合格而同結構可比性不足」，**不是**「已證明正式 2A 跨結構錯誤率」。
- **新開發優先順序**：先研究可比性／同結構樣本資格診斷與跨結構材料差異敏感度，其次時間前推／同涵蓋率公平比較。第五／六輪結果已分析完畢，不要重複做一模一樣的權重實驗。產學 .NET 及 Supabase 不變。


## 15. 2026-11-12：研究版歷史參考資格診斷

- 使用者 2026-11-12 凌晨貼出 `git merge --ff-only` 更新到 `cedf7df` 成功，以及 `python scripts/check_handoff_review.py --base 705efe3` 正常輸出 **6 個修改檔／1 筆新增審查紀錄**。這只確認文件完整性檢查通過，不代表第五／六輪研究已在真實公司案驗收。
- 第七輪新增 `research/2a_synthetic/eligibility_review.py` 與三個測試方法，於研究區驗證來源／用途／結構／材料／單位／工程範圍／審核狀態的可比性與正式資料資格。即使模擬 `Approved`，其 `source_kind=synthetic` 仍永遠不符合 production eligibility。
- 「hypothetical eligibility（僅研究）」、「production eligibility（真實來源審核）」與「樣本數門檻」分開呈現。程式不存取 DB，**尚未連接真正 2A 歷史表，也不會修改生產判定**。
- 下一任務：執行 `python research/2a_synthetic/eligibility_review.py --seed 20261111 --cases-per-combo 12 --minimum-count 5` 和 `python -m unittest discover -s research/2a_synthetic -p "test_*.py" -v`；回報實際輸出，再檢查研究規則是否可映射到正式 DB 的現有欄位。新對話必須先讀 Handoff Guardian Skill。


## 16. 2026-11-12 第七輪報告收到及資料模型對照

- 使用者上傳 `eligibility_report.md`，內容顯示 35 件合成候選、同結構限制有、假設來源真實時合格 1 件、真正具備正式來源資格 0 件；35 件 `unverified_source`，25 件 `wrong_structure`。各拒絕原因可能同案重複。**這是研究輸出，不是正式 DB 的合格案數。**
- 已新建 `docs/2A_REFERENCE_ELIGIBILITY_MAPPING_2026-11-12.md`：核對 C# 模型與研究資格欄位。現有 `ProjectMaster.ProjectType`、`StructureType`、`HistoryStatus`、`HistoricalTransaction.MaterialId` 等可支援基礎檢查；**已讀模型無可靠來源真實性／整體工程範圍完整性認證欄位**。
- 已新增 `research/2a_synthetic/reference_schema_readonly.sql`，下一步應由使用者在正確 Supabase 開發 DB 執行純 `information_schema.columns` 查詢，檢查真實 schema 再決定可否設計實務資格審查 UI／migration。**SQL 尚未執行，也未修改正式 DB。**
- 正式模型的權重、最少參考數、已核准歷史查詢、異常燈號全部保持現況。下一個對話先取 schema 結果，避免重跑第七輪同一批合成測試。


## 17. 2026-11-12：真正 PostgreSQL 欄位 Schema 已核對（最新）

使用者上傳在 Supabase 執行 `reference_schema_readonly.sql` 後的 `information_schema.columns` JSON 結果。**已直接驗證六張資料表共 86 個欄位**：`project_master` 17、`final_bid_item` 27、`historical_transaction` 8、`material_master` 5、`unit_master` 7、`estimation_settings` 22。

根據真實 Schema：有用途／結構／Approved／材料／單位／來源檔案 hash 等基本欄位，但**沒有獨立且具證據的 source provenance certification／整份結構體工程 scope completeness 欄位**。尤其 `historical_transaction.project_master_id` 與 `final_bid_item_id` 可為 NULL，而 `project_master.project_type`／`structure_type`／`total_floor_area` 也可為 NULL。這不能僅依 C# Model 推論所有資料品質達標；亦不能因 Approved 代表真實可比較。

詳見 **`docs/2A_VERIFIED_DB_SCHEMA_GAPS_2026-11-12.md`**。下一個操作點是研究用 **`research/2a_synthetic/reference_quality_counts_readonly.sql`**，在確認正確 Supabase 開發資料庫後，由使用者自行執行四組**唯讀聚合**查詢，查看用途 × 結構的獨立建案數、Approved 欄位缺漏與交易 FK／材料數量品質。**目前僅驗證存在的資料庫欄位，尚未讀取／查證當前實際建案筆數，新的聚合 SQL 亦尚未執行。**

此次不修改任何正式 Schema、C#、參數或歷史判定。轉新對話時不應再要求重貼同一份 86 欄清單，應改分析新聚合結果。

## 18. 2026-11-12：Q4 真實材料歷史聚合結果

使用者提供 `reference_quality_counts_readonly.sql` 的 Q4 輸出：**住宅 × RC** 有 material_id=1 的 **8 件獨立 Approved、未刪除、非異常且數量為正的歷史建案，11 筆交易、11 個不同連結工項**；material_id=2 同樣 **8 件獨立建案，10 筆交易、10 個連結工項**。不是 11／10 件工程，也不能判定 8 件皆為真實可用標單。尚未確認 material_id=1/2 名稱或 DEMO 資料是否仍在；Q1～Q3 尚未取得。

保存於 `docs/2A_APPROVED_MATERIAL_COHORT_2026-11-12.md`。下一步優先執行唯讀聚合 `research/2a_synthetic/reference_material_source_audit_readonly.sql` 確認材料名稱、來源疑似 DEMO 分組與同案同材料多筆交易；並補提供先前的 Q1～Q3 結果。**SQL 尚未執行，不能當成已完成核驗。** 正式 C#／DB／模型權重保持不變。


## 19. 2026-11-12：已取得 Q1～Q4 真實資料品質完整聚合

使用者已貼出 `reference_quality_counts_readonly.sql` **四組全部**查詢結果（不再只是 Q4）。詳細表格、證據範圍與不確定性見 **`docs/2A_HISTORICAL_DATA_QUALITY_Q1_Q4_2026-11-12.md`**。

- Q1：`project_master` 10 件，全部住宅 RC：8 Approved、2 Draft，沒有任何建案標記刪除。
- Q2：8 Approved 有 7 件缺少 `source_file_hash`（87.5%）；用途、結構、總樓地板面積、基地面積、日期、來源檔名、ApprovedAt/By 均無本次查詢定義的缺漏／不合法情況。這仍未證明來源真實與工程範圍完整。
- Q3：23 筆歷史材料交易；2 筆已標記刪除、2 筆連到非 Approved 或已刪除建案；全部有有效的 ProjectMaster／FinalBidItem FK，且無跨案工項錯連。**刪除與非 Approved 兩組是否重疊尚未確認，不能直接相加為四筆問題交易。**
- Q4：有效 Approved 歷史交易回傳住宅 RC、material_id=1 的 8 獨立建案／11 交易／11 工項、material_id=2 的 8 獨立建案／10 交易／10 工項；材料 ID 名稱與 DEMO 歷史來源仍待驗證。

`research/2a_synthetic/reference_material_source_audit_readonly.sql` 現已包含 Q5 材料名稱／標準單位、Q6 疑似 DEMO 來源聚合、Q7 同案同材料多筆交易、**Q8 歷史交易刪除狀態×關聯建案核准狀態交叉表**。下一步先執行 Q5～Q8 的唯讀 SQL，回報聚合，不要重查已完成 Q1～Q4，不得因此更改正式模型權重或把 DEMO 當真實。


## 20. 2026-11-12：Q5～Q8 真實資料庫結果已取得（最新）

使用者已提供 `reference_material_source_audit_readonly.sql` 的 Q5～Q8：**material_id=1 鋼筋/kg，2 混凝土/立方公尺**；8 Approved 中有 7 件專案名稱或來源檔名符合 DEMO/TEST/模擬/測試字樣、1 件未命中且**尚未核實**。2 Draft 中 1 件疑似展示/測試、1 件未核實。這是文字篩查，不能當真偽認證。Q2 的 7 件缺 Hash 與 Q6 的 7 件疑似 Demo 是否為同一批**未知**。

有效 Approved 同案同材料交易分布：鋼筋 7 案×1筆 + 1 案×4筆 = 11筆；混凝土 7案×1筆 + 1案×3筆 = 10筆。可能是不同來源工項，**不能直接當重複輸入或多個獨立建案**。Q8 證實 Q3 的 2 筆已刪除與 2 筆連至非 Approved 是**同一批兩筆**，其餘 21 筆未刪除交易連至 Approved、未刪除建案。

完整原值與證據限制見 **`docs/2A_HISTORICAL_DATA_QUALITY_Q5_Q8_2026-11-12.md`**。Q1～Q8 聚合結果已全部收到，**下輪不要再重查這些基本聚合**。優先事項：提出可稽核的來源／工程範圍認證設計；需區分業務 Approved 與模型 Verified Reference。未經核實不得聲稱有 8 件可靠真實公司最終標單，也不應擅自刪除展示資料。未改正式程式／DB。


## 21. 2026-11-12：Verified Reference 來源／範圍資格設計草案（尚未核准）

使用者回覆上一輪 GitHub 同步與 `python scripts/check_handoff_review.py --base ee3ff3e`「沒有錯誤」，作為**使用者口頭操作通過**證據。Q1～Q8 實際資料庫聚合已完整取得，主要問題是 8 Approved 當中 7 件名稱疑似 DEMO、另 1 件未核實，且不能將 Approved 當作來源真實完整之證明。

新建 **`docs/2A_VERIFIED_REFERENCE_DESIGN_V1_2026-11-12.md`**，是尚待產學窗口／指導教授確認的**設計草案**，非已核定需求與現有功能。核心：保留既有 `Draft/Approved` 發布與鎖定流程，增設獨立 `Unreviewed/InReview/Verified/Rejected/Revoked` 來源審核及各估算目的（鋼筋、混凝土、結構體價格）的適格資格；審核須有可追溯來源版本、工程範圍、標準單位／材料口徑、主管權限、事件紀錄、撤銷及重審機制。

草案包含欄位／資料表方案、API/UI 草案、角色矩陣、14 項驗收測試矩陣與舊 Approved 預設 Unreviewed 的遷移方向；未經決策**不能**建立 Migration、改 DB、改正式估算查詢、取消現有 Approved、或刪 DEMO。下一階段先針對現有 Authorization/History Publish/Reopen 的實際程式做接入影響清單，再與業務確認規格，才可進入實作。


## 22. 2026-11-12：Verified Reference 正式後端接入點靜態審查

已讀最新 `Program.cs`、`ProjectsApiController`、`ProjectManagementService`、`ProjectSimilarityService`、`DetectionCategoryRangeService`、`MajorCategoryPriceRangeService`、`AnalysisCaseService` 等，報告：**`docs/2A_VERIFIED_REFERENCE_INTEGRATION_AUDIT_2026-11-12.md`**。

**P0 兩項實際程式風險**：`Program.cs` 未看到 Authentication/Authorization 配置，Publish/Reopen/Delete/Audit API 無 `[Authorize]`，不能假設只有主管能執行；ProjectSimilarityService 的 `GetScoredCandidateProjectsAsync(int)` 缺 Approved 限制，DTO overload 有 Approved，但兩者用途應先辨清。外部部署網路限制尚未核對，不可宣稱實際遭攻擊。

PublishHistory 已有 advisory transaction lock／Idempotency receipt／SyncHistoricalTransaction／Audit；Reopen 會標記現有交易刪除、改 Draft；數量模型會按 ProjectId 加總多交易再計獨立建案、價格模型由 FinalBidItem 大項彙總並另有 Readiness。將來 Verified Reference 必須分目的在候選選擇器一致套用，不能只改其中一條 query，也不能假設已有 Verified 欄位。

本輪**僅文件審查**，無正式程式或 DB 變更、沒有 dotnet build 或部署權限驗證。下一步是確認實際部署身份／權限模式、前端入口，與崴正／老師核定審核規則，再走具回退的分期實作。


## 23. 2026-11-13：Verified Reference 前端／Nginx 靜態安全接入審查

使用者確認上一輪 GitHub 同步、Guardian 無錯並要求繼續。已完成對最新 Vue 前端 `api.ts`、`router/index.ts`、`App.vue`、`ProjectDetailView.vue`、`ProjectsView.vue`、`AuditLogView.vue`、`AnalysisCaseDetailView.vue`、以及 `frontend/nginx.conf`、`frontend/Dockerfile`、後端 `Program.cs` 的靜態查核，完整結果：**`docs/2A_VERIFIED_REFERENCE_FRONTEND_SECURITY_AUDIT_2026-11-13.md`**。

風險：前端沒有可辨識的登入／角色路由守衛，操作紀錄列在一般側欄，`api.ts` 不帶專屬 bearer token；目前 Nginx 只有將 `/api/` 代理到 `backend:8080`，未在這個檔案配置 API 認證。後端亦無 ASP.NET Core AuthN/AuthZ。這是程式層的安全缺口；**實際部署可能有外部代理、VPN、內網、防火牆，尚未核實，不能聲稱外部可直接存取。**

現有 ProjectDetailView Publish/Reopen 有保留 sessionStorage Idempotency-Key 的失敗對帳；Verified Reference 接入必須保留此機制，且將「Approved 已納入模型」的既有文案與「來源 Verified」區分。AnalysisCaseDetailView 現已有對名稱可辨識 DEMO 的提醒，**只能作為疑似標記**。

本次未修改正式前後端或資料庫；下一步須由部署管理員確認真正入口的防護方式、企業用戶與角色來源，據此設計後端授權及 401/403 行為，然後才可開始建立新的核准 API。


## 24. 2026-11-21：永久 CI 收斂、合成 Excel 測試鏈相依修復

使用者本機 `integration/src-closure` 於 `5263`／`5174` 的唯讀 API 預檢實際 **7/7 PASS**；進一步 Docker/SQL/API 唯讀比對 **4/4 PASS**，確認 `127.0.0.1:55433` 的 `bidding2a_graduation_src` 容器，API 與 DB 的 **14 個案件 ID/名稱指紋一致、210 個 Approved 歷史案一致、14 個版本／28 筆 Run 中抽讀 14 筆成功**。這不是新 UI 上傳、重算、發布或實際準確度驗證。

接著首次執行 `prepare_ui_case.py` 時因整合分支缺少 `export_full_project_excel.py` 發生 `ModuleNotFoundError`；後續 `verify-ui-preflight.mjs --excel` 的 `ENOENT` 是檔案尚未產生的連帶結果。**已針對根因**把 main 現有正式 `export_full_project_excel.py` 和 `generate_irregular_excel.py` 加入 feature 分支；新增真正執行 XLSX/JSON 輸出的永久 `test_prepare_ui_case.py`，檢查 test-only、SHA 來源一致、ZIP 可讀和拒絕覆寫。

依使用者明確決策：不再保留一次性、重複的 CI。已整併到 `.github/workflows/2a-build.yml`（backend Release、Parser Regression、Vue Build、Node 唯讀腳本檢查及 Python 產生器契約測試）；**刪除** `.github/workflows/graduation-ui-build.yml`，保留真正有用的 CLI 與 `2a-handoff-review.yml`，不刪使用者未追蹤 Python/JSON 或任何測試資料。使用方法見 README 第 19 節。

此輪沒有 DB/Migration/API/正式估算 Scope 變更。後續：同步 feature 本輪 5 commits、測試 GitHub Actions 實際結果；使用者再跑 `python prepare_ui_case.py` 及 `node verify-ui-preflight.mjs --excel`，後續才開始 UI 建案／兩種異常分析／Run 跨刷新保存。決標正式發布必須另用可回滾測試來源，禁止讓 test holdout 進歷史。


## 25. 2026-11-21：修復 UI 測試資料來源路徑缺失

使用者本機重新同步上一批永久 CI 修改，`python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v` **2/2 PASS**；但 `graduation-src-extreme-v3/manifest.json` **不存在**，先前指令強制帶入 `--source` 導致 `prepare_ui_case.py` 無法產生 XLSX，隨後 `verify-ui-preflight --excel` 的 `ENOENT` 僅是檔案不存在。不可推論 parser/API 或模型出錯，也不能將該次 Excel 檢查記為 PASS。

在 `feature/1d-api-integration` 修正 `tools/graduation-synthetic/prepare_ui_case.py`：新增**離線、無來源資料庫**的預設 UI smoke workbook 模式（不帶 `--source`），輸出有總表、結構體、基礎地下三工作表的合成明細，以及 Excel SHA／逐列數量單價／完整 UI metadata 的 JSON；所有資料明確標註 `SYNTHETIC_UI_ONLY_NOT_HOLDOUT_OR_HISTORY`、`eligible_for_training_history=false`、`is_accuracy_ground_truth=false`，**不依賴 5263/55433，也不准發布成 Approved History**。保留原有明確 `--source` 模式及 SHA 驗證；指定不存在來源時仍然 fail closed。

新增 `test_prepare_ui_case.py` 的**真 CLI 子程序**離線測試、Excel OOXML／明細／數量×單價一致性、來源標記、防重覆輸出和 RC/SRC 切換。更動 commits `de6a4f52fd3510164021216082bff0f21e729042`、`29b2a1d092dd951b097b9dca40a1626f2d9084f6`。僅使用既有永久 `.github/workflows/2a-build.yml`，沒有新 workflow；暫無 .NET、DB、Migration、正式模型或檢測類別變更。

**下一步**：同步兩筆 commit，改用 `python tools/graduation-synthetic/prepare_ui_case.py --output graduation-ui-1121-01.xlsx --truth graduation-ui-1121-01-truth.json`（**不帶 --source**），接著 `node scripts/verify-ui-preflight.mjs --excel graduation-ui-1121-01.xlsx`。唯讀解析成功後才做 UI 寫入＋Run 持久化；歷史發布只能使用可回滾、與 holdout 完全分開的測試庫。不能宣稱新功能 E2E 已成功。


## 26. 2026-11-21：Windows CP950 測試修正與 UI Run 持久化唯讀核對

**使用者實測證據**：上一批 7 個 feature commits 已 cherry-pick 到 `integration/src-closure`（本機 HEAD `e6af81a`）；合成 `graduation-ui-smoke-1121-02.xlsx` 和對應 JSON 成功生成，`worksheetCount=3`、`detailRows=15`，API `verify-ui-preflight.mjs --excel` **9/9 PASS**，含 `3` 張工作表、`2` 張已識別明細工作表、`15` 筆候選、`4` 筆警示。4 筆警示須查明原因（可能是特意排除非量估工項），9/9 不代表資料品質已人工通過或異常判定正確。

**同輪失敗必須保留**：Windows Python 3.12 跑永久 `test_prepare_ui_case.py` 時，原本離線 CLI 子程序仍用 Windows 預設 CP950 輸出，父程序指定 `encoding="utf-8"` 去讀，造成背景讀取執行緒 `UnicodeDecodeError`、`completed.stdout=None`，再造成 `json.loads(None)` 的 `TypeError`；總計 **2 PASS / 1 ERROR**。這不是 Excel 產生失敗，因為使用者之後執行產生器本體與 9/9 API 都成功。

修正 `tools/graduation-synthetic/test_prepare_ui_case.py`：用 `env=os.environ.copy()` 對 child 單獨設 `PYTHONIOENCODING=utf-8`，與父 `subprocess.run(..., encoding="utf-8")` 配對，**不改全域 Windows 設定**。增加既有永久 `scripts/verify-ui-state.mjs --case-id <ID>`：對新 synthetic UI 案件，確認同一 current version 有已保存 `Quantity`、`MajorPrice:結構體工程` Run，從 API 讀回兩份結果並維持 Docker/DB/API 四項強對照；不執行任何 POST 或 DB 修改。Commits：`79af4b822b523d8843d353f5b6fcd5144381f473`、`d6b3fb1463111e7c502d31f4657c47455e8d8d5a`。沒有新 Workflow，僅沿用永久 `2a-build.yml`。

**下一階段**：使用者同步新 commits、Windows Python 回歸再跑（預期修好 CP950，須以真實結果為準）；接著直接用現有 `graduation-ui-smoke-1121-02.xlsx` 進 `http://127.0.0.1:5174/analysis-cases`，「自行上傳」只勾結構體明細與基礎及地下工程，先於 UI 檢視 4 警示；建立名為 `SYNTHETIC-UI-驗收-graduation-ui-smoke-1121-02` 的 SRC 案件，輸入住宅／臺中市／12000 m²／基地 950 m²／地上 12／地下 2／2025-01，執行「數量偵錯」與「結構體工程」價格異常。重整頁面、開啟歷史 Run 後以 `node scripts/verify-ui-state.mjs --case-id <ID>` 查五項（**尚未實際執行**）。不把這份 smoke test 上傳決標草稿或發布 Approved history。資料庫/Migration/演算法/權限不變。


## 27. 2026-11-21：UI 人工實測已建立 AnalysisCase #15，數量與價格 Run 畫面成功；納入範圍需對照

**使用者直接提供瀏覽器畫面（E1 UI 證據）**：`http://127.0.0.1:5174/analysis-cases/15` 可見案件 `SYNTHETIC-UI-驗收-graduation-ui-smoke-1121-02`，住宅/SRC/臺中市、樓地板 12,000m²、基地 950m²、12F/B2，V1 自行上傳 `graduation-ui-smoke-1121-02.xlsx`，**15 筆工項**；價格分析顯示已保存 `Run #31` 且結果「結構體工程 29,268,000 元、每坪 8,062.81、P15/50/85 每坪 28,591.65/28,718.02/39,487.84、8 參考案、ESS 6.25、Low」。另一張畫面顯示數量分析鋼筋 **38,000 kg**、混凝土 **3,650m³**，二類各 8 個參考案（量範圍另見原圖），且數量分析結果成功呈現。這是確實的 UI 建案與分析操作成功證據；**仍未執行**獨立 `node scripts/verify-ui-state.mjs --case-id 15` 的 DB/Run 持久化五項核對，亦未證實歷次 Run 刷新後可讀回。

靜態對照 `tools/graduation-synthetic/prepare_ui_case.py`、`崴正/Services/TenderImportInspectionService.cs`、`崴正/Services/TwoAPreprocessService.cs` 和 `TwoAQuantityAnalysisService.cs`：合成 Excel 的結構體工項中鋼筋名義數量 **130,000 kg**，混凝土名義數量 **5,450m³**。預檢前述 **4 warning rows** 恰對應三條名稱含「加工／組立／綁紮／工資」的鋼筋工項（42,000+32,000+18,000kg）及一條「預拌混凝土澆置」（1,800m³）。這四條目前被分類為 `Excluded`，故正式數量偵錯只用 **38,000kg** 及 **3,650m³**；大項價格仍計入結構體所有 10 條數量×單價，合計 **29,268,000 元**，與畫面一致。**沒有證據是算術漏加或 DB 資料遺失**。但名稱型關鍵字分類不等於材料/勞務真正拆分，且「鋼筋施工」「混凝土泵送與搗實」同樣屬複合措辭、當前卻被納入；這是**待人工審核的分類邊界及 UI 可解釋性問題**，絕不可直接把統計 Low 當成真實工程異常或為消掉紅燈調演算法。

本次對應主程式並未修改；只驗證 UI 顯示與追查納入範圍。下一步直接在現有 5263→55433 獨立 DB 執行 `node .\scripts\verify-ui-state.mjs --case-id 15`，需要結果 5/5 才算 Run 保存／跨 API 讀回驗收；若失敗先查原案件，不重傳 Excel 或清空 DB。再評估在正式 UI 顯示「排除項數／工項名稱與理由」以避免把已排除含料複合工項的 38,000kg 誤讀為整案鋼筋用量。決標發布與真實準確率仍未驗證；smoke fixture 禁止 Approved History。


## 28. 2026-11-21：Case #15 隔離資料庫 Run 持久化已 5/5 PASS，新增唯讀工項排除說明

- 使用者 Windows 執行 `node scripts/verify-ui-state.mjs --case-id 15` **5/5 read-only PASS**。Docker SRC isolated DB identity (127.0.0.1:55433)、5263 API 對應 DB 全 15 個案件 ID/名稱指紋一致、210 Approved 合成 train 不變、15 版本/32 Runs，抽讀15份 Run payload；同版本 #15 的 `Quantity` 與 `MajorPrice:結構體工程` Run 都能重新讀取。這完成本輪「UI 上傳→數量與價格 Run 建立→API/DB 持久化」證據鏈；**不是**模型誤報率、真實工程推估準確度、決標回寫／Audit 的完成證據。
- 下一組程式修改均在 `feature/1d-api-integration`，未合併 main、亦尚未於使用者 Windows 執行：`崴正/Services/AnalysisCaseService.cs`、`崴正/Controllers/AnalysisCaseController.cs` 新增只讀 `GET /api/analysis-cases/{id}/versions/{versionId}/quantity-scope`。用相同 `TwoAPreprocessService.Build` 和相同工項對映產出每筆 Included／Excluded／Pending、排除理由、Excel 原列號、材料相關 Excluded 數。正式 RunQuantityAnalysis 也改用共用 `ToPreprocessSourceItems` 函式以確保二者來源一致，不改數量估算算法或類別門檻。Commits `eae57e4d`、`3819ad10`、`2bfb3166`。
- `frontend/src/views/AnalysisCaseDetailView.vue` commit `4f6ccc02`：新增數量納入範圍卡，列出總工項／納入／排除／待確認、可展開的工項及具體原因；材料相關名稱被排除時提醒人工審查複合勞務材料。附加 API 若未更新則顯示提醒，既有 V1／歷次 Run 仍可查看，不讓 UI 整頁失敗。
- `tests/WorkbookParserTests/Program.cs` commit `3be3a5bb`：增加同源 case #15 的 15 工項／4 被排除候選／候選量 38,000kg、3,650m³／價格總額 29,268,000 元，避免保守排除與價格統計混淆。擴充**永久** `scripts/verify-ui-state.mjs` commit `9c7ed7c8`：`--case-id 15` 新增唯讀 scope 回讀與逐列分類契約對照、預期**新版 backend** 6/6 PASS；不建立臨時 CI/CD。
- 分類現況：版本實際鋼筋名義 130,000kg、混凝土 5,450m³，4 條有加工／組立／綁紮／澆置的名稱被保守排除後只納入 38,000kg／3,650m³；其它工程類別排除是目前未核定數量模型，不能將單純「排除」解讀為工項無材料。重要限制：此唯讀分類是**目前規則重算**，並非舊 Run 的原始記錄或工程人工覆核結果。
- CI 真實狀態依 GitHub Actions 最新 run 查核；**使用者 Windows 新 backend/新版 UI 實測仍 NOT RUN**。下個實測：在本機 `integration/src-closure` cherry-pick 此批相關 commits，停掉舊 5263／以同一 55433 isolated DB 重新啟動後端，5174 重新整理 #15，確認新增卡片及4條材料相關 Excluded，執行 `node scripts/verify-ui-state.mjs --case-id 15` 取得新版 6/6；勿重傳 V1、重跑 Quantity/Price、清除 Docker volume。決標審核／稽核和準確度下一階段。


## 29. 2026-11-21：README 明確標示組員使用的 2A 專題分支
- GitHub 實際盤點（2026-11-21）：Repository 當時 **Public**、default branch 為 `main`；遠端只有 `main`、`feature/1d-api-integration`，舊 POC `feature/1d-detection-integration` 已由使用者另行處理，PR #1 已關閉；PR #2 `feature/1d-api-integration → main` **仍 Open／未合併**。
- 明確對組員公告：**撰寫本組 2A 專題的目前共享程式碼，請閱讀 `feature/1d-api-integration`**。這是持續開發分支而非正式發行，若報告需固定版本應記錄特定 Commit SHA；`main` 目前尚未整合最新 2A 功能，不宜當作本次報告技術唯一依據。
- 已實際更新兩個分支根目錄 `README.md` 最上方導覽：`main` commit `c159f85669d4e98504764312a3fbcb5219e5b9a8`，feature commit `42eac3eeeb668f6c5c531167e6a2955025fc982d`，提供 Github 可直接點開分支與交接文件的連結。
- 此為文件與對外溝通決定，沒有修改後端 API、資料庫、前端功能、CI 或歷史資料。本輪未再執行 E2E 或編譯，之前的測試成果不可挪作新版本驗收。
- 公開風險仍存在：公開 Git 原始碼及歷史的敏感檔與 `course-version.zip` 封存檔尚未完整安全檢查，README 告知只用授權內容與合成／去識別化資料；不因 README 提醒就視為授權公開完成。
