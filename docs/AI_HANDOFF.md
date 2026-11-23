## 2026-11-23 外部實驗啟動審查

基準 `1cebf7f81a0071191ef438ff901e8309783cc3fd`。核對原研究 ZIP 205/205、作業包 24/24，T00～T11 12 項索引共 45 條證據引用皆可解析。只補既有 manifest 鍵相容與敏感輸出路徑防護，使用原廠房凍結 manifest 實驗 9/9 SHA 通過，工作樹內拒寫、外部目錄成功。已通過的試驗沿用。可見候選工程檔的授權與核定版本未能確認。T03 n=0、T08 三法同條件 HTTP PARTIAL、T05 仍保留 A0。

[啟動閘與分工](../research/external-intake/LAUNCH_GATE.md) · [驗證紀錄](../research/external-intake/FINAL_REVIEW_EVIDENCE.json)。Codex 負責命令、計算、驗證與證據保存。外部正式啟動仍 NO-GO，研究不再擴充同類合成實驗。

## 2026-11-23 外部研究作業包驗收

[外部作業與結案矩陣](T00_T11_EXTERNAL_OPERATIONS_2026_11_23.md) 已更新。Run 45125356300 外部工具、隔離 EF T04、交接守門成功。外部工具驗證匯入、凍結、盲標裁決、計分、計時。EF 六個拒絕邊界通過。證據雜湊已驗。正式外部及真人仍待，既有 205 筆封存沿用。

## 2026-11-23 T04 隔離換算補測

外部匯入測試固定拒絕零負量價、未決標、複價及規格／單位不符；另新增隔離 EF 測六項換算拒絕，不重播 T07/T08/T09。以 CI 為準。待正式工料及部署驗證。

## 2026-11-23 外部驗收作業入口

[外部作業包](../research/external-intake/STAFF_GUIDE.md) 提供最終決標 XLSX/CSV 欄位映射、私有列溯源、雙工程師盲標及解盲計分、真人 AB/BA 計時核對。原 Run 45122237323 與 205 筆證據沿用。這次 T04 新增外部匯入邊界測試，正式應用／EF 層尚非完整驗收。A0/A1/B1 同條件 HTTP 維持 PARTIAL，T05 維持保留 A0。無正式外部資料或真人紀錄。新作業包 CI 結果須依 Run 判定。

## 2026-11-23 T08 500／1000 列驗收及外部入口

[本輪驗收矩陣](T00_T11_NEXT_ACCEPTANCE_2026_11_23.md) 記錄 Run 45122237323。兩規模量價與匯入均 PASS，84 筆原始、72 筆正式、0 失敗／棄答，兩份 Artifact 各 10/10 SHA 核對。舊證據 205/205 保留，三法同條件效能 PARTIAL，A0 結論維持。外部資料凍結、匿名隔離、雙人裁決及 AB/BA 工具已提供，受控真實包及真人資料待取得。

## 2026-11-23 T08 大型匯入與真實資料工具

研究量測增加 500／1000 列 matrix，每規模獨立程序與資料庫。附下載前既有 Evidence ZIP 的 SHA `bab494ecead36bac0b089aff9509df43f4712be7ab0898fe93375e7c5df391a5`，已再核對 205/205。新測試只在隔離 runner 以暫時覆蓋量測匯入的三次 EF 儲存及交易提交；正式程式碼未變。工具見 [真實資料與盲測](../research/external-intake/README.md)。A0 原生 HTTP 與 A1/B1 離線目前 PARTIAL，沒有從異質結果推斷同條件勝負。真實及真人驗收仍待資料。

## 2026-11-23 完成證據封存與隔離估算測速

