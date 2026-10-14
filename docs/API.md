# 2A API Reference

> Updated: 2026-10-15  
> Branch: `feature/1d-api-integration`  
> Base path: `/api`

This document reflects the current ASP.NET Core controllers on the active development branch. It documents the formal 2A workflow, the historical-final-bid review flow, master data, settings, and the optional 1D integration.

## 1. Conventions

### Response behavior

Most service-backed endpoints return:

- `200 OK` on success
- `400 Bad Request` for validation errors
- `404 Not Found` when the requested entity does not exist
- `409 Conflict` when the current state does not allow the requested action

Error body:

```json
{
  "message": "error description"
}
```

### Upload limit

Excel/CSV upload routes currently accept request bodies up to **100 MB**.

### Important data rule

Target-case APIs and historical-project APIs are intentionally separate:

```text
Target working data:
AnalysisCase -> Version -> Run

Historical truth:
ProjectMaster -> FinalBidItem -> HistoricalTransaction
```

A target case never automatically becomes historical truth.

---

# 2. System

## GET /api/system/health

Simple backend health check.

Example response:

```json
{
  "status": "ok"
}
```

## GET /api/system/options

Returns UI option lists and standard materials.

Current option groups include:

- project types: 住宅 / 廠房
- structure types: RC / SC / SRC
- Taiwan cities/counties
- active standard materials

---

# 3. Workbook preview / inspection

These endpoints are **read-only**. They do not create projects, target cases, historical transactions, or model samples.

## POST /api/tender-import/preview

Content type: `multipart/form-data`

Fields:

- `excelFile`: Excel/CSV file

Purpose:

- list workbook worksheets
- show parsed row counts and sample items
- force the user to decide which worksheet(s) are the authentic engineering details

Important:

- multi-sheet workbooks should not silently import every sheet
- summary / quotation / supplier-comparison sheets require manual review

## POST /api/tender-import/inspect

Content type: `multipart/form-data`

Fields:

- `excelFile`: Excel/CSV file
- `includedSheetNames`: JSON array of explicitly selected sheet names

Example:

```text
includedSheetNames = ["建築工程-預算單"]
```

Purpose:

- parse only selected worksheets
- return source sheet / row traceability
- preview candidate rebar / concrete quantities conservatively
- surface warnings before any database write

Rules:

- at least one worksheet must be explicitly selected
- duplicate or blank sheet names are rejected
- more than 5000 candidate rows are rejected
- preview quantities are not approved history and are not P15/P50/P85 results

---

# 4. Target cases — AnalysisCase

Base route:

```text
/api/analysis-cases
```

## GET /api/analysis-cases/policy

Returns the current provisional business policy, including:

- standalone upload enabled
- 1D optional
- current major/detail hierarchy
- current estimation scope

## GET /api/analysis-cases

Lists target cases.

Main response fields include:

- `analysisCaseId`
- `caseName`
- `projectType`
- `structureType`
- `city`
- `totalFloorArea`
- `siteArea`
- `floorCount`
- `basementFloorCount`
- `projectDate`
- `versionCount`
- `currentVersionId`
- `currentVersionNo`
- `currentSourceType`
- `updatedAt`

## GET /api/analysis-cases/{analysisCaseId}

Returns one target case and its version list.

## GET /api/analysis-cases/{analysisCaseId}/versions/{versionId}

Returns one immutable target-bid snapshot and all stored items.

## GET /api/analysis-cases/{analysisCaseId}/runs

Returns saved analysis-run summaries.

## GET /api/analysis-cases/{analysisCaseId}/runs/{analysisRunId}

Returns one saved analysis result from `analysis_run.result_json`.

## POST /api/analysis-cases/from-upload

Creates a new target case from a structured upload.

Content type: `multipart/form-data`

Fields:

- `excelFile`
- `caseName`
- `projectType`
- `structureType`
- `city`
- `totalFloorArea`
- `siteArea`
- `floorCount`
- `basementFloorCount`
- `projectDate`
- `note` optional
- `includedSheetNames` JSON array

Current provisional rule:

The upload must provide enough structure for 2A to resolve usable engineering-major/item/unit/quantity data. Arbitrary raw files are not silently guessed.

## POST /api/analysis-cases/from-1d/{oneDProjectId}

Creates a new target case by reading 1D **once**, then storing a local 2A snapshot.

Content type: `application/json`

Body:

```json
{
  "caseName": "A集合住宅",
  "projectType": "住宅",
  "structureType": "RC",
  "city": "臺中市",
  "totalFloorArea": 15000,
  "siteArea": 1800,
  "floorCount": 15,
  "basementFloorCount": 3,
  "projectDate": "2026-10-01"
}
```

Optional query parameter:

- `note`

1D is not called again for later analysis runs.

## POST /api/analysis-cases/{analysisCaseId}/versions/upload

Adds a new immutable target version.

Content type: `multipart/form-data`

Fields:

- `excelFile`
- `note` optional
- `includedSheetNames` JSON array

The previous version remains unchanged.

