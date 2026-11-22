# 研究 Actions 執行查核｜2026-11-23

目標提交 `b3bceb90b2d8775ba7006ac146bed3d0ecffbc27`。研究分支 `research/kde-comparison-20261122` 上可讀回 `.github/workflows/2a-research-acceptance.yml`，Git blob 為 `75b0d7be7fd94e03cc522afa635f9cdcd6582412`。此提交的分支與變更路徑符合 YAML 的 push 篩選。看見 workflow 原始碼只能確認檔案存在，GitHub 的註冊和啟用狀態尚無證據。

- GitHub 連接器的 commit workflow runs 查詢回傳 `workflow_runs: []`，其涵蓋 PR 事件。
- Commit combined status 回傳 `statuses: []`。
- 本機無 GitHub CLI，直接連 `api.github.com` 的 run 查詢回報 TCP 443 連線失敗。
- Run ID、job 狀態、日誌、Artifact ID 及執行產物 SHA-256 未取得。尚無法判定 Actions 停用、註冊失敗、push 未觸發或 run 已存在。

| 項目 | 驗收狀態 | 已有證據與缺項 |
|---|---|---|
| T00 | PARTIAL | 缺住宅原始包、獨立時點與 API／SQL 候選核對。 |
| T01 | PARTIAL | 缺授權的獨立標單及人工材料真值。 |
| T02 | PARTIAL | 缺獨立切分詞彙與跨來源標單。 |
| T03 | PARTIAL | 真人 AB/BA 計時待做。 |
| T04 | PARTIAL | 沿用研究版 23/23。隔離 EF 載入與換算的 run 證據未取得。 |
| T05 | PARTIAL | 缺住宅重播與獨立工程時點盲測。 |
| T06 | PARTIAL | 缺工程師真值。 |
| T07 | PARTIAL | 沿用乾淨 Parser 78 條 PASS。隔離 PostgreSQL HTTP 狀態流程未取得證據。 |
| T08 | PARTIAL | 沿用離線 K5。HTTP 延遲與執行數據未取得。 |
| T09 | PARTIAL | 沿用 Parser 與交接檢查。匿名稽核的實際 HTTP 狀態、角色與稽核 run 證據未取得。 |
| T10 | PARTIAL | 缺未見名稱與人工驗收。 |
| T11 | PARTIAL | 缺獨立盲測與原法實作參數證據。 |
| Word | 未驗證 | 尚未取得 PDF 及逐頁 PNG。無頁面視覺結論。 |

取得 run 後，以 `isolated-api` 的 `fixture.log` 判定 T04，以 `http.jsonl` 中 Draft、人工映射、發布、重開、重複提交與稽核紀錄逐項判定 T07，另判定 T09 匿名稽核是否回傳 401／403。Job 失敗須保留各先前步驟已完成的證據。以 `word-rendered-pages` 逐頁檢閱圖像及 PDF。此輪僅記錄可觀察結果，無正式分支、A0 或 Supabase 變更。
