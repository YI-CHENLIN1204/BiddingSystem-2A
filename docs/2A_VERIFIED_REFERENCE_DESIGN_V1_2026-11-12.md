# 2A 歷史參考資格（Verified Reference）設計規格草案 v1（2026-11-12）

> **狀態：設計提案，尚待指導老師／崴正業務窗口確認，不是已核定需求或已實作功能。**
> 本文件依據實際開發資料庫 Q1–Q8 盤點（詳見 `docs/2A_HISTORICAL_DATA_QUALITY_Q1_Q4_2026-11-12.md`、`docs/2A_HISTORICAL_DATA_QUALITY_Q5_Q8_2026-11-12.md`）提出資格管理方案。**禁止在本階段執行 Migration、更新 Approved 狀態、刪除 DEMO、修改正式權重或更改判定結果。**

## 1. 為什麼需要獨立的參考資格？

- 目前 10 件住宅 RC 建案，其中 8 Approved、2 Draft；8 Approved 之中 7 件名稱／檔名疑似展示或測試，剩下 1 件也未經實質來源核證。
- 8 Approved 有 7 件缺 `source_file_hash`；檔名、Hash 與審核時間都不足以證明商務定案與工程完整範圍。
- 數量歷史包含 11 筆鋼筋、10 筆混凝土有效交易，但各只涵蓋 8 個獨立建案。**交易行數 ≠ 樣本建案數**。
- 原有 `HistoryStatus=Approved` 是既有歷史審核／發布流程，不應被偷換定義成來源真實性證明；另一方面，真實來源核實亦不應繞過原有 Publish History。

## 2. 分層的資料責任

### A. 原有歷史發布狀態（保持不變）
`ProjectMaster.HistoryStatus`：`Draft` ↔ `Approved`（依原 Publish/Reopen 流程）。Approved 既有鎖定與操作記錄不變。

### B. 新的「來源認證狀態」（提案，獨立欄位／資料表，未實作）
狀態建議：
- `Unreviewed`：未核實，**所有既有記錄預設此狀態**；不因 Approved、檔名不像 DEMO 或有檔案 Hash 自動升級。
- `InReview`：審查進行中，尚未達資格。
- `Verified`：主管依核實的定案憑證、文件版本、來源與工程範圍明確核准。
- `Rejected`：確定不符合資格（例如僅為展示／預算估價稿）；記錄原因。
- `Revoked`：先前 Verified 的證據／有效性已遭撤銷；不得自動復原。

**重點：** 顯示「疑似 DEMO」只是待審標籤，不應以字串關鍵字自動判定 `Rejected`；`Approved` 與 `Verified` 必須**同時滿足**才有進一步成為參考候選的可能。

### C. 分估算目的的「適格範圍」（提案）
同一工程可能數量可用、價格不可用，反之亦然。因此至少分開：
- `quantity_rebar`（鋼筋標準 kg）；
- `quantity_concrete`（混凝土標準立方公尺）；
- `price_structure_major`（完整「結構體工程」大項價格口徑）。

**禁止僅以整案一個 `is_verified=true` 便放行所有模型**。同一建案各目的的核准、範圍說明與退回／撤銷要分別保存；工項單位與材料對應有誤不能靠人工勾選直接忽略。

## 3. 模型參考條件（設計規範，尚未套用 API）

待確認的預計資格謂詞：

```text
eligible(project, purpose, target)
  = project.HistoryStatus == Approved
    AND NOT project.IsDeleted
    AND provenance.status == Verified
    AND purpose_scope[purpose].status == Verified
    AND purpose_scope[purpose].evidence_current == true
    AND project.project_type == target.project_type
    AND required_input_fields_valid
    AND purpose_specific_data_checks_pass
    AND no_unresolved_critical_issues

candidate_count = COUNT(DISTINCT eligible.project_id)
if candidate_count < EstimationSettings.MinimumSampleCount:
    return InsufficientQualifiedHistory  # 不輸出經正式認證的估算合理區間
else:
    run existing similarity/range logic on eligible candidates
```