## POST /api/analysis-cases/{analysisCaseId}/versions/from-1d/{oneDProjectId}

Adds a new version by explicitly re-reading 1D.

Body:

```json
{
  "note": "重新取得 1D 最新結果"
}
```

Use this only when an updated 1D result is actually needed.

## POST /api/analysis-cases/{analysisCaseId}/versions/{versionId}/quantity-analysis

Runs the current rebar/concrete quantity anomaly flow against a stored version.

Body:

```json
{
  "topCount": 8
}
```

`topCount` is optional.

Current detail scope:

- 鋼筋
- 混凝土

## POST /api/analysis-cases/{analysisCaseId}/versions/{versionId}/major-price-analysis

Runs major-category price analysis against a stored target version.

Body:

```json
{
  "majorCategory": "結構體工程",
  "topCount": 8
}
```

Current supported major category:

- 結構體工程

---

# 5. Historical finalized bids

## POST /api/Estimation/upload-final-bid

Imports a finalized-bid candidate into the **history review** pipeline.

Content type: `multipart/form-data`

Fields:

- `excelFile`
- `projectName`
- `projectType`
- `structureType`
- `city`
- `totalFloorArea`
- `siteArea`
- `floorCount`
- `basementFloorCount`
- `projectMonth`
- `includedSheetNames` optional JSON array, but multi-sheet files should use explicit selection

Important:

Import does **not** make the data trusted model history.

New project state:

```text
HistoryStatus = Draft
```

## GET /api/Estimation/mapping-items

Query parameters:

- `projectId` optional
- `status` default `Pending`
- `search` optional
- `issueCode` optional
- `majorCategory` optional
- `category` optional
- `unit` optional
- `page` default 1
- `pageSize` default 50

Returns review rows for category/material/unit/data correction.

## GET /api/Estimation/mapping-summary

Query:

- `projectId` optional

Returns review summary counts.

## GET /api/Estimation/mapping-filter-options

Query:

- `projectId` optional

Returns filter option values for the review UI.

## GET /api/Estimation/material-options

Query:

- `search` optional

Returns standard materials for manual mapping.

## POST /api/Estimation/update-material-mapping

Content type: `multipart/form-data`

Fields:

- `finalBidItemId`
- `materialId`

Manually assigns a standard material.

Approved history must be reopened before editing.

## POST /api/Estimation/update-major-category

Content type: `multipart/form-data`

Fields:

- `finalBidItemId`
- `majorCategory` optional

Sets or clears `major_category_override`.

Source `Category1 / Category2 / Category3` values are preserved.

## POST /api/Estimation/update-bid-item-data

Content type: `multipart/form-data`

Fields:

- `finalBidItemId`
- `itemName`
- `qty`
- `unitPrice`
- `unit`

Used during Draft review to correct parsed source data.

## POST /api/Estimation/ignore-bid-item

Fields:

- `finalBidItemId`

Excludes a row from the historical baseline review.

## POST /api/Estimation/unignore-bid-item

Fields:

- `finalBidItemId`

Restores an ignored row.

---

# 6. Historical project management

Base route:

```text
/api/projects
```

## GET /api/projects

Query:

- `search` optional

Lists historical-project candidates and their review state.

## GET /api/projects/{projectId}

Returns one historical project.

## PUT /api/projects/{projectId}

Body:

```json
{
  "projectName": "A集合住宅",
  "projectType": "住宅",
  "structureType": "RC",
  "city": "臺中市",
  "totalFloorArea": 15000,
  "siteArea": 1800,
  "floorCount": 15,
  "basementFloorCount": 3,
  "projectMonth": "2026-10"
}
```

Approved history is locked. Reopen review before modifying model inputs.

## POST /api/projects/{projectId}/publish-history

Explicitly publishes a reviewed Draft project.

Expected state transition:

```text
Draft -> Approved
```

Publication is blocked while unresolved issues remain.

Current important blockers include:

- unrecognized engineering major on a non-ignored row
- structural-work row without usable price data
- rebar/concrete row with invalid quantity/unit
- other review conditions reported by the project summary

## POST /api/projects/{projectId}/reopen-history-review

State transition:

```text
Approved -> Draft
```

Reopening temporarily removes the project from active model baselines and permits corrections.

## DELETE /api/projects/{projectId}

Deletes/soft-deletes the project according to the current service rules.

## GET /api/projects/{projectId}/final-bid-items

Query:

- `search` optional
- `page` default 1
- `pageSize` default 50

Returns source final-bid rows.

## GET /api/projects/{projectId}/2a-summary

Returns 2A qualification diagnostics, including:

- history status
- structural item/priced/unpriced counts
- unclassified rows
- manual major-category correction count
- structural unmapped count
- rebar/concrete source/candidate/excluded counts
- publish readiness and blockers
- structural total amount
- structural price per ping
- rebar/concrete historical quantities and transaction counts

## GET /api/projects/{projectId}/historical-data

Returns standardized active quantity history for the project.

Current quantity-history scope:

- rebar
- concrete

---

