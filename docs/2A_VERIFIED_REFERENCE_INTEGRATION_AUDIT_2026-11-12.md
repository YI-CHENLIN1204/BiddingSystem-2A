# 2A Verified Reference：既有程式接入點與安全風險審查（2026-11-12）

> **審查性質**：讀取目前 `feature/1d-api-integration` 分支上的 C# 原始碼進行**靜態審查**，不等於已執行滲透測試、部署環境驗證或已建置新功能。此文為 `docs/2A_VERIFIED_REFERENCE_DESIGN_V1_2026-11-12.md` 的實作前接入評估。**未修改任何 C#、API、UI、DB schema、歷史資料或權重**。實際部署入口可能另有反向代理／外部權限防護，本審查無法證明其存在或不存在。

## 1. P0：身分驗證與角色授權不能直接假設已實作

| 位置 | 靜態查核 | 影響 |
|---|---|---|
| `崴正/Program.cs` | 有 `AddControllers`、CORS、EF、Swagger；**未見** `AddAuthentication`、`AddAuthorization`、`UseAuthentication`、`UseAuthorization` | 目前程式內看不到統一驗證身分或 enforce policy 的配置 |
| `崴正/Controllers/ProjectsApiController.cs` | `POST /api/projects/{id}/publish-history`、`POST /api/projects/{id}/reopen-history-review`、`DELETE /api/projects/{id}` 無 `[Authorize]`；`User.Identity?.Name ?? "System"` | **不能證明這些敏感操作限主管使用**；作業顯示身份可能是 fallback System |
| `崴正/Controllers/AuditLogsApiController.cs` | 查詢稽核紀錄的 GET 未見 `[Authorize]` | 「只有主管可看操作紀錄」目前無法依應用程式碼保證 |
| `崴正/Controllers/EstimationController.cs`、`DetectionCategoryController.cs` | 有修改材料、單位／偵錯類別、估算相關端點，控制器未見 `[Authorize]` | 新資格流程必須做**後端授權**及端點盤點，不可只隱藏前端按鈕 |

**必做**：先確認目前部署是否另有 API gateway、內網、防火牆或反向代理限制，並釐清身分提供者與角色清單。新增 Verified Reference 核准端點之前，必須完成後端身份驗證、權限、403 拒絕、事件記錄測試；設計文件的「一般／審核員／主管」是**建議角色**，不是現有實作。

**安全風險**：如果目前服務可被未授權網路客戶端直接觸及，這些審核／刪除操作可能暴露。尚未取得對外部署證據，**不能斷言實際遭到非法存取**。

## 2. P0：歷史發布／重開／刪除三個完整生命週期

| 操作 | 既有程式位置 | 確認行為 | 新資格方案必須考慮 |
|---|---|---|---|
| Publish | `ProjectsApiController.PublishHistory` → `ProjectManagementService.PublishHistoryAsync`（約 L554 起） | Advisory XACT lock、Idempotency-Key／receipt、比對工項大項／材料數量與單位、更新 `HistoryStatus=Approved`、SyncHistoricalTransaction、寫 AuditLog | Approved **不應**自動將來源或任何估算目的標記 Verified；需提交待審狀態、日後可稽核且與發布交易一致 |
| Reopen | `ProjectsApiController.ReopenHistoryReview` → `ProjectManagementService.ReopenHistoryReviewAsync`（約 L898 起） | 將有效歷史交易 `IsDeleted=true`、建案轉 Draft、清除核准人／時間、寫 AuditLog，使用同類型 advisory transaction lock + receipt | 即使來源文件未改，至少必須停止模型適格；對受影響的 scope 資格應失效／重審，不得隨舊 Verified 自動恢復 |
| Delete | `ProjectsApiController.Delete` → `ProjectManagementService.DeleteProjectAsync`（約 L1092 起） | 軟刪建案與相關歷史交易、寫 AuditLog | 原資格不得再被查為模型候選；保留來源核驗事件，避免抹除稽核歷史 |
| Edit | `FinalBidService`、`ProjectManagementService.UpdateProjectAsync` | 已有 Approved 編輯鎖定/衝突防護等路徑 | 編輯影響用途、結構、面積、工項材料、單位、結構體價格時須使受影響 scope 資格失效並要求重新審核 |
| Import | `FinalBidImportService.Import` | 產生 `Draft` 歷史資料及工項 | 新案的來源狀態預設 Unreviewed，且不得繞過既有工作表檢查與 Draft 流程 |

