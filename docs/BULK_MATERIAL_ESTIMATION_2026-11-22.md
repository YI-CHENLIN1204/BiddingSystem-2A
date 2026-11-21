# 大宗工項歷史估算與偏離檢查（2026-11-22）

識別：`bulk-material-range-20261122`。GitHub 起點 `db40e874e3267faa62557a28d528522a186a1f2d`；本機起點 `56cba382be73fde00a8e0f9397a0cf86064baf89`。提交 SHA 以此識別回查 Git 紀錄；實測摘要見同名 JSON。

## 完成的行為

本批以鋼骨為優先，連同模板、土方開挖接入可計算的歷史估算流程，取代僅顯示「待補條件」的狀態。使用者必須明確選一個啟用標準材料，不以名稱命中自動合併不同材料／工法。

| 工項 | 單位及概算分母 | 比較條件 |
|---|---|---|
| 鋼骨 | kg／總樓地板面積 | 僅 SC 或 SRC，結構完全一致，不跨結構借樣本 |
| 模板 | m²／總樓地板面積 | 同用途、同結構、同標準模板工項；不是採購板材量或圖說接觸面積精算 |
| 土方開挖 | m³／基地面積 | 同用途、同結構、同地下層數且地下層數大於零；不合併不同開挖／外運／回填材料 |

這些是歷史概算，不是宣稱不受建築或結構設計影響的固定工程係數。跨距、載重、節點、模板系統、開挖幾何與土質仍須核對。

沿用既有六因素相似度、相似度指數、時間半衰期、Top K 與加權分位數；未改權重或重新選參數。來源有效性檢查在選 Top K 前進行，按獨立建案計數。回傳來源建案、來源列數、分母、總量、單位面積量、權重、ESS 與最大權重占比；ESS／相似度不代表預測準確率。

有足夠樣本才提供下界／中位／上界；分位數標籤按實際設定顯示，不硬編碼 15／50／85。可選填待比總量，超出區間為 `ManualReviewRequired`，介面提供人工複核提示，`formalAnomalyEnabled=false`。樣本不足不給數值界限、不判正常／偏高／偏低。區間內也不代表工程合格。

## 來源、回寫限制與一致性

原 `FinalBidService.IsInTwoAHistoryScope`／既有數量白名單只允許鋼筋、混凝土。因此不能要求新材料一定先出現在歷史交易表，否則一般上傳／核准的鋼骨永遠可能無資料。

新大宗流程唯讀採用 **Approved、同用途／同結構建案中，已對應標準材料的決標工項**：

- 材料主檔與單位必須啟用；每次只選一個材料 ID。決標列須為 Manual／Auto，非忽略、數量為正、工程大項相容。
- 鋼骨接受結構體工程／鋼骨鋼構工程；模板接受結構體工程；土方接受土方及基礎工程及既有明確來源別名。
- 僅做明確的 kg／公噸、m²、m³ 換算，不猜密度、未知單位或跨維度換算。
- 若已有非刪除歷史交易，交叉核對材料、建案、決標來源 ID、來源數量及換算後數量；相同來源重複回寫、無法追溯、離群或不一致時整案排除。
- 若沒有歷史交易，直接讀取該筆已核准決標量；參考明細的 `finalBidOnlyRowCount` 明確標示，沒有捏造或補寫歷史交易。
- 已刪除的數量歷史不會透過直接讀決標重新啟用；孤立歷史、費用／輔助工項、單位不明或來源涵蓋不完整均保留排除原因，不以剩餘部分偷偷代替整案。
- 估算查詢使用 RepeatableRead，避免跨查詢混用不同時間的來源狀態。只讀取與計算，沒有 SaveChanges、發布、seed 或資料匯入。
- Approved 只是流程狀態，不認證資料真實性或工程完整性。原有合成來源維持明確標示。

歷史沒有一致的純材料價／加工／安裝／綜合價標記，因此 **本批不提供大宗單價區間**，`priceComparisonSupported=false`。未因為數量單位同為 kg 就混算不同計價範圍。

## API 與 UI

- `GET /api/bulk-material-estimates/options`：三種工項及名稱／單位符合的啟用標準材料候選；仍須使用者選定，不能把候選視為已核定工法。
- `POST /api/bulk-material-estimates/range`：`profileKey`、`materialId`、`target`；可選 `topCount`、`observedQuantity`、`excludeProjectId`。目標包含用途、結構、縣市、樓地板／基地面積、地上／地下樓層與年月。排除 ID 用於本案已在歷史庫時避免自我參考。
- 已接入工程估算頁與案件明細，沿用頁面建案條件。待比量由使用者輸入；本批沒有自動從待偵錯上傳列猜測材料、合併總量或建立新類型 Run。
- 修改輸入後會清除舊結果並拒收過期回應，來源載入失敗不影響既有結構體價格與鋼筋／混凝土流程。
- 能力矩陣對三種大宗工項顯示「支援歷史概算／須逐案檢查」；表上的舊交易表候選數與正式偵錯分類對應數，不等於新決標來源概算的實際可用數。

