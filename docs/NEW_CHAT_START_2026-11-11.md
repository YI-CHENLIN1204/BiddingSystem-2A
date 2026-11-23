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

## 最新接手優先項（2026-11-22）

先讀CURRENT_HANDOFF最上方與HOLDOUT_EVALUATION_2026-11-22.md/json。237組正式range配對、144組參數比較已完成，勿再跑來源9/9、城市/基地6組smoke、重seed或重傳13案。來源為210train/45validation/45test；Precision/Recall/FPR缺工程正常/異常核定，不能假造。保留正式參數，因全域validation最佳方案使SRC價格test變差。

本輪Unit-null與Relational版本警告已修，Windows build 0 warnings/0 errors、Parser與Python11/11已通過。下一個系統驗證點仍是將本輪feature改動安全整合至原機(原HEAD5c0ae5c、無衝突)，用原55433隔離庫重啟5263並完成quantity-scope6/6，之後收尾既定整合/版本；原機Docker/寫權限制未因此消失。不要把合成數值實驗當真實工程認證。

---

## 優先讀取：2026-11-21 最新接手任務

先讀 CURRENT_HANDOFF 頂部 Windows 工作區驗證記錄。GitHub feature 基線 a27f850d，桌面 integration/src-closure 仍在最後 HANDOFF_CHANGE_REVIEWS.jsonl 衝突；獨立 clone recovery commit ab32e344ef76f67e8c0134f0592ba5b74f136a1f 已完成合併，但未修復桌面原工作樹。

Windows 後端/Parser/Python 3/3 與 native config loader 前端 build 已實測通過。5263 case15 GET=200、新 quantity-scope GET=404；Docker checker 受 named-pipe 權限限制，6/6 未完成。下一步只需在使用者環境完成最後 cherry-pick，原隔離 DB 連線重啟新版 5263，再做唯讀 6/6。不得重 seed、重傳 V1、重算舊 Run、發布 smoke，勿重做已完成的生成器工作。

---

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
