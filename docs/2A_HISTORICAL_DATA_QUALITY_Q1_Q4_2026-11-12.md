# 2A 歷史資料品質 Q1–Q4 完整結果（2026-11-12）

> **來源：使用者直接提供**在 Supabase SQL Editor 執行 `research/2a_synthetic/reference_quality_counts_readonly.sql` 後的 Q1、Q2、Q3、Q4 JSON 聚合。這是真正資料庫在查詢當下的**計數輸出**，不是研究合成資料、不是原始工程明細，也**不是**對真實標單來源與整體工程範圍的實質審核。SQL 未修改資料庫。

## Q1：建案分布（原值）

| history_status | project_type | structure_type | distinct_projects | marked_deleted_projects |
|---|---|---|---:|---:|
| Approved | 住宅 | RC | 8 | 0 |
| Draft | 住宅 | RC | 2 | 0 |

合計 **10 個不同建案**、全部住宅 RC，**8 Approved、2 Draft**；此查詢包含現有 `project_master` 的所有狀態，沒有反映其他結構組合的資料。不能因此宣稱系統不支援其他結構，或公司實際歷史工程僅有這 10 件。

## Q2：Approved 且未刪除建案資料完整性（原值）

| 指標 | 數值 |
|---|---:|
| approved_active_projects | 8 |
| missing_project_type | 0 |
| missing_structure_type | 0 |
| invalid_floor_area | 0 |
| invalid_site_area | 0 |
| missing_start_date | 0 |
| missing_source_filename | 0 |
| **missing_source_hash** | **7** |
| missing_approved_at | 0 |
| missing_approved_by | 0 |

**7/8 = 87.5%** Approved 建案缺 `source_file_hash`。雖然其餘被檢核欄位均非缺漏或不合法，仍不能只靠名稱、Approved 或有 `history_approved_by` 就判定資料來源是真實最終標單、範圍完整。Hash 缺漏是**追溯性不足的風險訊號**，不是 7 件必定造假。

## Q3：歷史交易與外鍵品質（原值）

| 指標 | 數值 |
|---|---:|
| historical_rows_total | 23 |
| marked_deleted_rows | 2 |
| marked_outlier_rows | 0 |
| missing_project_reference | 0 |
| missing_bid_item_reference | 0 |
| nonpositive_quantity_rows | 0 |
| unresolved_project_reference | 0 |
| linked_to_nonapproved_or_deleted_project | 2 |
| unresolved_bid_item_reference | 0 |
| bid_item_project_mismatch | 0 |

查詢未發現交易外鍵為 NULL、對不到主表、或來源工項所屬建案不一致；但有 **2 筆已標記刪除**，另有 **2 筆關聯到非 Approved 或已刪除建案**。**兩類問題是否為同一批 2 筆，目前的聚合不能辨識**；不能直接加總為 4 筆不同交易，也不能僅憑 `historical_rows_total - marked_deleted_rows = 21` 推出 21 筆全部適格。

## Q4：有效材料歷史來源之用途／結構分布（原值）

Q4 使用 `Approved`、未刪除建案、未刪除交易、非 Outlier、正數量：

| project_type | structure_type | material_id | distinct_approved_projects | historical_rows | linked_bid_items |
|---|---|---:|---:|---:|---:|
| 住宅 | RC | 1 | 8 | 11 | 11 |
| 住宅 | RC | 2 | 8 | 10 | 10 |

各材料皆為 **8 件獨立住宅 RC Approved 建案**；11／10 為交易行數、11／10 為不同連結工項 ID，**不能當作 11／10 個獨立工程**。材料 ID=1、2 名稱仍未經 `material_master` Q5 查核。Q4 的 21 個有效查詢行與 Q3 的 23 筆全表資料、其中 2 筆刪除**數量吻合**，但仍需交叉統計 Q8，才能得知已刪除與非 Approved 兩組是否重疊。

## 綜合結論與尚未完成的驗證

1. **目前 DB 中名義可用的材料歷史僅涵蓋住宅 RC**。其他結構組合的可靠性沒有資料可直接估算。
2. **資料追溯風險高**：8 Approved 案中 7 件缺檔案 Hash。不得把 Hash 存在等同真偽審核，也不得把缺 Hash 直接等同假資料。
3. **來源真實性尚無證明**：此前開發 DB 曾加入 `DEMO_2A_20261015` 展示資料；本次 Q1–Q4 沒有查文件來源內容，**仍未知當下 8 Approved 中有多少是 DEMO**。
4. **外鍵基礎一致性良好**：無空／錯連結的歷史交易；要再釐清刪除狀態與非 Approved 關聯的交集，不能把 Q3 互相重疊的異常計數錯相加。
5. **正式最低參考數是名義門檻，不是驗真證書**。即使 8 件達到 5 件，不代表 8 件是真實、完整、同口徑公司最終標單。
6. 後續由使用者在正確開發 DB 執行 `research/2a_synthetic/reference_material_source_audit_readonly.sql` 的 Q5–Q8 聚合，確認材料 ID、來源疑似 DEMO、同案同材料多筆交易及 Q3 狀態交叉表。只用於資料品質調查，不修改正式資料庫。

**本輪僅分析使用者提供之 SQL 結果並更新研究／交接文件與唯讀查詢。沒有更新 C#、參數或資料庫。**
