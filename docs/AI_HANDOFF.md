## 2026-11-22 廠房發布前檢查與歷史基準標籤

提交識別 `history-baseline-label-20261122`，基線 `a8047e4`。建案管理篩選與標籤、建案明細、決標回寫統一顯示「已納入歷史基準」。前端 vue-tsc 通過，瀏覽器待確認。

本機 5264 API 確認合成廠房 RC、SC、SRC 各 42 案，均為 Draft；126 案發布檢查全數成功回應，0 案可發布、126 案有未辨識工程大項。#256 發布前檢查有 106 筆未辨識工程大項。下一步對照 train 原始分類，走既有審核／發布流程，再用未匯入的 test 標單新增待偵錯案件。這次沒有寫入資料庫。

## 2026-11-22 估算入口與建案條件搜尋

- 提交識別 `case-workspace-search-20261122`，基線 `d7f7eb3e`。
- 估算移入「待偵錯案件 → 工程與材料估算」頁籤，側欄與總覽移除獨立入口，舊 /estimation 導向新頁籤。兩份列表共用關鍵字、用途、結構、縣市及清除；案件另有來源，建案管理另有歷史基準狀態。案件列表移除四張計數卡與重複的暫定流程／歷史基準摘要。
- 程式位於 `frontend/src/views/`、`frontend/src/components/ProjectSearch.vue`、`frontend/src/services/projectSearch.ts`、`frontend/src/App.vue` 與 `frontend/src/router/index.ts`。沿用清單 API，於前端套用組合條件。
- vue-tsc 通過；Vite runner 建置通過（75 modules）；9 項條件比對檢查通過。預設 Vite 設定載入受 Windows 沙箱限制。真實瀏覽器、5184 與雲端資料操作待驗。
- 下一步在 5184 驗證合成廠房的建案篩選、舊估算網址轉導、案件內估算及頁籤試算。

前次資料與研究紀錄見 [主交接](CURRENT_HANDOFF_2026-11-11.md)。
