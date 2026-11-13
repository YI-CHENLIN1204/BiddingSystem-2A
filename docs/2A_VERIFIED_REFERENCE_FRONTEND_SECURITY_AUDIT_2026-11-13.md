# 2A Verified Reference 前端與部署權限靜態接入審查（2026-11-13）

> **證據等級：GitHub 開發分支靜態程式碼查核**。未連線實際部署主機、未測遠端可達性、未閱讀伺服器外部 ACL/WAF/VPN 設定、未執行破壞性測試。**本文不是實際系統已確認對外開放的結論**。此輪不修改生產 C#/Vue、權重或資料庫。

## 1. 前端實際接入位置

| 頁面／檔案 | 已有功能 | Verified Reference 需新增或調整的地方（提案） |
|---|---|---|
| `frontend/src/views/ProjectDetailView.vue` | 可觸發 `POST /projects/{id}/publish-history` 與 `reopen-history-review`，透過 `sessionStorage` 保留待查證的 Idempotency-Key；頁面有 Approved 鎖定與已納入 2A 模型提示 | 發布與核實應兩階段顯示；不能在 Approved 後直接宣稱是真實已認證模型來源；新顯示來源狀態、鋼筋/混凝土/價格目的資格、證據、撤銷理由。**保留原收據對帳與冪等重試機制** |
| `frontend/src/views/ProjectsView.vue` | 建案列表用 `historyStatus===Approved` 顯示「已納入 2A 模型」 | 建案發布狀態與模型資料資格獨立顯示；避免未核實的 Demo Approved 被誤認正式參考 |
| `frontend/src/views/AnalysisCaseDetailView.vue` | 數量分析已有來源參考明細、ESS、可識別 DEMO 名稱提醒 | 目前只是名稱 heuristic，不是來源證據。加入資料資格版本、每模型 purpose 的有效/排除建案件數及範圍缺口，舊 AnalysisRun 應保持歷史記錄不可回填 |
| `frontend/src/views/AuditLogView.vue` | 操作紀錄頁查 `/audit-logs`，並顯示舊值新值 | 新增來源驗證、核准／撤銷／補證據事件檢視；只有經後端授權角色可讀。不能把此頁當作授權保護本身 |
| `frontend/src/App.vue` | 側邊欄包含「操作紀錄」給所有導航使用者 | 未有身份/角色判斷；日後 UI 可依角色顯示功能，但後端必須實際驗證 |
| `frontend/src/router/index.ts` | 所有路由無 `meta.requiresAuth` 或 `beforeEach` 權限守衛；僅 `afterEach` 設標題 | 需要清楚的登入/session 首頁與 401/403 互動；前端守衛不是安全邊界 |
| `frontend/src/services/api.ts` | `fetch` API 封裝：`Content-Type`、錯誤包裝；未看到內建 Bearer token 加入及身份更新流程 | 未來接入具體採用身份方案後加 session/cookie/Bearer 及 401/403 錯誤處理，嚴禁在前端寫死 API 金鑰 |
| `frontend/src/views/SettingsView.vue` | 參數管理頁 | 設定變更與資格核准都應後端驗權限，避免同一未授權操作者改門檻逃避審核 |
| `frontend/src/views/AnalysisCasesView.vue` | 待偵錯案件與歷史流程分離 | 保持 `AnalysisCase` 不自動升格 `ProjectMaster` 歷史來源的既有架構 |

### 發現的文案風險（不是新功能的既成事實）

- `ProjectDetailView.vue`、`ProjectsView.vue` 當 `historyStatus=Approved` 顯示「已納入 2A 模型」，**在目前 DB 8 Approved 中 7 件名稱疑似 DEMO 的條件下，容易誤導為正式來源已核真實**。
- `ProjectDetailView.vue` 的 Publish 確認文字明說「參與後續相似建案與結構體價格估算」，這是目前既有行為描述，未來一旦切換為 Verified-only 候選，必須同步修正，不能只改後端而留下舊文案。
- `AnalysisCaseDetailView.vue` 已有 DEMO 名稱提醒，應保留作為**疑似標記**而不是自動封鎖依據。

## 2. 身份／授權來源：程式層確認、部署層仍未知

