# BiddingSystem-2A

崴正營造 × 雲科大資管產學合作之 **2A 量價估算與異常偵測系統**。

目前正式開發方向已收斂為：

- **大項估算／價格異常：結構體工程**
- **細項數量偵錯：鋼筋、混凝土**
- 待偵錯案件與歷史最終標單完全分流
- 1D 為選配資料來源，2A 必須可獨立運作
- 歷史最終標單必須經人工審核與 Publish History 才能成為模型基準
- 相似建案使用建案條件 + 時間權重建立 P15 / P50 / P85 合理範圍

> Active development branch: `feature/1d-api-integration`

---

## 1. Current system concept

```text
                         2A
                          │
            ┌─────────────┴─────────────┐
            │                           │
        Target Pipeline            History Pipeline
        待偵錯案件                  已定案最終標單
            │                           │
    ┌───────┴───────┐             Draft review
    │               │                  │
自行上傳        選配 1D 匯入          Publish History
    │               │                  │
    └───────┬───────┘               Approved
            │                           │
       AnalysisCase                     │
            │                           │
       V1 / V2 / V3                     │
            │                           │
       AnalysisRun                      │
            │                           │
            └─────────────┬─────────────┘
                          ↓
                   2A estimation
                   / anomaly detection
```

Important:

```text
AnalysisCase != ProjectMaster
```

A working target bid must never automatically become historical model truth.

---

## 2. Current analysis scope

### Major-category estimation

```text
結構體工程
-> historical price per ping
-> similar-project weighting
-> time weighting
-> weighted P15 / P50 / P85
```

### Detail quantity anomaly detection

```text
結構體工程
├─ 鋼筋      -> kg / m²
└─ 混凝土    -> m³ / m²
```

The system intentionally does not estimate every engineering category.

---

## 3. Technology stack

### Frontend

- Vue 3
- TypeScript
- Vite
- Nginx in Docker production container

### Backend

- ASP.NET Core Web API
- .NET 10
- Entity Framework Core
- Npgsql

### Database

- PostgreSQL
- current development database hosted on Supabase

### Deployment / local integration

- Docker Compose
- frontend and backend are fully separated
- frontend calls backend only through HTTP API

---

## 4. Repository structure

```text
BiddingSystem-2A/
├─ frontend/                 Vue 3 frontend
├─ 崴正/                     ASP.NET Core backend
├─ database/                 SQL migrations / schema changes
├─ docs/                     architecture and handoff documentation
├─ tests/                    parser regression tests
├─ compose.yaml
├─ .env.example
└─ README.md
```

---

## 5. Documentation index

New development chats and new team members should read the documentation instead of reconstructing the project from old conversations.

| Document | Purpose |
|---|---|
| `docs/README.md` | Documentation index and recommended reading order |
| `docs/NEXT_CHAT_HANDOFF.md` | Short handoff for the next ChatGPT/development conversation |
| `docs/HANDOFF.md` | Current development state and immediate next work |
| `docs/2A_CURRENT_ARCHITECTURE.md` | Formal system architecture and data flow |
| `docs/DECISIONS.md` | Architecture decisions that should not be silently reversed |
| `docs/API.md` | Current backend API reference |
| `docs/DATABASE.md` | Database tables, responsibilities, relationships and eligibility rules |
| `docs/DEVELOPMENT.md` | Local development, Docker, build, testing and change rules |
| `docs/LOCAL_TEST_GUIDE.md` | Hands-on validation procedure |
| `docs/VENDOR_QUESTIONS.md` | Questions still awaiting vendor confirmation |
| `docs/WORKLOG.md` | Development history |

Recommended reading order:

1. `docs/README.md`
2. `docs/NEXT_CHAT_HANDOFF.md`
3. this README
4. `docs/2A_CURRENT_ARCHITECTURE.md`
5. `docs/DECISIONS.md`
6. `docs/API.md`
7. `docs/DATABASE.md`
8. `docs/DEVELOPMENT.md`
9. `docs/LOCAL_TEST_GUIDE.md`
10. `docs/WORKLOG.md`

---

## 6. Quick start — Docker

Create `.env` from `.env.example`.

Required:

```text
DB_CONNECTION_STRING
```

Optional:

```text
ONE_D_API_BASE_URL
```

Update the local branch:

```powershell
git status
git fetch origin
git switch feature/1d-api-integration
git pull origin feature/1d-api-integration
```

Build and start:

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

---

