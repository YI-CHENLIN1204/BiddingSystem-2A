# 2A Documentation Index

> Repository: `YI-CHENLIN1204/BiddingSystem-2A`  
> Active development branch: `feature/1d-api-integration`  
> Updated: 2026-10-15

This folder is the current documentation source for the 2A quantity/price estimation and anomaly-detection system.

## Read this first

For a new development conversation or a new developer, read in this order:

1. [NEXT_CHAT_HANDOFF.md](./NEXT_CHAT_HANDOFF.md)
2. [HANDOFF.md](./HANDOFF.md)
3. [2A_CURRENT_ARCHITECTURE.md](./2A_CURRENT_ARCHITECTURE.md)
4. [DECISIONS.md](./DECISIONS.md)
5. [API.md](./API.md)
6. [DATABASE.md](./DATABASE.md)
7. [DEVELOPMENT.md](./DEVELOPMENT.md)
8. [LOCAL_TEST_GUIDE.md](./LOCAL_TEST_GUIDE.md)
9. [VENDOR_QUESTIONS.md](./VENDOR_QUESTIONS.md)
10. [WORKLOG.md](./WORKLOG.md)

## What each document is for

| Document | Purpose |
|---|---|
| `NEXT_CHAT_HANDOFF.md` | Short copy-ready handoff for the next ChatGPT/development conversation |
| `HANDOFF.md` | Current implementation state and immediate next steps |
| `2A_CURRENT_ARCHITECTURE.md` | Formal Target/History architecture, model scope, and workflow |
| `DECISIONS.md` | Decisions that must not be silently reversed |
| `API.md` | Current backend endpoints and request/response responsibilities |
| `DATABASE.md` | Current tables, columns, relationships, and model-data responsibilities |
| `DEVELOPMENT.md` | Local build, Docker, Git, schema-change, and documentation rules |
| `LOCAL_TEST_GUIDE.md` | Hands-on local validation checklist |
| `VENDOR_QUESTIONS.md` | Open questions that still require vendor confirmation |
| `WORKLOG.md` | Chronological development record |

## Current formal scope

```text
Major-category estimation / price anomaly
└─ 結構體工程

Detail quantity anomaly detection
└─ 結構體工程
   ├─ 鋼筋
   └─ 混凝土
```

## Current data split

```text
Target / working data
AnalysisCase
└─ AnalysisBidVersion
   ├─ AnalysisBidItem
   └─ AnalysisRun

Verified history
ProjectMaster
└─ FinalBidItem
   └─ HistoricalTransaction
```

Critical rule:

```text
AnalysisCase != ProjectMaster
```

A target bid must never automatically become model truth.

## Current development phase

The architecture is defined enough for real-data validation.

Current priority:

1. run the latest `feature/1d-api-integration`
2. inspect a real workbook before import
3. import a truly finalized bid as Draft history
4. resolve review blockers
5. Publish History -> Approved
6. repeat until there are enough valid historical cases
7. validate structural-work P15/P50/P85
8. create a real target case and compare V1/V2

Do not insert fake history merely to make the model return a range.

## Branch visibility note

The `docs/` directory is currently maintained on:

```text
feature/1d-api-integration
```

If GitHub is showing the default `main` branch, this folder may not appear yet. Use the branch selector and switch to `feature/1d-api-integration`.

Do not merge to `main` unless explicitly requested.
