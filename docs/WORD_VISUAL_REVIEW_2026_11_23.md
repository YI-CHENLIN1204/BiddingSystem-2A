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

# 2026-11-23 Word 八頁逐頁目視驗收

**來源**：[GitHub Actions Run #45055170040](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45055170040)，Word Job SUCCESS，Artifact `14767261344`。實際 ZIP 檔含 8 頁 A4 PDF、`page-1.png` 至 `page-8.png`；SHA256SUMS 全部 PASS。DOCX SHA-256 `0bd69fc4e139ca3d26cd2a3d685562ed96328b08d8af6b1308a2ea18e9c928a0`；PDF SHA-256 `7fe896141e88d08d915f5397ea45edd3ea9855b5b1f6f95a1485a4c47a7e98cf`。Run #2 八張 PNG 與 Run #1 同頁位元組相同（PDF SHA 不同）。

本次是**實際逐頁開啟 PNG 目視**，不是僅靠文字檢查；非完整圖表數字重算。

| 頁 | 結果 |
|---|---|
| 1 | 標題／摘要／E1 表完整；未見破字或裁切 |
| 2 | E2／E3 表正常；可引用性表跨下一頁 |
| 3 | 續表未重複欄名表頭；最後段落「合成變體與真實」跨頁斷句 |
| 4 | 頁首僅「來源的部署錯配率仍需分開解讀。」為跨頁孤行；其餘可見 |
| 5 | 圖 1、圖 2、圖說完整；下半部留白偏多 |
| 6 | 圖 3、圖 4、圖說完整；下半部留白偏多 |
| 7 | 圖 5、圖 6、圖說完整；下半部留白偏多 |
| 8 | 圖 7、圖 8、圖說完整；下半部留白偏多 |

**整體**：8 圖完整，繁體中文未見亂碼、圖形裁切或黑方塊；全篇沒有頁碼。教授版缺目錄，是否新增取決於交付要求。第 4 頁文內 PostgreSQL NOT RUN 是 2026-11-22 歷史快照，不能用來描述 2026-11-23 的 CI 實測。

**判定**：Word render PASS；逐頁目視已完成；排版 PARTIAL，應補續表重複表頭、避免頁首孤行、加入頁碼並重渲染複驗。T09 修正版 E2E 因 CS0246 編譯失敗仍 NOT RUN；不可把 T09 本機合成成功當成新 Actions PASS。正式程式與資料庫無異動。

## 2026-11-23 研究驗收編譯與 Word 排版修正

以 `addac2f247512c12ece609c8ae35517bc67c1501` 為起點。Run [45055170040](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45055170040) 的隔離 API 在 Fixture 編譯缺少 `ILoggerFactory` 命名空間，本次補入 `Microsoft.Extensions.Logging`。保留 T09 的匿名 401／403、無效憑證 401、Reader 403、Supervisor 200，以及主管讀回隔離 T07 Publish／Reopen 各一筆的門檻，新增明確的稽核事件驗證輸出。原 Run 45049455364 的 T04／T07 PASS 與 T08 30 次 p50 19.04 ms／p95 26.33 ms 仍有效。

研究 Word 四張表皆設定續頁表頭，原跨頁段落設為整段同頁，頁尾加入置中的 PAGE 欄位。本機已驗證 OOXML／ZIP，原內文及八張內嵌圖逐位元組保持一致（內文比較為文字值）。新版 DOCX SHA-256 `d9fb05c1e4b83422a257f2fb3d3e9ed3d8abbbc275265311b931d511ee2f9ccf`。Workflow 檢查 PDF 頁碼依序及舊孤行未重現，新版逐頁目視仍待渲染產物。研究 CI 新增僅用於發布 Run 網址的狀態，不代表驗收結果。

下一驗證點是新版 Workflow 的獨立 API／Word Job、重新渲染的逐頁判讀與產物雜湊。正式 2A、A0、Supabase 及既有算法維持原版本，沒有 Migration。

