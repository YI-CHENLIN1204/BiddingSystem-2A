# 2A Local Test Guide

> Branch: `feature/1d-api-integration`

## 1. Update local code

From the repository root:

```powershell
git status
git fetch origin
git switch feature/1d-api-integration
git pull origin feature/1d-api-integration
```

Expected status:

```text
On branch feature/1d-api-integration
Your branch is up to date with 'origin/feature/1d-api-integration'.

nothing to commit, working tree clean
```

## 2. Rebuild Docker

```powershell
docker compose down
docker compose up -d --build
docker compose ps
```

Open:

```text
http://localhost:8081
```

If startup fails:

```powershell
docker compose logs --tail=150
```

## 3. Smoke-test pages

Check that these pages load:

- 系統總覽
- 待偵錯案件
- 建案管理
- 決標回寫 / final bid import
- 結構體工程估算
- 參數管理
- 操作紀錄

Also confirm:

- no old generic material-estimation page remains
- no old one-shot anomaly page remains
- standalone mode works even if 1D is not configured

## 4. First real historical-bid test

Use a real finalized bid.

Expected workflow:

```text
Upload finalized bid
-> ProjectMaster created as Draft
-> FinalBidItem rows created
-> review
-> resolve blockers
-> Publish History
-> Approved
```

### Review checks

For each relevant row inspect:

- effective major category
- original Category1 / Category2 / Category3
- optional major-category override
- material mapping
- quantity
- unit
- unit price / amount
- ignore state
- current 2A model impact

Expected model-impact labels:

- structural price baseline
- rebar / concrete quantity baseline
- not in current 2A baseline

## 5. Publish-history checks

Publication should be blocked when unresolved issues remain.

Important blockers include:

- unrecognized engineering major on a non-ignored row
- structural row with unusable price data
- mapped rebar / concrete row with invalid quantity / unit

After all blockers are resolved:

```text
Publish History
-> HistoryStatus = Approved
```

Verify:

- project is now counted by readiness
- approved history is locked
- historical quantity rows exist only for rebar / concrete
- structural price remains derived from `FinalBidItem`

## 6. Reopen-history test

For an approved project:

```text
Reopen history review
```

Expected:

- status becomes Draft
- project is temporarily excluded from active model baseline
- historical quantity rows are disabled / excluded
- editing becomes available again
- after correction, publish is required again

## 7. Target-case test

Create a target case from:

- direct structured upload, or
- 1D when configured

Expected:

```text
AnalysisCase
└─ V1
```

Run:

- rebar / concrete quantity analysis
- structural-work price analysis

If history is insufficient, the correct result is:

```text
Insufficient data
```

Do not treat that as a system failure.

## 8. Version test

Upload a revised target bid:

```text
V1 -> V2
```

Verify:

- V1 remains unchanged
- V2 becomes current
- each version can have its own analysis runs
- old runs can be reopened from history

## 9. 1D test

If `OneD:BaseUrl` is configured:

- 1D import option should be enabled
- import reads 1D once
- result is stored as local 2A snapshot
- later analysis must not call 1D again

If 1D is not configured:

- direct upload remains fully usable
- 1D option should be unavailable without breaking the page

## 10. Current expected limitation

The cleaned development database may have fewer than 5 approved historical projects.

Therefore:

- structural price readiness may remain below 5
- rebar readiness may remain below 5
- concrete readiness may remain below 5

Until enough valid history exists, P15 / P50 / P85 should not be forced.
