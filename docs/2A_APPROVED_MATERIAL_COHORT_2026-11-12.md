# 2A 實際 Approved 材料樣本分布（2026-11-12）

> **證據範圍：** 使用者提供 `reference_quality_counts_readonly.sql` 的**第四組 Q4** 聚合結果，共兩列。只代表該查詢當時的 DB 狀態；Q1、Q2、Q3 尚未由使用者提供，不能假稱已查到資料來源、完整性或 FK 品質。

## Q4 實際結果（原值）

| project_type | structure_type | material_id | distinct_approved_projects | historical_rows | linked_bid_items |
|---|---|---:|---:|---:|---:|
| 住宅 | RC | 1 | 8 | 11 | 11 |
| 住宅 | RC | 2 | 8 | 10 | 10 |

**查詢資格**：`p.history_status='Approved'`、`NOT p.is_deleted`、`NOT h.is_deleted`、`NOT h.is_outlier`、`h.actual_quantity > 0`，並按用途×結構×材料分組。**本查詢沒有排除示範／假歷史，也沒有確認來源是真實商務定案標單。**

- 各材料在**8 件獨立住宅 RC 建案**有符合上述條件的數量歷史；**11／10 是材料歷史交易行數，不是建案數**，表示有建案可能有多個材料工項。linked_bid_items 11／10 是關聯到的不同工項 ID 數，**還不能證明工項 FK 都有效或與同一個 ProjectMaster 一致**。
- 這次查詢結果未返回其他用途/結構組合的有效材料歷史。不能由此推論整個資料庫沒有其他 Draft 案、其他工程用途或其他價格明細。
- 尚未核實 material_id=1、2 分別指哪種材料；不得因既有文件提到鋼筋、混凝土就把 ID 硬寫成名稱，需讀 `material_master`。
- 即使 8 >= 名義最低樣本 5，仍不代表 8 件皆來自真實歷史；先前 2026-10-15 的 `DEMO_2A_20261015` 展示資料存在過，且 2026-11-10 查核曾觀察 8 Approved 全住宅 RC。**此批是否仍為展示資料待資料來源核實**，不得直接作為產學正式準確率樣本。
- Q1（不同 HistoryStatus／用途／結構總案數）、Q2（Approved 欄位缺漏）、Q3（歷史交易 FK 品質）尚未提供；這份文件僅證明 Q4。

## 下一個安全調查步驟

執行 `research/2a_synthetic/reference_material_source_audit_readonly.sql`：以唯讀聚合方式確認材料 ID 對應名稱、來源檔名疑似 DEMO/測試的**總數**、不同工程用途/結構的數量與一案多筆材料交易的重複分布。文字檔名判斷**只是疑似示範的 heuristic，不是正式真偽鑑定**。不輸出建案名稱、原始檔名、Hash、標單細項或客戶名稱。

與 Q1～Q3 結果一起檢視後，再決定是否設計專用來源／完整性驗證與介面。此階段只做資料診斷，不改 C#、DB schema、最低樣本或正式權重。
