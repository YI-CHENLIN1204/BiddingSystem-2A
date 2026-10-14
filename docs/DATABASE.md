# 2A Database Design

> Updated: 2026-10-15  
> Database: PostgreSQL  
> Active development branch: `feature/1d-api-integration`

This document describes the current 2A database responsibilities. The development database has been deliberately cleaned so obsolete experimental tables and duplicate fields do not remain in the formal design.

## 1. Core rule

There are two independent data domains:

```text
TARGET / WORKING DATA
analysis_case
  -> analysis_bid_version
      -> analysis_bid_item
      -> analysis_run

HISTORICAL / VERIFIED DATA
project_master
  -> final_bid_item
  -> historical_transaction
```

Do not merge these domains.

A target case can be revised many times and must never automatically become model truth.

---

# 2. Target pipeline tables

## analysis_case

One in-progress project being checked by 2A.

Main columns:

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

Purpose:

- store target project metadata
- serve as parent of all target bid versions
- never act as historical truth

## analysis_bid_version

Immutable bid snapshot belonging to one target case.

Main columns:

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

Current source types:

- `Upload`
- `OneD`

Rule:

```text
V1 != V2 != V3
```

Do not overwrite an older version when the bid changes.

## analysis_bid_item

Parsed item snapshot for one target version.

Main columns:

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

Purpose:

- preserve the exact target-version snapshot
- keep source traceability
- support quantity and major-price analysis without re-reading 1D

## analysis_run

Saved result of one analysis executed against one target version.

Main columns:

- `analysis_run_id`
- `analysis_bid_version_id`
- `run_type`
- `status`
- `result_json` JSONB
- `created_at`

Examples of run type:

- quantity analysis
- major-category price analysis

Purpose:

- preserve historical results even after V2/V3 is uploaded
- allow users to reopen old analysis runs

---

# 3. Historical pipeline tables

## project_master

One candidate or approved finalized historical project.

Main columns:

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

Current historical state model:

```text
Draft
  -> review
  -> Publish History
  -> Approved

Approved
  -> Reopen History Review
  -> Draft
```

Only `Approved` projects may participate in active model baselines.

## final_bid_item

Original / reviewed source rows from a finalized bid.

Main columns:

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

Important design rule:

Source `category1/category2/category3` is preserved.

If the engineering major requires human correction, use:

```text
major_category_override
```

Do not overwrite the source hierarchy.

### Price responsibility

Structural-work historical price is derived from `final_bid_item`:

```text
Amount
or
Qty * UnitPrice
```

Then normalized to price per ping.

## historical_transaction

Standardized quantity history only.

Main columns:

- `transaction_id`
- `material_id`
- `actual_quantity`
- `transaction_date`
- `project_master_id`
- `final_bid_item_id`
- `is_outlier`
- `is_deleted`

Current scope:

- rebar quantity
- concrete quantity

Important:

`historical_transaction` intentionally has **no unit-price column**.

Price responsibility belongs to `final_bid_item`.

---

# 4. Material / detection master tables

## material_category

Main columns:

- `category_id`
- `category_name`
- `is_active`
- `sort_order`

Current core category:

- 結構體工程

## material_master

Main columns:

- `material_id`
- `material_name`
- `category_id`
- `unit_id`
- `is_active`

Current important materials:

- 鋼筋 / kg
- 混凝土 / 立方公尺
- 鋼筋續接器 / 組

Why keep 鋼筋續接器:

It prevents keyword matching from incorrectly treating couplers as rebar weight.

## item_dictionary

Main columns:

- `dictionary_id`
- `keyword`
- `material_id`
- `created_at`

Purpose:

- map source item/spec text to a standard material
- retain learned/manual matching rules

## detection_category

Main columns:

- `detection_category_id`
- `major_category_name`
- `detection_category_name`
- `unit_id`
- `is_active`
- `sort_order`

Current formal detection categories:

```text
結構體工程 / 鋼筋 / kg
結構體工程 / 混凝土 / 立方公尺
```

## detection_category_material_map

Main columns:

- `material_id`
- `detection_category_id`
- `is_active`
- `created_at`
- `updated_at`

Purpose:

- connect standard materials to the 2A detection category used by the quantity model

---

# 5. Unit master tables

## unit_category

Main columns:

- `unit_category_id`
- `category_name`
- `allow_conversion`
- `is_active`
- `sort_order`

Current important categories:

- 重量
- 體積
- 其他

## unit_master

Main columns:

- `unit_id`
- `unit_name`
- `unit_category_id`
- `to_base_factor`
- `is_base_unit`
- `is_active`
- `sort_order`

Current examples:

```text
重量
├─ kg      base
├─ 公斤
├─ t       1000 kg
└─ 噸      1000 kg

體積
├─ 立方公尺  base
└─ m3

其他
└─ 組
```

Unit conversion is used before standardized quantity history is created.

---

# 6. Model settings

## estimation_settings

Main columns:

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

Current similarity inputs:

- project type
- structure type
- city
- total floor area
- site area
- ground floors
- basement floors
- project year

Current default minimum history sample count:

```text
5 projects
```

Do not fabricate history just to reach the threshold.

---

# 7. Audit table

## audit_log

Main columns:

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

Purpose:

Track review and configuration actions such as:

- material mapping changes
- final-bid row edits
- major-category overrides
- project changes
- publish/reopen operations
- master-data changes
- settings changes

---

# 8. Current model data eligibility

## Structural price

Source:

`final_bid_item`

Eligibility requires:

- project is Approved
- effective major category is 結構體工程
- row is not ignored
- usable amount or `Qty * UnitPrice`

## Rebar / concrete quantity

Source:

`historical_transaction`

Eligibility requires:

- project is Approved
- effective major category is 結構體工程
- mapped standard material is rebar or concrete
- positive quantity
- source unit can be converted to the standard unit
- transaction is active and not outlier

---

# 9. Removed database design

The following obsolete design must not be restored without a new explicit architecture decision:

- `quantity_rule_base`
- `historical_transaction.unit_price`
- old duplicated source fields in `historical_transaction`
- obsolete project columns no longer used by the model
- fake/test history retained only to make ranges appear

---

# 10. Development database state

As of 2026-10-15, the cleaned development database intentionally contains no imported working/history cases:

```text
project_master             0
final_bid_item             0
historical_transaction     0

analysis_case              0
analysis_bid_version       0
analysis_bid_item          0
analysis_run               0
```

Master/configuration tables remain populated as required by the application.

This empty state is intentional. The next phase is real-data validation.

---

# 11. Migration rule

Database schema changes should be recorded under:

```text
database/
```

When changing a model:

1. update C# model / EF mapping
2. add a matching SQL migration
3. apply it to the development database
4. verify the resulting schema
5. update this document if the data responsibility changes

Do not keep obsolete columns “just in case” during the current development phase when the current architecture has explicitly removed them.
