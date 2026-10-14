# 2A Current Architecture

> Updated: 2026-10-15  
> Development branch: `feature/1d-api-integration`

This document records the current production direction of 2A. It is intentionally separate from the older POC branch and from legacy generic material-estimation experiments.

## 1. Current product scope

2A currently has two analysis levels:

- **Major-category estimation / price anomaly:** `結構體工程`
- **Detail quantity anomaly detection:** `鋼筋`, `混凝土`

Current hierarchy:

```text
結構體工程
├─ 鋼筋      -> quantity anomaly detection
└─ 混凝土    -> quantity anomaly detection
```

Other structural items remain traceable in the final bid and may contribute to the total structural-work amount, but do not enter the current rebar/concrete quantity history baseline.

For finalized historical bids, the original Category1 / Category2 / Category3 values are preserved. If the parser cannot identify the correct engineering major, reviewers can set a separate `major_category_override`. This correction changes 2A eligibility and price grouping without destroying the source hierarchy.

## 2. Target data and historical data are separate

### Target Pipeline

Used for projects that are currently being estimated or repeatedly checked.

```text
Upload ───────────────┐
                     ├─> AnalysisCase
Optional 1D import ──┘      │
                            ├─ V1
                            ├─ V2
                            └─ V3
                                 │
                                 └─ AnalysisRun
```

Tables:

- `analysis_case`
- `analysis_bid_version`
- `analysis_bid_item`
- `analysis_run`

A target case NEVER writes directly to `historical_transaction`.

### History Pipeline

Used only for finalized official bids that are candidates to become model baselines.

```text
Final bid
  -> ProjectMaster (Draft)
  -> FinalBidItem
  -> engineering-major / material / unit review
  -> explicit "Publish History" approval
  -> ProjectMaster (Approved)
       ├─ structural-work price baseline stays in FinalBidItem
       └─ rebar/concrete quantity baseline -> HistoricalTransaction
```

An imported final bid is therefore not automatically trusted by the model. It must be reviewed and explicitly published first. Reopening review temporarily removes the project from similarity/price baselines and marks its quantity-history transactions inactive.

The review page continuously recalculates publication readiness. Publishing is blocked while any non-ignored row has an unrecognized engineering major, while any structural-work row lacks usable price data, or while a row already mapped as rebar/concrete still has invalid quantity/unit data. This prevents partially reviewed imports from silently entering the model.

Tables:

- `project_master`
- `final_bid_item`
- `historical_transaction`

## 3. 1D is optional

2A must remain fully usable without 1D.

### Standalone / school-project environment

```text
Upload structured target bid
-> AnalysisCase snapshot
-> repeated versions
-> quantity / price anomaly analysis
```

### Industry integration environment

If `OneD:BaseUrl` is configured:

```text
1D result
-> one-time import
-> AnalysisCase snapshot
-> all later analysis runs inside 2A
```

1D is an optional target-data source, not a historical-data source and not a runtime dependency for every analysis run.

## 4. Direct upload provisional rule

Until the vendor confirms the final interface contract, direct target upload is restricted to structured files that can provide:

- major category
- item name and/or specification
- unit
- quantity

For major-category price analysis, the target also needs unit price and/or amount.

Unclassified raw files are not silently sent to 1D and are not automatically guessed by 2A.

## 5. Historical quantity eligibility

A final-bid item enters `historical_transaction` only after its project is `Approved` and all current row-level conditions are satisfied:

- item is not ignored
- item is mapped to a standard material
- major category resolves to `結構體工程`
- material is `鋼筋` or `混凝土`
- quantity is positive
- source unit can be converted to the standard unit

Unit price is NOT required for quantity history.

`historical_transaction` is quantity-only. Price remains in `final_bid_item`.

## 6. Historical review and immutability

Imported finalized bids first enter a `Draft` history-review state.

```text
Final bid import
-> Draft
-> review major category / mapping / quantity / unit / price
-> Publish history baseline
-> Approved
```

When a project is `Approved`, its history inputs are locked:

- project similarity metadata cannot be edited
- major-category overrides cannot be edited
- material mappings cannot be edited
- quantity / unit / unit price cannot be edited
- ignore / unignore cannot be changed

To correct an approved baseline, the user must explicitly use **Reopen history review**. Reopening temporarily disables the project's active quantity-history rows and returns the project to `Draft`; after corrections, it must pass review and be published again.

This prevents a verified historical baseline from silently changing after approval.

## 7. Historical price baseline

Major-category price estimation currently supports only approved historical projects and only:

- `結構體工程`

For each historical project:

1. sum valid structural-work line amounts
2. fall back to `Qty * UnitPrice` when Amount is unavailable
3. divide by project floor area in ping
4. rank / weight similar projects
5. apply time decay
6. calculate weighted P15 / P50 / P85