## 修改檔案

- `崴正/Models/BulkMaterialEstimateModels.cs`、`崴正/Services/BulkMaterialRangeCalculator.cs`、`崴正/Services/BulkMaterialEstimateService.cs`、`崴正/Controllers/BulkMaterialEstimateController.cs`、`崴正/Program.cs`。
- `崴正/Services/ProjectSimilarityService.cs` 僅將既有純計分方法公開為 static 供來源快照重播重用；公式、排序及其他流程篩選未變。
- `崴正/Models/MaterialCapabilityModels.cs`、`崴正/Services/MaterialCapabilityService.cs`、`frontend/src/components/MaterialCapabilityPanel.vue`。
- `frontend/src/components/BulkMaterialEstimatePanel.vue`、`frontend/src/views/EstimationView.vue`、`frontend/src/views/AnalysisCaseDetailView.vue`。
- `tests/WorkbookParserTests/BulkMaterialTests.cs`、`tests/WorkbookParserTests/Program.cs`。

## 實際驗證與限制

| 驗證 | 實際結果 |
|---|---|
| 後端及測試專案 Release build --no-restore | 0 warnings / 0 errors |
| WorkbookParserTests | 原 42 + 新 36，共 78 個檢核通過 |
| 前端 vue-tsc + Vite | 通過，72 modules；本機沿用既有依賴、configLoader native |
| Npgsql SQL 轉譯 | 歷史追溯、來源涵蓋、決標直接讀取、已刪除歷史 4 種查詢通過，不開 DB 連線 |
| 新 HTTP 路由輸入驗證 | 6/6 回 400：未知工項、未選材料、Top K 過大、負量、RC 鋼骨、無效面積；臨時 :5264 使用不可用 DB 位址，驗證在 DB 查詢前完成，測後停止 |
| 合成決標 API 快照重播 | 5/5 產生區間，每組 8 個來源案；使用既有純計分方法與新計算核心 |
| 新服務實際 DB 整合與 options 查詢 | **未執行**：隔離 DB 環境密碼未提供至此工作程序；SQL 轉譯／來源快照不能替代完整整合 |
| 真正瀏覽器操作驗收 | **未執行**：目前沒有瀏覽器控制工具 |
| GitHub CI | 提交後查實際 workflow 結果，不能預先當作成功 |

合成重播案例（非工程係數、非準確度評估）：

| 工項／結構／材料 ID | 決標來源列 | 來源建案 | 本次參考 |
|---|---:|---:|---:|
| 鋼骨／SRC／3 | 52 | 8 | 8 |
| 鋼骨／SC／3 | 58 | 8 | 8 |
| 模板／RC／8 | 53 | 8 | 8 |
| 模板／SRC／52 | 49 | 8 | 8 |
| 土方／SRC／5 | 48 | 8 | 8 |

成功擷取版從既有隔離 API 發出 87 次 GET；標單列數／總量逐案比對歷史聚合，settings 前後不變。每組僅挑 8 個可用合成來源做功能重播，不是新服務的完整候選池，也不是留案／獨立 Holdout。舊 API 不暴露逐筆歷史離群／刪除旗標；重播採直接決標路徑，該類旗標由單元測試涵蓋，不能宣稱已驗證 DB 真實旗標。

中間擷取腳本曾因 Request.method 用法失敗，已修正；初版錯借鋼筋參考池導致 SC 鋼骨快照為零，改為同結構原始建案及既有純計分方法後通過。這是驗證工具修正，不是把 SC 鋼骨誤判為沒有資料，也沒有為補數據新增歷史或重生合成資料。

可重跑核心驗證：

```powershell
dotnet build tests/WorkbookParserTests/WorkbookParserTests.csproj -c Release
dotnet run --project tests/WorkbookParserTests/WorkbookParserTests.csproj -c Release --no-build
npm run build --prefix frontend
```

可選的來源快照測試介面是 `--bulk-snapshot <snapshot.json>`，輸入必須符合測試 Snapshot DTO；原始快照屬本機隔離研究輸出，不作正式訓練資料。JSON 摘要保留各案例雜湊、來源 ID、區間與測試界線。

## 整合、回滾與下一步

沒有 schema／Migration、資料修補、歷史寫入或既有 Run 變更。回滾為 revert 本批程式提交並重建前後端，無資料回滾需求。程式交付 feature 分支；原桌面工作樹及 :5263／:5174 未部署本批，main 程式未合併。

仍遵循使用者順序：先完成工項擴充與實際 DB 查詢驗證，再做真正瀏覽器上傳／解析／人工確認／估算／異常偵錯／決標回寫／歷史／操作紀錄，集中修正回歸後，最後才以固定範圍的獨立 Holdout 比較效果。本批未調參、未重跑 SRC smoke／敏感度／準確度。

