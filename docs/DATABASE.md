# 2A 資料庫設計

> 更新日期：2026-10-15  
> 資料庫：PostgreSQL  
> 目前正式開發 Branch：`feature/1d-api-integration`

本文件說明目前 2A 各資料表的責任。開發資料庫已經刻意清理，已淘汰的實驗資料表與重複欄位不應留在正式設計中。

## 1. 核心規則

系統有兩個完全獨立的資料領域：

```text
待偵錯／工作資料
analysis_case
  → analysis_bid_version
      → analysis_bid_item
      → analysis_run

歷史／已驗證資料
project_master
  → final_bid_item
  → historical_transaction
```

不得合併這兩個資料領域。

待偵錯案件可以反覆修改，但絕對不能自動變成模型歷史真值。

---

## 2. 待偵錯流程資料表

### analysis_case

代表一個目前正在被 2A 檢查的建案。

主要欄位：

- `analysis_case_id`
- `case_name`
- `project_type`
- `structure_type`
- `city`
- `total_floor_area`
- `site_area`
- `floor_count`
- `basement_floor_count`
- `project_date`
- `status`
- `created_at`
- `updated_at`

用途：

- 保存待偵錯建案基本資料
- 作為所有待偵錯標單版本的父資料
- 永遠不能直接視為歷史真值

### analysis_bid_version

屬於某一個待偵錯案件的不可覆寫標單快照。

主要欄位：

- `analysis_bid_version_id`
- `analysis_case_id`
- `version_no`
- `source_type`
- `source_1d_project_id`
- `source_file_name`
- `source_file_hash`
- `source_note`
- `is_current`
- `created_at`

目前來源類型：

- `Upload`
- `OneD`

規則：

```text
V1 != V2 != V3
```

標單修改後必須建立新版本，不可以覆寫舊版本。

### analysis_bid_item

屬於單一待偵錯版本的解析工項快照。

主要欄位：

- `analysis_bid_item_id`
- `analysis_bid_version_id`
- `source_sequence`
- `sheet_name`
- `row_number`
- `item_code`
- `source_category1`
- `source_category2`
- `source_category3`
- `major_category`
- `item_name`
- `spec`
- `unit`
- `quantity`
- `unit_price`
- `amount`
- `source_confidence`
- `note`

用途：

- 保存當下版本完整快照
- 保留來源追溯
- 不需要重新讀取 1D 就能執行數量與大項價格分析

### analysis_run

某一個待偵錯版本執行一次分析後保存的結果。

主要欄位：

- `analysis_run_id`
- `analysis_bid_version_id`
- `run_type`
- `status`
- `result_json`：JSONB
- `created_at`

Run Type 範例：

- 數量分析
- 大項價格分析

用途：

- 即使後續上傳 V2／V3，也能保留舊分析結果
- 允許使用者重新開啟以前的 Run

---

## 3. 歷史流程資料表

### project_master

代表一個歷史最終標單候選或已核准建案。

主要欄位：

- `project_id`
- `project_name`
- `project_type`
- `structure_type`
- `city`
- `total_floor_area`
- `site_area`
- `floor_count`
- `basement_floor_count`
- `start_date`
- `source_file_name`
- `source_file_hash`
- `imported_at`
- `history_status`
- `history_approved_at`
- `history_approved_by`
- `is_deleted`

目前歷史狀態：

```text
Draft
  → 審核
  → Publish History
  → Approved

Approved
  → Reopen History Review
  → Draft
```

只有 `Approved` 建案可以參與有效模型基準。

### final_bid_item

保存最終標單原始／審核後的來源工項。

主要欄位：

- `final_bid_item_id`
- `project_master_id`
- `source_file_name`
- `sheet_name`
- `row_number`
- `item_code`
- `category1`
- `category2`
- `category3`
- `major_category_override`
- `item_name`
- `spec`
- `unit`
- `qty`
- `unit_price`
- `amount`
- `note`
- `material_id`
- `mapping_status`
- `matched_keyword`
- `mapping_issue_code`
- `mapping_issue_message`
- `data_issue_code`
- `data_issue_message`
- `is_ignored`
- `created_at`
- `updated_at`

重要設計規則：

來源 `category1 / category2 / category3` 必須保留。

若工程大項需要人工修正，使用：

```text
major_category_override
```

不得覆寫來源分類層級。

#### 價格責任

結構體工程歷史價格從 `final_bid_item` 推導：

```text
Amount
或
Qty × UnitPrice
```

再依總樓地板面積換算為每坪單價。

### historical_transaction

只保存標準化後的數量歷史。

主要欄位：

- `transaction_id`
- `material_id`
- `actual_quantity`
- `transaction_date`
- `project_master_id`
- `final_bid_item_id`
- `is_outlier`
- `is_deleted`

目前範圍：

- 鋼筋數量
- 混凝土數量

重要：

`historical_transaction` 刻意**沒有單價欄位**。

價格責任屬於 `final_bid_item`。

---

## 4. 材料／偵錯主檔

### material_category

主要欄位：

- `category_id`
- `category_name`
- `is_active`
- `sort_order`