If the valid reference-project count is below `MinimumSampleCount`, the system reports insufficient data instead of forcing an anomaly result.

## 8. Similar-project features

Current similarity inputs:

- project type
- structure type
- city
- total floor area
- **site area**
- ground-floor count
- basement-floor count
- project year

Site area is now a first-class feature in:

- `project_master.site_area`
- `analysis_case.site_area`
- target forms
- final-bid import metadata
- structural estimation input
- similarity scoring
- settings UI

Current configurable weights include:

- structure weight
- floor-area weight
- site-area weight
- floor weight
- city weight
- year weight

Site area also has its own difference-cutoff ratio.

## 9. Current readiness rules

Default minimum sample count: **5 projects**.

Quantity readiness is tracked independently for:

- rebar
- concrete

Price readiness is tracked independently for:

- structural work

The dashboard displays how many valid historical projects exist for each baseline and how many are still missing.

Each historical-project detail page also shows qualification diagnostics for the imported final bid. The final-bid review list additionally marks each row as `結構體價格基準`, `鋼筋／混凝土數量基準`, or `不納入目前 2A 基準`, so reviewers can see the model impact of each correction immediately.

Qualification diagnostics include:

- rows with no recognized engineering major
- rows manually corrected to a major category
- structural rows with no standard-material mapping
- structural rows without usable price data
- rebar source rows that did or did not form quantity-history transactions
- concrete source rows that did or did not form quantity-history transactions

## 10. Main APIs

### Target cases

- `GET /api/analysis-cases`
- `GET /api/analysis-cases/{id}`
- `GET /api/analysis-cases/{id}/versions/{versionId}`
- `GET /api/analysis-cases/{id}/runs`
- `GET /api/analysis-cases/{id}/runs/{runId}`
- `POST /api/analysis-cases/from-upload`
- `POST /api/analysis-cases/from-1d/{oneDProjectId}`
- `POST /api/analysis-cases/{id}/versions/upload`
- `POST /api/analysis-cases/{id}/versions/from-1d/{oneDProjectId}`
- `POST /api/analysis-cases/{id}/versions/{versionId}/quantity-analysis`
- `POST /api/analysis-cases/{id}/versions/{versionId}/major-price-analysis`

### Readiness

- `GET /api/detection-category/readiness`
- `GET /api/major-category-price/readiness`

### Final-bid review

- `GET /api/Estimation/mapping-items` supports effective-major-category filtering, including unclassified rows
- `POST /api/Estimation/update-major-category` sets or clears the reviewed major-category override
- major-category changes immediately refresh candidate eligibility and are recorded in the audit log
- the final-bid review page shows live publish blockers and can publish the project directly once the review is ready

### Historical projects

- `GET /api/projects`
- `POST /api/projects/{projectId}/publish-history`
- `POST /api/projects/{projectId}/reopen-history-review`
- `GET /api/projects/{projectId}`
- `GET /api/projects/{projectId}/historical-data`
- `GET /api/projects/{projectId}/2a-summary`

### Structural estimation

- `POST /api/major-category-price/range`

## 11. Removed legacy architecture

The old generic material-estimation stack has been removed because it conflicts with the current scope.

Removed:

- `RangeEstimationService`
- `BatchEstimationService`
- `CoreMaterialService`
- `EstimationTargetService`
- `RangeEstimationController`
- legacy range/core-material DTOs
- `quantity_rule_base` model and database table
- core-material settings UI
- legacy one-off `/api/bid-anomaly` flow

The formal target-analysis entry is now the versioned `AnalysisCase` workflow.

## 12. Current database state

The development database has been hard-cleaned.

Historical projects now use `project_master.history_status` with `Draft` / `Approved`. Only `Approved` projects can participate in the similarity model or structural-price baseline.

No fake historical projects are seeded.

Expected current data state:

- `project_master`: 0 rows until real finalized bids are imported
- `historical_transaction`: 0 rows until real rebar/concrete history is created
- `analysis_case`: 0 rows until a real target test is created

Current detection master:

- `結構體工程 / 鋼筋 / kg`
- `結構體工程 / 混凝土 / 立方公尺`

The standard material `鋼筋續接器` is retained only to prevent it from being incorrectly merged into rebar weight history.

## 13. Still awaiting vendor confirmation

The implementation currently follows provisional assumptions for:

1. whether direct upload must remain in the industry version
2. exact schema accepted by direct upload
3. final major/detail granularity
4. exact 1D-to-2A operational workflow
5. which categories should be excluded from quantity estimation and whether those categories may still use major-category price analysis

The current architecture allows these rules to be adjusted without redesigning the Target/History separation.

## 14. Current limitation

There are currently no official historical projects in the cleaned development database.

Therefore P15 / P50 / P85 cannot yet produce meaningful real-world ranges. The system must continue returning insufficient-data status until official finalized bids are imported and mapped.
