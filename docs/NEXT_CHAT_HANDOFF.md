# 2A Next Chat Handoff

> Updated: 2026-10-15  
> Repository: `YI-CHENLIN1204/BiddingSystem-2A`  
> Active branch: `feature/1d-api-integration`

## Instruction to the next chat

Directly continue the existing project.

Do **not** redesign the system from scratch.
Do **not** restore removed generic material-estimation code.
Do **not** merge `main` unless the user explicitly requests it.

First read:

1. `README.md`
2. `docs/HANDOFF.md`
3. `docs/2A_CURRENT_ARCHITECTURE.md`
4. `docs/DECISIONS.md`
5. `docs/API.md`
6. `docs/DATABASE.md`
7. `docs/DEVELOPMENT.md`
8. `docs/LOCAL_TEST_GUIDE.md`
9. `docs/VENDOR_QUESTIONS.md`
10. `docs/WORKLOG.md`

Then continue from the current real-data validation phase.

---

## Current formal scope

```text
Major-category estimation / price anomaly:
結構體工程

Detail quantity anomaly:
鋼筋
混凝土
```

Current hierarchy:

```text
結構體工程
├─ 鋼筋
└─ 混凝土
```

Do not automatically re-add template/formwork, steel structure, doors/windows, landscape, or all-category estimation.

---

## Critical architecture

### Target / working pipeline

```text
Direct upload ─────────┐
                      ├─ AnalysisCase
Optional 1D import ───┘      │
                             ├─ V1
                             ├─ V2
                             └─ V3
                                  │
                                  └─ AnalysisRun
```

Target tables:

- `analysis_case`
- `analysis_bid_version`
- `analysis_bid_item`
- `analysis_run`

### Historical pipeline

```text
Finalized bid candidate
-> ProjectMaster (Draft)
-> FinalBidItem
-> review
-> Publish History
-> ProjectMaster (Approved)
   ├─ structural price baseline from FinalBidItem
   └─ rebar/concrete quantity baseline in HistoricalTransaction
```

Historical tables:

- `project_master`
- `final_bid_item`
- `historical_transaction`

Never treat an AnalysisCase as historical truth.

---

## 1D rule

1D is optional.

School-project / standalone environment:

```text
direct structured upload
-> 2A snapshot
-> analysis
```

Industry environment:

```text
1D
-> one-time import
-> local 2A snapshot
-> later analysis stays inside 2A
```

Do not make 1D a runtime dependency for every check.

---

## Historical review rule

Import does not equal approval.

```text
Import
-> Draft
-> review source rows / major category / material / quantity / unit / price
-> resolve blockers
-> Publish History
-> Approved
```

Approved data is locked.

Corrections require:

```text
Reopen History Review
-> Draft
-> correct
-> publish again
```

Source `Category1 / Category2 / Category3` must remain traceable.
Human major-category correction uses `major_category_override`.

---

## Workbook safeguard already implemented

Both historical import and target upload have a shared worksheet-preview / inspection concept.

Read-only APIs:

```text
POST /api/tender-import/preview
POST /api/tender-import/inspect
```

Rules:

- explicitly choose included worksheet(s)
- preserve original sheet and row
- do not silently import all sheets
- do not guess numeric columns from row endings
- do not treat summary / quotation / supplier-comparison rows as engineering details
- candidate rebar/concrete totals are inspection aids only
- inspection must not write model history

A sheet title containing `預算單` or `自估` does not prove that the prices are finalized.

---

## Similar-project features

Current formal inputs:

- project type
- structure type
- city
- total floor area
- site area
- ground-floor count
- basement-floor count
- project year

Model uses:

- similarity weights
- similarity exponent
- time decay
- weighted P15/P50/P85
- insufficient-history fallback

Default minimum sample count:

```text
5 Approved projects
```

Never create fake history to satisfy the threshold.

---

## Price and quantity responsibility

Structural-work historical price:

```text
FinalBidItem.Amount
or Qty * UnitPrice
-> project structural-work total
-> price / ping
```

Rebar / concrete historical quantity:

```text
HistoricalTransaction
```

Standard units:

- rebar = kg
- concrete = 立方公尺

`historical_transaction` intentionally does not store price.

---

## Development database state

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

This clean state is intentional.

---

## Local startup

From repository root:

```powershell
git status
git fetch origin
git switch feature/1d-api-integration
git pull origin feature/1d-api-integration

docker compose down
docker compose up -d --build
docker compose ps
```

Open:

```text
http://localhost:8081
```

If it fails:

```powershell
docker compose logs --tail=150
```

The system is ready for actual operation when both containers are running and the UI loads without API errors.

---

## What to do next

The next task is **real-data validation**.

Do this, in order:

1. run the latest system locally
2. open the finalized-bid import page
3. upload a real workbook
4. preview worksheets
5. select only authentic detail worksheet(s)
6. run read-only inspection
7. compare parsed quantities / prices / rows with the Excel source
8. do not create Approved history until commercial finality and project metadata are confirmed
9. if it is a truly finalized bid, create Draft history
10. resolve review blockers
11. Publish History
12. verify the 2A project summary
13. repeat with additional real historical projects
14. when enough Approved history exists, test structural-work P15/P50/P85
15. create a real target AnalysisCase
16. compare V1/V2 quantity and price runs

The next chat should prioritize testing the real system, not inventing another architecture.

---

## Removed code that must not return

Do not restore:

- `RangeEstimationService`
- `BatchEstimationService`
- `CoreMaterialService`
- `quantity_rule_base`
- legacy `/api/bid-anomaly`
- old generic material estimation page
- old one-shot anomaly page
- POC granularity tradeoff demo

---

## Vendor questions still pending

Keep current provisional implementation until confirmed:

1. direct upload vs mandatory 1D in the industry version
2. exact direct-upload schema
3. accepted major/detail hierarchy
4. whether one-time 1D snapshot is correct
5. which engineering categories should participate in quantity / price analysis

See:

`docs/VENDOR_QUESTIONS.md`

---

## Short prompt for the next conversation

Paste this after opening a new chat:

```text
請直接接手 GitHub 專案 YI-CHENLIN1204/BiddingSystem-2A。

目前正式開發 branch 是 feature/1d-api-integration，請不要重新設計整套系統，也不要恢復已移除的 generic material estimation。

請先閱讀：
README.md
docs/NEXT_CHAT_HANDOFF.md
docs/HANDOFF.md
docs/2A_CURRENT_ARCHITECTURE.md
docs/DECISIONS.md
docs/API.md
docs/DATABASE.md
docs/DEVELOPMENT.md
docs/LOCAL_TEST_GUIDE.md
docs/VENDOR_QUESTIONS.md
docs/WORKLOG.md

目前核心：
- 結構體工程 = 大項估算/價格異常
- 鋼筋、混凝土 = 細項數量偵錯
- AnalysisCase/Version/Run 是待偵錯工作資料
- ProjectMaster/FinalBidItem/HistoricalTransaction 是已定案歷史資料
- 1D 是選配，沒有 1D 時 2A 必須能獨立上傳使用
- 歷史標單匯入後先 Draft，人工審核後才能 Publish History -> Approved
- Approved 歷史要修改必須 Reopen History Review
- HistoricalTransaction 只存鋼筋/混凝土數量，不存價格
- 相似建案包含 SiteArea
- 樣本不足就顯示不足，不准塞假資料

現在不要再做架構重整，直接進入本機啟動與真實標單資料驗證。
```
