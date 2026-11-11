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
