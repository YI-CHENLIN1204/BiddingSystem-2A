# 工項擴充第一批與驗收邊界（2026-11-22）

本輪識別：`material-capability-phase1-20261122`。遠端起點 `ab565eaaca905eb75f2bf2bdbdd764574ad2b61c`；實際提交 SHA 以包含此識別的 Git commit 為準。

## 最新收尾順序

依使用者最新指示：①擴充估算與異常偵錯工項 → ②完整瀏覽器 UI 操作驗收 → ③集中修正／回歸／分支整合 → ④獨立 Holdout 準確度與演算法比較。此順序取代舊段落的「準確度優先」。本批完成第一批程式與離線驗證，第一階段尚未全部驗收；原有 Holdout 報告保留為歷史基線，本輪未重跑。

## 工項選擇與實作

沒有將任何材料宣稱為完全不受設計影響。較穩定的是「已知實際施工面積、指定產品及工法後」的單位面積用量。建築面積、造型、結構、現場條件仍影響總量。

新增能力矩陣涵蓋鋼筋、混凝土、養護劑、脫模劑、鋼骨、模板、土方開挖及防水材料。名稱命中只是候選；正式對應必須有啟用的 CategoryMaterialMap、正確大項／分類與相容單位。歷史樣本須是 Approved、非刪除／非離群、正數用量、工程面積／樓層／日期完整的獨立建案；重複材料列不增加建案數。可依 RC／SC／SRC 篩選，樣本門檻沿用設定，缺值時 5。

正式統計模型仍為既有鋼筋／混凝土白名單，未修改 TwoAScopePolicy。新候選即使歷史數量多，缺少核准工程條件模型仍不能自動啟用紅燈。能力矩陣顯示材料、單位、正式對應、獨立案例數及不足原因；名稱中的 SYNTHETIC: 僅是可識別標記，不能認證真實來源。

新增兩種材料、三組指定產品規格試算：

| 材料／規格 | 依據面積 | 廠商理論用量 | 試算 |
|---|---|---|---|
| Sika Antisol E 養護劑 | 實際待養護混凝土表面 | 0.15–0.20 kg/m² | 面積 × 下／上限 |
| Sika Separol 木模板 | 實際模板接觸施作面積 | 13–21 m²/L | 下限 = 面積/21，上限 = 面積/13 |
| Sika Separol 金屬／塑膠模板 | 實際模板接觸施作面積 | 27–42 m²/L | 下限 = 面積/42，上限 = 面積/27 |

