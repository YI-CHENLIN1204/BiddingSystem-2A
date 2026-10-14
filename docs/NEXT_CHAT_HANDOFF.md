# 2A Next Chat Handoff

> Updated: 2026-10-12  
> Repository: `YI-CHENLIN1204/BiddingSystem-2A`  
> Active branch: `feature/1d-api-integration`

## What the next chat should do first

Do not redesign the system and do not restore removed legacy estimation code.

Read, in this order:

1. `docs/HANDOFF.md`
2. `docs/2A_CURRENT_ARCHITECTURE.md`
3. `docs/DECISIONS.md`
4. `docs/LOCAL_TEST_GUIDE.md`
5. `docs/VENDOR_QUESTIONS.md`
6. `docs/WORKLOG.md`

Then help the user run the current branch locally and move into real-data validation.

## Current development state

The formal current scope is:

- major-category estimation / price anomaly: **結構體工程**
- detail quantity anomaly detection: **鋼筋、混凝土**
- target working data: `AnalysisCase / AnalysisBidVersion / AnalysisRun`
- finalized historical truth: `ProjectMaster / FinalBidItem / HistoricalTransaction`
- 1D is optional; 2A must remain fully usable without 1D
- finalized historical imports start as `Draft`, require review, then explicit `Publish History`
- approved history is locked until explicitly reopened
- `HistoricalTransaction` is quantity-only
- structural price history remains in `FinalBidItem`
- `SiteArea` is part of target/history metadata and similarity scoring
- insufficient history must remain insufficient; do not create fake history

## The user can begin actual system operation when

The local machine has completed all of the following:

```powershell
git status
git fetch origin
git switch feature/1d-api-integration
git pull origin feature/1d-api-integration

docker compose down
docker compose up -d --build
docker compose ps
```

The user is ready to operate the system when:

1. `git status` says the branch is up to date and working tree is clean.
2. `docker compose ps` shows both frontend and backend containers running.
3. `http://localhost:8081` opens successfully.
4. The following pages load without API errors:
   - 系統總覽
   - 待偵錯案件
   - 建案管理
   - 決標回寫 / 最終標單匯入
   - 結構體工程估算
   - 參數管理
   - 操作紀錄

At that point, tell the user explicitly:

> **現在可以開始實際操作系統。**

## First real operation sequence

Do not start by fabricating history.

Use this sequence:

1. Import **one real finalized historical bid**.
2. Confirm the created project enters `Draft`.
3. Review:
   - effective major category
   - material mapping
   - quantity
   - unit
   - unit price / amount
   - ignored rows
   - publication blockers
4. Correct issues.
5. Use **Publish History**.
6. Confirm the project becomes `Approved`.
7. Confirm:
   - structural-work price baseline appears
   - rebar quantity baseline appears when eligible
   - concrete quantity baseline appears when eligible
   - unrelated rows do not enter rebar/concrete history
8. Open the project detail and verify the **2A 歷史基準摘要**.
9. Repeat with additional finalized bids.
10. Once enough approved history exists, create a real target case and test V1/V2 analysis.

## Expected behavior with the clean database

The database intentionally starts without fake historical projects.

Therefore, before at least `MinimumSampleCount` valid approved projects exist:

- structural price estimation may show insufficient data
- rebar quantity anomaly detection may show insufficient data
- concrete quantity anomaly detection may show insufficient data

This is expected behavior, not a bug.

## Do not restore

Do not reintroduce unless a new explicit decision is made:

- generic material estimation
- `RangeEstimationService`
- `BatchEstimationService`
- `CoreMaterialService`
- `quantity_rule_base`
- legacy `/api/bid-anomaly`
- old one-shot anomaly page
- POC granularity tradeoff demo

## Vendor-confirmation items still pending

Keep the provisional implementation until confirmed:

1. whether industry 2A must preserve direct upload in addition to 1D
2. exact direct-upload schema
3. whether `結構體工程 -> 鋼筋 / 混凝土` is the accepted major/detail hierarchy
4. whether 1D should be a one-time snapshot source
5. which categories should participate in quantity estimation and whether excluded quantity categories may still use major-category price analysis

## Branch rule

Continue development on:

`feature/1d-api-integration`

Do not merge `main` unless the user explicitly asks.

## 2026-10-13 pre-import inspection continuation

Read-only quantity preview is now part of the same workbook sheet picker for both target uploads and finalized history import. The API is `POST /api/tender-import/inspect` and requires selected worksheet names. It returns all parsed candidate rows with original line/source and conservative rebar/concrete candidate quantities and warnings; it does not persist or publish anything.

The next hands-on test should open `http://localhost:8081/final-bid`, choose `最終標單.xls`, select only `建築工程-預算單`, and click **檢查選取工作表的全部候選明細**. Inspect steel and concrete filters against source lines before creating any Draft. In the historical budget/quotation workbook, financial finality and project metadata remain unconfirmed, so it must not be published as Approved.

Never interpret candidate quantity totals as P15/P50/P85 anomaly results or insert fake history to make the model ready.
