# 2A Architecture Decisions

> Updated: 2026-10-12

This file records decisions that should not be silently reversed during development.

## D-001 — Separate target cases from historical truth

**Decision:**  
`AnalysisCase` is not `ProjectMaster`.

Target cases are working data. Historical projects are finalized official bid data.

**Reason:**  
A target case may be revised repeatedly and must never contaminate its own estimation baseline.

---

## D-002 — Use immutable target versions

**Decision:**  
Repeated checks use `AnalysisBidVersion` snapshots.

```text
AnalysisCase
├─ V1
├─ V2
└─ V3
```

Do not overwrite old quantities or prices in place.

**Reason:**  
The system must preserve how a bid changed between submissions.

---

## D-003 — 1D is optional

**Decision:**  
2A must operate standalone.

1D is only an optional source for creating a target snapshot.

**Reason:**  
The school-project version will not have 1D, and repeated analysis must not require re-running 1D.

---

## D-004 — Current analysis scope is deliberately narrow

**Decision:**

- major-category estimation / price anomaly: `結構體工程`
- detail quantity anomaly: `鋼筋`, `混凝土`

**Reason:**  
These currently have the clearest historical-comparison meaning. Do not estimate every category simply because 1D can classify it.

---

## D-005 — Drawing-driven categories are not forced into quantity estimation

**Decision:**  
Do not force quantity-history models for categories such as doors/windows solely from coarse project metadata.

**Reason:**  
Their quantities may be driven much more directly by drawings and design choices.

This does **not** automatically mean major-category price analysis is permanently excluded; that remains a separate business decision.

---

## D-006 — Preserve source hierarchy; use reviewed override separately

**Decision:**  
Keep source `Category1 / Category2 / Category3` unchanged.

If the effective engineering major is wrong or missing, store a separate `major_category_override`.

**Reason:**  
Review corrections must not destroy source traceability.

---

## D-007 — Historical import starts as Draft

**Decision:**  
Importing a finalized bid does not immediately make it model truth.

```text
Import -> Draft -> review -> explicit publish -> Approved
```

**Reason:**  
Parsing, category recognition, unit conversion and material mapping can be wrong.

---

## D-008 — Approved history is immutable until reopened

**Decision:**  
Approved project metadata, row edits, major overrides, mappings and ignore state are locked.

Corrections require explicit `Reopen history review`.

**Reason:**  
A verified baseline must not silently change after being used by the model.

---

## D-009 — HistoricalTransaction is quantity-only

**Decision:**  
`historical_transaction` stores standardized quantity history for rebar / concrete.

It does not store price.

**Reason:**  
Structural-work price comes from finalized bid lines in `final_bid_item`; mixing quantity and price responsibilities caused duplicate and inconsistent data.

---

## D-010 — Structural price history stays in FinalBidItem

**Decision:**  
Structural-work historical price uses the finalized bid rows:

```text
Amount
or
Qty * UnitPrice
```

Then normalize by floor area to price per ping.

---

## D-011 — Site area is a formal similarity feature

**Decision:**  
Site area is required for target and historical project metadata and participates in similarity scoring.

**Reason:**  
It is part of the formal project input requirement and can materially affect project form / constraints.

---

## D-012 — Insufficient history must remain insufficient

**Decision:**  
Do not create fake history or force a range when valid samples are below `MinimumSampleCount`.

Default minimum sample count: **5**.

**Reason:**  
A visible “insufficient data” result is more trustworthy than a fabricated estimate.

---

## D-013 — Remove obsolete generic material-estimation architecture

**Decision:**  
The old generic material-estimation stack is removed.

Removed concepts include:

- `quantity_rule_base`
- core-material estimation
- generic range-estimation stack
- legacy one-shot anomaly route

**Reason:**  
It conflicts with the current major-price / detail-quantity architecture.

---

## D-014 — Current direct-upload rule is provisional

**Decision:**  
Until vendor confirmation, direct target upload accepts structured bid data with usable major category, item/spec, unit and quantity.

Price analysis additionally requires amount or unit price.

**Reason:**  
Do not silently guess arbitrary raw files while the interface contract is still unresolved.
