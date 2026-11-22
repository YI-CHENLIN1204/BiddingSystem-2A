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

## 2026-11-23 第二次 GitHub Runner 執行與視覺核對

[研究 Run #45055170040](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45055170040) 已核實，對應 `89faf4f6ad1ff5eccb71ecad15c2c00f8ad23b44`（Push、Completed、Failure）。`isolated-api` 在 `research/acceptance-ci/ResearchAuditAuthentication.cs(10,5)` 遇到 `CS0246 ILoggerFactory` 編譯錯誤，因此 **T09 修正後隔離 API+DB E2E NOT RUN**。檔案缺 `using Microsoft.Extensions.Logging;`，需修正後取得新 Runner 證據，不得降低匿名 401/403、Reader 403、Supervisor 200 的驗收標準。原 Run #45049455364 的 T04 EF PASS、T07 Draft／人工映射／Publish／Reopen／Audit PASS、T08 30 次 HTTP 讀取 p50=19.04ms、p95=26.33ms 均保留已執行證據。

同 Run `word` Job SUCCESS，Artifact [`14767261344`](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45055170040/artifacts/14767261344) 已取得 8 頁 PDF + PNG，全部 ZIP SHA256SUMS 通過。PDF 原始 SHA-256 `7fe896141e88d08d915f5397ea45edd3ea9855b5b1f6f95a1485a4c47a7e98cf`。真正逐頁目視：8 圖及繁體中文完整、無明顯裁切或亂碼；第 2→3 頁跨頁表格未重複表頭、第 3→4 頁首孤行、全篇無頁碼、圖表頁留白過大。因此 **Word 渲染 PASS、目視已執行、最終排版 PARTIAL**。詳細見 [Word 八頁覆核](WORD_VISUAL_REVIEW_2026_11_23.md)。T00～T11 全項尚未完成，正式 2A/A0/Supabase 均未修改。

Job `isolated-api`：137308670725 FAIL，Artifact `14766524842`；Job `word`：137308670933 SUCCESS，Artifact `14767261344`。DOCX 原稿 SHA-256 `0bd69fc4e139ca3d26cd2a3d685562ed96328b08d8af6b1308a2ea18e9c928a0`，Run #2 八張 PNG 與 Run #1 對應圖像位元組相同。測試與版面評分分別保留，CI FAILURE 不可籠統覆寫已成功的 Job 或原 Run 的驗收。

# 研究 Actions 執行查核｜2026-11-23

[Run 45049455364](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45049455364) 已核實，測試提交為 `b3bceb90b2d8775ba7006ac146bed3d0ecffbc27`。本次修改從 `e0109ade56e34f225efd001022ba3745bcbde838` 接續，提交訊息為 `research: enforce isolated audit roles and reconcile acceptance evidence`。

- T04 隔離 EF 換算 PASS。T07 隔離 Draft 讀取、人工映射、Publish、Reopen、重複提交與稽核核對 PASS。兩項保留原本完整驗收門檻。
- T08 隔離建案讀取 30 次，p50 19.04 ms、p95 26.33 ms。正式全流程效能仍待測。
- T09 原 Run FAIL，匿名稽核 HTTP 200。研究 runner 編譯時加入隔離授權，門檻為匿名 401／403、無效憑證 401、一般使用者 403、主管 200 且讀回 T07 稽核紀錄。修正後 Actions 結果待核實。正式角色整合仍待驗收。
- Word 渲染 job PASS，產出 8 頁 PDF 與 PNG。逐頁視覺仍待取得可開啟的頁面。下載產物後需按下列雜湊核對來源。
- 本批範圍是 `research/acceptance-ci/` 與研究交接文件。隔離 runner 使用一次性 PostgreSQL，無 Migration。下一步核實新 Run 角色矩陣並完成八頁視覺判讀。研究算法與 A0 維持既有版本。


本機以 .NET 10 合成端點載入相同隔離授權程式，建置 0 警告／0 錯誤。匿名 401、無效憑證 401、Reader 403、Supervisor 200，以及未列入政策的建案 GET 200，五項通過。Python 探測程式與啟動時的覆蓋程式通過語法檢查。實際研究 API 加 PostgreSQL 的修正後 E2E 待新 Run。

| 產物 | GitHub SHA-256 |
|---|---|
| [隔離 API](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45049455364/artifacts/14764044670) | `f377309bc77ed310d2a19ffe4f61db8973f33661462dce790b418d769f82c078` |
| [Word 渲染](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45049455364/artifacts/14764009657) | `211cd7572a9f37798e4118ce70cb8e4e36a3945cfadb354acd37101ec35d2680` |

GitHub 已核實兩件產物與相同的 head SHA。環境未能下載頁面位元組。第 1–8 頁的字型、圖表、分頁、頁邊與溢出均待視覺覆核。