目前核心分類：

- 結構體工程

### material_master

主要欄位：

- `material_id`
- `material_name`
- `category_id`
- `unit_id`
- `is_active`

目前重要材料：

- 鋼筋／kg
- 混凝土／立方公尺
- 鋼筋續接器／組

為什麼保留鋼筋續接器：

避免關鍵字比對時把「鋼筋續接器」誤判成鋼筋重量。

### item_dictionary

主要欄位：

- `dictionary_id`
- `keyword`
- `material_id`
- `created_at`

用途：

- 將來源工項／規格文字對應到標準材料
- 保存人工學習／手動建立的比對規則

### detection_category

主要欄位：

- `detection_category_id`
- `major_category_name`
- `detection_category_name`
- `unit_id`
- `is_active`
- `sort_order`

目前正式偵錯細項：

```text
結構體工程 / 鋼筋 / kg
結構體工程 / 混凝土 / 立方公尺
```

### detection_category_material_map

主要欄位：

- `material_id`
- `detection_category_id`
- `is_active`
- `created_at`
- `updated_at`

用途：

- 將標準材料連接到數量模型使用的 2A 偵錯細項

---

## 5. 單位主檔

### unit_category

主要欄位：

- `unit_category_id`
- `category_name`
- `allow_conversion`
- `is_active`
- `sort_order`

目前重要類別：

- 重量
- 體積
- 其他

### unit_master

主要欄位：

- `unit_id`
- `unit_name`
- `unit_category_id`
- `to_base_factor`
- `is_base_unit`
- `is_active`
- `sort_order`

目前範例：

```text
重量
├─ kg      基準單位
├─ 公斤
├─ t       1000 kg
└─ 噸      1000 kg

體積
├─ 立方公尺  基準單位
└─ m3

其他
└─ 組
```

建立標準化數量歷史前會先進行單位換算。

---

## 6. 模型參數

### estimation_settings

主要欄位：

- `settings_id`
- `top_k`
- `minimum_sample_count`
- `lower_quantile`
- `median_quantile`
- `upper_quantile`
- `similarity_exponent`
- `time_half_life_years`
- `minimum_similarity_score`
- `structure_weight`
- `area_weight`
- `site_area_weight`
- `floor_weight`
- `year_weight`
- `city_weight`
- `area_difference_cutoff_ratio`
- `site_area_difference_cutoff_ratio`
- `ground_floor_penalty_ratio`
- `basement_floor_penalty_ratio`
- `year_difference_cutoff_years`
- `updated_at`
- `updated_by`

目前相似度輸入：

- 建案類型
- 結構類型
- 縣市
- 總樓地板面積
- 基地面積
- 地上樓層
- 地下樓層
- 建案年份

目前預設最低歷史樣本數：

```text
5 個建案
```

不要為了達到門檻而建立假歷史資料。

---

## 7. Audit 資料表

### audit_log

主要欄位：

- `log_id`
- `module`
- `action_type`
- `entity_type`
- `entity_id`
- `project_id`
- `description`
- `old_value`
- `new_value`
- `action_user`
- `created_at`

用途：

記錄審核與設定操作，例如：

- 材料對應變更
- 最終標單工項修改
- 工程大項人工覆寫
- 建案資料修改
- Publish／Reopen 操作
- 主檔異動
- 參數設定異動

---

## 8. 目前模型資料資格

### 結構體工程價格

來源：

`final_bid_item`

資格：

- 建案為 Approved
- 實際工程大項為結構體工程
- 工項未被忽略
- 有可用 Amount 或 `Qty × UnitPrice`

### 鋼筋／混凝土數量

來源：

`historical_transaction`

資格：

- 建案為 Approved
- 實際工程大項為結構體工程
- 已對應標準材料為鋼筋或混凝土
- 數量為正值
- 原始單位可轉為標準單位
- Transaction 為有效且非 Outlier

---

## 9. 已移除資料庫設計

除非未來有新的明確架構決策，以下舊設計不得恢復：

- `quantity_rule_base`
- `historical_transaction.unit_price`
- `historical_transaction` 舊版重複來源欄位
- 模型已不使用的舊建案欄位
- 只為了讓估算區間顯示而保留的假／測試歷史資料

---

## 10. 開發資料庫目前狀態

截至 2026-10-15，清理後的開發資料庫刻意沒有任何已匯入的工作／歷史案件：

```text
project_master             0
final_bid_item             0
historical_transaction     0

analysis_case              0
analysis_bid_version       0
analysis_bid_item          0
analysis_run               0
```

系統必要的主檔與參數表仍保留必要資料。

這個空白狀態是刻意的。下一階段是真實資料驗證。

---

## 11. Migration 規則

資料庫 Schema 異動必須記錄在：

```text
database/
```

修改 Model 時：

1. 更新 C# Model／EF Mapping
2. 新增對應 SQL Migration
3. 套用到開發資料庫
4. 驗證實際 Schema
5. 如果資料責任改變，更新本文件

目前仍在開發階段，既然某個欄位或資料表已被正式架構淘汰，就不要因為「可能以後會用」而繼續保留。