| 路徑 | 靜態事實 | 判讀 |
|---|---|---|
| `崴正/Program.cs` | 無 ASP.NET Core `AddAuthentication`、`AddAuthorization`、`UseAuthentication`、`UseAuthorization`，Controllers 以 `MapControllers` 對外掛載 | 此檔本身看不到授權邊界，不得假設 Controller 只允許主管 |
| `崴正/Controllers/ProjectsApiController.cs` | `PublishHistory`、`ReopenHistoryReview`、`Delete` 未標 `[Authorize]`；稽核操作者使用 `User.Identity?.Name ?? "System"` | 目前無法由這條程式路徑可靠識別實際主管；日後必須驗證身份後再發放權限 |
| `崴正/Controllers/AuditLogsApiController.cs` | `GET /api/audit-logs` 未看到 `[Authorize]` | 使用者先前要求「只有主管能看 LOG」在應用程式碼中**尚未證實** |
| `frontend/src/router/index.ts` + `App.vue` | 無角色守衛，操作紀錄所有使用者都可從側欄找到 | UI 目前沒有能替代後端權限的機制 |
| `frontend/src/services/api.ts` | `fetch` 未自動附帶 Bearer Token；瀏覽器對同源 cookie 的預設行為仍取決於部署 | 未找到已整合的登入服務；但不能排除在外部代理進行驗證 |
| `frontend/nginx.conf` | `location /api/` 代理到 `http://backend:8080`，未見 `auth_request`、`allow/deny`、Basic Auth 或反向代理層的權限規則 | **此 Nginx 檔案沒有身份保護**；實際站點入口是否還有前置閘道、內網或 WAF 尚未知 |
| `frontend/Dockerfile` | 使用 Nginx 作前端產線伺服器 | 依附部署網路與容器入口設定，單靠本檔不能推定外網可達 |
| 部署主機及外部 Reverse Proxy | **本輪無主機存取資訊** | 不能確認實際開放 Port、防火牆、VPN 或身份保護，**不得透過未授權外部探測下結論** |

## 3. 安全接入決策門檻（實作前需回答）

**第一級：誰能連到 API？** 必須確認執行中的容器/服務公開 port、伺服器反向代理、防火牆、VPN 與企業內網限制。**第二級：誰是使用者？** 決定是否採公司現有 SSO／身份提供者、ASP.NET Core Cookie + 防 CSRF、或 Bearer JWT；不直接以共用密碼、前端 token 常數取代。**第三級：誰能做什麼？** 主管審核、一般人員查詢、資料匯入、稽核紀錄查看要用 server-side policy 控制；涵蓋 API 直打與現有舊路由。

這三個層級都未經核實前，不應直接新增 `POST .../reference-verify` 開放核准端點。選擇 Cookie 時需設 Secure/HttpOnly/SameSite 及 CSRF；Bearer 則需處理 token storage、refresh/expiration 與 scope；後端還需 CORS origin、401/403 與 AuditActor 身份的完整設計。

## 4. 實作接入順序（安全／相容性優先）

1. **先證實部署防護**：管理員明確回答目前入口是只限內網/VPN，還是有其它中介驗證，並以授權方式核對實際服務 config（不在聊天貼 token、cookie、DB connection strings）。
2. **身分與角色業務決策**：至少定義操作者、審核主管、只讀人員；依現有企業身份系統整合，避免造另一個無人維護的密碼系統。
3. **驗證流程的端點與回歸測試**：先把 Publish/Reopen/Delete/AuditLog/Settings 管控做好；僅對已核實角色提供核准 API。測未授權拒絕、登入/過期/CSRF、Idempotency receipt 與 concurrency。
4. **資料表與審核證據**：核對審核版本與工項快照，來源 Verified/三目的 Scope Verified、evidence revision 與 append-only event，既有 Approved 全部預設 Unreviewed；此步須經業務確認再 Migration。
5. **影子評估再切換模型**：明確標示已核實/未核實的獨立案數，不把展示案當可靠實務資料；全部數量及價格候選路徑一致套用資格，舊 Run 保留版本。

## 5. 最少驗收案例

| ID | 驗證 | 必要結果 |
|---|---|---|
| AUTH-01 | 未登入呼叫 Publish/Reopen/Delete | 401/403，沒有資料異動 |
| AUTH-02 | 一般人員讀取 AuditLogs 或呼叫核准 | 403，不洩漏明細 |
| AUTH-03 | 登入主管操作 Publish/Reopen 或核准 | 由後端 policy 授權，AuditActor 可追溯 |
| AUTH-04 | 角色變動、session 過期、跨站呼叫 | 立即拒絕無效身份且沒有資料異動 |
| AUTH-05 | 沒有身份時前端直接訪問敏感路由 | UI 呈現無權限，不能只靠隱藏按鈕 |
| AUTH-06 | 第三方從 API 直接呼叫（不經 Vue） | 後端拒絕未授權，不可信賴 Vue router |
| VR-FE-01 | Approved 但來源 Unreviewed | UI 不顯示「已驗證可用模型基準」 |
| VR-FE-02 | 重新開啟審核及網路斷線重試 | Idempotency-Key 延續且正確查 operation receipt，不產生重複操作 |
| VR-FE-03 | 舊 AnalysisRun | 結果歷史留存，來源資格 version 另列、不可擅改舊數據 |
| VR-FE-04 | 不同估算目的資格不同 | 鋼筋、混凝土、結構體價格各自呈現，數量／價格 Readiness 同步 |

## 6. 本次限制與決策狀態

此文件是**實作前接入盤點**，並非正式企業授權架構核准。已讀 GitHub 上指定後端與前端檔案，未檢查部署管理員持有的實際主機配置；未執行正式 API 測試、權限掃描、前端 Build 或 E2E，也**未修改任何生產行為**。

**下一步**：取得管理員對部署入口及公司身份驗證方式的**必要資訊**；若無此資訊，應先做不改 DB 的權限方案/回歸測試計畫，避免假裝完成安全加固。