**「是否強制結構相同」暫不作成決策。** 前幾輪模擬顯示不同結構混用有風險，但仍缺真實工程樣本驗證；新資格診斷應揭示同結構件數與跨結構件數、被排除原因，不默默改掉現有相似度規則。在規則核准前不得把這個設計直接上線。

價格與數量模型應分開計算 eligible projects。對數量模型可將同案同材料的**合格工項量聚合成每案一個參考值**（依現有業務規則驗證），不能把 4 筆鋼筋交易當 4 件參考建案。

## 4. 人工審核流程及最低證據要求（提案）

1. 既有 `Draft → Publish History → Approved` 不變；取得 Approved 只是開始資格審核的必要條件。
2. 具有權限的人員啟動 `Reference Verification Review`；列明定案文件的版本、來源單位、工程名稱／用途、適用範圍，及來源證明方式。
3. 逐目的確認：數量是否完整、結構體工程價格是否涵蓋全部應含項目、單位轉換／材料對應是否成功、是否有未解決的重要映射問題。
4. 保留**審核結果、原因、操作者、時間、證據索引與使用的來源檔版本/Hash**。Hash 可校驗文件版本，**不可單獨充當真偽證明**。原始商業檔案應循既有安全儲存與權限，不把檔案內容明文放入稽核紀錄。
5. 核准後才可作為符合目的之參考候選。若重開歷史、改工項、覆寫大項、修正材料、更新來源文件或撤銷核准，對受影響的資格**失效／需重新驗證**，並留下不可覆寫審核事件。
6. 舊的 Approved 案件預設 `Unreviewed`；保留資料與既有操作功能。模型放行策略及既有 DEMO 展示資料的處理需另經核准並提供遷移／回退方案，避免一上線就悄悄清空現有範圍。

## 5. 角色權限矩陣（建議，未確認現行登入角色名稱）

| 操作 | 一般使用者 | 審核員 | 主管 |
|---|---|---|---|
| 查看自己允許範圍內的資格摘要 | 允許 | 允許 | 允許 |
| 上傳／補充審核證據（不得自行核准） | 依工作流程授權 | 允許 | 允許 |
| 核准「真實來源」 | 禁止 | 待授權確認 | 允許 |
| 核准「數量／價格適格」 | 禁止 | 待授權確認 | 允許 |
| 撤銷／退回已 Verified | 禁止 | 待授權確認 | 允許 |
| 修改審核歷史與抹除稽核事件 | 禁止 | 禁止 | 禁止 |

後端必須強制執行，不可靠隱藏 UI 按鈕。角色名稱／身分來源必須先盤點目前 ASP.NET 授權方式，不創造未存在的角色作為「已實作」。

## 6. 資料表／欄位方案（**proposal only**，不執行 DDL）

建議使用**獨立資格資料表＋不可覆寫事件表**，避免把來源真實性和多個目的適格性硬塞成 `ProjectMaster` 一個布林：
- `reference_verification`：`project_master_id`、`source_status`、`source_evidence_ref`（僅索引）、`source_file_hash`（可選）、`verified_by`、`verified_at`、`review_version`、`record_revision`、`updated_at`。
- `reference_purpose_review`：`project_master_id`、`purpose_key`、`review_status`、`scope_description`、`unit_mapping_verified`、`material_mapping_verified`、`critical_issues_resolved`、`evidence_ref`、`verified_by`、`verified_at`、`review_version`。
- `reference_verification_event`：`event_id`、`project_master_id`、`purpose_key`（nullable）、`previous_status`、`new_status`、`reason_code`、`evidence_ref`、`actor_id`、`occurred_at`、`entity_revision`。

正式設計前要再確認：誰負責定案、證據存放/Retention、審核證據是否涉及客戶資訊、資料表唯一鍵與併發控制、舊 Approved 展示資料如何標示、何時把資格檢查接入模型、以及 rollback 期間 UI 如何表達 `LegacyUnverified`。

