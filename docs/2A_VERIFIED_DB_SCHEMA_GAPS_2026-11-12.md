# 2A 真實 PostgreSQL Schema 核驗與來源資格缺口（2026-11-12）

> 依據：使用者在 Supabase SQL Editor 執行 `reference_schema_readonly.sql` 後上傳的完整 `information_schema.columns` JSON 列表。這份輸出提供**實際 public schema** 的欄位、型別、nullable，不包含資料列或任何建案內容。已解析 86 筆欄位、6 張表；與先前只有 C# 模型可讀的靜態對照不同，本輪是**資料庫欄位存在性的直接證據**。並非資料列品質、資料完整性或企業來源真實性驗證。

## 已確認的欄位數

| 表名 | 欄位數 |
|---|---:|
| `project_master` | 17 |
| `final_bid_item` | 27 |
| `historical_transaction` | 8 |
| `material_master` | 5 |
| `unit_master` | 7 |
| `estimation_settings` | 22 |
| **合計** | **86** |

資料庫此次沒有查 `analysis_case`、`unit_category`、`audit_log`、`detection_category`、`history_operation_receipt` 等其他表；**不得將本次結論擴大為整座 DB 的完整 Schema**。

## 資格條件映射（本輪已由 PostgreSQL 確認）

| 目的 | 已確認真實欄位 | 本次能證明什麼／不能證明什麼 |
|---|---|---|
| 建案用途／結構 | `project_master.project_type`, `structure_type`（皆 nullable） | 能保存類型；**不能**保證每筆都有正確類型 |
| 建案面積與日期 | `total_floor_area` nullable；`site_area` NOT NULL；`start_date` nullable | 存在欄位；**不能**證明正值、面積口徑一致、年代無洩漏 |
| 歷史核准與經手人 | `history_status` NOT NULL；`history_approved_at/by` nullable | 可表示核准流程；Approved **不代表**真實完整最終標單 |
| 檔案可追溯 | `source_file_name/hash` nullable；工項有 `source_file_name` NOT NULL、`sheet_name/row_number` nullable | 來源資訊可記錄；Hash 是識別值，**不是**商務定案或真偽證明 |
| 工項單位、數量、單價 | `final_bid_item.unit/qty/unit_price/amount` 均 nullable | 可檢查值、空值與來源明細；不保證完整結構體工程大項 |
| 工項映射／資料警示 | `material_id`, `mapping_status`, `data_issue_code`, `is_ignored`, `major_category_override` | 有人工審核與分類輔助；**無法由此直接宣告整份標單範圍完整** |
| 材料基準單位 | `material_master.unit_id` NOT NULL；`unit_master.to_base_factor` nullable、`is_base_unit` NOT NULL、`is_active` NOT NULL | 存在轉換主檔；不等於每筆 Excel 單位已成功標準化 |
| 歷史材料量 | `historical_transaction.material_id`、`actual_quantity` NOT NULL；`project_master_id` 和 `final_bid_item_id` **nullable** | 可記錄歷史用量；**要確認有效列是否都能回溯到合法歷史建案／工項** |
| 相似度參數 | `estimation_settings` 22 欄，包含 `minimum_sample_count`、六因素權重與截斷值 | 只確認設定欄位存在；**尚未讀取實際當前參數值** |

## 直接得到的缺口

在上述六張表的 86 欄中，沒有 `source_kind`、`is_synthetic`、`source_verified`、`scope_quality`、`scope_verified` 或等效的**專用**來源真實性／工程範圍完整性資格欄位。因此：
1. 不能僅用 `history_status='Approved'` 或 `source_file_hash` 判斷「真實且完整的公司最終標單」。
2. 不能直接把第七輪 `eligibility_review.py` 的研究性 `source_kind/scope_quality` 欄位照搬進查詢；**目前正式 Schema 沒有它們**。
3. 舊文件若仍寫「2026-10-15 開發 DB 0 件」屬歷史快照。此次只查欄位，**沒有驗證目前資料筆數、核准案件來源或 DEMO 資料是否仍在**。
4. 研究架構可以提出「來源待核實／工程範圍待確認」等狀態，但在沒有新審核流程與證據之前，不得把這些狀態冒充完成審核；未經使用者同意不得改正式 DB schema 或正式異常燈號。

## 下一輪唯讀驗證（已備好 SQL）

研究檔案：`research/2a_synthetic/reference_quality_counts_readonly.sql`。

這份 SQL **不會回傳建案名稱、標單明細或檔案 Hash**。以聚合方式觀察：
- 歷史 `history_status` 與每用途／結構組的**獨立建案數**；
- Approved 案件的基礎欄位缺漏與來源檔案欄位完整性（不代表來源已驗證）；
- 歷史數量紀錄是否可連到 Approved、未刪除建案，以及是否遺漏 `project_master_id`／`final_bid_item_id`；
- 各用途／結構／材料的獨立建案數與材料數量筆數分開顯示；
- `estimation_settings` 僅列參數欄位存在性；實際數值可在後續另經核實。

**執行前核實目標 DB**，先確認查詢權限；輸出是唯讀聚合，避免在聊天中貼出供應商商業敏感的明細。這不會把資料自動標記為真實或正式模型可用。

## 新對話接手提醒

目前真正已證實的是：六張表的 86 欄存在／nullable，且尚無專用真偽／工程範圍欄位。**下一步要看實際資料品質的聚合指標，再設計使用者可審核的來源及完整性證據流程；不能跳到 Migration 或正式權重改動。** 一切研究結果須符合 `waizheng-2a-handoff-guardian` Skill 留下審查記錄。