來源：[Antisol E PDS](https://twn.sika.com/dam/dms/tw01/s/sika-antisol-e.pdf)（2022-03 / 02.01）、[Separol PDS](https://twn.sika.com/dam/dms/tw01/3/sika-separol.pdf)（2023-03 / 03.01）；2026-11-22 查核。僅適用指定產品與條件，不泛化其他品牌。風速、溫濕度、表面粗糙度、吸水性與施工耗損可能影響實際用量。必須符合本案核定施工規格；不增加任意耗損率或未提供的密度轉換。

回傳 `ManufacturerTheoreticalRange`，不是 P15/P50/P85。超出範圍只回 `ManualReviewRequired`，`formalAnomalyEnabled=false`；不是工程不合格、採購核准量或正式異常。未選產品、面積不合理、負用量回 400。

## 既有合成資料候選盤點

下表為既有 SYNTHETIC CSV 的名稱提示候選、Train 基線正量列之獨立建案數；不是即時 DB 能力端點結果，也不是合格統計樣本數或真實工程證據。混凝土包含修補候選及防水跨單位，正說明必須加單位／對應檢核。

| 候選 | Train 建案 | 其中 SRC | 原始單位 |
|---|---:|---:|---|
| 鋼筋 | 148 | 72 | kg |
| 混凝土名稱候選 | 206 | 77 | m2、m3 |
| 養護劑 | 0 | 0 | 無 |
| 脫模劑 | 0 | 0 | 無 |
| 鋼骨 | 125 | 75 | kg |
| 模板 | 152 | 75 | m2 |
| 土方開挖 | 200 | 74 | m3 |
| 防水材料 | 209 | 77 | m、m2、組 |

養護劑／脫模劑目前沒有對應合成歷史，故提供規格試算，不啟用統計異常。鋼骨缺跨度／載重／結構系統／接合等工程條件；模板缺接觸面積與工法；土方缺開挖幾何／深度／土質；防水缺施作面積／產品厚度層數，不能以總樓地板面積直接套用。

## API、UI 與檔案

- `GET /api/material-capabilities?structureType=SRC`：唯讀 DB 能力矩陣。
- `GET /api/material-capabilities/specifications`：三組廠商規格，不查 DB。
- `POST /api/material-capabilities/specification-estimate`：純計算，不寫 DB。
- 工程估算頁新增能力矩陣與規格試算；案件明細新增能力矩陣。查詢失敗不阻擋既有估算流程；需啟動新後端才能取得新端點。
- 後端：`崴正/Models/MaterialCapabilityModels.cs`、`崴正/Services/MaterialCapabilityService.cs`、`崴正/Services/SpecificationQuantityService.cs`、`崴正/Controllers/MaterialCapabilityController.cs`、`崴正/Program.cs`。
- 前端：`frontend/src/components/MaterialCapabilityPanel.vue`、`frontend/src/views/EstimationView.vue`、`frontend/src/views/AnalysisCaseDetailView.vue`。
- 測試：`tests/WorkbookParserTests/Program.cs`。

無 schema、Migration、seed、回寫、正式模型參數或既有 Runs 變更。回滾為 revert 本批程式提交、重建前後端，無資料遷移需求。另精確修正文檔中誤寫的權重 `25/30/10/15/11/11` 為實際 `25/30/10/15/10/10`；程式參數從未改動。

## 實際驗證

| 項目 | 結果與範圍 |
|---|---|
| Release 後端 build --no-restore | 通過，0 warnings / 0 errors |
| WorkbookParserTests Release --no-restore | 全部通過，含 11 個新增檢核；純計算、單位反向、缺樣本、相同建案去重、缺工程條件不啟用、白名單不擴大 |
| frontend build -- --configLoader native | 通過 vue-tsc + Vite，69 modules；最初 option 結尾標記錯誤已修正後重跑通過 |
| Python unittest | 11/11 通過；沒有重跑 Holdout 批次 |
| 兩支既有 Node 檢查與 git diff --check | 通過 |
| 臨時 API :5264 HTTP 規格契約 | 7/7：規格 GET、3 組有效計算 200、3 組無效請求 400；使用不可用 DB 位址且未呼叫 DB 查詢、不新增案件／Runs；測完停止 |
| DB 能力矩陣 GET | 尚未執行：隔離 DB 的密碼由 GRADUATION_SRC_DB_PASSWORD 環境提供，本工作程序無該值；未猜密碼、未改用正式 DB |
| 真正瀏覽器完整操作 | 尚未執行：本工作階段未提供瀏覽器控制工具；前端建置與 HTTP 契約不等於操作驗收 |
| GitHub CI | 提交後另查 workflow 實際狀態，勿預先宣稱通過 |

原有 :5263 後端／:5174 UI 未重啟、未部署本批；Desktop 原工作樹不在此階段可寫範圍，本批在可寫工作副本實作並交付 GitHub feature 分支。不能宣稱使用者原 UI 已更新。

## 下一個驗證點

先在可取得隔離 DB 環境的開發程序啟動本批前後端，查證能力矩陣 DB LINQ 實際查詢與資料計數、樣本不足顯示；再用真正瀏覽器走上傳 → 解析 → 人工確認 → 估算／規格試算 → 異常偵錯 → 決標回寫 → 歷史 → 操作紀錄，保留各步證據及失敗。其後集中修正／回歸／整合，最後才對固定範圍進行獨立 Holdout 比較。不要重複 SRC 9 組 smoke、6 組敏感度或重新匯入既有合成資料。

