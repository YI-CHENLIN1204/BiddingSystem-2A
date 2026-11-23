## 2026-11-23 新增待執行的完整 HTTP 匯入驗收

在隔離 PostgreSQL 中以 ClosedXML 生成**完全合成**的 `synthetic-final-bid.xlsx`（兩個工作表，鋼筋與混凝土正量價，附有假比價列），以 multipart 上傳到實際 `/api/Estimation/upload-final-bid`。新增驗收包含：錯誤工作表被拒 400、選對工作表匯入 200、兩筆保留且比價列不進來、回讀 Draft 且 HistoryTransaction=0、同一檔案 SHA 重複上傳回 409、讀取 2A 摘要及 30 次 p50/p95。只有取得新 Run 原始日誌與 Artifact 後可判定 PASS，不能以程式已提交當作驗收完成；此測試不使用任何連接 Drive 原始標單。既有 T04/T07/T09 及 Word 結果沿用原 Run 證據。相關資料來源審查見 [來源適格性](../../docs/T00_T11_SOURCE_ELIGIBILITY_2026_11_23.md)。

# 2A 研究驗收 CI
以研究分支 push 自動觸發。隔離作業使用 GitHub Ubuntu runner 的 PostgreSQL 17 service 與合成資料。流程中的 Draft 由 EF fixture 建立。後續流程呼叫實際 API。換算副本只覆蓋 runner 的編譯工作樹。Workflow 不載入 GitHub Secrets。

作業保留資料庫身分及空 schema 的寫入前驗證、EF 轉換、HTTP 請求日誌、專案狀態與稽核檢查。runner 以一次性亂數憑證驗收稽核角色。匿名應回 401／403、無效憑證 401、Reader 403，Supervisor 應回 200 並讀回已發布與重開事件。這些條件均納入作業通過門檻。驗收使用實際研究 API 路由與隔離身分，正式部署授權另行驗收。HTTP 效能只量測建案讀取。

Word 作業輸出 PDF、逐頁 PNG、每頁文字診斷及 SHA-256。每頁均保留待人工覆核。

## 尚缺外部輸入
- 住宅原始包：CSV、manifest、雜湊、案例及建案鍵、資料分割、來源時間和真值（T00、T05）。
- 一批授權去識別的獨立住宅與廠房標單：來源／廠商、版本、估算可得時間、決標時間、章節、原名、規格、工料、單位、數量、價格與標準材料映射。由資料保管人分割 train/validation/blind，供 T00、T01、T02、T05、T10、T11 共用。
- 同一批標單附工程師獨立核定映射與正常／異常／不確定標註、理由、案例鍵及歧義覆核（T06、T10）。T11 若需逐步驗證原法，另需作者原碼及參數。
- 以同一批授權標單建立至少兩份難度相配且去重的受測資料，記錄匿名受測員、AB/BA 順序、設定／對應／覆核／修正秒數及品質驗收（T03、T10）。