**重要**：現有 Publish/Reopen 已有交易、冪等、防重入機制。若日後新增驗證事件，應與業務狀態變化的資料交易原子一致，並避免事後背景補寫造成競態。此次沒有改動既有流程。

## 3. P0：估算參考候選的入口不一致

`崴正/Services/ProjectSimilarityService.cs` 有兩個 overload：

1. `GetScoredCandidateProjectsAsync(ProjectSimilarityTargetDto target)`（約 L352）：查詢 `!IsDeleted && HistoryStatus == "Approved" && ProjectType == target.ProjectType`；**沒有** Verified Reference 來源／用途專屬資格檢查（目前 DB 亦無該資格）。
2. `GetScoredCandidateProjectsAsync(int targetProjectId)`（約 L266）：查詢 `!IsDeleted && ProjectId != targetProjectId && ProjectType == target.ProjectType`，**沒有明文 HistoryStatus=Approved 的條件**。此入口應先查明是否僅用於「一般相似建案展示」或也被其他 API 當正式資料參考；不能在不知道呼叫者用途下直接更動共享 overload。

另外 `ScoreCandidates` 對不同結構給較低相似度而不是預設硬排除，結構硬門檻仍是研究政策提案，非已核准正式業務規則。

**建議接法**：保持一般相似建案展示 API 與正式估算候選兩種「用途」分離，正式數量／價格的共同資格選擇器須接受 `referencePurpose`、有效資料稽核版本及審核狀態；避免只在 Controller 或其中一個模型加過濾，另一條估算路徑仍使用未審核的 Approved。

## 4. P0：數量與價格不能共用單一資料可用旗標

| 路徑 | 已檢查程式 | 已有的參考選取方式 | 預計 Verified Reference 接入 |
|---|---|---|---|
| 細項數量 | `TwoAQuantityAnalysisService.AnalyzeAsync` → `DetectionCategoryRangeService.EstimateAsync` | 使用 DTO 相似候選、依 `materialIds` 讀 `HistoricalTransaction`、排除交易 `IsDeleted/IsOutlier`，以 `ProjectId` 累加換算後用量，再依 `MinimumSampleCount` 判不足 | 分別核鋼筋／混凝土 scope 的合格工項、來源認證、單位與正數量，**COUNT DISTINCT ProjectId**；不得把同案 4 筆鋼筋交易當 4 件獨立歷史案 |
| 結構體價格 | `AnalysisCaseService.RunMajorPriceAnalysisAsync` → `MajorCategoryPriceRangeService.EstimateAsync` | 使用 DTO 相似候選，再彙總 `FinalBidItem` 結構體大項金額；另有 `GetReadinessAsync` | 價格資料資格要有**完整結構體工程範圍**；不可用鋼筋／混凝土個別量核准直接推出整個大項價格核准；Readiness 與 Estimate 必須同一資格定義 |
| 歷史展示／檢查 | `ProjectManagementService.GetHistoricalDataAsync`、`GetTwoASummaryAsync`、`ProjectsApiController` | 查看已有資料；與模型用途不同 | 可顯示 Approved、來源未核實、三種適格狀態及待補原因；展示不應代表已列入正式模型 |

目前 **`DetectionCategoryRangeService` 已使用 `quantityByProject[row.ProjectId] += conversion.Quantity` 按建案累加**。這是現行程式直接確認的邏輯，不必重新發明；但仍需測試工項來源是否符合完整範圍，且加總不是證明有 8 件真實工程。

最低 5 件為既有名義歷史條件，日後應以核實來源且適合該 purpose 的**獨立建案數**為準；確認遷移後不讓現有 DEMO 資料默默維持適格。

## 5. P1：資料模型、事件與 API 兼容性