## 7. API / UI 行為（提案）

- `GET /api/reference-verification/projects/{id}`：權限控管下查看來源資格、各目的資格、缺漏原因與歷史版本。
- `POST /api/reference-verification/projects/{id}/submit`：建立審核申請，要求至少一項可檢查證據。
- `POST .../review`：主管依 revision 與 evidence 更新指定來源／目的審核狀態；衝突返回 409；未授權 403；資料不完整回 422 或 ServiceResult validation。
- `POST .../revoke`：附原因撤銷；須記錄事件、使受影響的目的資格無效。
- UI 應分欄呈現「歷史發布狀態／來源資格／鋼筋資格／混凝土資格／結構體價格資格」，並顯示待補證據及不足的**獨立建案數**；不要把 DEMO 文字篩查直接當作驗證結果。
- 資料不足時模型可以顯示 `InsufficientQualifiedHistory` 與各原因件數，**不得回傳看似經正式認證的正常/異常燈號**。如何與既有舊 UI 相容需另訂版本化 API 規格。

## 8. 驗收測試矩陣（實作前的書面規格）

| 案例 | 輸入條件 | 預期 |
|---|---|---|
| VR-01 | 舊 Approved、無任何來源審核 | Unreviewed；不取得 Verified Reference 資格 |
| VR-02 | Draft 但主管勾選來源 Verified | 不能成為正式模型候選 |
| VR-03 | Approved、來源 Verified、鋼筋範圍 Verified、混凝土仍未審 | 只允許鋼筋作為**條件式候選**，混凝土仍排除 |
| VR-04 | 同案同材料四筆交易 | 聚合仍只計一個建案樣本，量值依工項口徑彙總 |
| VR-05 | 以名稱命中 DEMO | 只標記待人工審核，不自動刪除或自動拒絕 |
| VR-06 | 主管對來源撤銷並附原因 | 全部依賴此來源的適格資格立即失效，保留歷史 |
| VR-07 | 已 Verified 之後修改工項/來源/單位 | 受影響資格需重審，不默默沿用 |
| VR-08 | 一般使用者直接呼叫核准 API | 403；無狀態變更且有適當安全事件記錄 |
| VR-09 | 兩位審核員使用相同舊 revision 送出 | 後送更新 409，審核歷史不可被覆寫 |
| VR-10 | 5 筆交易但只有 3 個不同工程 | InsufficientQualifiedHistory |
| VR-11 | 真實來源／scope 不明且只有模擬資料 | 不可升格為正式參考 |
| VR-12 | Approved 但其他結構占大多數 | 報告同結構/異結構件數，依正式核准政策處理，不自動改變權重 |
| VR-13 | 來源 Verified、價格 scope 不完整 | 數量目的可獨立核對；價格目的仍不適格 |
| VR-14 | 證據失效或來源新版本 Hash 不符 | 禁止沿用舊 Verified；明確提示需重新審核 |

## 9. 實作前的必經里程碑

1. **業務／老師確認**：定案文件如何證明、不同工程是否具可比較範圍、Verified 審核權限與展示資料策略。
2. **DB 變更設計審查**：ERD、唯一鍵、外鍵、狀態約束、稽核事件 append-only、現行表的 Migration/rollback、舊資料回填全部 Unreviewed。
3. **整合影響核對**：逐條檢查 ASP.NET 查詢、量價兩種模型、操作紀錄、前端 UI、1D 選配介面及事件重開；不可只改某一條查詢而造成其他路徑仍使用未驗證案。
4. **測試與上線**：隔離資料庫 E2E 與角色權限測試、可重現資料工廠、舊資料保護、API 回歸、部署計畫。
5. **真實工程留案驗證**：在來源真實、範圍完整、日期合理且樣本數足夠後，再做正式相似度與 P15/P85 區間的產學驗收。

**本稿明確不包含已實作之核准功能，也沒有授權當前開發分支直接切換正式模型資格門檻。**
