# 2A Development Guide

> Updated: 2026-10-15  
> Repository: `YI-CHENLIN1204/BiddingSystem-2A`  
> Active development branch: `feature/1d-api-integration`

This guide explains how to continue development without reintroducing removed architecture.

## 1. Branch rule

Formal current development continues on:

```text
feature/1d-api-integration
```

Do not merge `main` unless the user explicitly asks.

The older POC branch:

```text
feature/1d-detection-integration
```

is not the formal production direction and should remain separate.

---

# 2. Required documents before changing architecture

Read in this order:

1. `docs/NEXT_CHAT_HANDOFF.md`
2. `README.md`
3. `docs/2A_CURRENT_ARCHITECTURE.md`
4. `docs/DECISIONS.md`
5. `docs/API.md`
6. `docs/DATABASE.md`
7. `docs/LOCAL_TEST_GUIDE.md`
8. `docs/VENDOR_QUESTIONS.md`
9. `docs/WORKLOG.md`

Do not infer old behavior from deleted services or earlier branches.

---

# 3. Local Docker workflow

From repository root:

```powershell
git status
git fetch origin
git switch feature/1d-api-integration
git pull origin feature/1d-api-integration
```

Then rebuild the containers:

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

Backend-only logs:

```powershell
docker compose logs backend --tail=150
```

Frontend-only logs:

```powershell
docker compose logs frontend --tail=150
```

---

# 4. Environment variables

Create:

```text
.env
```

from:

```text
.env.example
```

Required:

```text
DB_CONNECTION_STRING
```

Optional:

```text
ONE_D_API_BASE_URL
```

If `ONE_D_API_BASE_URL` is blank:

- 2A still works
- direct upload remains available
- 1D buttons should be unavailable/disabled
- no target analysis should depend on 1D

Never commit passwords, connection strings, API keys, or local `.env` content.

---

# 5. Non-Docker local development

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

Default frontend URL:

```text
http://127.0.0.1:5174
```

Vite proxies `/api` to the backend.

---

# 6. Build verification

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

Do not consider a change complete if one side fails to build.

---

# 7. Parser regression test

The repository contains workbook-parser regression checks.

Run:

```powershell
dotnet run --project .\tests\WorkbookParserTests\WorkbookParserTests.csproj -c Release
```

Use this after changes to:

- worksheet selection
- quantity/price column recognition
- Excel/CSV parsing
- rebar/concrete candidate inspection
- source-row handling

---

# 8. Current formal architecture

Do not redesign away from:

```text
Target
AnalysisCase
  -> AnalysisBidVersion
      -> AnalysisBidItem
      -> AnalysisRun

History
ProjectMaster
  -> FinalBidItem
      -> HistoricalTransaction
```

Target and historical data are separate by design.

---

# 9. Current analysis scope

Do not broaden automatically.

Current major-category price scope:

```text
結構體工程
```

Current quantity-detection scope:

```text
鋼筋
混凝土
```

Do not re-add:

- generic material estimation
- template/formwork estimation
- steel-structure estimation
- doors/windows quantity estimation
- arbitrary all-category estimation

unless a new requirement explicitly changes the scope.

---

# 10. Finalized-history development rule

Import does not mean approval.

Formal workflow:

```text
Upload finalized-bid candidate
-> Draft
-> review
-> fix blockers
-> Publish History
-> Approved
```

Approved model input is immutable until:

```text
Reopen History Review
```

Never bypass publication review just to create model samples.

---

# 11. Target-version development rule

A changed target bid creates a new version.

Correct:

```text
AnalysisCase
├─ V1
├─ V2
└─ V3
```

Incorrect:

```text
V1 overwritten repeatedly
```

Saved runs must remain tied to the version that produced them.

---

# 12. Workbook-import rule

Both target upload and historical-final-bid import must use explicit worksheet selection when needed.

Required safeguards:

- preview workbook sheets
- user explicitly selects included worksheets
- preserve sheet name and row number
- do not infer quantity/price from arbitrary trailing numbers
- do not treat summaries/explanations as engineering rows
- do not automatically trust a worksheet because its name contains 預算單 or 自估
- inspection is read-only

Useful endpoints:

```text
POST /api/tender-import/preview
POST /api/tender-import/inspect
```

---

# 13. Database-change rule

Current project phase allows hard cleanup of development data/schema when a design is explicitly obsolete.

When changing schema:

1. change the C# model
2. change EF mapping
3. add SQL under `database/`
4. apply migration to development DB
5. verify actual schema
6. remove obsolete code
7. update `docs/DATABASE.md`
8. update `docs/DECISIONS.md` if responsibility/architecture changed

Do not leave dead columns or compatibility fields without a concrete reason.

---

# 14. Documentation-change rule

After meaningful development, update at least one of:

- `docs/WORKLOG.md`
- `docs/HANDOFF.md`
- `docs/NEXT_CHAT_HANDOFF.md`

If API changes:

- update `docs/API.md`

If schema/data responsibility changes:

- update `docs/DATABASE.md`

If architecture changes:

- update `docs/2A_CURRENT_ARCHITECTURE.md`
- update `docs/DECISIONS.md`

README should remain the entry point, not the full internal design document.

---

# 15. Git workflow

Before editing:

```powershell
git status
git pull origin feature/1d-api-integration
```

After code changes, verify build/tests before committing.

Suggested commit style:

```text
feat: ...
fix: ...
refactor: ...
db: ...
docs: ...
test: ...
```

Do not create a new branch for every small change unless explicitly requested.

---

# 16. Current real-data validation sequence

The immediate next phase is system operation, not architecture expansion.

Recommended sequence:

1. update local branch
2. rebuild Docker
3. open `http://localhost:8081`
4. preview a real workbook
5. explicitly select authentic detail sheets
6. run read-only inspection
7. verify parsed rows against Excel
8. only then create a Draft historical project if it is truly finalized history
9. review category/material/unit/price blockers
10. publish only after verification
11. repeat with additional real history
12. once enough history exists, test a target AnalysisCase
13. compare V1/V2 runs

---

# 17. Definition of “ready to operate”

The local system is ready for hands-on testing when:

- branch is up to date
- working tree is clean
- backend container is Up
- frontend container is Up
- `http://localhost:8081` opens
- system overview loads
- target case page loads
- project/history page loads
- final-bid import page loads
- structural estimation page loads
- settings page loads
- audit log page loads

At that point, move to real-data validation rather than adding synthetic history.
