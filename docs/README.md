## 2026-11-22 最新指示與工項擴充第一批

本節優先於下方較舊的工作順序。識別 `material-capability-phase1-20261122`（起點 `ab565eaa`；以含此識別的 Git commit 回查提交）。

- 最新四階段：**工項擴充 → 真正瀏覽器 UI 驗收 → 集中修正／回歸／整合 → 最後 Holdout 準確度與演算法比較**。既有回測報告為歷史基線，本輪未重跑準確度。
- 能力矩陣新增 8 類工項，檢查材料候選、正式對應、單位、獨立 Approved 建案及缺少工程條件。新候選未經工程條件模型驗證不啟用正式紅燈。
- 新增養護劑及脫模劑之 3 組指定產品規格試算，使用實際施作面積；是廠商理論範圍，不是 P15/P85。超出只提示人工確認，不修改原統計白名單。
- 工程估算／案件明細已加 UI。無 schema／seed／DB 寫入或模型參數變更。另修正文檔誤植權重為實際 `25/30/10/15/10/10`。
- 已通過：後端 0 警告建置、Parser 全部（含 11 新檢核）、前端建置（初次 option 標籤錯誤修復後通過）、Python 11/11、HTTP 規格契約 7/7。
- **未驗證**：DB 能力端點（隔離 DB 環境密碼未提供至本工作程序）、真正瀏覽器全流程（本工作階段無瀏覽器控制工具）。原 :5263／:5174 未部署此批。不可將編譯或 HTTP 通過記為 UI 驗收完成。
- 下一步先啟動本批於隔離 DB 環境核對能力矩陣，再進入完整 UI 流程；不要重匯合成資料、重跑既有 SRC smoke／敏感度。
- 詳細檔案、API、廠商來源、合成候選數及回滾：[`MATERIAL_CAPABILITY_PHASE1_2026-11-22.md`](MATERIAL_CAPABILITY_PHASE1_2026-11-22.md)。

# 2A 文件索引

> **⚠ 2026-11-11 更新：本檔是歷史交接／索引，部分內容記載 2026-10-15 快照，不能代表現在的資料庫或研究進度。請優先閱讀 [完整交接主檔](./CURRENT_HANDOFF_2026-11-11.md)、[研究與版本回溯](./RESEARCH_TRACEABILITY_2026-11-11.md)、[新對話可複製提示](./NEW_CHAT_START_2026-11-11.md)、[模擬研究指南](../research/2a_synthetic/README.md)。最新測試：使用者已貼出 5760 組／96 彙總與原 7/7 單元測試成功，後續六因子審查亦回報無錯，詳細輸出待分析。**


> Repository：`YI-CHENLIN1204/BiddingSystem-2A`  
> 目前正式開發 Branch：`feature/1d-api-integration`  
> 更新日期：2026-10-15

此資料夾為 2A「量價估算與異常偵測系統」目前正式文件來源。

## 建議先閱讀

如果是新的開發對話或新的開發人員，請依照以下順序閱讀：

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

## 各文件用途

| 文件 | 用途 |
|---|---|
| `NEXT_CHAT_HANDOFF.md` | 可直接複製到下一個 ChatGPT／開發對話的精簡交接資料 |
| `HANDOFF.md` | 目前實作狀態與接下來應優先處理的工作 |
| `2A_CURRENT_ARCHITECTURE.md` | Target／History 正式架構、模型範圍與資料流程 |
| `DECISIONS.md` | 已確認、不應被默默推翻的架構決策 |
| `API.md` | 目前後端 API 與各端點責任 |
| `DATABASE.md` | 目前資料表、欄位、關聯與模型資料責任 |
| `DEVELOPMENT.md` | 本機建置、Docker、Git、Schema 異動與文件維護規則 |
| `LOCAL_TEST_GUIDE.md` | 本機實際驗證操作清單 |
| `VENDOR_QUESTIONS.md` | 仍等待廠商確認的問題 |
| `WORKLOG.md` | 依時間整理的開發紀錄 |

## 目前正式分析範圍

```text
大項估算／價格異常
└─ 結構體工程

細項數量異常偵測
└─ 結構體工程
   ├─ 鋼筋
   └─ 混凝土
```

## 目前資料分流

```text
待偵錯／工作資料
AnalysisCase
└─ AnalysisBidVersion
   ├─ AnalysisBidItem
   └─ AnalysisRun

已確認歷史資料
ProjectMaster
└─ FinalBidItem
   └─ HistoricalTransaction
```

關鍵規則：

```text
AnalysisCase != ProjectMaster
```

待偵錯標單不得自動成為模型歷史真值。

## 目前開發階段

目前架構已經足以進入真實資料驗證。

目前優先順序：

1. 啟動最新 `feature/1d-api-integration`
2. 在正式匯入前先檢查真實 Excel
3. 將真正已定案的最終標單匯入為 Draft 歷史資料
4. 處理審核阻擋項目
5. 執行 Publish History → Approved
6. 持續匯入直到有足夠有效歷史案件
7. 驗證結構體工程 P15／P50／P85
8. 建立真實待偵錯案件並比較 V1／V2

不要為了讓模型產生合理區間而塞入假歷史資料。

## Branch 顯示注意事項

`docs/` 目前維護在：

```text
feature/1d-api-integration
```

如果 GitHub 當前顯示的是預設 `main`，可能看不到這個資料夾的最新內容。請使用 Branch 選擇器切換到 `feature/1d-api-integration`。

除非使用者明確要求，否則不要合併到 `main`。


## 自動化交接審查規則（2026-11-11）

- **Skill**：[waizheng-2a-handoff-guardian](../.agents/skills/waizheng-2a-handoff-guardian/SKILL.md)
- **代理入口**：[AGENTS.md](../AGENTS.md)
- **變更審查紀錄**：[HANDOFF_CHANGE_REVIEWS.jsonl](./HANDOFF_CHANGE_REVIEWS.jsonl)
- **本機檢查器**：`python scripts/check_handoff_review.py --base <變更前 SHA>`
- **PR 檢查**：`.github/workflows/2a-handoff-review.yml`

每次任何變動都審查；即使不更新交接內容，也必須記錄原因。新對話必讀 Skill。
