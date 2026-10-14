# 2A Development Handoff

> Updated: 2026-10-15  
> Repository: `YI-CHENLIN1204/BiddingSystem-2A`  
> Active development branch: `feature/1d-api-integration`

## 1. Read first

Do not reconstruct the project from old conversations or restore removed generic-estimation code.

Read in this order:

1. `docs/NEXT_CHAT_HANDOFF.md`
2. `README.md`
3. `docs/2A_CURRENT_ARCHITECTURE.md`
4. `docs/DECISIONS.md`
5. `docs/API.md`
6. `docs/DATABASE.md`
7. `docs/DEVELOPMENT.md`
8. `docs/LOCAL_TEST_GUIDE.md`
9. `docs/VENDOR_QUESTIONS.md`
10. `docs/WORKLOG.md`

## 2. Formal current scope

Current model scope:

- major-category estimation / price anomaly: **結構體工程**
- detail quantity anomaly: **鋼筋、混凝土**

Current hierarchy:

```text
結構體工程
├─ 鋼筋      -> quantity anomaly
└─ 混凝土    -> quantity anomaly
```

Do not automatically broaden to all engineering categories.

## 3. Formal data split

### Target / working data

```text
Upload ───────────────┐
                     ├─ AnalysisCase
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

### Verified historical data

```text
Finalized-bid candidate
-> ProjectMaster Draft
-> FinalBidItem
-> review
-> Publish History
-> ProjectMaster Approved
   ├─ structural price baseline -> FinalBidItem
   └─ rebar/concrete quantity baseline -> HistoricalTransaction
```

Tables:

- `project_master`
- `final_bid_item`
- `historical_transaction`

Critical rule:

```text
AnalysisCase != ProjectMaster
```

Target data must never automatically become historical model truth.

## 4. 1D rule

2A must work with or without 1D.

Standalone / school project:

```text
structured upload
-> AnalysisCase snapshot
-> V1/V2/V3
-> analysis
```

Industry integration:

```text
1D
-> one-time import
-> local 2A snapshot
-> all later analysis inside 2A
```

1D is not a runtime dependency for every analysis run.

## 5. Historical review rule

Import is not approval.

```text
Import
-> Draft
-> review
-> resolve blockers
-> Publish History
-> Approved
```

Review can correct:

- effective engineering major through `major_category_override`
- material mapping
- quantity
- unit
- unit price
- ignore state

Source `Category1/Category2/Category3` must remain preserved.

Approved history is locked.

To fix it:

```text
Approved
-> Reopen History Review
-> Draft
-> correct
-> publish again
```

## 6. Workbook safeguards

A major current focus is preventing bad Excel parsing from contaminating model history.

Use:

```text
POST /api/tender-import/preview
POST /api/tender-import/inspect
```

Current rules:

- multi-sheet workbooks require explicit worksheet selection
- inspection is read-only
- selected source sheet and row are preserved
- quantity/price must come from positively identified columns
- do not guess from arbitrary trailing numbers
- summary / supplier-comparison / explanatory sheets must not silently become detail data
- candidate steel/concrete totals are only inspection aids
- worksheet names such as 預算單 or 自估 do not prove commercial finality

## 7. Current similarity model

Target / historical project features:

- project type
- structure type
- city
- total floor area
- site area
- ground-floor count
- basement-floor count
- project date/year

Model behavior:

- weighted similar-project scoring
- similarity exponent
- time decay
- weighted P15 / P50 / P85
- minimum-sample fallback

Default minimum valid history:

```text
5 approved projects
```

Insufficient history must remain insufficient.

## 8. Current price / quantity responsibility

### Structural-work price

Source:

`final_bid_item`

Per project:

```text
Amount
or Qty * UnitPrice
-> structural-work total
-> floor area in ping
-> price / ping
```

### Rebar / concrete quantity

Source:

`historical_transaction`

Current standard units:

- rebar -> kg
- concrete -> 立方公尺

`historical_transaction` is quantity-only and intentionally has no unit-price field.

## 9. Current database state

Development database is intentionally clean.

As of 2026-10-15:

```text
project_master             0
final_bid_item             0
historical_transaction     0

analysis_case              0
analysis_bid_version       0
analysis_bid_item          0
analysis_run               0
```

Do not insert fake cases merely to make the model produce ranges.

## 10. Removed architecture — do not restore

Unless a new explicit decision is made, do not reintroduce:

- generic material estimation
- `RangeEstimationService`
- `BatchEstimationService`
- `CoreMaterialService`
- `quantity_rule_base`
- legacy `/api/bid-anomaly`
- old one-shot anomaly page
- POC granularity tradeoff demo

## 11. Immediate next phase

The system is now in **real-data validation**, not architecture redesign.

Next sequence:

1. pull the latest `feature/1d-api-integration`
2. rebuild Docker
3. open `http://localhost:8081`
4. use workbook preview
5. explicitly select authentic detail worksheet(s)
6. run read-only inspection
7. compare parser output with source Excel
8. only import a historical project if the file is truly finalized history
9. review Draft blockers
10. publish first Approved history
11. verify project 2A summary
12. repeat with additional real historical bids
13. only after enough approved history exists, validate P15/P50/P85
14. create a target AnalysisCase and compare V1/V2 runs

## 12. Vendor questions still pending

Continue using provisional assumptions until confirmed:

1. whether industry 2A must preserve direct upload in addition to 1D
2. exact direct-upload schema
3. whether `結構體工程 -> 鋼筋 / 混凝土` is the accepted hierarchy
4. whether 1D should be a one-time snapshot source
5. which categories are valid for quantity history and whether excluded quantity categories can still use major-category price analysis

See:

`docs/VENDOR_QUESTIONS.md`

## 13. Branch rule

Continue on:

```text
feature/1d-api-integration
```

Do not merge `main` unless explicitly requested.