## 7. Local frontend/backend development

Backend:

```powershell
$env:ConnectionStrings__DefaultConnection = "<PostgreSQL connection string>"
dotnet run --project .\崴正\崴正.csproj
```

Default backend development URL:

```text
http://127.0.0.1:5261
```

Frontend:

```powershell
cd .\frontend
npm install
npm run dev
```

Default frontend development URL:

```text
http://127.0.0.1:5174
```

Vite proxies `/api` to the backend.

---

## 8. Environment behavior

### Standalone / school-project mode

Leave:

```text
ONE_D_API_BASE_URL=
```

2A remains fully usable through direct upload.

### Industry integration mode

Configure:

```text
ONE_D_API_BASE_URL=<1D API base URL>
```

Then the UI additionally exposes 1D import.

1D is used only to create a local 2A snapshot; later analysis runs operate on the local `AnalysisBidVersion`.

---

## 9. Historical data workflow

A finalized-bid import does not immediately become model truth.

```text
Excel/CSV
-> worksheet preview
-> explicit sheet selection
-> read-only inspection
-> import
-> ProjectMaster Draft
-> row/category/material/unit/price review
-> publish blockers resolved
-> Publish History
-> Approved
```

Only Approved history participates in estimation.

To edit an Approved baseline:

```text
Reopen History Review
-> Draft
-> edit
-> publish again
```

---

## 10. Target-case workflow

```text
Create AnalysisCase
-> V1
-> run quantity / price analysis
-> upload revised bid
-> V2
-> analyze again
-> keep V1 and previous AnalysisRun results
```

Old target versions are immutable snapshots.

---

## 11. Read-only workbook inspection

Before importing a workbook, use the shared preview/inspection flow.

Backend:

```text
POST /api/tender-import/preview
POST /api/tender-import/inspect
```

The inspection flow:

- does not create a project
- does not create an AnalysisCase
- does not write historical transactions
- preserves source worksheet and row number
- does not guess arbitrary numeric columns
- surfaces candidate rebar/concrete quantities and warnings

---

## 12. Model readiness

Default minimum valid history:

```text
5 approved projects
```

Readiness is independent for:

- 結構體工程價格
- 鋼筋數量
- 混凝土數量

If history is insufficient, the correct result is:

```text
Insufficient data
```

Do not seed fake history just to make P15/P50/P85 appear.

---

## 13. Build verification

Backend:

```powershell
dotnet restore .\崴正\崴正.csproj
dotnet build .\崴正\崴正.csproj -c Release
```

Frontend:

```powershell
cd .\frontend
npm ci
npm run build
```

Parser regression:

```powershell
dotnet run --project .\tests\WorkbookParserTests\WorkbookParserTests.csproj -c Release
```

---

## 14. Security / configuration rule

Do not commit:

- PostgreSQL passwords
- API keys
- `.env`
- production secrets

Database connection must be supplied through:

```text
ConnectionStrings__DefaultConnection
```

Docker Compose maps:

```text
DB_CONNECTION_STRING
-> ConnectionStrings__DefaultConnection
```

---

## 15. Removed legacy architecture

Do not restore without an explicit new decision:

- generic material estimation
- `RangeEstimationService`
- `BatchEstimationService`
- `CoreMaterialService`
- `quantity_rule_base`
- old one-shot `/api/bid-anomaly`
- old one-shot anomaly UI
- POC granularity tradeoff demo

The formal target-analysis entry is:

```text
AnalysisCase -> AnalysisBidVersion -> AnalysisRun
```

---

## 16. Current development phase

The architecture is sufficiently defined for hands-on validation.

The immediate goal is:

1. run the latest branch locally
2. inspect a real workbook against its source rows
3. import the first truly finalized historical bid
4. resolve Draft review blockers
5. publish the first Approved history
6. repeat with additional real cases
7. when enough history exists, validate P15/P50/P85
8. create a real target case
9. compare V1/V2 results

Do not redesign the system before this real-data validation reveals an actual problem.

---

## 17. Branch policy

Current development:

```text
feature/1d-api-integration
```

Do not merge to `main` unless explicitly requested.


---

## 18. GitHub branch visibility

The documentation and current formal implementation are maintained on:

```text
feature/1d-api-integration
```

If GitHub is currently showing `main`, the `docs/` directory and the latest 2A changes may not be visible yet. Use the branch selector to switch to `feature/1d-api-integration`.

Do not merge to `main` unless explicitly requested.
