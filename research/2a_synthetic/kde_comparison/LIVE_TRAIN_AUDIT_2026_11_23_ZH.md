# 2026-11-23 正式資料庫合成廠房訓練集／字典／單位唯讀實測

**結論：T00「目前 DB 快照之合成 TRAIN 匯入內部一致性」PASS；T01「正式67詞對獨立未知廠商標單的自動對應準確率」NOT RUN；T04「正式DB單位配置統計」PASS（非換算工作流程全測）。**

- 資料來源：連接的 Supabase project `sbykuwypriudrgurmssb`，實際唯讀 SQL，執行日期 2026-11-23（臺灣時間）；無 INSERT／UPDATE／DELETE／Migration／正式 API 操作。
- 可重現查詢：[`live_train_integrity_readonly_2026_11_23.sql`](sql/live_train_integrity_readonly_2026_11_23.sql)。查詢**只回傳彙總**，不匯出原始標單、任何憑證或機敏資料。
- 本批實際執行了該檔第一個 SELECT；另用多條唯讀 SELECT 核查資料狀態及核准日期。未在獨立 Postgres 重跑正式 API 的發布／重開／稽核端到端流程。

| 指標 | SQL回傳 |
|---|---:|
| item_dictionary / 不重複 keyword | 67 / 67 |
| 包含「廠房作業區」的字典詞 | 67 |
| 合成廠房訓練建案 | 126 |
| 其中 history_status=Approved | **126** |
| 合成廠房 final_bid_item | **16,786** |
| mapping_status=Auto | 16,786 |
| material_id 缺漏 | 0 |
| matched_keyword 無法回溯現有字典 | 0 |
| 同一 matched_keyword 對應 material_id 衝突 | 0 |
| mapping_issue / data_issue | 0 |
| qty 或 unit_price 不為正 | 0 |
| amount 與 qty×unit_price 差大於 0.02 | 0 |
| unit_category / unit_master | 11 / 11 |
| allow_conversion=true 的單位類別 | **0** |
| 單位主檔缺失／非正換算係數 | 0 |
| 正規化單位名稱衝突 | 0 |
| 每個單位類別無基準單位／有多個基準單位 | 0 / 0 |

**關鍵進度更正：** `main` 2026-11-22 早期交接曾描述廠房126案為 Draft，但依**目前資料庫**其 `history_status` 已全部為 **Approved**，且 `history_approved_at` 全非空；廠房的核准時間（UTC）介於 2026-11-22 06:30:07 與 06:34:15。這是狀態已推進後的舊文件，不代表測試期間我們有執行發布；沒有寫入行為。

**絕不能過度解讀：**
1. 67個正式字典詞皆來自本批合成廠房資料範式，16,786筆Auto與字典一致只證明**匯入的參照一致性**；無法代表「現場真實工項自動辨識 100%」、未知詞／新規格的精確率，亦不構成**train/test 獨立**的驗證。
2. 11個單位類別均不允許換算，因此 kg↔噸／m²↔其他別名不可假設已在正式 DB 可用；需按使用者實際配置測試允許與拒絕路徑。現有合成主檔 C# 18筆單元契約 PASS，不等於正式資料庫允許換算。
3. 126件廠房是合成 TRAIN（經核准）的基準案；正式 67詞不可當成外部 blind test 的 train-only 字典。
4. 不應以 approved 的合成 TRAIN 當企業真實已決標工程；對外報告務必標明 SYNTHETIC。

下一驗證點：在可丟棄的獨立DB做 Draft→人工→Publish→Reopen→Audit 全流程；使用另外保留、人工確認的未見工項和實際來源日期做 T01／T02 獨立驗證。T00「住宅 test 原始快照」仍未取得，不能以 DB 的 210件住宅 TRAIN 取代。
