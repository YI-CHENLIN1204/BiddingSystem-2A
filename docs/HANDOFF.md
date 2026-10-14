# 2A Development Handoff

> Updated: 2026-10-12  
> Repository: `YI-CHENLIN1204/BiddingSystem-2A`  
> Active development branch: `feature/1d-api-integration`

## 1. Start here

Do **not** redesign the system from the old generic material-estimation architecture.

Read these files first:

1. `docs/NEXT_CHAT_HANDOFF.md`
2. `README.md`
3. `docs/2A_CURRENT_ARCHITECTURE.md`
4. `docs/DECISIONS.md`
5. `docs/LOCAL_TEST_GUIDE.md`
6. `docs/WORKLOG.md`

The current formal direction is:

- major-category estimation / price anomaly: **結構體工程**
- detail quantity anomaly detection: **鋼筋、混凝土**
- target data and historical data are strictly separated
- 1D is optional and only an additional target-data source
- finalized history must pass review and explicit publication before entering the model

## 2. Current data architecture

### Target pipeline

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

Target data must never directly become historical model truth.

### History pipeline

```text
Final bid
-> ProjectMaster (Draft)
-> FinalBidItem
-> review
-> Publish History
-> ProjectMaster (Approved)
   ├─ structural-work price baseline: FinalBidItem
   └─ rebar/concrete quantity baseline: HistoricalTransaction
```

Tables:

- `project_master`
- `final_bid_item`
- `historical_transaction`

## 3. Current review workflow

Imported final bids are not trusted automatically.

A project starts as:

```text
HistoryStatus = Draft
```

Before publication, reviewers can:

- correct the effective major category using `major_category_override`
- correct material mapping
- correct quantity / unit / unit price
- ignore irrelevant rows
- inspect publication blockers

After publication:

```text
HistoryStatus = Approved
```

Approved history is locked.

To correct it:

```text
Approved
-> Reopen history review
-> Draft
-> edit
-> publish again
```

Reopening disables the project's active quantity-history rows until re-approved.

## 4. Current similarity features

Current target / historical project metadata:

- project type
- structure type
- city
- total floor area
- site area
- ground-floor count
- basement-floor count
- project date / year

Current model also supports:

- similarity weighting
- time decay
- weighted P15 / P50 / P85
- insufficient-history fallback

Default minimum valid project count: **5**.

## 5. Current database state

The development database has been hard-cleaned.

No fake historical projects should be inserted only to make the model return a range.

Expected at the start of real validation:

- `project_master`: empty until real finalized bids are imported
- `historical_transaction`: empty until reviewed rebar/concrete history is published
- `analysis_case`: empty until a target case is created

Removed legacy database design:

- `quantity_rule_base`
- `historical_transaction.unit_price`
- old duplicate source fields
- old project columns no longer used by the current model

## 6. Current model scope

### Major-category price

Current supported major category:

- `結構體工程`

Price source:

- sum `FinalBidItem.Amount`
- fallback to `Qty * UnitPrice`

Normalize to:

- structural-work price per ping

### Quantity anomaly detection

Current supported detection categories:

- `鋼筋` -> kg
- `混凝土` -> 立方公尺

Quantity history only uses approved history.

## 7. 1D integration rule

2A must work without 1D.

Standalone / school-project mode:

```text
structured upload -> AnalysisCase -> versions -> analysis
```

Industry integration mode:

```text
1D -> one-time import -> 2A snapshot -> later analysis inside 2A
```

Do not make 1D a runtime dependency for every analysis.

## 8. Do not restore removed legacy paths

Do not reintroduce unless there is a new explicit decision:

- generic material estimation page
- `RangeEstimationService`
- `BatchEstimationService`
- `CoreMaterialService`
- `quantity_rule_base`
- legacy `/api/bid-anomaly`
- old one-shot anomaly page
- POC granularity tradeoff demo

## 9. Immediate next work

The next development phase is **real-data validation**, not another architecture redesign.

Recommended order:

1. run the latest branch locally
2. import one real finalized historical bid
3. review effective major categories
4. review material mappings
5. fix quantity / unit / price issues
6. confirm publish blockers are correct
7. publish history
8. verify project summary
9. repeat with additional finalized bids
10. once enough history exists, create a target `AnalysisCase`
11. run structural price and rebar/concrete quantity checks
12. upload V2 and compare saved runs

## 10. Current vendor questions

Still awaiting confirmation:

1. Must industry 2A preserve direct upload in addition to 1D?
2. What exact direct-upload schema is required?
3. Is current hierarchy accepted: structural work -> rebar/concrete?
4. Is one-time 1D snapshot import the correct operational workflow?
5. Which categories are meaningful for historical quantity checks, and can excluded quantity categories still use major-category price analysis?

Until confirmed, keep the current provisional implementation.