- `崴正/Models/ProjectMaster.cs` 只有 `HistoryStatus` 等；`FinalBidItem`、`HistoricalTransaction` 無 `source_verified`、`scope_verified` 之正式欄位，與真實 PostgreSQL 86 欄 Schema 查核一致。
- `崴正/Data/ApplicationDbContext.cs`：日後需加入獨立 `ReferenceVerification`、`ReferencePurposeReview`、`ReferenceVerificationEvent` 映射與 migration，但目前不得執行。
- `崴正/Services/AuditLogService.cs` 與 `AuditLogsApiController`：既有可查操作記錄，來源證據索引、主管身份與事件唯一修訂版本仍須獨立設計；不能只靠舊文字 AuditLog 當為不可覆寫版本化證據。
- `崴正/Services/ServiceResult.cs`：已有 `Conflict`／`Validation` 類型；新端點須統一處理 401/403/409/422 與匿名情形。目前 `ProjectsApiController` 的 `ToActionResult` 對未知驗證錯誤採 BadRequest，不能直接假設已支援 403。
- `崴正/Services/AnalysisCaseService.cs`：既有 `AnalysisRun.ResultJson` 保存歷次結果；若引入資料資格版本，應把篩選條件、審核版本與參考樣本計數納入執行快照，不應讓舊 Run 的結果暗中被新資格覆寫。
- 前端 Vue3：下一階段需逐項確認現有 Projects/History/Analysis 頁面與 API 呼叫位置（**本輪只核對後端，沒有逐檔完整確認前端接入**）。

## 6. 分期落地建議（尚非批准的開發排程）

| 階段 | 工作 | 完成門檻 |
|---|---|---|
| S0 授權與治理盤點 | 實際部署入口與身分／權限來源確認；業務核准角色、檔案真實性證明與工程範圍定義 | 主管核准者、角色、證據保管和 Demo 策略可追溯 |
| S1 先獨立可查 | migration design、來源與 per-purpose 審核表及事件、狀態 UI/API，但**先不改正式模型候選篩選** | 新舊資料可遷移、取消／重開可失效、前後端權限測試通過 |
| S2 影子比較 | 既有 Approved-only 和候選 Verified-only 並列「獨立建案數、拒絕原因、模型 Readiness」但**不產生虛假的已驗證燈號** | Demo／來源不明被標註而非誤判、確定是否仍有可用真實樣本 |
| S3 導入生產模型 | 對所有數量及價格路徑一致套用核准用途的 Verified qualification | 正確回報不足樣本，正式 API 與舊 Run 不被破壞，關鍵 E2E 測試過 |
| S4 工程留案驗證 | 真實、完整、可追溯、依時間排序的公司工程資料回測 | 量價模型分別驗收，未校準 P15/P85 不做過度聲稱 |

### 必備回歸測試（實作前最重要）

- 未登入、非主管直打 Publish／Reopen／Delete／驗證核准／Audit API，應安全拒絕；不只檢查前端顯示。
- 兩人並發使用相同 `Idempotency-Key` 與審核修訂版，沒有雙重審核、死鎖或事件重複寫入。
- Reopen / Delete / 修改工項 / 修改結構及面積後，舊來源證據及受影響 purpose 不能繼續作正式參考。
- 8 Approved 但 7 疑似 Demo 的資料集，只能顯示**未核實**或需人工認證，不能因為看似有 8 件自動放行。
- 數量多工項按建案加總但只計獨立案數；價格須全大項範圍確認，不能用數量 verified 替代。
- 無適格歷史且低於最低五件，API 與 UI 需明確 InsufficientQualifiedHistory／灰色／不得輸出正式異常燈號。
- 一般相似展示和正式數量／價格候選不得互相混用授權及審核資格。
- 既有 AnalysisRun 保留舊結果且明確標記當時引用資料資格版本，不能事後回填偽造的 Verified 訊息。

## 7. 本次審查沒有完成的事情

- 沒有執行 dotnet build、集成測試、連線掃描、授權滲透測試、部署入口或反向代理檢查。
- 沒有修改任何 C# Controller/Service、Vue UI 或 Supabase DB。
- 沒有取得業務／指導老師對 Verified Reference 流程的正式確認。
- 只對**明確列出的後端原始碼路徑**作靜態審查，前端路由／元件接點待逐檔盤點。

**因此，以下是「接入風險識別與實作排序」，不是 Verified Reference 已正式完成的聲明。**