# 7. Structural-work price estimation

## GET /api/major-category-price/readiness

Returns current price-baseline readiness for `結構體工程`.

Readiness is based on valid **Approved** historical projects.

## POST /api/major-category-price/range

Body:

```json
{
  "target": {
    "projectType": "住宅",
    "structureType": "RC",
    "city": "臺中市",
    "totalFloorArea": 15000,
    "siteArea": 1800,
    "floorCount": 15,
    "basementFloorCount": 3,
    "projectDate": "2026-10-01"
  },
  "majorCategory": "結構體工程",
  "observedAmount": null,
  "topCount": 8
}
```

Main response fields:

- candidate / available / reference project counts
- `hasEnoughSamples`
- P15 / P50 / P85 price per ping
- P15 / P50 / P85 total amount
- optional observed amount / observed price per ping
- analysis status / traffic light
- weighted historical references

If valid samples are below `MinimumSampleCount`, the correct response is insufficient history rather than a forced range.

---

# 8. Detection categories — rebar / concrete quantity

Base route:

```text
/api/detection-category
```

## GET /api/detection-category

Returns configured detection categories.

Current formal categories:

- 結構體工程 / 鋼筋 / kg
- 結構體工程 / 混凝土 / 立方公尺

## GET /api/detection-category/readiness

Returns independent historical-sample readiness for each quantity detection category.

## GET /api/detection-category/mappings

Query:

- `detectionCategoryId` optional

Returns standard-material mappings.

## POST /api/detection-category/create

Form fields:

- `majorCategoryName`
- `detectionCategoryName`
- `unitId`

## POST /api/detection-category/assign-material

Form fields:

- `detectionCategoryId`
- `materialId`

## POST /api/detection-category/unassign-material

Form fields:

- `materialId`

## POST /api/detection-category/{detectionCategoryId}/range

Body:

```json
{
  "target": {
    "projectType": "住宅",
    "structureType": "RC",
    "city": "臺中市",
    "totalFloorArea": 15000,
    "siteArea": 1800,
    "floorCount": 15,
    "basementFloorCount": 3,
    "projectDate": "2026-10-01"
  },
  "topCount": 8
}
```

Returns weighted quantity / unit-area range and historical references.

---

# 9. Optional 1D integration

Base route:

```text
/api/one-d
```

## GET /api/one-d/status

Returns:

- `configured`
- human-readable message

When 1D is not configured, 2A direct upload remains available.

## GET /api/one-d/projects/{projectId}/result

Integration-check / preview endpoint only.

The formal workflow should use:

```text
POST /api/analysis-cases/from-1d/{projectId}
```

to create a local target snapshot.

Expected 1D item fields include:

- category
- chapter
- sequence
- code
- name
- unit
- quantity
- confidence

1D currently does not provide the monetary fields required for target structural-price analysis.

---

# 10. Settings

## GET /api/settings

Returns the current estimation settings.

## PUT /api/settings

Body fields:

- `topK`
- `minimumSampleCount`
- `lowerQuantile`
- `medianQuantile`
- `upperQuantile`
- `similarityExponent`
- `timeHalfLifeYears`
- `minimumSimilarityScore`
- `structureWeight`
- `areaWeight`
- `siteAreaWeight`
- `floorWeight`
- `yearWeight`
- `cityWeight`
- `areaDifferenceCutoffRatio`
- `siteAreaDifferenceCutoffRatio`
- `groundFloorPenaltyRatio`
- `basementFloorPenaltyRatio`
- `yearDifferenceCutoffYears`

---

# 11. Audit log

## GET /api/audit-logs

Query parameters:

- `search`
- `module`
- `actionType`
- `startDate`
- `endDate`
- `page`
- `pageSize`

Returns paginated audit records.

---

# 12. Material-category master

Base route:

```text
/api/material-category
```

Endpoints:

- `GET /`
- `POST /create`
- `POST /update`
- `POST /set-active`

Create/update uses form data.

---

# 13. Standard-material master

Base route:

```text
/api/standard-material
```

Endpoints:

- `GET /`
- `GET /filter-options`
- `POST /create`
- `POST /update`
- `POST /set-active`

Main material fields:

- name
- material category
- standard unit
- active state

---

# 14. Unit-category master

Base route:

```text
/api/unit-category
```

Endpoints:

- `GET /`
- `POST /create`

Create fields:

- `categoryName`
- `allowConversion`
- `baseUnitName`

For conversion-enabled categories, a base unit is required.

---

# 15. Unit master

Base route:

```text
/api/unit-master
```

Endpoints:

- `GET /`
- `POST /create`
- `POST /update`
- `POST /set-active`

Main fields:

- `unitName`
- `unitCategoryId`
- `toBaseFactor`

---

# 16. Current API boundaries

Do not reintroduce these removed legacy endpoints unless a new architecture decision is made:

- `/api/bid-anomaly`
- generic material range-estimation endpoints
- core-material estimation endpoints
- old quantity-rule endpoints

The formal target-analysis entry is the versioned `AnalysisCase` workflow.
