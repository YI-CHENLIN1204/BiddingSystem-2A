## 2026-11-22 估算入口與建案條件搜尋

- 提交識別 `case-workspace-search-20261122`，基線 `d7f7eb3e`。
- 估算移入「待偵錯案件 → 工程與材料估算」頁籤，側欄與總覽移除獨立入口，舊 /estimation 導向新頁籤。兩份列表共用關鍵字、用途、結構、縣市及清除；案件另有來源，建案管理另有歷史基準狀態。案件列表移除四張計數卡與重複的暫定流程／歷史基準摘要。
- 程式位於 `frontend/src/views/`、`frontend/src/components/ProjectSearch.vue`、`frontend/src/services/projectSearch.ts`、`frontend/src/App.vue` 與 `frontend/src/router/index.ts`。沿用清單 API，於前端套用組合條件。
- vue-tsc 通過；Vite runner 建置通過（75 modules）；9 項條件比對檢查通過。預設 Vite 設定載入受 Windows 沙箱限制。真實瀏覽器、5184 與雲端資料操作待驗。
- 下一步在 5184 驗證合成廠房的建案篩選、舊估算網址轉導、案件內估算及頁籤試算。

前次資料與研究紀錄見 [主交接](CURRENT_HANDOFF_2026-11-11.md)。
