# 2A Worklog

## 2026-10-12 — Target / History architecture formalized

Completed:

- added versioned `AnalysisCase` target pipeline
- separated target tables from historical tables
- added one-time 1D snapshot import
- preserved standalone direct-upload path
- added target versions V1 / V2 / V3
- added persisted `AnalysisRun`
- added ability to reopen saved run results
- prevented target cases from writing historical truth

## 2026-10-12 — Scope narrowed

Current formal working scope:

- structural-work major-category estimation / price anomaly
- rebar quantity anomaly
- concrete quantity anomaly

Removed from current detection scope:

- formwork
- steel structure

Other engineering categories remain source data but are not forced into the current quantity model.

## 2026-10-12 — Database hard cleanup

Development database cleaned.

Removed obsolete / duplicate design including:

- old project fields no longer used
- redundant history source fields
- historical price from `historical_transaction`
- `quantity_rule_base`
- fake / old test history

Current history responsibilities:

- `final_bid_item`: source rows and structural-work price
- `historical_transaction`: standardized rebar / concrete quantity only

## 2026-10-12 — Site area integrated

Added `SiteArea` to:

- `ProjectMaster`
- `AnalysisCase`
- final-bid import metadata
- target-case metadata
- structural estimation form
- similarity scoring
- settings and audit display

Added configurable:

- `SiteAreaWeight`
- `SiteAreaDifferenceCutoffRatio`

## 2026-10-12 — Legacy generic estimation removed

Removed obsolete stack:

- generic material range estimation
- batch material estimation
- core-material estimation
- legacy quantity rule table
- old one-shot bid anomaly route
- old generic estimation UI

The formal system now uses:

- `MajorCategoryPriceRangeService`
- `DetectionCategoryRangeService`
- `AnalysisCaseService`

## 2026-10-12 — Historical review / publish workflow added

Finalized bid imports now enter:

```text
Draft
```

Added review support for:

- effective major-category override
- material mapping
- quantity / unit / unit price corrections
- ignore state
- live publish blockers

Added explicit:

- `Publish History`
- `Reopen history review`

Approved projects participate in model baselines.

Draft projects do not.

## 2026-10-12 — Approved history locking added

When history is `Approved`, lock:

- project similarity metadata
- bid row edits
- major-category override
- material mapping
- ignore / unignore

Corrections require reopening history review.

## 2026-10-12 — Current validation phase

The next phase is not more architecture work.

Next focus:

1. run system locally
2. import first real finalized bid
3. inspect review blockers
4. verify category / mapping / unit correction flow
5. publish first approved historical project
6. verify 2A project summary
7. accumulate enough real history for P15 / P50 / P85
8. create real target case and compare V1 / V2 analysis


## 2026-10-12 — Next-chat operation handoff added

Added:

- `docs/NEXT_CHAT_HANDOFF.md`
- explicit local-operation readiness gate
- exact pull / Docker rebuild commands
- first real finalized-bid validation sequence
- explicit instruction to tell the user when the local system is ready for actual operation

The next phase is hands-on validation with real finalized bid data, not architecture redesign.

## 2026-10-13 — Read-only pre-import quantity inspection

Added on the existing `feature/1d-api-integration` branch:

- `POST /api/tender-import/inspect` requires an explicit selected-worksheet list and does not write to the database.
- The shared `WorkbookSheetPicker` now lets users inspect all parsed candidate rows, including the original sheet/row, source categories, quantity, unit, price, amount and warnings before entering project metadata.
- Conservative steel/rebar and concrete candidate detection: supported kg/T and m³ units are converted only when unambiguous; steel couplers, rebar work/services and concrete placement/services are not counted as material quantities.
- Unknown units, missing quantities, uncertain structural major category and identical source-row fingerprints are surfaced for review.
- Candidate summaries are **not** model anomalies, approved history, or official quantities. No P15/P50/P85 values are fabricated.
- Regression checks added to `tests/WorkbookParserTests`.

Next: verify the uploaded real `.xls` case manually in the UI, check its 49 parsed candidate rows against Excel, then fix any concrete/rebar omission or misclassified source rows. Do not publish budget or supplier-quotation prices as approved historical truth.

## 2026-10-15 — Documentation / project-transfer checkpoint

Completed:

- expanded `README.md` into the current project entry point
- added `docs/API.md` with the current controller/API contract
- added `docs/DATABASE.md` with current table responsibilities and live development-schema state
- added `docs/DEVELOPMENT.md` with Docker, local build, parser-test and change-management rules
- refreshed `docs/HANDOFF.md`
- refreshed `docs/NEXT_CHAT_HANDOFF.md` with a copy-ready next-chat prompt
- aligned documentation around the current Draft -> review -> Publish History -> Approved historical workflow
- documented the read-only workbook preview / inspection safeguards
- documented the clean separation between structural-work price history and rebar/concrete quantity history

Current next phase remains:

1. pull and run the latest branch locally
2. verify workbook preview/inspection against a real source file
3. import only truly finalized historical bids
4. resolve Draft review blockers
5. publish Approved history
6. accumulate enough real history for P15/P50/P85
7. then validate real target V1/V2 analysis

No fake history should be inserted for demonstration.