[證據 ZIP](https://chatgpt.com/library?file_id=libfile_83649d0002948191a74ddcf17e255d69&account_id=personal) 205 筆清冊逐檔 SHA-256 通過，ZIP SHA-256 `bab494ecead36bac0b089aff9509df43f4712be7ab0898fe93375e7c5df391a5`。`EVIDENCE-INDEX.json` 對應 T00～T11 逐案、摘要、圖表、測試版本及原生 Actions 證據。`summary-reconciliation.json` 為凍結逐案重算。

[T08 Run 45121107120](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45121107120) 的 `run-link`、`estimate-benchmark` 及交接守門通過，測試提交 `26990ada410ea12621886290bd0ecc687fe9500b`。[Artifact 14793503144](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45121107120/artifacts/14793503144) 已下載，11/11 成員 SHA 通過。198 次原始請求包含 18 暖機；正式量測 180 次、0 失敗。132 個估算 Run ID 互異，數量各有兩種材料實際分析，價格各具足額樣本與下限。首次失敗 Artifact 14794050408 也封存。

| 工項列數 | 版本 GET p50/p95 ms | 數量 POST p50/p95 ms | 價格 POST p50/p95 ms | 該階段結束的累計峰值 KiB |
|---:|---:|---:|---:|---:|
| 2 | 20.27/29.32 | 113.95/142.75 | 44.85/52.02 | 234,276 |
| 20 | 21.73/33.54 | 117.90/127.97 | 48.65/59.21 | 256,524 |
| 100 | 24.76/28.96 | 120.57/151.59 | 54.63/70.25 | 268,684 |

同一隔離 runner、32 個合成 Approved 參考案、順序固定而交錯端點、每格 20 次。RSS 為單一服務程序的累計峰值，涵蓋暖機與先前規模；不可視作各端點獨立測量。既有 O/S 與 Python 的離線流程 RSS 記錄保留。A1／B1 尚無對等正式 HTTP 路徑，大規模壓力與外部工程證據仍待。已通過的 T07／T09 測試沿用，既有 workflow 的共享 fixture 異動亦自動回歸。T05 採用門檻未過，維持 A0，未重選樣本或調參。正式 2A、Supabase 及《專題書面報告_林.docx》未變動。

## 2026-11-23 證據 ZIP 與估算環境修正

凍結證據封存已交付，197 筆 SHA-256 全數通過，外層 SHA-256 `0e0c9213e686480f6497ff0e2373b4c0d0500801c6e0dfdbce7fb8388f870b58`。表格已從逐案重算 14 組學習曲線、252 格分層、64 組候選摘要與 18 格群數比較，合成資料品質標籤 6 筆。

[Run 45120872654](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45120872654) 的價格 POST 60/60 回 500（隔離 settings 缺失），數量 POST 200 但全為未設定的棄答。保留 Artifact 14794050408。已在隔離 fixture 加入參數與類別及材料映射，並要求量價確實產出估算才算成功。正式參數未變。原隔離 T07/T09 驗收維持，後續以新 Run 確認。

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

## 2026-11-23 Research Run #2 實測與 Word 八頁目視

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

## 2026-11-22 算法採用門檻覆核

[採用判斷](../research/2a_synthetic/kde_comparison/FACTORY_ADOPTION_DECISION_ZH.md)：目前保留 A0，隔離環境重查仍阻塞。

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

## A0／B1 的 90% 敏感度分析

以相同測試案項與真值配對。B1 減 A0 的相對區間分數差，混凝土 +0.460（95% 區間 +0.200 至 +0.834）、鋼筋 +0.979（+0.624 至 +1.480）、結構體金額 +1.878（+1.231 至 +2.623）。共同可估案數為 9、9、26。方向與 70% 主分析一致。此補測使用已檢視的合成案。重跑 `python factory_a0_b1_uncertainty.py <factory_b1_pairs.csv> evening_results/factory_a0_a1_90pct_pairs.csv`。結果見研究目錄的 `evening_results/factory_a0_b1_90pct_uncertainty.csv`。

## A0／B2 同面積分母金額比較

26 合成廠房以相同真值配對，兩法均用總樓地板面積。70% 區間相對區間分數 A0 0.460、B2 1.551，差 +1.091（結構分層 2,000 次重抽樣 95% 區間 +0.880 至 +1.312），涵蓋 13/26 與 14/26。90% 差 +0.817（+0.573 至 +1.041），涵蓋 21/26 與 24/26。A0 分數較低的方向在消除分母差異後維持，但兩法仍有選案、權重和區間算法等差異。結果限於已檢視的合成資料。重跑 `python factory_a0_b2_price.py <factory_b1_pairs.csv> evening_results/factory_a0_a1_pairs.csv evening_results/factory_a0_a1_90pct_pairs.csv`。

## T05 A2 等權 KDE 次要比較

A2 保留 A0 選案與面積口徑，固定 A1 的帶寬係數 0.125，把權重替換為等權。78 案項全可估。70% 九層等權區間分數／涵蓋／相對寬度為 0.4900／68.83%／0.3832；90% 為 0.6777／88.89%／0.5981。70% A2 點估計低於 A0 與 A1，90% A1 分數低於 A2。A2 是同資料次要分析，未作獨立選參或採用門檻檢驗。重跑將 A1 入口改為 `factory_a2.py`，輸出 `factory_a2_70.csv` 與 `factory_a2_90.csv`。

## A2 建案配對不確定性

同一 26 案按結構分層整案重抽 2,000 次，種子 20261122。70% A2 減 A0 為 −0.0403（95% −0.0949 至 +0.0131），減 A1 為 −0.0126（−0.0428 至 +0.0170）。90% 兩差也跨零。帶寬仍為 A1 的 0.125，探索性分析，維持 A0。重跑沿用 factory_a2.py 原指令。

## A2 較早開工參考案敏感度

沿用日期篩選、候選池及 0.125 係數。三法共同可估各工項 20/26，共 60 案項。70% A0／A1／A2 分數 0.7501／0.7266／0.7640，A2 減 A1 +0.0374（案級分層 95% 重抽樣區間 +0.0080 至 +0.0813），A2 減 A0 的區間跨零。90% 分數 1.1570／1.0006／1.0127，A2 差異均跨零。A2 在未限制參考日期的 70% 點估計領先未在本條件重現。這是同批合成案的探索性比較，日期以開工日為代理。重跑 factory_a2.py 時在原參數後加入 `--past-only`。

## 實驗公平性稽核

78 個案項的配對與 312 組分數及命中核對通過。詳見 `FACTORY_FAIRNESS_AUDIT_ZH.md`。

## 參考案來源稽核

核對凍結 manifest 與 projects.csv。A0 的 1,272 筆、B1/B2 的 9,264 筆、日期限制兩區間各 480 筆參考紀錄全部源自 train，未含目標案。紀錄跨工項及區間可重複引用同案。未限制日期的 A0 有 699 筆、B1/B2 有 4,528 筆在目標同日或之後開工，應明確標為回溯結果。兩個日期限制輸出均為零。開工日仍是代理欄位。重跑 `python factory_reference_provenance.py <廠房來源目錄> <factory_a0_b1_pairs.csv> <B1輸出目錄>`。

## A0／B1 日期限制比較

兩法皆只用較早開工的訓練案，B1 每個截止日以固定四群重新分群，無足夠早期案則棄答。26 案中 A0 三項各可估 20，B1 各 6／6／18，均落在 A0 可估集合。70% 成對平均分數 A0／B1 為混凝土 0.389／0.464、鋼筋 0.381／0.672、結構體金額 0.479／2.056，90% 方向一致。此為同批合成資料的探索性結果，開工日代理可用日期。執行 replay_factory_b1.py 時對獨立輸出目錄加入 `--past-only`，再執行 `python factory_past_b1_compare.py <日期限制B1的factory_b1_pairs.csv> <日期限制A0結果目錄>`。結果見研究目錄 `evening_results/factory_past_b1_comparison.csv`。
