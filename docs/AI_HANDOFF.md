## 廠房跨裝置測試接續

案件 #256、固定測試版本、雙終端啟動指令及現有 404 證據，見 [跨裝置測試手冊](FACTORY_CROSS_DEVICE_TEST.md)。

## 2026-11-22 廠房合成訓練資料已匯入 Supabase 草稿

批次 `FACTORY_20261122_V1_TRAIN` 已匯入。來源固定為前次交付資料包的 train 子集。RC、SC、SRC 各 42 案，合計 126 案。新增 16,786 筆決標明細，全部為 Auto 且有材料 ID。比對名稱、單位和分類後沿用 63 種材料，建立四個新標準工項（金屬鋼板屋面、金屬外牆板、工業捲門、耐磨地坪）及 67 個帶「－廠房作業區」的合成標單專用字典詞。數量、單價與金額來自 CSV，明細項次使用建案合成 ID 加匯入列序。

以單一資料庫交易新增，存在同批或同前綴時拒絕重複匯入。匯後 SQL 查核各結構草稿數、已對應列、材料數、字典數與單位一致性。住宅仍 210 案。新批皆為 Draft，新增歷史交易 0，待依流程審核後才可發布作合成估算參考。驗證與測試集未匯入。GitHub 本次僅同步交接，無程式碼或 Migration 變更。

## 2026-11-22 廠房資料包已完成並交付（雲端尚未匯入）

使用者同意補獨立廠房資料後，已完成離線生成、檢核與資料包交付。**這不等於 Supabase 已有廠房或新服務／瀏覽器已驗收。** 最新工具／測試提交 [d7f7eb3e](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/commit/d7f7eb3e868e9422dc852623789243e91ac63fdf)，`feature/1d-api-integration`；main 本次僅同步交接。

- 廠房180案：RC／SC／SRC各60；每結構42 train／9 validation／9 test，共126／27／27。獨立FAC前綴ID，原住宅300案與原Holdout未動；住宅來源三CSV SHA均與原manifest相符。
- 最終23,980筆正量正價基準明細，歷史CSV一對一；每案39–59種工項。型錄104種、基準實際67種。4個新增廠房素材模板為金屬鋼板屋面、金屬外牆板、工業捲門、耐磨地坪，尚未建立雲端主檔。
- 情境區分單層／倉儲附辦公／多層生產／地下室／耐磨地坪／裝卸場，含有無辦公區、不同屋面及地下條件。適用性sidecar區分ApplicablePresent、NotApplicableAssumed、OutOfScope，缺項不補0；6個test配對漏項／明確零量變體與基準及歷史隔離，不算6個獨立案。
- 交付 `factory-synthetic-v1-20261122.zip`（2,110,357 bytes；SHA256 `b6d4d69188d7713ab8d86624e3f4eaa015ec4d1b51910ddf558d4ba7f53f4e4e`），內含來源、假設、真值、6份完整Excel、雜湊，以及**僅126 train／16,786列**的匯入候選。已實際保存並在對話交付；Git保留可重現程式、報告及雜湊，不塞大型CSV。
- 新 `generate_factory_dataset.py`、`factory_material_catalog.json`、`prepare_factory_delivery.py`、10項廠房測試；Excel exporter新增 `--complete` 保留所有數量／單位／價格，舊預設缺欄測試保留。生成與封裝拒覆寫，沒有資料庫寫入路徑。
- 已通過：Python21/21、API／Parser專案Release0 warnings/errors、原78回歸及6份train/test Excel逐列數量／單價／金額／工程分類核對、ZIP CRC與全部封裝SHA、住宅SHA；交接審查16檔1筆通過。GitHub CI [44984804587](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/44984804587) 三項 completed/success。
- **本輪雲端只SELECT，最後實查仍住宅210、廠房0；沒有匯入、建立主檔、自動Approved、Schema/Migration或模型權重變更。** 用量、價格與適用性都是明示的未校準假設；是所選工種示例，非完整結構合約、真實工程標準或企業準確率。
- 初版辦公區套用過廣及型錄／實際材料數混淆已在發布前修正，最終是23,980列及39–59種，不使用中間輸出的25,690列／47–59種。以最終ZIP及報告雜湊為準。
- 下一步確認隔離合成測試目標、4個新模板的名稱／單位／範圍，準備只新增train的匯入／回滾；不能把validation/test/配對變體發布成歷史或覆寫住宅。接著新服務DB及完整UI、集中回歸整合，最後準確度。既有5184→5264→Supabase讀取成功紀錄仍保留，但不代表已載入本批。
- [完整生成與實測報告](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/blob/d7f7eb3e868e9422dc852623789243e91ac63fdf/docs/FACTORY_DATASET_2026-11-22.md)及同名JSON；識別 `factory-dataset-v1-20261122`。

## 2026-11-22 資料範圍確認：目前全住宅，廠房仍是缺口

使用者指出廠房缺資料，並確認工項不必每個建案都有。本輪 Supabase 純 SELECT 與既有 CSV 已核對；**沒有修改程式或雲端資料，也尚未生成／匯入廠房**。

- 雲端 210 個 Approved 合成案全為住宅：RC 78、SC 55、SRC 77；廠房 0。既有 CSV 300 案也全住宅（210 train／45 validation／45 test）。SC 是結構類型，不代表廠房用途；不能宣稱廠房歷史／估算已驗收。
- 鋼骨、模板、土方原住宅資料已存在，新增估算不必重建、覆寫或重新回寫住宅。非忽略決標工項存在案數：結構鋼骨132、模板ID8為127／ID52為132、土方ID5為130／ID49為179；這是存在數，非完整篩選後有效樣本，不可直接相加。
- 養護劑／脫模劑／脫膜劑材料主檔匹配0，目前只是指定產品規格試算，沒有歷史模型資料。未來若需歷史基準，另補適用案例、施工面積、產品與工法。
- 共100種標準材料，每案實際63–92種，並非每案全有。**不適用／確定未施作、未知漏列、明確數量0應區分**；不得強塞工項、把缺項補0或因未出現就判偏低。現有資料沒有完整逐案逐工項適用性真值，缺項也不能自動認定合理不適用。
- 後續廠房應獨立用途條件、工項適用情境、ID／批次及 train-validation-test，保留既有住宅／Holdout；不僅改住宅案名或用途欄。先處理資料適用範圍，再完成 UI／回歸整合，最後準確度比較。
- 盤點及交接提交 [5e01a374](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/commit/5e01a37463d3b27c70edc1879d6134e9e11eb7cb)，`feature/1d-api-integration`；程式功能仍為前次 `19a4c508`。main 本次僅同步索引與審查，保留後續雲端搬遷及 5184→5264→Supabase 讀取成功的既有紀錄。
- [完整實查與資料存在數](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/blob/5e01a37463d3b27c70edc1879d6134e9e11eb7cb/docs/DATASET_APPLICABILITY_AUDIT_2026-11-22.md)；交接審查7檔1筆通過，無需重跑無關功能測試。識別 `dataset-applicability-audit-20261122`。

## 2026-11-22 最新：鋼骨、模板、土方已接入歷史估算

本節優先於下方第一批候選狀態。最新程式：[19a4c508](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/commit/19a4c5086958d0f9aae0890a6d0bcb1aa6bc1c4c)，`feature/1d-api-integration`；**main 這次只更新交接索引及審查，不合併功能程式，也不代表原本執行中的 UI 已更新。**

- 鋼骨限 SC／SRC 且分開比較；模板同用途／同結構／同標準材料；土方另限相同且大於零的地下層數。鋼骨／模板按樓地板面積歷史比例、土方按基地面積比例概算，仍須人工核對設計與工法。
- 新 `GET /api/bulk-material-estimates/options`、`POST /api/bulk-material-estimates/range` 及工程估算／案件明細 UI。可選材料、Top K、待比總量、自身歷史建案排除 ID；回傳來源、分位區間、ESS 及排除原因。
- 原正常回寫只允許鋼筋／混凝土；新大宗唯讀估算直接使用 Approved 已對應決標列，已有歷史交易則交叉核對。明確排除／刪除／離群、重複來源與不完整範圍不會因回退而重新納入。使用 RepeatableRead；無 DB schema／seed／資料寫入、既有 Run 或參數變更。
- 超出區間只提示人工複核，資料不足不給界限；尚未啟用新正式紅燈。價格未有一致的純材料／加工安裝標記，故不混算大宗單價。
- **已通過**：API／測試專案 Release 0 warnings / 0 errors；Parser 78 個檢核（新增 36）；前端 vue-tsc/Vite 72 modules；4 種 Npgsql SQL 轉譯；6 個無效 HTTP 請求 400；5 組合成決標來源功能重播（SRC/SC 鋼骨、RC/SRC 模板、SRC 土方，各 8 案）。重播非新服務 DB 整合、非準確度，未重跑獨立 Holdout 或調參。
- GitHub CI [44933218707](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/44933218707)：completed / success，backend-build、frontend-build、synthetic-fixture-contracts 三項通過。交接審查 23 檔、1 筆新紀錄通過。
- **尚未測**：新服務實際 DB 整合與 options（此工作程序缺隔離 DB 連線環境）；真正瀏覽器操作（目前無控制工具）。原 Desktop 工作樹、:5263／:5174 未部署本批。前端編譯／來源快照／SQL 轉譯不能當作 UI 全流程驗收。
- 下一步：先啟動新服務驗 DB 與 UI；維持 **工項擴充 → 真正瀏覽器 UI 驗收 → 集中修正回歸整合 → 最後獨立 Holdout 準確度／演算法比較**。不要重匯合成資料或重跑已完成 SRC smoke／敏感度。
- [詳細檔案、API、來源、實測與回滾](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/blob/19a4c5086958d0f9aae0890a6d0bcb1aa6bc1c4c/docs/BULK_MATERIAL_ESTIMATION_2026-11-22.md)；同名 JSON 保留實測摘要。識別 `bulk-material-range-20261122`。

## 2026-11-22 最新：改採四階段收尾，工項擴充第一批已提交

**本節覆蓋下方歷史段落的工作順序。** 使用者最新順序為 **①擴充估算與異常偵錯工項 → ②真正瀏覽器完整 UI 驗收 → ③集中修正／回歸／分支整合 → ④最後獨立 Holdout 準確度與演算法比較**。既有回測只保留為歷史基線，本輪未重跑準確度。

- 最新程式提交：[db40e874](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/commit/db40e874e3267faa62557a28d528522a186a1f2d)，分支 `feature/1d-api-integration`。**本次 main 僅更新交接索引與審查紀錄，功能程式尚未合併至 main。** 請讀 feature 實際程式及下列報告，不能以 main 舊程式判定本批功能已部署。
- 新增 8 類工項能力矩陣：鋼筋、混凝土、養護劑、脫模劑、鋼骨、模板、土方開挖、防水材料；名稱命中僅是候選，檢查正式材料對應、相容單位、Approved 獨立建案及缺少工程條件。新候選不因合成樣本多而直接開啟正式紅燈。
- 新增養護劑／脫模劑的 3 組指定產品規格試算，使用實際施工面積；材料總量仍受設計及工程條件影響，未宣稱完全不受設計影響。廠商理論用量不是 P15/P85，超出只提示人工確認；既有統計模型白名單未擴張。
- UI 位於工程估算與案件明細；新增 `GET /api/material-capabilities`、`GET /api/material-capabilities/specifications`、`POST /api/material-capabilities/specification-estimate`。無 DB schema／Migration／seed／回寫或既有模型參數改動。
- 實際通過：Release build 0 warnings / 0 errors；Parser 全部（含 11 新檢核）；前端 vue-tsc + Vite（初次 option 標籤錯誤已修復）；Python 11/11；臨時獨立服務純計算 HTTP 契約 7/7；交接審查 16 檔、1 筆新紀錄。另修正 feature CURRENT_HANDOFF 的權重誤植，實際參數一直是 `25/30/10/15/10/10`。
- GitHub CI [44930323986](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/44930323986)：completed / success；backend-build、frontend-build、synthetic-fixture-contracts 三項成功。
- **未測且不得宣稱完成**：DB 能力矩陣 GET（隔離 DB 環境變數密碼未提供至工作程序）、真正瀏覽器全流程（本工作階段無瀏覽器控制工具）。前端編譯與 API 契約不能替代 UI 驗收。
- 原 `Desktop/BiddingSystem-2A-integration` 對此工作程序仍唯讀；程式在可寫副本實作並已提交 GitHub。原 :5263／:5174 未重啟或部署此批；不再要求使用者重加資料夾，也未猜 DB 密碼或連正式 DB。
- **下一個驗證點**：用可取得隔離 DB 環境的開發程序啟動本批前後端，核對能力矩陣實際資料及不足原因；再以真正瀏覽器走上傳、解析、人工確認、估算、異常偵錯、決標回寫、歷史資料與操作紀錄。第一階段尚未全部驗收，不跳過第二階段宣稱結案。
- [完整變更／產品來源／驗證／回滾報告](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/blob/db40e874e3267faa62557a28d528522a186a1f2d/docs/MATERIAL_CAPABILITY_PHASE1_2026-11-22.md)；交接識別 `material-capability-phase1-20261122`。

# BiddingSystem-2A — AI 開發交接（動態更新）

> **單一事實來源**：以本儲存庫 `main` 的實際程式碼、配置及驗證結果為準。本文件是開發交接索引，不保證未讀取 GitHub 的 ChatGPT 對話自動同步。本文件建立日期：2026-11-15。

## 0. 新對話先讀這裡
1. 確認最新 `main` commit SHA 與日期。
2. 讀本檔、根目錄 `README.md`、相關原始程式碼與測試。
3. 依當前任務查閱前端 `frontend/` 與後端 `崴正/` 的實作。
4. 如文字紀錄與程式碼衝突，標註差異，先以程式碼與實測為準。
5. 修改完成後，與程式碼同次提交更新本文件的「變更紀錄」。

## 1. 已確認專案架構
以下依 2026-11-15 讀取的根目錄 README：
- 儲存庫：`YI-CHENLIN1204/BiddingSystem-2A`，預設分支 `main`，Private。
- 前端：`frontend/`，Vue 3 + TypeScript + Vite，獨立 SPA。
- 後端：`崴正/`，ASP.NET Core Web API、EF Core、PostgreSQL。
- 前端只以 HTTP API 呼叫後端；後端不負責 Razor Views 或 SPA 靜態檔案。
- README 本機預設：前端 `http://127.0.0.1:5174`，後端 `http://127.0.0.1:5261`；Vite 代理 `/api`。
- 資料庫連線由 `ConnectionStrings__DefaultConnection` 環境變數注入。
- 舊 Razor UI 已移除，不可用舊課程版、舊簡報覆蓋新架構。

## 2. 尚待程式碼逐項查證的功能
以下僅為交接時應查核的業務範疇，**不代表已實作或已驗收**：
- 標單匯入、工項字典對應／未對應清單
- 量價異常偵測、人工覆核／推翻判定
- 最終決標匯入與歷史成交回寫
- 系統參數、單位主檔與換算控制
- 操作紀錄、權限與部署配置

## 3. 重要禁則
- 不要預設目前使用 Z-score、LSTM、時間加權或任何舊演算法；須查最新 Service/計算路徑。
- 不要根據 2026-11-15 以前簡報推定最新功能，尤其已淘汰的技術。
- 不提交密碼、API key、連線憑證、真實標單或敏感合作資料。
- 未驗證不得寫「已完成」、「已上線」或「PASS」。

## 4. 驗證基線
根目錄 README 提供以下建置指令，但建立本交接檔時**未執行建置／E2E 測試**：

```powershell
dotnet publish .\崴正\崴正.csproj -c Release
cd .\frontend
npm ci
npm run build
```

正式環境需按實際設定驗證 CORS、API base URL 和部署埠號，不得照抄開發埠號。

## 5. 目前待辦
- [ ] 讀最新核心偵測 Service，明確記錄真正運算方法、參數及資料不足時的退避路徑。
- [ ] 查證單位管理、工項字典、歷史回寫、稽核與權限的現行實作和測試。
- [ ] 建立可重複執行的驗證清單／測試結果，並連結對應 commit。
- [ ] 重要功能變更後，更新本文件與 README（若架構／指令有變）。

## 6. 變更紀錄

### 2026-11-15 — 建立 AI 交接基線
- 新增 `docs/AI_HANDOFF.md` 和 `skills/bidding-system-2a/SKILL.md`，提供跨對話啟動與交接更新規則。
- 來源：當日 GitHub `main` 的 README 與儲存庫檔案清單。
- 驗證：完成儲存庫與 README 讀取；未執行程式編譯、部署、資料庫查核或功能測試。
- 後續：在下一次開發時先以現行程式碼補齊「目前實作」及確切測試結果。

### 後續更新格式（每次變更新增一節）
```markdown
### YYYY-MM-DD — 變更摘要
- Commit／Branch：
- 需求或問題：
- 修改檔案與行為：
- DB／API／權限影響：
- 驗證指令與結果（PASS／FAIL／未執行）：
- 已知問題與風險：
- 下一步：
- 已淘汰方案或決策修正：
```


### 2026-11-17 — 企業資料覆蓋率唯讀診斷
- Commit／Branch：`b0ff9589dc7003a87f9977fc6fa7dc1f5561d8e7`（資料診斷文件），`main`；本節更新另以後續 commit 記錄。
- 需求或問題：評估可信度前，先量測建案、材料實際樣本與現行 TopK 的資料限制。
- 修改檔案：新增 `docs/DATA_COVERAGE_BASELINE_2026-11-17.md`；本節更新 `docs/AI_HANDOFF.md`。**沒有修改演算法、API、Migration 或 DB 資料**。
- 實測（唯讀 SELECT）：總建案 10、有效建案特徵 10、具初步有效交易建案 8；23 筆歷史交易中 21 筆初步有效；2 種材料各涉及 8 個建案。
- 實際設定：TopK 8、MinimumSampleCount 5、MinimumSimilarityScore 0、P15/P50/P85、時間半衰期 3 年。
- 驗證：Supabase 唯讀資料庫查詢 **PASS**；編譯、E2E、估算逐案回測 **未執行**；不能據此宣稱演算法準確或企業正式驗收通過。
- 已知問題：材料種類僅 2；最終估算參考數、權重有效樣本數及區間涵蓋率仍未知；現行 TopK=8 限制最大參考數。
- 下一步：建立逐案留一回測與相似度過濾漏斗，評估是否調整 TopK 與分層估算；再設計公開資料與模擬資料隔離。
- 已淘汰方案：舊版 Z-score 不可用於描述現行方法；目前是相似建案、時間權重及加權分位數。


### 2026-11-17 — 資料來源歸屬更正（使用者確認）
- Commit／Branch：本節更新於 `main`（請以 Git 歷史中的本次文件提交為準）。
- 最新業務資訊：企業為新公司，已提供目前能提供的全部歷史建案；不得把「向企業再索取更多既有建案」列為既定解法。
- **重要更正**：此前 `docs/DATA_COVERAGE_BASELINE_2026-11-17.md` 中的 10 建案、23 歷史交易（21 初步有效）、2 材料、TopK=8，是**目前資料庫現況**，不是經核驗之企業真實資料基線。使用者指出 DB 中很可能主要是先前生成的測試資料；目前尚無資料來源欄位與逐筆驗證證據，不得據此宣稱實際企業樣本數、企業估算精度或可用於真實回測。
- 已在個人已提供之產學合作檔案庫發現多份 `最終標單` Excel 及明確標示為合成測試的 Excel；來源／版本／是否具完整價格／是否正式決標，須逐檔確認，不能僅依檔名判定。
- 原始企業工程標單限制：僅作內部分析及欄位、材料、單位、數量／價格級距參考；不得將公司、建案、供應商識別資訊複製進公開或模擬資料集。尚無正式對外發布授權。
- 分類策略：原始企業資料、外部公開資料、生成資料需紀錄來源並隔離；正式企業準確度測試只使用可確認來源與定義的企業樣本。未確認來源前禁止用 DB 10 案跑真實精度宣稱。
- 本次驗證：僅重新查找使用者先前提供的檔案清單與閱讀既有交接；**未執行新建案回測、未修改演算法或資料庫**。
- 下一步：建立檔案 manifest（檔案 hash、版次、來源證據、建案對應、數量／單價完整度、標單狀態），去重並識別最終決標紀錄；對企業有限真實資料改採工程知識／外部來源輔助及小樣本評估。


### 2026-11-19 — 畢業專題優先與 1A 資料來源邊界
- Commit／Branch：本次僅更新文件，`main`；具體 commit 以 Git 歷史為準。
- 決策：期限將近，優先完成畢業專題可展示、可驗證的版本；產學合作非必要整合工作延後。
- **畢專版沒有 1A 匯入的資料來源，也不得將 1A 當成啟動或運作前提。** 由 2A 使用者自行提供建案條件及上傳 Excel；歷史參考庫使用隔離的模擬資料／合法公開資料，不能混入企業真實資料。
- 既有程式路徑核對：`frontend/src/views/FinalBidView.vue` 可直接呼叫 `/Estimation/upload-final-bid`；`崴正/Controllers/EstimationController.cs` 具有相應 POST，交由 `FinalBidImportService`；畢專版可沿用此入口，不需 1A。這是程式碼存在性查核，尚非功能驗收。
- 產學版仍可按整合需求另行規劃 1A／其他組資料來源，但不能因此讓畢專版依賴跨組流程。
- 環境與資料安全：原始企業資料、學校合成資料分庫；任何模式旗標須經後端實作與測試，不可只因配置文字就視為隔離完成。
- 驗證：GitHub main 的路由、Controller 讀取 **PASS**；本次未執行建置、E2E、資料庫變更、Migration。
- 下一步：畢專模擬資料生成與獨立 DB → 手動標單匯入驗證 → 相似度／P15-P85 估算回測 → 異常與覆核驗證 → 系統分析報告同步更新。


### 2026-11-19 — 畢專離線模擬資料生成器 v0.1
- Branch：`main`；檔案提交：`3cbb109b`（生成器）、`4f2448f`（測試）、`488fbd59`（README）；本節另行提交。
- 需求：畢專優先，不依賴 1A、不讀企業原始資料，不對尚未隔離的 Supabase 正式資料庫匯入合成資料。
- 新增 `tools/graduation-synthetic/generate.py`、`test_generate.py`、`README.md`。
- 產物：離線 `projects.csv`、`historical_transactions.csv`、`manifest.json`；僅合成住宅、RC/SC 和示意材料，依建案切分 train/validation/test，seed 固定且輸出 SHA-256，具受控異常標籤。
- 實測：本機 Python 兩項 unittest **PASS**（同種子重現、檔案 hash/關聯、拒絕覆寫）；`--count 300 --seed 20261119` 實際產出 300 建案、600 材料交易。未執行正式 DB 寫入。
- 注意：材料用量係數／價格是**示範假設**，未由工程專家認證，不能宣稱真實工程可信度。合成資料格式為中介 CSV，尚未作為資料庫 seed 亦未連動 API/Excel 上傳。
- 風險與下一步：建立畢專專用隔離 DB 與主檔映射；完成安全匯入 dry-run、回滾、Excel 上傳與服務 E2E；學校時程優先，不擴大 1A 整合。
- 淘汰：舊 Z-score 不作為目前核心模型；畢專示範資料與產學企業資料嚴格分離。


### 2026-11-19 — 畢專 v0.2：離線標單 XLSX 輸出與 CI 驗證入口
- Branch／檔案 commit：`9ae6c5b3` `tools/graduation-synthetic/export_demo_excel.py`；`061d1113` `test_export_demo_excel.py`；`470d499a` README；`da77d89c` `.github/workflows/graduation-synthetic.yml`；本節另行提交。
- 功能：從 v0.1 生成的 `projects.csv`、`historical_transactions.csv`、`manifest.json` 校驗來源與 SHA-256，依指定建案或預設 test 建案輸出「材料明細」單張 XLSX；表頭匹配最新 `TenderExcelParser` 辨識的項次／項目名稱／規格／單位／數量／單價／金額。含拒絕覆寫、孤兒交易及跨 split 防護。
- 安全與相容性：純 Python 標準庫離線 OOXML 輸出，不呼叫 1A、API 或資料庫；不新增 Migration，也不變更原有服務、資料主檔或正式庫。可從 CLI 回傳建案屬性供手動測試。
- 驗證：GitHub 檔案建立成功；已配置 GitHub Actions Python 3.12 unittest + 300 案 smoke test，**CI 執行結果尚未確認**；未進行本輪獨立 ASP.NET API／前端 Excel 上傳 E2E，不能聲稱已完成系統整合。
- 已知限制：模擬用量與單價不是工程標準；材料 code `SYN-*` 尚需畢專 DB 專用主檔對應；生成資料含已標記異常，作為歷史基準或回測時須正確隔離，不能將異常混入正常參考。
- 下一步：先建立與企業正式資料庫物理隔離的畢專 DB 並確認資料表版本，再新增可回滾主檔映射／匯入流程，完整測試 Excel→匯入→工項對應→估算→異常判定；僅在隔離環境執行 DB 寫入。


### 2026-11-19 — 畢專不規則標單模擬器 v0.3.1 與人工教學
- Branch：`main`。主要 commits：`776860d8` 生成器、`7670ed3e` 自動測試、`ca4a9250` CI、`2043997c` README；本交接更新以目前 GitHub 歷史為準。
- 新增／修改 `tools/graduation-synthetic/generate_irregular_excel.py`、`test_irregular.py`、`README.md` 與 `.github/workflows/graduation-synthetic.yml`。
- 用 v0.1 產生的 CSV/manifest 做輸入，輸出 4 工作表（工程總表與三張明細）、不同表頭排列與偏移、同材料分拆跨表、非規則長名稱／規格、42 筆完整／未對應／缺數量／缺單價／缺單位測例，外加 JSON 逐列答案與 SHA-256。36 筆額外工程工項僅作標單測試，**不**混入歷史成交資料。
- 來源限制：先前個人檔案庫的企業 Excel 有檔案索引，但嘗試讀取原始位元組及 materialize 均無授權可用路徑；故生成格式為**以解析器程式行為推導的測試樣式**，不是逐份企業標單統計，不可宣稱與真實檔案完全一致。資料不得代表工程用量標準或真實單價。
- 本機測試：`python -m unittest -v test_irregular` 兩項 **PASS**，30 件來源產生 42 測例；OOXML ZIP 完整性 **PASS**，artifact_tool 可成功匯入 4 個工作表。GitHub CI 的實際 workflow 執行狀態尚未查核；ASP.NET `TenderExcelParser`／Vue 與 DB E2E **尚未測試**。
- 重要風險：`TenderExcelParser` 在部分欄位缺漏時可能使用數字位置推斷，需逐列對照 JSON，避免把單價誤判為數量；缺欄位測例不可當作有效歷史成交紀錄。
- DB/API/權限／Migration：此輪無變更，生成器只有離線檔案 I/O；不涉及 1A，不讀寫正式 Supabase。
- 下一步：確定畢專獨立 DB 後，用 `graduation-irregular.xlsx` 做人工上傳與解析回歸，確認缺欄位與未對應清單，必要時修正解析器；待能重新取得企業檔案位元組後再用真實範例校準不規則生成器。


### 2026-11-19 — 修正 Windows CP950 造成的測試 JSON 解碼失敗
- 程式 Commit：`3b20691de26424047510383941df35736e928872`（`main`）；本交接紀錄另行提交。
- 原因：使用者在 Windows Python 3.13、`feature/1d-api-integration` 分支，以 `origin/main` 的生成器檔案執行 8 項 unittest；7 項通過、1 項 ERROR：`test_irregular.py` 使用 `Path.read_text()` 預設 CP950 讀取生成器產出的 UTF-8 JSON，觸發 `UnicodeDecodeError`。
- 修正檔案：`tools/graduation-synthetic/test_irregular.py`，將 3 處 JSON `read_text()` 明確指定 `encoding='utf-8'`，與生成器輸出編碼一致。
- DB／API／Migration／權限影響：無；只修正離線單元測試的讀檔方式，不變動演算法和生成資料。
- 驗證狀態：使用者回報舊版 **7 PASS／1 ERROR**；新版本 GitHub 程式更新 **成功**；Windows 重跑和 CI 結果**尚待確認**，不可宣稱已通過。
- 使用者本機在 `feature/1d-api-integration`，有未提交的前端檔案修改；更新此單檔可用 `git fetch origin main` + `git restore --source=origin/main --worktree -- tools/graduation-synthetic/test_irregular.py`，不切分支、不覆蓋前端修改。
- 下一個驗證點：Windows 重新執行 `python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v`，確認 8 項全通過後再產生合成 CSV／不規則 Excel。


### 2026-11-19 — Windows Python 3.13 不規則標單測試重新驗證
- Branch／Commit：`main`，此更新為交接紀錄提交；前一程式修正 `3b20691de26424047510383941df35736e928872`。
- 使用者在 Windows PowerShell、本機分支 `feature/1d-api-integration`，透過 `git fetch origin main` 和 `git restore --source=origin/main --worktree -- tools/graduation-synthetic/test_irregular.py` 更新單一測試檔。
- 驗證指令：`python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v`。
- **實測結果 PASS：Ran 8 tests in 0.114s，OK。** `test_deterministic_multisheet_and_defect_truth`、`test_no_overwrite_and_tamper` 均通過，原 `cp950` 對 UTF-8 JSON 的 `UnicodeDecodeError` 已不再出現。另 `test_refuse_overwrite` 印出的 argparse error 屬預期拒絕覆寫測試，結果仍為 ok。
- 受影響範圍：本輪僅確認既有 Python 測試；無 DB、Migration、API、產學正式資料修改；尚未確認 300 案實際產出和 ASP.NET 匯入 E2E。
- 下一驗證點：執行 300 案合成資料命令，再執行 `generate_irregular_excel.py --source .\\graduation-data --output .\\graduation-irregular.xlsx --truth .\\graduation-irregular-truth.json`；檢查輸出摘要與 JSON 標準答案，正式 DB 隔離前勿匯入。


### 2026-11-19 — 畢專不規則標單 v0.4：由 42 筆提高為 360 筆
- 來源：使用者檢查實際生成 Excel，指出 42 筆工項明顯不足以代表一般詳細工程標單。
- 程式 Commit：`c69f68fb`（調整預設 360 筆／7 張工作表）、`59c4e3c6`（補齊不同工作表名稱欄位）、`7af33f60`（測試預期修正）、`25fb1d46`（依工程分類分配明細表）；文件 Commit `fe9b3d1c`，本交接另行提交。
- 修改 `tools/graduation-synthetic/generate_irregular_excel.py`、`test_irregular.py`、`README.md`；新命令 `--items 360`，合法範圍 60–2000，預設 360。**300 個歷史建案數與每份 Excel 的工程明細數量是不同維度**。
- 明細工作表由 3 張增至 6 張，另有 1 張摘要；加入多種工程類型、長名稱、樓層、棟別、規格變體；缺漏／未對應情境以小比例植入並在 truth JSON 記錄，支援各 sheet 計數與 SHA-256 校驗。合成標單包含額外工程工項，但仍**不會自動寫入歷史交易 CSV**。
- DB/API/Migration/權限：沒有更動；不依賴 1A，不讀寫 Supabase。
- 驗證：GitHub 程式碼提交完成；更新後的新測試尚**待使用者 Windows 執行驗證**，不能宣稱 v0.4 已通過；v0.3.1 的 8/8 PASS 不可直接套用到本次修改。應在既有 `feature/1d-api-integration` 分支以 `git fetch origin main`、`git restore --source=origin/main --worktree -- tools/graduation-synthetic` 取得測試器，不影響前端未提交修改。
- 未完成：真實企業標單內容目前無法重讀，比對比例與場景真實性尚未校準；ASP.NET 解析器 E2E 和畢專 DB 隔離未執行。若大規模測試觸發 parser 錯誤，應依 truth JSON 修正解析器，而非默默排除有缺漏的工項。
- 下一驗證點：重跑 Python 全部單元測試並產生 `graduation-irregular-v04.xlsx`；檢查回傳 `test_cases=360`、7 張工作表及對照 JSON；之後才進行隔離環境的 API 驗證。


### 2026-11-19 — 使用者 Windows v0.4 大型標單實測 PASS
- Commit／Branch：`main`，本節為驗證交接提交；v0.4 程式碼維持原版本未再修改。
- 本機背景：使用者仍在 `feature/1d-api-integration`，採 `git fetch origin main` 加 `git restore --source=origin/main --worktree -- tools/graduation-synthetic` 同步工具，避免合併 main 或覆寫未提交前端工作。
- 實測指令：`python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v`；**8/8 PASS**，`Ran 8 tests in 0.150s; OK`。防覆寫測試中印出的 `error: output directory must be empty` 是預期負面案例，該測試回傳 ok。
- 實際生成：`python tools/graduation-synthetic/generate_irregular_excel.py --source .\\graduation-data --output .\\graduation-irregular-v04.xlsx --truth .\\graduation-irregular-v04-truth.json --items 360`；成功產出 `SYN-00002` 的 7 工作表、360 測例。明細分布：結構體 15、基礎／地下 73、樓層 15、防水門窗外裝 97、機電消防弱電 93、室內裝修雜項 67（合計 360）；Excel SHA-256 `aff3245ce9ad7637bbc08dc23de9250d5ef57d19f53a68d239dd835a32cfd447`。使用者已呼叫 `Invoke-Item`，但沒有另附 Excel 人工檢視結果。
- 驗證界線：**PASS 僅涵蓋 Python 生成器單元測試與離線檔案產出**，不代表最新 ASP.NET 解析器上傳／工項對應／量價估算的整合或實務準確度通過。沒有使用 1A，也沒有資料庫寫入或 Migration。
- 已知差距：`generate.py` 歷史交易每案仍僅 2 種示意材料；v0.4 每案雖有 360 筆工程工項，多數工項來自共用模板，尚未依不同建案的結構、面積、樓層與工程範疇調整工作表分布。這是下一個改善點，不得稱為具真實工料表統計分布。
- 下一驗證點：建立可量化的跨建案多樣性測試，確認不同建案不只是參數／名稱改變；在隔離畢專 DB 完成前繼續禁止正式庫匯入。


### 2026-11-19 — 工作表 0 筆不可選取：修正於使用者開發分支
- 需求：使用者反映上傳真實或不規則 Excel 時「所有工作表都不能勾選」，阻擋畢專流程。
- 程式修正：**`feature/1d-api-integration` 分支** commit `7e272718c2da4de75a52757687df7851b13ba4ad`，修改 `frontend/src/components/WorkbookSheetPicker.vue`；**尚未合併 main**，不要在 main 找此檔案（main 目前沒有相同工作表選擇元件）。
- 根因（程式查證）：該分支在 preview API 的 `itemCount=0` 時前端設 `:disabled="sheet.itemCount === 0"`；但後端 `崴正/Controllers/TenderImportController.cs` 的 itemCount 來自 `TenderExcelParser.Parse`，僅代表解析器成功擷取的筆數，**不能證明整張表沒有明細**。
- 變更：移除 0 筆工作表的強制 disabled，提供「勾選已辨識明細」快捷鍵與 0 筆警示；允許使用者手動選取未辨識工作表進行唯讀 inspect。保留多表明確選擇原則，不自動把未辨識內容當正式成交資料；`FinalBidImportService` 仍拒絕 0 筆匯入。
- 驗證：已在 GitHub 該 feature 分支重新讀取元件原始碼，確認 disabled 移除與警示新增 **PASS（靜態查核）**；前端 `npm run build`、實際預覽／檢查／回寫 E2E **尚未測試**；也尚未證明原始 Excel 的 0 筆解析原因。
- 影響：前端 UI；**不改 DB、Migration、API 或 1A**；正式交易安全限制維持。
- 下一驗證點：在本機 `feature/1d-api-integration` 取回單檔、執行 `npm run build`，再上傳 `graduation-irregular-v04.xlsx`；若顯示 0 筆仍須檢查 `TenderExcelParser` 表頭辨識與工作表資料，而不能直接當成有效匯入。
- 已淘汰判定：「解析 0 筆 = 工作表不可選」已取消；改由人工選取＋唯讀診斷＋匯入階段嚴格確認。


### 2026-11-21 — 11/25 畢專交付加速：資料真實性、可讀性、合成回測
- 緊急期限：使用者需於 **2026-11-25** 前提供測試標單與精準度；不再將非必要產學整合列為目前優先。
- 已查詢官方公開來源：工程會工程價格資料庫規則、水利署電子標單格式規定、雲林縣學校預算資料審查規定；可供建立資料來源與工項分類規範，但**尚未確認取得、清洗或核驗足以支撐真實估算精準度的多案原始 Excel**。工程預算與決標／結算資料不可混為一談。
- 修改 `tools/graduation-synthetic/generate_irregular_excel.py`：Commits `94604c35`（欄寬／列高）、`df499758`（儲存格換行樣式）、`43cb867d`（依地下室與樓層限制部分工項）；`test_irregular.py` commits `d536fa4a`（OOXML 欄寬與換行驗證）、`0ce7666e`（有無地下室驗證）。
- 新增 `tools/graduation-synthetic/evaluate_synthetic_holdout.py` Commit `2266671f`，修正不同單位量測不得混算 `da0a8e84`；新增 `test_evaluate_synthetic_holdout.py` Commit `7a26a007`。輸出 `SYNTHETIC`、`OFFLINE_PROXY_NOT_API`，按建案 train/test，排除植入異常，輸出加權分位數對加權常態模型的數量區間回測；**代理相似度不等同正式 C#，不得稱為真實工程精度**。
- 使用者前端功能分支 `feature/1d-api-integration`：`frontend/src/components/WorkbookSheetPicker.vue` commit `c09fea4b`，長工作表名稱、工項／規格、警示文字換行與橫向捲動；**尚未合併 main**。既有本機前端 package 修改不得覆寫。
- 新增 11/25 交付規範文件 `docs/GRADUATION_DELIVERY_2026-11-25.md` Commit `a339656e`；更新工具 README Commit `8f2c6e39`。
- 驗證：GitHub 相關檔案已成功提交；**本次 v0.5 變更的 Python unittest、Excel 實開、前端 npm build、後端解析器 E2E、資料庫隔離及真實材料精度皆尚待實測**。不得以先前 v0.4 的 8/8 PASS 代替此次驗證。沒有 DB、Migration、正式資料修改。
- 不足與風險：只有來源標記 `SYNTHETIC` 的 300 建案／600 筆材料交易，每案兩種核心材料；360 筆標單工項多數仍是額外虛構測試明細，未與相同案的真實歷史價量對齊，不能用它當企業精度真值；缺真實匿名標單與建案屬性時，11/25 只能交付合成資料回測和資料可用性限制。
- 下一驗證點：在本機同步 `tools/graduation-synthetic`；重跑全部 Python 測試；輸出全新 `graduation-irregular-v05.xlsx` 和 `graduation-synthetic-holdout-v01.json`，檢查生成量與模型指標。取得具有合法用途的真實建案資料後，另外進行獨立真實資料保留樣本回測。
- 淘汰／未採用：目前正式用量估算仍為相似建案加權分位數；**未將常態分布正式替換進 C# 估算 API**；只有離線合成比較基準，不得宣稱正式系統已改採常態分布或其效能較佳。


### 2026-11-21 — 11/25 衝刺補充：全面長欄位與公開範本
- `feature/1d-api-integration` branch commit `1a34875c`：修改 `frontend/src/views/AnalysisCaseDetailView.vue` 的工項和分析結果表格，設定水平捲動、長欄位折行、項目名稱最小寬度與數字不折行；先前同分支 `c09fea4b` 已修正 `WorkbookSheetPicker.vue`。**兩檔均未合併 main**，前端建置／實際畫面驗證仍未完成。
- `main` commit `294a532d`：`docs/GRADUATION_DELIVERY_2026-11-25.md` 新增國立中興大學總務處官方 Excel/ODS 工程預算書下載頁、臺中市住宅發展工程處總標單範本。這些是**格式參考資料**，不是已取得的實際工程決標數量資料。
- 尚未解決：缺實際可核對來源和建案特徵的多案價格／用量資料，不能回報真正的企業精準度；最新 v0.5 Python 測試及實際編譯結果仍尚未取得。
- 下一步：以 Windows 執行新的 unittest、v0.5 XLSX、離線 holdout 指標，再收集可授權使用且已去識別化的真實詳細標單做外部驗證。


### 2026-11-21 — Windows v0.5 測試 10/12，修正兩個生成器回歸
- 用戶端：Windows `C:\\Users\\User\\Desktop\\BiddingSystem-2A`，執行 `python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v`；結果 **Ran 12 tests in 0.526s; FAILED (failures=2)**。其中 `test_refuse_overwrite` 顯示的 argparse error 為預期測試且回傳 ok。
- 失敗 1：`test_deterministic_multisheet_and_defect_truth` 斷言任一工作表欄寬 >=28；`工程總表` 沒有明細表表頭，預設寬度均 17。修正為摘要表預設欄寬 38，正式明細表仍按工項名稱、規格、備註及價量欄設定寬度。
- 失敗 2：`test_no_basement_project_does_not_invent_underground_items`；無地下室時僅過濾「地下室、連續壁、擋土、基坑」，卻漏掉「地下停車場標線及標誌」等名稱。統一將「地下」納入過濾（只在 `basement_floor_count=0` 適用），對有地下室建案保留相關工程。
- 程式 Commit：`2abb1179510ed5d55fe3e904f774746a2fad97e9`（`main`）；修改 `tools/graduation-synthetic/generate_irregular_excel.py`；本交接另行提交。未修改原測試斷言，無 DB、Migration、API 或正式估算演算法變更。
- 目前驗證：**修正前 10 項 PASS／2 項 FAIL**，修正已提交；修正後的 Windows unittest 尚未回報，不可聲稱已修復通過。正式企業資料來源、真實精準度與 ASP.NET 解析器 E2E 仍未驗證。
- 下一步：在 feature 分支安全同步 `git fetch origin main`；`git restore --source=origin/main --worktree -- tools/graduation-synthetic/generate_irregular_excel.py`；重跑 12 個測試，全部 OK 後才產生新 v0.5 Excel 和合成 holdout JSON。


### 2026-11-21 — Windows v0.5 生成器修正後實測 12/12 PASS；資料集目錄缺失
- Commit／Branch：本節為 `main` 交接紀錄；前一修正 `2abb1179510ed5d55fe3e904f774746a2fad97e9`。
- 使用者於 `C:\\Users\\User\\Desktop\\BiddingSystem-2A` 執行 `git fetch origin main`、`git restore --source=origin/main --worktree -- tools/graduation-synthetic/generate_irregular_excel.py` 後重跑全部測試。
- **實測 PASS**：`Ran 12 tests in 0.968s; OK`，含 `test_deterministic_multisheet_and_defect_truth`、`test_no_basement_project_does_not_invent_underground_items` 均為 ok。防覆寫測試顯示 argparse error 是預期被拒案例，該測試仍為 ok。
- 後續 CLI 產生 `graduation-irregular-v05.xlsx` 與 `graduation-synthetic-holdout-v01.json` **均失敗**，原因為 `[Errno 2] No such file or directory: 'graduation-data\\manifest.json'`，即目前工作目錄沒有指定的合成 CSV manifest。此前成功的資料來源工作路徑為另一個 `C:\\Users\\lyich\\OneDrive\\桌面\\BiddingSystem-2A`，不可假定現有檔案已同步或跨電腦存在。
- 指引：先執行 `python tools/graduation-synthetic/generate.py --count 300 --seed 20261119 --output .\\graduation-data`，再重跑 Excel 與 holdout 命令；輸出檔不得覆寫，如目錄已有資料改用新名稱並同步調整 `--source`。
- 目前狀態：Python 單元測試 **PASS 12/12**；v0.5 360 列 XLSX 生成 **未完成（缺來源檔）**；離線合成 holdout 報告 **未完成（缺來源檔）**；ASP.NET API E2E、實際企業資料精準度 **尚未測試**。無 DB／Migration／API 修改。
- 下一步：在目前 Windows 工作目錄先生成 300 建案／600 合成交易與 manifest，再產出 XLSX、JSON 標準答案、離線分位數／常態基準比較結果；記錄真實執行數值供 11/25 報告。


### 2026-11-21 — 畢專合成資料 v2.0：從每案 2 材料改為同源多工項歷史資料（11/25 優先）
- 用戶確認舊 `generate.py` 每案只有 2 筆交易不符合「完整且條件各異的建案」；另使用者本機已成功產生 300 案／600 交易及 360 筆標單，並成功跑出**舊版離線合成**結果：加權分位數代理 MAPE 9.246%、P15-P85 涵蓋率 54.22%；加權常態代理 MAPE 9.133%、涵蓋率 51.81%，有效測試材料案例 83。**僅適用簡化合成資料，不是系統正式精準度／真實工程精準度。**
- 新增 `tools/graduation-synthetic/generate_full_project_dataset.py`（`8fb0354b`、`39f992a0`、`c753570f`）：繼承舊案基本特徵，加入合成立面工法、裝修等級、基地條件；擴充不同工程類別模板；RC/SC、地下室與樓層條件控制工項是否出現；示例量價及不同工種施工時間產生，**所有數值仍是 SYNTHETIC，不含企業價格及工程真實係數**。
- 新版資料三表：`projects.csv` 建案特徵、`project_boq_rows.csv` 每案各工項詳細來源、`historical_transactions.csv` 每工項 1:1 對應交易；共用 `synthetic_transaction_id`，不再將獨立造出的 360 測例誤當真實歷史；SHA-256 manifest 確保來源一致，train/validation/test 仍按「建案」切分。
- 新增 `tools/graduation-synthetic/export_full_project_excel.py`（`9dfc4c91`）：從**相同建案同一批來源工項**匯出多工作表 Excel + truth JSON；少量缺單價／數量／單位僅在 Excel 展示，來源歷史交易保留完整值。預設選 test 案，可指定 --project-id；不寫 DB。
- 新增 `tools/graduation-synthetic/evaluate_full_project_holdout.py`（`0a105cc2`）：用 train 案推估 test 案的相同材料代碼，**先彙總同建案同材料所有樓層工項**成一個獨立樣本，避免資料洩漏與虛增樣本；比較離線加權分位數代理、加權常態代理的每工項 MAPE、涵蓋率、區間評分，**不等於正式 C# 模型 E2E**。
- 新增 `tools/graduation-synthetic/test_full_project_dataset.py` commit `378aaefd`；更新 `.github/workflows/graduation-synthetic.yml` commit `a9d20367`（CI 也測完整資料集、Excel、回測）；更新 `tools/graduation-synthetic/README.md` commit `eac426e7`。
- 變更界線：舊版 2 材料生成器未刪除，以免破壞舊測試；v2 需輸出**新資料夾** `graduation-data-v2`，舊 `graduation-data` 不覆寫也不直接兼容。未修改 PostgreSQL、Migration、現有 ASP.NET API 或正式估算邏輯。
- 驗證狀態：上述新增程式及測試已提交 GitHub **但尚未在使用者 Windows 執行新版測試，CI 也未核實結果**；不能以舊版 12/12 PASS 取代新測試。公開資料之工程細目類別/價格規範已查考工程會與水利署，但 **v2 模板與數值目前並非真實標單抽取或經工程師校準**。
- 下一驗證點：使用者在 feature 分支安全取回工具目錄，跑全套 unittest，生成 300 案 v2；輸出與對應歷史一致的 Excel／JSON 和新 holdout；確認 `boq_line_count=transaction_count`、歷史材料碼種類 >2、各建案明細數與範圍差異。取得可合法使用的住宅工程詳細標單後，另做真實資料驗證；**未來如需進 DB，先定獨立畢專 DB、主檔映射、Migration、回滾方案**。
- 已淘汰：舊 300 案／600 交易不再作為「廣泛工程標單數量精度」主要展示資料；常態分布仍只在離線對照模型，沒有替換正式估算 API。


### 2026-11-21 — 目標重定義：完整測試環境（歷史 DB、未見過的待估標單、正式估算與視覺化）
- 使用者明確表示：生成模擬資料的目標是讓 2A **實際跑起來且驗證演算法可行性**，不是為了得到一個好看的「精準度」數字；歷史建案與目標標單要完整、條件多樣，11/25 報告應能以圖表證明而非只列數值。
- 現有 v2 多工種完整資料生成、歷史/明細 1:1 對應與 test-only Excel 匯出仍是正確方向；但 **CSV 生成 ≠ 資料已進 ASP.NET 的 PostgreSQL**。不可宣稱應用 E2E 已完成。
- 新增 `tools/graduation-synthetic/build_evidence_report.py` Commit `32a3932d`，用具來源驗證的 v2 `projects.csv`、`project_boq_rows.csv`、`historical_transactions.csv` 和 `evaluate_full_project_holdout.py` 結果，輸出離線 HTML 圖表：P50 MAPE、P15-P85 涵蓋率、工項工程類別筆數、RC/SC 建案數與資料量，含醒目 `SYNTHETIC / OFFLINE_FULL_BOQ_PROXY_NOT_CSHARP_API` 標記，禁止冒稱真實精度。
- 新增 `tools/graduation-synthetic/test_build_evidence_report.py` Commit `c913acdd`：檢驗圖表包含必要資訊、來源和跨檔案筆數一致，偽造 `REAL` 標記會被拒絕。
- 新增 `docs/GRADUATION_E2E_ACCEPTANCE_2026-11-25.md` Commit `a492e59a`：明確分隔 train 歷史基準、validation 模型選型與 test 目標標單，列出與 ASP.NET 實際 DB 主檔、歷史審核流程、`AnalysisCaseController` 上傳接口、`DetectionCategoryRangeService` 可估算細項政策的差異及驗收證據。
- DB 現況（以 feature 分支程式碼查證）：`ApplicationDbContext` 需要 `project_master`、`material_master`、`unit_master`、`historical_transaction`、`detection_category` 等表；`FinalBidImportService` 的新標單先是 Draft，不直接發佈歷史資料，並須完成各材料碼的 `MaterialId` / `UnitId` 映射。尚無獨立畢專 PostgreSQL 的連線／Migration 驗證，**本輪不碰產學正式 DB**。
- 目前狀態：v2 新版程式碼、離線 HTML 圖表、測試與交付規範 **均已提交**；新腳本 Windows、GitHub CI、C# E2E 與資料庫匯入 **尚未實測**；不能以先前 12/12 PASS 當新版本驗收，不能將舊 9.246% MAPE 當完整模型的精度。
- 下一步優先：Windows 執行全套工具測試 → v2 多類別 300 案資料 → 確認 test-only Excel → v2 holdout JSON → HTML 圖表；下一組將對**隔離畢專 PostgreSQL**做唯讀 schema preflight、設計可回滾種子匯入、依正式 AnalysisCase 進行真實端到端估算。


### 2026-11-21 — 本機隔離畢專 PostgreSQL 與完整 train-only 種子程式
- 核對 feature 分支 `崴正/Data/ApplicationDbContext.cs`、`FinalBidService.cs`、`FinalBidImportService.cs`、`UnitConversionService.cs`、`TwoAScopePolicy.cs`：正式模型需 project/material/unit/detection category mapping；新決標匯入先 Draft 不直接寫歷史；`TwoAScopePolicy` 目前只開放結構體工程「鋼筋」「混凝土」正式數量異常模型；`SiteArea` 已列入相似度，v2 生成器先前缺 `site_area`，已補上。
- 程式新增：`tools/graduation-synthetic/prepare_training_seed.py` commits `955def44`、`704ca0f8`：驗證 v2 manifest 及 3 份 CSV、BOQ/交易 1:1 關係，僅產 train 專用建案＋材料＋完整標單＋交易 JSON，禁止混入 validation/test。
- 新增 `tools/graduation-synthetic/compose.graduation-db.yaml` commit `90ecb853`：本機專用 PostgreSQL 17，監聽 `127.0.0.1:55432`、DB `bidding2a_graduation`，獨立 Docker Volume，密碼由 `GRADUATION_DB_PASSWORD` 注入，不共用產學 DB。
- 新增 `tools/graduation-synthetic/GraduationSeed/GraduationSeed.csproj` / `Program.cs` commits `06708f8f`、`dd24588f`、`80f5ebf0`、`b13cc451`；嚴格拒絕非本機 host、不同 DB 名/port/帳號；以 EF `EnsureCreated` 建立一次性隔離資料表，拒絕非空 DB，單一 transaction 寫入 train-only 建案、單位/材料主檔、鋼筋／混凝土偵錯細項、測試估算設定、完整 `FinalBidItem` 與其歷史交易（含 FK）、並保留異常標記。隔離合成環境的 `Approved` 不代表真實企業歷史核准，也不能取代正式 Migration。
- 增加 `tools/graduation-synthetic/test_prepare_training_seed.py` commit `d09feec2`，驗證 holdout 隔離、價量與來源一致；CI workflow commits `6c1f7080`、`ae08b5f6` 加入 seed 驗證與 .NET 10 build 工作；正式 CI 執行結果尚未核實。
- 修正 `generate_full_project_dataset.py` 加入 `site_area` commit `a089a9ea`、校正核心鋼筋項目名稱為可辨識材料而非工資加工項 `9e37de68`；`test_full_project_dataset.py`、`test_build_evidence_report.py` 配套 site area commits `14c8e758`、`b99c24cc`。
- 修改 `generate_irregular_excel.py` / `export_full_project_excel.py` commits `7ad6608f`、`36efea58`，不再為不同建案硬加無資料的空白工程明細分頁；仍保留多格式表頭與 truth JSON。
- 新增 `docs/GRADUATION_E2E_ACCEPTANCE_2026-11-25.md` commit `a492e59a`，後續更新 `c13efdc3`；更新 `tools/graduation-synthetic/README.md` commit `c02c9a96`，列出分階段執行及 DB 不得污染產學資料庫的安全檢查。
- **實測狀態**：使用者舊版 v0.5 Python unittest 12/12 PASS、舊 300 案／600 交易成功、舊 360 行 Excel 已生成、舊代理回測 83 項結果已取得；**v2 新功能 Python unittest、PostgreSQL Docker 啟動、`GraduationSeed` C# 編譯與執行、正式 ASP.NET AnalysisCase API、HTML 圖表均尚未實測**。不可混淆。
- 下一個驗證點：使用者同步 `tools/graduation-synthetic`、完整 Python unittest、生成 v2 300 建案及 train-only seed，先確認樣本多樣性與來源，再於**全新本機隔離 DB**執行 dotnet build + 匯入，最後 test-only Excel 上傳正式 AnalysisCase API 和視覺化。


### 2026-11-21 — 11/25 畢專範圍收斂：數量估算與價格異常偵錯，並修正 v2 測試型別
- 使用者明確要求**停止擴張功能**；本次畢專只驗證 (1) 相似建案工程數量合理範圍／數量異常、(2) 價格異常偵錯。非結構工項依舊保留在真實感標單和歷史資料中作為解析／建案組成／未來擴充依據，**不等於全部工項都已提供正式估算與偵錯**。
- 核對 `feature/1d-api-integration`：`崴正/Services/TwoAScopePolicy.cs` 目前將正式數量偵錯限制在「結構體工程／鋼筋與混凝土」；`PriceEstimationMajorCategories` 也只包含「結構體工程」。需要優先實測現有 `DetectionCategoryRangeService`、`TwoAQuantityAnalysisService` 及 `MajorCategoryPriceRangeService`，不要自行宣稱其它工程項目已在正式範圍。
- 使用者 Windows 實測 `python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v`：**Ran 17 tests in 5.379s; FAILED (failures=1)**，其他 16 項 PASS。
- 失敗根因：`tools/graduation-synthetic/test_full_project_dataset.py` 在直接取得 `generate_full()` 的 in-memory 建案時，以字串 `"0"` 比對 `basement_floor_count`；但 `generate.py` 中該欄原型別是 `int`，故 `0 == "0"` 永遠為 false，並非測試已證實生成器完全沒有無地下室建案。該測試其餘欄位是 CSV 讀回時可能成字串，混用型別造成誤判。
- 修正 commit `f33cf801158d6e738123d1a7ee831f6029a99c3c`（`main`）：只改 `test_full_project_dataset.py` 斷言為 `int(p["basement_floor_count"]) == 0`，不更動合成數量、工程條件、估算算法及 DB。未刪除斷言，不放寬「必須有無地下室案例」要求。
- **修正後 17 項測試仍待 Windows 重跑**；不能宣稱已全數 PASS。v2 大型資料生成、實際隔離 PostgreSQL 匯入、正式 API 數量估算／價格異常端到端和圖表仍未取得本機實證。
- 下一驗證點：使用者維持 `feature/1d-api-integration`，先 `git fetch origin main` 和 `git restore --source=origin/main --worktree -- tools/graduation-synthetic/test_full_project_dataset.py`，重跑全部 Python unittest。通過後只先生成 v2 歷史資料與 test-only Excel，確認兩種偵錯的資料庫可用性；不得連線產學正式 Supabase。
- 11/25 報告核心圖形：數量預測 P15/P50/P85 與真值、P50 誤差與區間涵蓋率；價格異常的歷史大項單位面積金額分布與待估值位置及判定；所有數據註明是合成模擬或已驗證真實來源，不將離線代理精度稱為正式 C# 系統精度。


### 2026-11-21 — Windows v2 全套 Python 測試 17/17 PASS
- 用戶於 `C:\\Users\\User\\Desktop\\BiddingSystem-2A` 執行 `git fetch origin main`，Git 顯示 `origin/main` 由 `8f32767` 更新至 `d64d528`，但亦顯示 `fetching ref refs/remotes/origin/main failed: incorrect old value provided`，屬遠端追蹤參照更新衝突；不可單憑 fetch 文字認定本機 ref 一定完整更新，且不建議清理／重設使用者尚未提交工作樹。
- 用戶後續執行 `git restore --source=origin/main --worktree -- tools/graduation-synthetic/test_full_project_dataset.py` 無錯誤；`python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v` 顯示 **Ran 17 tests in 5.346s; OK（17/17 PASS）**，含 `test_variety_history_alignment_and_scope`、`test_export_same_history_and_holdout`、`test_seed_contains_training_data_only_and_exact_lineage`、`test_verified_report_contains_comparison_and_category_figures`。
- 防覆寫 `test_refuse_overwrite` 列印的 argparse error 為刻意觸發的正常拒絕流程，結果為 `ok`。
- 版本與驗證界線：這證明 Python 單元測試通過，但**尚未證明 300 案 v2 資料實際在用戶工作目錄產生、資料已匯入隔離 PostgreSQL、C# 種子程式已建置、正式 API 數量／價格偵錯 E2E 通過，或真實工程精度已驗證**。
- 畢專目標已收斂：只驗證數量估算與價格異常偵錯，完整多工種資料仍用來驗證標單多樣性及歷史參考，不擴充其它估算模型。
- 下一驗證點：在目前工作目錄生成 `graduation-data-v2`；由來源資料產出 `graduation-v2-train-seed.json`，另以 test 建案產生 `graduation-v2-project.xlsx`、`graduation-v2-project-truth.json`，確認來源、筆數與 train/test 隔離；再做隔離 DB 的 .NET 建置／匯入，最後通過正式服務 API。
- 此段僅紀錄測試結果與下一步，**無新程式碼、DB 或 Migration 修改**。


### 2026-11-21 — v2 test-only Excel 首次實際輸出成功
- 使用者已執行 `python tools/graduation-synthetic/export_full_project_excel.py --source .\\graduation-data-v2 --output .\\graduation-v2-project.xlsx --truth .\\graduation-v2-project-truth.json`，CLI 正常完成、無錯誤。
- 實測輸出：`project_id=SYN-00002`、`detail_rows=574`、`worksheets=7`。詳細列數：結構體明細 12、基礎及地下工程 68、樓層材料明細 43、防水門窗及外裝 138、機電消防及弱電 194、室內裝修與雜項 119；加總 574。輸出 Excel SHA-256 `0b240db9282116318e5913b44a7d770faa3090c43ec1864688711e6f48bbc14c`。
- 使用者先前一條 `prepare_training_seed.py` 輸出僅貼最後的 `disclaimer=Disposable localhost graduation database ONLY...`，尚未貼出其 train 筆數，故不能宣稱資料庫成功匯入。
- 本次僅新增驗證交接紀錄；生成器及 C# 程式未修改。舊 17/17 PASS 仍成立於使用者先前版本；本次是獨立 Excel 匯出成功，尚無正式解析器 API E2E。
- 下一驗證點：用戶 Windows 先執行 `dotnet build tools/graduation-synthetic/GraduationSeed/GraduationSeed.csproj`，確認 C# 編譯；確認 Docker 環境後才建立獨立 localhost Postgres `127.0.0.1:55432`，限制只匯入 train-only，禁止連正式 Supabase。待估算標單 `SYN-00002` 應保持 test-only，絕不可先寫入歷史基準。
- 畢專主要範圍：結構體工程鋼筋／混凝土的數量估算，以及結構體工程大項的價格異常偵錯；不擴大其他工項模型。報告仍需實際 API 結果與標準答案比較。


### 2026-11-21 — Windows Docker /.NET CLI 編譯通過，修正 EF Relational 版本衝突
- 使用者執行 `docker version`：Docker Desktop 4.91.0、Client 29.8.0 / Server Engine 29.8.0，Context desktop-linux，**Docker Engine 運作正常**；尚未啟動本專用畢專 PostgreSQL 容器。
- 使用者執行 `dotnet build .\\tools\\graduation-synthetic\\GraduationSeed\\GraduationSeed.csproj -c Release`：ASP.NET `崴正 net10.0` 與 `GraduationSeed net10.0` 均成功，**0 errors / 1 warning (MSB3277)**；相依 `Microsoft.EntityFrameworkCore.Relational` 10.0.4.0 與 10.0.5.0 衝突，解析至 10.0.4.0。使用者執行 `Test-Path .\\graduation-v2-train-seed.json` 回傳 `True`。
- 程式修正：`main` commit `c2a5f55bee16836675a8add2dd372cbe79a0bbe2`，`tools/graduation-synthetic/GraduationSeed/GraduationSeed.csproj` 增加直接套件 `Microsoft.EntityFrameworkCore.Relational` 10.0.5，對齊 `崴正/崴正.csproj` 的 EF Core 10.0.5；沒有升級其它套件、沒有 DB schema 改動。
- **修正後建置結果尚待用戶重跑**，不可宣稱 warning 已經排除；若仍有衝突需要先檢查 `dotnet list ... package --include-transitive` 再處理，避免不必要地修改主程式套件。
- 下一驗證點：`git fetch origin main` + 在 feature 分支只 restore `tools/graduation-synthetic/GraduationSeed/GraduationSeed.csproj` + `dotnet build ... -c Release`，確認警告消失；然後設置本機畢專資料庫密碼，透過 `compose.graduation-db.yaml` 啟動 **127.0.0.1:55432/bidding2a_graduation**，以 Docker health status 驗證。**尚未有匯入成功的證據，不可碰正式 Supabase**。
- 11/25 目標僅數量估算／價格異常偵錯；現有 Python 17/17 PASS 及 Excel 574 行輸出已完成，C# 實際 DB 匯入、API 數量/價格 E2E 尚未測試。


### 2026-11-21 — 本機隔離 PostgreSQL 容器實際啟動、Seed 專案無警告建置成功
- 用戶在 Windows `C:\\Users\\User\\Desktop\\BiddingSystem-2A` 執行 `dotnet build .\\tools\\graduation-synthetic\\GraduationSeed\\GraduationSeed.csproj -c Release`，輸出 `崴正 net10.0 成功`、`GraduationSeed net10.0 成功`、3.3 秒內 `建置 成功`；本次沒有出現之前的 EF Core Relational MSB3277 警告。此為編譯成功，不代表 DB 寫入已成功。
- `Test-Path .\\graduation-v2-train-seed.json` 回傳 True（本輪先前已確認）。用戶執行 `docker version` 顯示 Client/Server 29.8.0，Docker Engine 運作正常。
- 用戶 `docker compose -f tools/graduation-synthetic/compose.graduation-db.yaml up -d` 成功下載 postgres:17-alpine，創建 `graduation-synthetic_graduation_pgdata`、`graduation-synthetic_default`，啟動 `graduation-synthetic-graduation-postgres-1`。隨後 `docker compose ... ps` 顯示 `Up (healthy)` 且只映射 `127.0.0.1:55432->5432/tcp`。
- **安全事項**：用戶將此本機測試庫初始密碼直接輸入並貼於對話內容；交接文件**不記錄密碼明文**。下一步先使用容器內 `psql` 的 `\\password graduation` 交互命令輪替，避免明文出現在指令歷史。更新本機 `GRADUATION_DB_PASSWORD` 與 `GRADUATION_DB_CONNECTION`，**只改 PowerShell 環境變數不會更新已初始化的 Postgres 角色密碼**。
- 現況：ASP.NET 與 GraduationSeed 編譯通過、Docker local Postgres **healthy**；`train` 歷史 JSON **尚未匯入**，`AnalysisCase` 的數量估算和價格異常偵錯 API **尚未端到端驗證**，不得標示為完成。
- 下一驗證點：先輪替本機測試庫密碼；以 `GRADUATION_DB_CONNECTION` 本機限定連線執行 `dotnet run --no-build -c Release --project tools/graduation-synthetic/GraduationSeed/GraduationSeed.csproj -- --seed-json .\\graduation-v2-train-seed.json`；紀錄真實建案數、材料數、歷史交易與決標明細筆數及錯誤，之後再啟動 ASP.NET 使用相同隔離連線進行數量估算/價格異常 E2E。不得指向 Supabase。
- 本段是執行結果與安全交接紀錄，沒有資料庫 Migration/正式程式碼改動。


### 2026-11-21 — 畢專 train-only DB 匯入成功 210 案／99,959 筆；待估標單分類相容性修正
- 使用者在已啟動的 **localhost:55432 / bidding2a_graduation** 隔離 PostgreSQL 執行 `dotnet run --no-build -c Release --project .\\tools\\graduation-synthetic\\GraduationSeed\\GraduationSeed.csproj -- --seed-json .\\graduation-v2-train-seed.json`，CLI **成功退出**並輸出 `{"Database":"bidding2a_graduation","DataOrigin":"SYNTHETIC","TrainingProjects":210,"MaterialMasters":100,"HistoricalTransactions":99959,"FinalBidRows":99959,"ExcludedHoldoutProjects":90,"TrainingOnly":true}`。這是 C# 實際 DB 寫入階段的首次通過；尚未由 SQL 二次核對筆數，亦未呼叫正式估算 API。
- 用戶最新 .NET 10 Release build 成功且未顯示 MSB3277 版本警告；Docker PostgreSQL healthy。**不可再次執行 DB seed**，設計上拒絕重複匯入，亦不可先將 test 案 SYN-00002 寫進 train DB。
- 查證正式 feature 分支 `AnalysisCaseController.cs` 的路由是 `api/analysis-cases`（複數），`POST api/analysis-cases/from-upload`；數量端點 `POST api/analysis-cases/{id}/versions/{versionId}/quantity-analysis`；價格端點 `POST api/analysis-cases/{id}/versions/{versionId}/major-price-analysis`；價格 readiness `GET api/major-category-price/readiness`；數量設定 `GET api/detection-category/readiness`。
- 查證 `AnalysisCaseService.ValidateUploadStructure` 要求每一詳細列都能由 `Category1/2/3` 辨識工程大項，`TenderExcelParser` 接受標頭為 `工程大項` 的逐列分類；原 v2 Excel 沒有明確分類欄，很可能導致整張待偵錯標單被拒絕。
- 修正 `tools/graduation-synthetic/export_full_project_excel.py` commit `16ac12d0`：新輸出的每一工程列增加 `工程大項` 欄，將合成較細工程分類映射到 `TwoAScopePolicy` 可接受的大項名稱；原始 `major_category` 仍留在 CSV / truth，新增 `analysis_major_category`。**不擴充正式估算政策**，仍僅數量鋼筋/混凝土與價格結構體工程。
- 修改 `tools/graduation-synthetic/test_full_project_dataset.py` commit `760267ca`：測試 truth 中每筆輸出列的有效工程大項。原已生成的 `graduation-v2-project.xlsx` 不受 Github 修改影響，需要**另以新檔名重新生成**待估標單（如 `graduation-v2-project-ready.xlsx`）；不可覆寫既有證據。
- 驗證界線：輸出修正已提交 GitHub **但未取得用戶 Windows 回測或 ASP.NET API 上傳結果**；不得宣稱正式數量/價格偵錯已可成功執行。完整 train DB 已匯入；圖表與獨立 test 案尚需 API E2E。
- 下一步：同步新版 exporter/test，重跑 Python unittest → 新檔名輸出有 `工程大項` 的 test-only Excel → 以 `ConnectionStrings__DefaultConnection=$env:GRADUATION_DB_CONNECTION` 啟動**本機獨立 DB**的 ASP.NET → read-only `GET /api/analysis-cases/policy`、`/api/detection-category/readiness`、`/api/major-category-price/readiness` → 正式 POST 上傳 / quantity-analysis / major-price-analysis；最後比對真值與圖表。沒有 DB Migration，原產學資料庫未動。


### 2026-11-21 — 正式 ASP.NET 已從獨立畢專資料庫讀取數量與價格偵錯準備狀態
- 使用者在 `C:\\Users\\User\\Desktop\\BiddingSystem-2A`，後端 `http://127.0.0.1:5261` 連接已種入 210 案的本機畢專庫，`Invoke-RestMethod http://127.0.0.1:5261/api/analysis-cases/policy` 成功回覆 `provisional=True`、`targetSources={Upload,OneD}`、`selfUpload.mode=StructuredOnly`、數量偵錯限結構體工程鋼筋／混凝土及價格大項限結構體工程。
- `GET /api/detection-category/readiness` 回覆 `categoryCount=2`、`minimumSampleCount=5`、`readyCount=2`；列表 PowerShell 截斷，但依政策與前次 Seed mapping 為鋼筋、混凝土。此為 readiness，而非實際預測已完成。
- `GET /api/major-category-price/readiness` 回覆 `majorCategory=結構體工程`、`eligibleMetadataProjectCount=210`、`availableProjectCount=210`、`minimumSampleCount=5`、`missingProjectCount=0`、`status=ReadyForEstimation`。確認價格估算的正式查詢路徑已可取得全部 210 案合成歷史價格；不代表判定精準度已被驗證。
- 本輪無 DB 內容變更、Migration 或正式估算演算法更動。待驗證：使用新版包含逐筆 `工程大項` 欄的 `graduation-v2-project-ready.xlsx` 作為 test-only；以 `POST /api/analysis-cases/from-upload` 建立案件，取得 `analysisCaseId` / `analysisBidVersionId`；然後分別呼叫 `POST /api/analysis-cases/{caseId}/versions/{versionId}/quantity-analysis` 和 `major-price-analysis`（request majorCategory=結構體工程），核對回傳與 truth。沒有實際 API 成功輸出前不得聲稱偵錯已通過。
- 注意：此前初版 `graduation-v2-project.xlsx` 尚未包含 `工程大項`，不應拿它當正式 API 驗證。PowerShell 只印回傳中的非敏感欄位，不提供任何 DB 密碼或連線字串。


### 2026-11-21 — SYN-00002 574-row structured workbook upload HTTP 400, diagnosed missing sheet selection
- 使用者確定 `graduation-v2-project-ready.xlsx` 與 `graduation-v2-project-ready-truth.json` 存在，從 truth 讀出 `SYN-00002` 且 `split=test` 的建案參數，使用 Windows PowerShell `curl.exe -sS --fail-with-body -X POST http://127.0.0.1:5261/api/analysis-cases/from-upload` 傳 Excel 和全部建案參數，收到 **HTTP 400**。API 錯誤內容在 PowerShell 被錯誤解碼為亂碼，`ConvertFrom-Json` 隨之失敗；`$upload` 不存在。
- 依 `feature/1d-api-integration` 的 `崴正/Services/TenderExcelParser.cs` 查證：`includedSheetNames == null && worksheetNames.Count > 1` 時**明確丟出 InvalidDataException**「此檔案包含多個工作表，請先預覽並明確選擇要匯入的工作表。」本次上傳恰好沒有傳 `includedSheetNames`，而待估 Excel 有 7 張工作表。這是高度確定且可由程式直接推導的 400 主因；因實際訊息亂碼，目前尚未驗證伺服器是否同時存在其它拒絕原因。
- 下一步**不修改 DB、不重跑 seed、不修改估算算法**。只重新上傳同一 test-only 標單，在 multipart form 加入依 `truth.detail_rows_per_sheet` 選出的 6 張詳細表 `includedSheetNames` JSON 陣列（不含「工程總表」），使用 PowerShell `System.Net.Http.HttpClient` 的 UTF-8 解碼讀出伺服器回應及具體錯誤。上傳成功才取得 `analysisCaseId`、`analysisBidVersionId`，再執行數量與價格 API；若仍是 400 先觀察原始 UTF-8 message，不可猜測其它修正。
- 驗證狀態：正式 ASP.NET 兩個 readiness 先前 PASS，SYN-00002 **上傳 FAIL (400)**，Quantity 和 MajorPrice 分析 **NOT RUN**；沒有程式碼 / DB / Migration 變更。


### 2026-11-21 — SYN-00002 正式 2A 待估標單上傳首次全數成功（HTTP 200、574/574）
- 使用者改以 UTF-8 `System.Net.Http.HttpClient` multipart 上傳 `graduation-v2-project-ready.xlsx`，並從 `graduation-v2-project-ready-truth.json` 的 `detail_rows_per_sheet` 傳入 6 張明細工作表 `includedSheetNames`，不含工程總表。
- `POST http://127.0.0.1:5261/api/analysis-cases/from-upload` **實測 HTTP 200**，回應 `analysisCaseId=1`、`analysisBidVersionId=1`、`versionNo=1`、`sourceType=Upload`、`sourceFileName=graduation-v2-project-ready.xlsx`、`itemCount=574`，原始訊息「已建立 2A 待偵錯案件，上傳標單只儲存於 Target Pipeline，不會寫入歷史資料。」確認 574 列已由正式 parser 接受並存放 target pipeline。
- 驗證狀態：train-only 210 歷史案／99,959 筆資料庫寫入成功，數量偵錯 readiness 2/2、價格估算 readiness 210/210，**SYN-00002 574/574 上傳及解析 PASS**；正式 C# `quantity-analysis` 與 `major-price-analysis` **尚未執行，不能把上傳成功視為估算成功**。價格／數量演算法沒有修改。
- 下一實際驗證點：不再重複上傳；用現有 caseId=1、versionId=1 分別呼叫 `POST /api/analysis-cases/1/versions/1/quantity-analysis` body `{}`，與 `POST /api/analysis-cases/1/versions/1/major-price-analysis` body `{"majorCategory":"結構體工程","topCount":8}`，保存原始 JSON（注意 PowerShell UTF-8），記錄 HTTP Status、run IDs、正式 P15/P50/P85 判定、有效樣本數、無法判定原因，再與 test-only truth 比對。若任一步失敗應停止，檢查該 API 錯誤，不可重新 seed。DB 尚未再次用 SQL 獨立核對資料筆數。
- 本次更新僅交接文件，沒有新的程式碼/資料庫/Migration 修改。隱私：正式連線字串及密碼不可貼到 GitHub／對話。


### 2026-11-21 — 正式 API 數量及價格分析首次成功：SYN-00002，結果有紅燈、需辨識結構限制
- 使用者在已建立的 `analysisCaseId=1`、`analysisBidVersionId=1` 後執行正式 API，提交 `$quantity.result` 文字檔和 `$price.result` JSON 文字。數量回傳 `analysisCaseId=1`、`totalGroupCount=1`、`analyzedGroupCount=1`、`insufficientDataCount=0`、`skippedGroupCount=0`：僅「結構體工程／混凝土」 m³，觀測 2,575.546，usage/m²=0.091296，P15/P50/P85 數量為 4,330.57／5,312.71／6,097.52 m³，analysisStatus=Low、trafficLight=Red、8 參考案、effectiveSampleSize=6.46、可用參考案 178。來源詳見使用者附檔，不含個資。
- 價格 API `analysisRunId=2`，結構體工程 `candidateProjectCount=210`、`availableProjectCount=210`、`referenceProjectCount=8`、`effectiveSampleSize=6.49`、`hasEnoughSamples=true`；合理每坪價格 P15/P50/P85：8,814.29／10,615.02／13,718.69 元，觀測 14,466.24 元／坪；合理總金額 P15/P50/P85：75,219,159.31／90,586,148.64／117,072,210.25，觀測 123,451,604.71；analysisStatus=High、trafficLight=Red。量與價均是真正正式 ASP.NET API（不是 Python 離線代理）合成資料上的演算法執行成功；**不代表真實工程準確度或判定正確**。
- 關鍵資料問題待確認：數量分析只產生混凝土一組，沒有鋼筋。查證 `generate_full_project_dataset.py` 的 RC CORE 含鋼筋+混凝土、SC CORE 是鋼骨+混凝土；正式 `TwoAPreprocessService` 鋼筋/混凝土才允許判定，鋼骨不在 2A 現有數量估算範圍。應先讀本機 `graduation-v2-project-ready-truth.json` 的 `project.structure_type` 與該案 `SYN-REBAR/SYN-STEEL/SYN-CONCRETE` 筆數，確認 SC 案只出現混凝土是否**符合設計**，不可先認定為鋼筋漏辨識 bug；如 SC，補一個未參與訓練的 RC 測試案，驗證鋼筋與混凝土兩類。不可擴增鋼骨判定規則或修改正式模型政策。
- 來源限制：歷史及待估都是 SYNTHETIC，項目量價係數並非實際建築工程校準；本次紅燈只是演算法在合成情境下成功分類，**未完成真值標記下的異常檢出準確性／FP/FN 比較**。價格來源權重含 timeWeight（依實際 API 回傳），不得說目前完全沒有時間權重。待做分案型、數量及價格圖表，先確認模型涵蓋案例。
- 下一驗證點：在本機唯讀查看 `$truth.project.structure_type` 與 truth 材料碼分布；保存已得的數量/價格 API JSON，確認訊息與異常狀態，再選 test split 的 RC 案產生新版標單（新檔名，不覆寫）、上傳另建案件並跑相同 API；後續產生真實 API 對照圖表。原始 train PostgreSQL 210 案／99,959 筆不必重新匯入。
- 交接為本輪實測紀錄，無程式碼/資料庫/Migration 更新。


### 2026-11-21 — 查證 SYN-00002 標單材料組成為 SC 核心：鋼骨與混凝土各 6 筆
- 使用者於 `graduation-v2-project-ready-truth.json` 執行 `$truth.cases | Group-Object material_code | Sort-Object Name | Select-Object Name,Count`，核心結果 `SYN-CONCRETE=6`、`SYN-STEEL=6`，`SYN-REBAR=0`；其他多種 `SYN-WORK-xxx` 工項。
- 依本 repo `generate_full_project_dataset.py` CORE 的結構條件，`SYN-STEEL + SYN-CONCRETE` 而無 `SYN-REBAR` 符合 SC 建案的材料配置；現有 `TwoAPreprocessService` 與 `TwoAScopePolicy` 限制正式數量偵錯只涵蓋結構體鋼筋與混凝土、不包含鋼骨，因此先前 quantity API 僅返回混凝土 1 組為**符合預期的結果**，不應誤報鋼筋辨識 bug。建案 structure_type 欄位的實際 PowerShell 輸出尚未單獨貼出，該結構判讀來自已知生成器與材料組合。
- 下一驗證點：從 v2 `projects.csv` test split 選取 `structure_type=RC` 且 `project_boq_rows.csv` 同時具 `SYN-REBAR` 與 `SYN-CONCRETE` 的未訓練案，指定 `--project-id` 輸出獨立 workbook + truth（不同檔名），正式 `POST /api/analysis-cases/from-upload` 明確帶入 includedSheetNames，隨後 quantity+major-price 分析。上傳成功件數與兩種 category 結果都要實際核實；不修改 seed / 產學 DB。
- 測試結果區分：SC quantity+price API **執行 PASS**，但數量只涵蓋混凝土；**RC 雙材料案例尚未測試**。合成價格紅燈與混凝土用量紅燈不代表真實工程異常判定準確率。


### 2026-11-21 — RC test-only SYN-00029 409-row workbook generated; live API runs pending
- 使用者以 `graduation-data-v2/projects.csv` 的 `split=test`、`structure_type=RC` 條件，且實際 `project_boq_rows.csv` 同時具 `SYN-REBAR` 和 `SYN-CONCRETE` 的材料碼，挑出 `SYN-00029`。測試案總樓地板面積 17,262.89 m²、地上 14 層、地下 0 層。
- Windows 實測 `python tools/graduation-synthetic/export_full_project_excel.py --source .\\graduation-data-v2 --project-id SYN-00029 --output .\\graduation-v2-rc-ready.xlsx --truth .\\graduation-v2-rc-ready-truth.json`：**成功**，明細 `409`、工作表 `7`（實際 6 張明細+1 總表），明細分布結構體 12、基礎及地下 36、樓層材料 38、防水門窗及外裝 95、機電消防及弱電 136、室內裝修與雜項 92，SHA256 `376b320c03aec3d9cfd7cd44c90d43c470677127cbaca838d1aec2521238d464`。
- 這只是新 RC test-only workbook 實際生成，**尚未經 AnalysisCase/from-upload 正式上傳、尚未執行 RC quantity/price API**；不能聲稱鋼筋/混凝土雙材料已正式分析。
- 下一驗證點：重用 `127.0.0.1:5261` 同一隔離資料庫、以 UTF-8 HttpClient multipart 明確選取 `truth.detail_rows_per_sheet` 的六張明細表傳 `includedSheetNames`，建立新的 RC target analysis case；保存真實 case/version ID，才依序呼叫 quantity-analysis、major-price-analysis 並保存各 result JSON；檢查是否真的產生鋼筋與混凝土兩組，否則追查 `TwoAPreprocessService` 的工項分類，不改模型範圍。
- 不重跑 seed，不刪除舊 SC 案件，不連正式 Supabase；本次僅交接文件更新，沒有程式碼及 Migration 修改。


### 2026-11-21 — RC SYN-00029 正式 2A 量價雙路徑端到端實測 PASS
- 使用者使用本機隔離 PostgreSQL 的 ASP.NET API：`POST /api/analysis-cases/from-upload` **HTTP 200**，`analysisCaseId=3`、`analysisBidVersionId=3`、`versionNo=1`、`sourceType=Upload`、`itemCount=409`，代表新生成的 RC 未訓練測試標單 `graduation-v2-rc-ready.xlsx` 409 列全數解析成功並儲存於 Target Pipeline。
- 正式 `POST /api/analysis-cases/3/versions/3/quantity-analysis` 成功，`analysisRunId=5`、`totalGroupCount=2`、`analyzedGroupCount=2`、`insufficientDataCount=0`，混凝土觀測 3885.108 m3、判定 `Low/Red`；鋼筋觀測 496658.648 kg、判定 `Low/Red`。兩個正式偵錯細項都已實際出現，前次 SC 案只有混凝土的結果不表示鋼筋偵錯失效。
- 正式 `POST /api/analysis-cases/3/versions/3/major-price-analysis` 成功，`analysisRunId=6`，結構體工程觀測金額 91,153,245.71、P15/P50/P85 金額 69,512,978.87／120,860,965.10／146,643,899.77，`analysisStatus=Normal`、`trafficLight=Green`。計算：觀測金額處於預測上下限內，但低於中位數。
- 用戶在本機已保存 `graduation-v2-rc-quantity-result.json` 與 `graduation-v2-rc-price-result.json`；文件中記錄的是用戶貼出的摘要，**RC 數量預測上下界與有效樣本數等細節未貼出，尚不可寫入未驗證數字**。
- 與 SC SYN-00002 對照：SC 標單 574/574 PASS，混凝土量 Low/Red，結構體價格 High/Red（price runId=2）；RC 標單 409/409 PASS，鋼筋+混凝土量均 Low/Red，結構體價格 Normal/Green。此為**兩個獨立 held-out 建案上的正式 API 功能實證**，非模型可靠度或真實工程準確性統計。
- 下一實驗目標只聚焦數量估算與價格異常偵錯：讀取本機保存的四份真實 API 結果，對照兩份 truth JSON，繪製觀測值與正式 P15/P50/P85（含材質單位和建案）比較圖；擴充多個 held-out 案才可計算 MAPE、涵蓋率、異常檢出率及混淆矩陣；明確標示 SYNTHETIC，不能把兩案 Red/Green 算成正確率。不要額外擴充工種模型或重跑歷史資料 seed。
- 以上是 2026-11-21 使用者實測結果補登；未修改 C# 估算邏輯、DB Migration 或產學正式資料庫。


### 2026-11-21 — RC SYN-00029 quantity API P15/P50/P85 complete
- 使用者已讀取本機正式 C# `graduation-v2-rc-quantity-result.json`，按 `result.items` 輸出完整區間。
- `SYN-00029` 結構體混凝土 m3：observed=3885.108，P15=4635.50、P50=5127.43、P85=5696.79，參考建案數=8，analysisStatus=Low（觀測低於 P15 約16.19%，低於 P50 約24.23%）。
- 結構體鋼筋 kg：observed=496658.648，P15=567635.47、P50=830851.50、P85=953640.20，參考建案數=8，analysisStatus=Low（觀測低於 P15 約12.50%，低於 P50 約40.22%）。
- 與前一輪價格 API 合併：同一 RC 案結構體價格 observedAmount=91153245.71，P15/P50/P85=69512978.87/120860965.10/146643899.77，Normal/Green。SC SYN-00002 混凝土 Low/Red、結構體價格 High/Red。
- 測試狀態：兩個案例的正式 Quantity/MajorPrice API **執行 PASS**，但量價判定是否與合成異常標準答案相符、回測誤差及涵蓋率等 **尚未驗證**。兩個數量觀測值均低於下限，需要評估生成器規則與服務彙整/分類/相似建案參考差異，**不能先推定一定是算法錯誤或真實工程量偏低**。
- 下一驗證點：以唯讀腳本比對 target `truth.cases` 中鋼筋/混凝土 `expected_quantity` 總量與正式 API 的 observedQuantity，並比對參考歷史 train-only 用量；接續多個 test holdout 才能計算可解釋的回測指標，製作 SYNTHETIC 註記的報告。不要重 seed、不擴充模型、不修改正式 DB 或 migration。
- 本次只更新交接文件，無程式碼變更。


### 2026-11-21 — RC SYN-00029 truth 對正式 API 觀測數量完全一致
- 使用者由 `graduation-v2-rc-ready-truth.json` 的 `cases` 篩選材料碼 `SYN-REBAR`、`SYN-CONCRETE`，按材料彙整 `expected_quantity`：鋼筋 **4/4 有效列、合計 496658.648 kg**；混凝土 **8/8 有效列、合計 3885.108 m3**。
- 這與先前正式 API `analysisCaseId=3`、`quantity-analysis` `analysisRunId=5` 的 `observedQuantity` **逐項完全相符，數量差異都是 0**。本次已排除這兩類材料在標單匯入、分類或數量加總時發生總量遺漏的疑慮。此驗證不代表其他材料行或其它案完全無誤。
- RC 結果仍為混凝土 P15/P50/P85=4635.50/5127.43/5696.79 m3，實際 3885.108 Low/Red；鋼筋 P15/P50/P85=567635.47/830851.50/953640.20 kg，實際 496658.648 Low/Red。由於 target 觀測數量正確，後續優先比對類別 mapping 的歷史建案 `ActualQuantity` 彙總、建案面積歸一化、Top8 相似案件權重、生成器的組數與量係數／注入異常，避免僅為降低紅燈而修改模型。
- 下一驗證點：使用既有 `graduation-v2-rc-quantity-result.json` 查看鋼筋與混凝土各 Top8 `references` 的 `usagePerArea`、`actualQuantity`、`floorArea`、`similarityScore`、`finalWeight`，和待估案的 `observedUsagePerArea` 比對。用相同 SYNTHETIC 測試集多案回測才能談 coverage/FP/FN/MAE，兩例不足以支持準確率結論。
- 本次僅文件更新；沒有更動 DB、Migration、估算演算法、訓練資料與檔案解析。


### 2026-11-21 — RC SYN-00029 Top8 歷史參考樣本核查完成；本階段功能驗收收尾
- 使用者以正式 API 保存的 `graduation-v2-rc-quantity-result.json` 唯讀顯示 `references`，核對兩類材料待估案單位面積用量與 Top8 歷史建案的用量範圍。
- 混凝土：待估 `observedUsagePerArea=0.225055 m3/m2`、`effectiveSampleSize=6.65`；Top8 參考建案的 `usagePerArea` 依各列為 0.252721、0.301232、0.269415、0.268524、0.272406、0.297020、0.334799、0.330002，範圍 **0.252721～0.334799 m3/m2**。待估值低於全部八個參考案，與此前 P15=4635.50 m3、Low/Red 的方向一致。
- 鋼筋：待估 `observedUsagePerArea=28.770307 kg/m2`、`effectiveSampleSize=6.99`；Top8 `usagePerArea` 依各列為 44.377275、30.088898、32.881833、48.129340、55.242210、39.231814、51.806698、56.700138，範圍 **30.088898～56.700138 kg/m2**。待估值亦低於全部八個參考案，與此前 P15=567635.47 kg、Low/Red 的方向一致。
- **本階段已完成的功能驗證**：210 train-only 歷史建案、99959 筆歷史交易和標單入隔離 PostgreSQL；Docker/.NET 建置；readiness Quantity 2/2 和 Price 210/210；SC SYN-00002 574/574 解析，Quantity 混凝土 Low/Red、MajorPrice High/Red；RC SYN-00029 409/409 解析，Quantity 鋼筋與混凝土 2/2 有結果且 Low/Red、MajorPrice Normal/Green；RC truth 數量逐項與 API observedQuantity 完全吻合；參考樣本數值與異常方向一致。
- **尚未完成、非本階段阻擋事項**：需以更多 test-only 建案進行正式 API 批次回測，才有條件計算 P15/P85 區間涵蓋率、準確度、假陽性/假陰性、分類別誤差；合成歷史非真實工程校準，不得以此聲稱真實營造工程準確度。價格異常的正確性亦需有獨立正常/異常真值標籤才能判斷。不要將「API 執行 PASS」寫成「模型準確率 PASS」。
- 重要決策：使用者希望在已完成此輪獨立資料庫／上傳／RC+SC 數量與價格 E2E 的功能驗收後轉入下一項工作；本輪功能驗收可收尾，不繼續要求零碎逐案檢查。下階段由使用者新任務決定；若轉成模型品質/報告工作，需先規劃 batch holdout 回測與實測 API 證據圖表。
- 已淘汰方案：不採用已移除的 Z-score、LSTM 或 Razor 來敘述目前方法。現行 API 參考樣本具有 similarityWeight/timeWeight，依實際程式和回傳描述。無新資料庫 schema / Migration / C# 演算法改動，本次僅驗證紀錄。


### 2026-11-21 — 新階段需求盤點：SRC、低設計依賴工項、城市因素查證（提案尚未實作）
- 用戶指出結構不應只有 RC/SC，尚有 **SRC**；希望研究其他有可辨識單位用量 pattern 的工項/建材，但排除高度受建築師/設計師設計影響者；詢問城市是否已納入估算。此為新一輪需求/技術範圍評估，**尚未授權或完成實際擴模實作**。
- 依 `崴正/Services/ProjectSimilarityService.cs` 現行 `main`：`ValidateTarget` 明確接受 RC、SC、SRC；`ScoreCandidates` 包含 `CalculateCityScore`，只有候選城市與目標完全相同才獲 `settings.CityWeight`，不同城市得 0，未實作距離或跨縣市市場區域相似度。City 是相似建案加分，**不是直接乘到工程量**。價格及數量的相似度參考都使用此服務。候選 `ProjectType` 必須相同。
- 本機合成 DB seed `tools/graduation-synthetic/GraduationSeed/Program.cs` 的權重：Structure=25, Area=30, Floor=15, Year=10, City=10, **SiteArea=10**；但現行 `ScoreCandidates` 分子/分母並未納入 SiteAreaWeight、也未呼叫基地面積比較（當前五權重和=90）。此為現況缺口，需獨立規格/測試後再修正，不能說基地面積已發揮加權作用。
- `tools/graduation-synthetic/generate_full_project_dataset.py` `CORE` 只定義 RC (SYN-REBAR+SYN-CONCRETE) 與 SC (SYN-STEEL+SYN-CONCRETE)；`choose_templates` 使用 `CORE[structure]`，**SRC 無對應模板、無法保證合成 SRC 資料可產生**，即使 API 已允許 SRC。因此先前兩案 RC/SC E2E PASS 不等於 SRC 已驗證。後續應在不破壞既有合成資料/isolated DB 的新版本中研究 SRC 獨立模板與 heldout 案；不得在未確認前重建現有訓練 DB。
- 工項候選技術建議（尚未核准為正式 Scope）：優先研究 **結構鋼骨** (SC/SRC 以 kg/m2 檢視，須控制跨度/載重等)、**模板** (RC/SRC，須分工法且有適用面積)、**土方開挖** (基地/地下室/開挖深度/地質條件)，保留現有鋼筋與混凝土。以上仍受結構或工法影響，應先做條件化 P15/P50/P85 回測、資料涵蓋和異常標籤評估，不能直接變成正式紅綠燈。門窗、玻璃、立面、磁磚、天花板、裝修、機電設備等高度設計依賴者先排除。
- 城市對價格比對可能很重要，但建議避免把城市的價格差異當作工程數量差異；需要實測相同條件跨縣市敏感度以及地區樣本數不足時的退路。合成資料生成器的 region_factor 僅模擬單價變化，不是真實地區價格指數。
- 新階段下一步：定義 SRC 的核心材料/結構參考樣本隔離策略；列出可計量的工項候選與必要 metadata，先於獨立測試資料研究；設計城市加分/城市優先篩樣與 SITE area 分母不一致的測試；**待用戶確認擴模範圍後才對正式 `TwoAScopePolicy` / 種子 / DB 做相關修改**。原 RC/SC 正式 API 成功紀錄保留。
- 本次僅更新需求與查證交接，無程式碼、Migration 或測試新結果。


### 2026-11-21 — 嚴格模式第一批實作：SRC opt-in、SRC 同結構參考隔離、城市/基地面積 C# contract
- 使用者明確同意 **嚴格模式**：候選工項要經回測驗證才可取得正式紅綠燈；SRC、城市、基地面積相似度須有可核對的測試。此批集中修改沒有觸碰正式 Supabase、隔離畢專既有 210 案 DB、Migration 或既有 RC/SC 測試結果。
- 查證版本差異：`main` 的 `ProjectSimilarityService.cs`／DTO 是較舊架構，**實際操作 `feature/1d-api-integration` 已有完整 `SiteArea` 欄位、`CalculateSiteAreaScore` 及 `SiteAreaWeight` 分子分母**，且已接受 SRC、城市 exact match 加分。前一輪僅看 main 後認定基地面積缺失，不能套用到 feature；此次沒有重寫既有加權演算法。
- `main` commit `1f31a964` 更新 `tools/graduation-synthetic/generate_full_project_dataset.py`：增加 `--include-src`（預設 OFF），以固定 seed 對第三分之一建案賦予 SRC 結構、三材料 `SYN-STEEL/SYN-REBAR/SYN-CONCRETE`，僅產生研究合成資料，新增 manifest structure counts 和 experimental_src_enabled；既有不帶旗標的 RC/SC 資料生成保持原邏輯及不覆寫輸出。
- `main` commit `bf029d16` 修改 `tools/graduation-synthetic/test_full_project_dataset.py`：增加 opt-in SRC 三核心材料、RC/SC 原始 fixture 不變與固定 seed 重現性測試。
- `feature/1d-api-integration` commit `86c068b4` 只改 `崴正/Services/ProjectSimilarityService.cs` 的兩條歷史候選查詢：若目標 `StructureType=SRC`，**僅允許 SRC 歷史建案**，不再借 RC/SC 歷史案例生成 SRC 的正式樣本；RC/SC 選樣規則保持不變。現有隔離 DB 無 SRC 時應顯示不足／無樣本，不能假裝驗證過 SRC 準確度。
- `feature/1d-api-integration` commits `b355b28b`、`8263675b` 新增 `tools/graduation-synthetic/SimilarityContract/SimilarityContract.csproj` 與 `Program.cs`：透過 reflection 真正呼叫 C# `CalculateStructureScore`（SRC 對 RC/SC 不加分）、`CalculateCityScore`（同城 10、異城 0）、`CalculateSiteAreaScore`（同面積 10、差 50% 得 5、差 100% 或缺值 0），完全離線不接 DB。
- `main` commit `8938ae95` 新增 `docs/SRC_STRICT_SCOPE_AND_SIMILARITY_VALIDATION_2026-11-21.md`：驗收門檻、城市及基地面積敏感度、SRC 參考群隔離、候選項目（鋼骨／模板／土方）研究且無量價紅綠燈的規定，並列出 Windows 重跑指令。
- **已提交 GitHub；但尚未由用戶 Windows 執行最新 Python unittest、SRC research dataset CLI、C# SimilarityContract build/run、正式 SRC 無參考樣本 API，全部列為 NOT RUN**。只有先前 RC/SC live E2E PASS 仍成立，不能把新 SRC 宣稱為正式經過數量/價格估算驗收。
- 後續：使用者 feature 工作樹不可全量 reset/merge；僅同步所需 main 腳本/測試及 feature C# 服務與 SimilarityContract 目錄，先做 Python unittest 和 .NET contract。若 PASS，再在**新**`graduation-src-research-v1` 產生 60 案 SRC 合成研究資料，查看 manifest，接著獨立 API 和隔離 DB 無樣本安全性回歸，不改現有庫。RC/SC 正式 API 舊證據應保留。
- 本批沒有新增任何 `TwoAScopePolicy` 正式候選類別，沒有 C# schema/Migration。修改錯誤可透過以上 commit 還原程式碼，資料庫無回滾需求。


### 2026-11-21 — 嚴格模式 SRC+極端條件新隔離研究套件已提交（尚待 Windows 實測）
- 用戶已明確批准嚴格模式：除現有鋼筋/混凝土以外的候選工項必須經分層留出案例回測及工程適用性審查，**不得因資料中出現或 proxy 回測數字好看就自動啟用紅綠燈**。要求加入 SRC 歷史建案、SRC 待估標單，以及各種極端條件建案；同時測試城市與基地面積相似度。
- 查證 `main` `generate_full_project_dataset.py` 已有 opt-in `--include-src` 和實驗用三材料 SRC CORE；不可重複宣稱 SRC 從零新增。本輪基於現行代碼增加 opt-in `--include-extremes`，且需同時 `--include-src`，每個 train/validation/test split 依序配置 6 個有標籤極端案例：`tiny_lowrise`、`large_highrise`、`deep_basement`、`constrained_site`、`wide_lowrise`、`older_remote_city`，含 RC/SC/SRC，修改建案結構、總樓地板面積、基地面積、地上地下樓層、縣市及/或日期；保留原始不加旗標時之產出行為。實驗 SRC CORE 材料生成率在 `include_src` 時設為 100%，避免缺鋼筋、鋼骨或混凝土使測試無法覆蓋。**這些極端設定刻意不代表合理營造量。**
- 本輪已提交到 `main` 的新增/修改程式及路徑：
  - `tools/graduation-synthetic/generate_full_project_dataset.py` commit `707d9504`：明確 train/validation/test 情境、manifest 計數與 `extreme_scenario` 欄。
  - `tools/graduation-synthetic/test_full_project_dataset.py` commit `0e3cd82c`：涵蓋 6 種情境各 split、RC/SC/SRC 材料及 deterministic/guard checks。
  - `tools/graduation-synthetic/prepare_training_seed.py` commit `d0dca773`：`--target-database bidding2a_graduation_src` 僅支援兩個本機白名單 DB 名，保留舊預設。
  - `tools/graduation-synthetic/GraduationSeed/Program.cs` commit `eaf85ef6`：白名單只接受本機 55432/legacy 庫或 55433/SRC 新庫，檢查種子宣稱 DB 與連線相符，維持不可對非空 DB 重匯／禁止 Supabase。
  - `tools/graduation-synthetic/compose.graduation-src-db.yaml` commits `95c87f1b`,`d3d91cda`：獨立 project `graduation-src-synthetic`，獨立 volume、Postgres17、只綁 127.0.0.1:55433。
  - `tools/graduation-synthetic/export_src_holdout_suite.py` commit `dafd1acf`：輸出一個未訓練普通 SRC 和六個 test split 極端案件的 7 份 Excel/真值，不進 train。
  - `tools/graduation-synthetic/verify_similarity_sensitivity.py` commit `259ee10d`：對實際 ASP.NET `POST /api/major-category-price/range` 同條件切換城市、基地面積，唯讀比對 Top8 參考分數與排序，未有敏感差異時標示 INVESTIGATE，**不是模型準確度測試**。
  - `tools/graduation-synthetic/audit_candidate_scope.py` commit `52537246`：鋼骨、模板、土方等候選生成研究資料與 preliminary gate，全部 `formal_red_green=false`，不得自動修改 Scope。
  - `tools/graduation-synthetic/test_src_extended.py` commit `010ca64f`：SRC 新 DB seed split 防洩漏、候選遮罩、7 個 holdout 輸出規格回歸測試。
  - `tools/graduation-synthetic/README.md` commit `bb318dfb`：從 Python 測試到新版獨立 DB/seed/標單與敏感度驗證流程。
- **版本風險**：GitHub `main` 的 `ProjectSimilarityService` 與 Model 尚未具備 `SiteAreaWeight` 計算；正在開發的 `feature/1d-api-integration` 中 `ProjectSimilarityService.cs`、`EstimationSetting.cs`、`ProjectSimilarityTargetDto.cs` 具有 `SiteArea` 實作。上列唯讀測試**必須對實際啟動且含該功能的後端執行**。沒有將 feature 整支複製到 main，以免影響主系統 Migration / API 相容性；目前仍有分支整合待辦。城市 `CityWeight` 在兩分支均已存在，完全相同城市加權，其餘城市得 0，不會自動調價。
- **測試狀態**：已完成 GitHub 實際提交，尚未在使用者 Windows 執行新 Python unit tests、建置修改後 Seed、創建新 SRC DB、匯入新 SRC 訓練案、上傳 SRC/極端標單或跑正式 API；以上皆標記 **NOT RUN**，不得宣稱 PASS。舊 RC/SC 17 tests、210 案 DB 及兩案 E2E 成功仍是之前版本的事實。
- **資料庫與回滾**：本輪沒有任何 DB/Migration 變更或現場資料寫入；新 DB 先用獨立 Docker project/volume 進行，有問題可停止新 SRC 服務而不影響舊 55432 資料；禁止未經同意刪除任何 volume。所有 credentials 僅置使用者環境變數且不可回貼到對話。
- 下一驗證點：使用者本機 scoped restore 最新 `tools/graduation-synthetic`、執行 unittest → `--include-src --include-extremes` 在新 `graduation-src-extreme-v3` 輸出 300 案 → 檢查 manifest `structure_type_counts` 與各 split extremes → 匯出新 SRC train-only JSON 及 7 份 test-only Excel → 新 55433 DB 建置/匯入 → 用 feature ASP.NET 服務驗證城市/基地敏感度與 SRC 正式數量/價格 API → 批次留出測試、決定各候選工項是否應繼續研究。先一組集中完成測試，避免小碎步重複 push。


### 2026-11-21 — SRC/extreme 套件納入 CI 驗證路徑
- 新增 `.github/workflows/graduation-synthetic.yml` commit `ae8e085a` 的 SRC/extreme 離線驗證步驟：產生獨立 120 案含 SRC/極端 train/validation/test 資料 → 產生 SRC-only DB 指向的 train seed JSON → 輸出 SRC+極端七份 test-only 標單與真值 → 產生候選工項 STRICT research audit → 以 `test -s` 檢查關鍵輸出非空。原有 .NET build 與 Python unittest 步驟仍保留。
- 因目前尚未從 GitHub Actions 取得該 commit 對應的 run/step 結果，也未取得使用者 Windows 回測，**CI 工作定義已提交但此輪是否執行 PASS 尚未驗證**；請以下輪實際測試結果為準。此工作流程不會啟動 Docker、不會修改任何 DB，也不會把候選工項正式啟用。


### 2026-11-21 — SRC 大量訓練種子 CLI 輸出限縮
- `tools/graduation-synthetic/prepare_training_seed.py` commit `b3eefde1`：因原本 CLI stdout 會額外印出全部 `boq_rows`（300 案可達 10 萬行），現只輸出摘要 metadata/數量，不把完整資料列灌入終端；JSON 原始 seed 檔仍保存全部 train-only project、BOQ、transaction，內容與 DB 契約未刪減。Windows 尚未實際重跑此變更。
- 整組 SRC/extreme 最新交接 commit 以前後續 `main` 最新為準；任何實測 FAIL 需更新此文件，修正後才執行新 DB 種子匯入。仍禁更動原本 55432 正式 RC/SC 畢專資料。


### 2026-11-21 — 用戶追加「設計造成材料量過高/低、價格過貴/便宜」受控異常案例（已提交，尚待本機/CI測試）
- 用戶要求在已有 RC/SC/SRC 及極端基地、樓層、城市條件之外，**加入可能由不同設計造成的混凝土等材料用量暴增或暴減、價格過高或過低**。確認這是「標單量價異常情境」，不是把所有差異直接當成真實設計缺陷。為避免異常污染歷史正常參考，實作**只對 test split 的 Excel 輸出複本注入倍率**，原 train、validation、history CSV、DB seed 完全不變。
- `tools/graduation-synthetic/export_full_project_excel.py` commits `d16e167a`,`f652aa8a`：新增可選 `anomaly_scenario` / CLI `--anomaly-scenario`，原預設不變。支援 10 個標籤：
  1. `design_concrete_high/low` 混凝土用量 ×2.50/×0.35；
  2. `design_rebar_high/low` 鋼筋用量 ×2.40/×0.40；
  3. `design_steel_high/low` 鋼骨用量 ×2.50/×0.35，仍然非核准 2A quantity 類別；
  4. `structural_price_high/low` 結構體工程全體工項單價 ×3.00/×0.20，數量不變；
  5. `combined_concrete_high_price_high/low_price_low` 混凝土數量 ×2.50/×0.35 且整體結構體單價 ×3.00/×0.20。
- 此 exporter 不改原歷史資料；每筆 truth JSON 保留 `baseline_quantity`、`baseline_unit_price`、`baseline_amount`、`expected_quantity`、`expected_unit_price`、`expected_amount` 與 `injection_label`；同時 `injection.changed_lines` 紀錄各筆原始與注入後值、量價倍率。對注入的行禁止附加與本目的衝突的隨機缺價／缺量測例，所有金額一致化 `amount=quantity*unit_price`。輸出拒絕覆寫，只允許 test split，不含異常方案時仍維持舊行為。
- `tools/graduation-synthetic/export_anomaly_holdout_suite.py` commits `7910e63b`,`37e9c6f7`：從新版 SRC/extreme v3 test-only 建案匯出 **10 個異常變體 XLSX + truth**，也匯出每個實際來源建案的**未注入 baseline XLSX + truth**，寫入 `anomaly_suite_manifest.json`，記載 source project id、結構與相關性。多種變體可來自同一來源建案，**不可充當獨立建案計算統計**。
- `tools/graduation-synthetic/test_src_extended.py` commits `bdc413ac`,`89fc685e`：新增 10 種情境的 XLSX 真實產出、原始 CSV SHA 未變、train split 注入被拒、來源與 target 量價對照、期望金額算式與輸出不覆寫測試。
- `.github/workflows/graduation-synthetic.yml` commit `7aba70d0`：擴充既有 CI 離線套件，產生異常 suite 並檢查 manifest 非空。
- `tools/graduation-synthetic/README.md` commits `56099f4a`,`9558a1ab`：新增 `python tools/graduation-synthetic/export_anomaly_holdout_suite.py --source .\\graduation-src-extreme-v3 --output-dir .\\graduation-anomaly-holdouts`，說明 10 個量價異常類型與成對基準、標籤不等於模型判定，以及真值和正式紅綠燈分離。
- **驗證狀態**：上述檔案已透過 GitHub 工具提交及重新讀取程式（程式碼審核/路徑已確認），但**尚未取得新版 Python unittest、GitHub Actions 及 ASP.NET 端到端的實際 PASS**；目前標記 NOT RUN。須先跑 `python -m unittest discover ...`，再生成 SRC v3 / anomaly holdouts，核對 truth、SC/RC/SRC API quantity/major price 異常與預期方向。不可單憑已知注入倍率宣稱偵測成功。
- 資料版本相容：舊 `graduation-data-v2`、`55432/bidding2a_graduation` 一律保留；新 SRC 專用 `55433/bidding2a_graduation_src` 不需因本輪增補異常 workbook 重跑 seed，只要使用者尚未匯入時沿用最新版產生的原 train-only seed 即可。無 Migration/正式 Supabase/production 調整。後續新工項仍採 **嚴格模式：正式 C# API 留出回測及設計適用性核准後方可開放紅綠燈**。
- **下一驗證點**：先新/舊 Python regression，進入 SRC 合成 300 案 + 七件基本/極端測試標單 + 10 個量價異常變體與 paired baseline，確認 `injection.changed_lines`、原 CSV digest 和 workbook 金額；再到新隔離 DB 執行正式 API，將數量異常（尤其混凝土用量變多）與單價異常拆開評估，並產生 per-source-project 的 paired delta report（不重複計算獨立樣本）。


### 2026-11-21 — 量價異常註記補正：合理設計變動不得自動視為違失
- `tools/graduation-synthetic/export_full_project_excel.py` commit `41c98701`：test-only 注入真值中的 `injection.cause_kind` 明確分為 `DESIGN_VARIATION_POSSIBLY_JUSTIFIED`、`UNIT_PRICE_STRESS_NOT_PROOF_OF_OVERPRICING`、`MIXED_DESIGN_AND_PRICE_STRESS`，並設定 `manual_review_required=true`。建築師或結構設計造成高低材料用量可為合理設計，價高/低也不必然代表不當；正式 2A 紅燈僅供稽核/人工審核，不能將標籤直接視為違規或系統判定應當通過的真值。
- 狀態：已提交程式碼，單元測試、CI 與正式 API 仍待實際驗證；資料庫未更動。


### 2026-11-21 — Windows 21/21 Python 回歸測試 PASS；匯出尚缺來源資料集
- 使用者在 `C:\Users\User\Desktop\BiddingSystem-2A` 完成 `git restore --source=FETCH_HEAD --worktree -- tools/graduation-synthetic`，執行 `python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v`：**Ran 21 tests in 19.480s，OK（21/21 PASS）**。包括 SRC/extreme 分割檢查、10 個標單量價異常注入、來源不覆寫與 test-only 安全守門。輸出中 `test_refuse_overwrite` 的 argparse error 為測試刻意驗證覆寫拒絕，整個 suite 仍 OK，不是程序失敗。
- 隨後執行 `python tools/graduation-synthetic/export_anomaly_holdout_suite.py --source .\\graduation-src-extreme-v3 --output-dir .\\graduation-anomaly-holdouts` **FAIL（輸入前置條件未成立）**：`[Errno 2] No such file or directory: graduation-src-extreme-v3\\manifest.json`。表示此新來源目錄尚未產生（或不在當前路徑），**不是量價注入邏輯的單元測試失敗**。
- 下一可執行步驟：`python tools/graduation-synthetic/generate_full_project_dataset.py --count 300 --seed 20261119 --include-src --include-extremes --output .\\graduation-src-extreme-v3`；檢查 manifest `project_count=300`、結構分布與 `extreme_cases_per_split` 各6，然後重跑 `export_anomaly_holdout_suite.py` 與 `anomaly_suite_manifest.json`。所有輸出拒絕覆寫，已存在時先查而不是刪。
- 仍未執行新 55433 SRC DB 匯入與正式 ASP.NET quantity/price API E2E，不能宣稱新異常案例有檢出準確性。舊 55432 RC/SC 資料保持不動。本輪無新程式碼修改，僅實際測試結果交接。


### 2026-11-21 — SRC/extreme 300 案與 10 種量價異常標單本機產物確認
- 使用者於本機執行 `generate_full_project_dataset.py --count 300 --seed 20261119 --include-src --include-extremes --output .\\graduation-src-extreme-v3` 顯示 `output directory must be empty (refusing overwrite)`；隨後成功**讀取現有** `graduation-src-extreme-v3/manifest.json`，實際 `project_count=300`、`transaction_count=143700`、`boq_line_count=143700`，結構分布 SRC=102、RC=110、SC=88，極端條件 train/test/validation 各6。這表示來源資料集已存在，拒覆寫為安全守門，**本輪不是重新產生**。
- 使用者執行 `export_anomaly_holdout_suite.py` 也收到 `refusing to overwrite output directory`，接著成功**讀取現有** `graduation-anomaly-holdouts/anomaly_suite_manifest.json`：`scenario_count=10`、`baseline_count=3`、`test_only=True`。情境與注入列數：`design_concrete_high` RC 8，`design_concrete_low` SRC 6，`design_rebar_high` RC 4，`design_rebar_low` SRC 9，`design_steel_high` SC 4，`design_steel_low` SRC 7，`structural_price_high` SRC 46，`structural_price_low` RC 30，`combined_concrete_high_price_high` SRC 46，`combined_concrete_low_price_low` RC 30。這是**manifest 已產生且能讀取**的確認，尚未在本輪逐檔 hash/實際 Excel 驗證，亦不是正式 API 偵測 PASS。
- 此次回報沒有新的 Python unittest 失敗。前輪 Windows Python 21/21 PASS 保留，但尚未完成新 SRC 55433 DB train-only seed、匯入、API E2E。已給使用者安全的唯讀路徑檢查與 train-only seed JSON 指令，僅當檔案不存在才產生。舊資料庫與既有資料夾皆不刪除、不重 seed、不連正式 Supabase。
- **下一驗證點**：確認所有 10 個異常 + 3 個基準標單的 xlsx/truth 存在，再確認 `graduation-src-train-seed.json` 只含 train、`target_database=bidding2a_graduation_src`、排除 holdout 數 90；接著新隔離 55433 PostgreSQL 匯入與正式 SRC/量價 API 成對比對。未確認的實測一律標記 NOT RUN。


### 2026-11-21 — 新 SRC train-only Seed 生成及異常 workbook 存在性確認 PASS
- 使用者 Windows PowerShell 在 `C:\Users\User\Desktop\BiddingSystem-2A` 驗證 `graduation-anomaly-holdouts/anomaly_suite_manifest.json` 中 10 個 `scenarios` 與 3 個 `baseline_workbooks` 的全部 Excel／truth 路徑皆可 `Test-Path` 找到，輸出「異常測試與基準檔案完整」。**此為存在性驗證，尚未逐 XLSX 解析或 hash 驗證**。
- 使用者首次成功執行 `python tools/graduation-synthetic/prepare_training_seed.py --source .\\graduation-src-extreme-v3 --target-database bidding2a_graduation_src --output .\\graduation-src-train-seed.json`，工具實際輸出 `data_origin=SYNTHETIC`、`dataset_version=2.0.0`、`split=train`、`target_database=bidding2a_graduation_src`、`source_manifest_sha256=aca3e4274589ad66a7011a9f1163e4fb9ccc9c5d6715a88c4194f179a5e7c3b8`、`train_project_count=210`、`train_transaction_count=101166`、`excluded_holdout_projects=90`。第二次 PowerShell 讀取 JSON 摘要數字一致。
- 本輪 **SRC 新庫尚未啟動、尚未匯入 Seed、尚未進行正式 C# API 估算**。下一檢查點依序 `dotnet build tools/graduation-synthetic/GraduationSeed`、啟動獨立 `127.0.0.1:55433 / bidding2a_graduation_src` PostgreSQL、確認健康且非既有資料，再執行受本機 DB/port/name 白名單保護的 Seed CLI。不可重跑 55432 舊庫，不可使用 `down -v`。
- 新的嚴格模式候選鋼骨／模板／土方仍 **非正式紅綠燈**；數量/價格情境判斷需未修改 baseline 對比與獨立 API 結果，不可把手動注入的倍率當成模型命中率。沒有改動程式碼或 DB schema，本次僅測試交接。


### 2026-11-21 — SRC 新獨立 PostgreSQL Seed 已在 Windows 真正完成 PASS
- 使用者執行 `docker compose -f tools/graduation-synthetic/compose.graduation-src-db.yaml up -d`，回傳獨立 `graduation-src-synthetic` Docker volume/network 建立，`graduation-src-synthetic-graduation-src-postgres-1` 啟動；`docker compose ... ps` 顯示 `127.0.0.1:55433->5432/tcp`、Postgres17 Alpine、`health: starting`（**健康狀態尚未重新確認**）。
- 使用者以環境變數 `GRADUATION_DB_CONNECTION` 指向 `Host=127.0.0.1;Port=55433;Database=bidding2a_graduation_src;Username=graduation` 並執行 `dotnet run --no-build -c Release --project .\\tools\\graduation-synthetic\\GraduationSeed\\GraduationSeed.csproj -- --seed-json .\\graduation-src-train-seed.json`。實際成功回傳：`{"Database":"bidding2a_graduation_src","DataOrigin":"SYNTHETIC","TrainingProjects":210,"MaterialMasters":100,"HistoricalTransactions":101166,"FinalBidRows":101166,"ExcludedHoldoutProjects":90,"TrainingOnly":true}`。
- **本階段資料庫匯入 PASS**，且明確 90 validation/test 專案未植入訓練歷史；新庫已非空，**禁止重跑 seed**、重建或刪除 volume。原 55432 `bidding2a_graduation` 完整保留，沒有任何正式 Supabase 操作。用戶未貼 `dotnet build` 的文字輸出，僅 `dotnet run --no-build` 的可執行結果可證實 Seed 執行成功。
- 下一驗證點：`docker compose ... ps` 重新確認健康、在獨立命令視窗將 `ConnectionStrings__DefaultConnection` 設為新的 SRC DB、確認當前 C# Git 分支確實含 `AnalysisCaseController` 及基地面積相似度，不可直接在未確認 branch 的情況覆蓋主系統。使用獨立 HTTP Port（例如5262）啟動 ASP.NET API → GET readiness → 唯讀城市/基地敏感度 → 完整 SRC baseline + quantity/major price 雙路徑 → 10 組注入標單 pairwise API 測試（互相關聯的衍生案不得充當獨立建案計數）；記錄正式結果而非注入的真值倍率。
- 模型嚴格模式維持鋼筋/混凝土範圍，鋼骨、模板、土方僅研究；正式量價異常是否符合注入預期、模型品質仍 NOT RUN。無本次新程式碼變更、Migration 或工程資料外洩。


### 2026-11-21 — 使用者指出多資料庫造成跨裝置遷移負擔；調整長期架構決策
- 使用者詢問為何 RC/SC 之前的 `55432/bidding2a_graduation` 與新增 SRC `55433/bidding2a_graduation_src` 分開，擔心換裝置麻煩。明確釐清：此分庫**僅為暫時隔離舊有 RC/SC E2E 成果，避免新版種子毀損既有資料**；並非每種結構應使用不同 PostgreSQL 的長期架構。
- **長期方向**：RC、SC、SRC 及歷史標單應在同一套 PostgreSQL 中按 `ProjectMaster.StructureType`、個案/版本/資料表分辨，避免新增結構就增加資料庫。現行新版 `55433` 已含三種結構的 210 train 建案和 101166 交易，可視作目前**整合測試的目標庫**，但尚未完成正式 SRC API E2E，也不代表正式 production 資料庫。原 `55432` 只保留歷史比較/回退，在新版正式驗證及備份策略完成之前不可刪除。
- **不能直接 append 合併新舊 Seed**：舊/新兩套合成資料共用 `SYN-xxxxx` ID，而新生成器變更材料量、結構類型及極端情境，盲目合併會造成跨建案關聯或基準污染。使用者尚未批准實際資料搬遷/刪除；本次不修改任何 DB。
- **換裝置策略**：程式與腳本/Compose 在 GitHub；合成測試資料靠固定 seed+manifest 重建；需要保存分析案件、真實資料或既存 DB 時，必須有加密/限制存取的 `pg_dump`、`pg_restore` 與版本相容的 EF Core Migration/建庫程序。**Git clone 不會攜帶 Docker Volume 或 DB 真實資料**。未來要建立單一權威測試庫的重建指南、完整可攜備份與還原演練，才可標記跨裝置流程 PASS。
- 本次為重要技術決策與架構釐清，僅同步更新交接；並未完成 DB 合併、搬遷、Migration 或跨裝置測試。


### 2026-11-21 — SRC 整合庫 API Readiness 與城市/基地敏感度 Windows 實測 PASS
- 使用者目前 C# Git branch `feature/1d-api-integration`。新 PowerShell 視窗呼叫 `docker compose -f tools/graduation-synthetic/compose.graduation-src-db.yaml ps` 顯示 `GRADUATION_SRC_DB_PASSWORD` 環境變數缺值而 Compose 插值失敗；**不是 PostgreSQL 啟動或 API 失敗證據**（環境變數只存在先前 PowerShell 工作階段，已匯入且 API 可用）；不要重新執行 seed、刪資料庫或重建容器。
- 使用者在 `http://127.0.0.1:5262` 成功讀取 `GET /api/analysis-cases/policy`、`GET /api/detection-category/readiness`、`GET /api/major-category-price/readiness`：數量偵錯 `categoryCount=2`,`readyCount=2`, 鋼筋 `historicalProjectCount=148`/`historicalTransactionCount=958`、混凝土 `historicalProjectCount=193`/`historicalTransactionCount=1215`，均為 `ReadyForEstimation`、最低樣本數 5。價格估算 `eligibleMetadataProjectCount=210`,`availableProjectCount=210`,`missingProjectCount=0`,`ReadyForEstimation`。
- 使用者實際執行 `verify_similarity_sensitivity.py --source .\\graduation-src-extreme-v3 --api http://127.0.0.1:5262 --output .\\graduation-src-city-site-audit.json`，回傳 `{"project_id":"SYN-00002","city_scores_changed":true,"site_area_scores_changed":true,"status":"PASS"}`。**僅證明相同 SRC 測試案改變城市/基地面積會使 API 參考案分數/排名不同**；尚未證明估算數量/價格的誤差、召回率或紅綠燈性能。
- 下一階段先取得一個 test-only SRC baseline 的真實 `POST /api/analysis-cases/from-upload`（multipart，帶全部工項表+建案 metadata）結果，再依 `analysisCaseId`、`analysisBidVersionId` 走 `quantity-analysis` 與 `major-price-analysis`，核對 1) 成功解析且不寫歷史 2) 鋼筋混凝土數量與 truth 3) 價格含單價 4) 有正常參考區間。基準案 PASS 之後才批次跑 10 個相互關聯的異常標單與 paired delta 回測。
- 本次為真實 Windows API 執行成果及環境變數辨析，沒有程式碼或 DB Migration 修改；正式 SRC 上傳/量價偵測仍 NOT RUN。


### 2026-11-21 — SRC baseline E2E Windows PowerShell HttpClient 缺程序集導致未上傳（失敗已定位）
- 使用者以 `feature/1d-api-integration` 在 `http://127.0.0.1:5262` 嘗試上傳 `SYN-00030` SRC 基準標單，truth 預期 `446` 筆。PowerShell 在 `[System.Net.Http.HttpClient]::new()`、`MultipartFormDataContent`、`StringContent`、`ByteArrayContent` 回報 `TypeNotFound`，致使 `$client`/`$form` 均為 null、multipart **根本未送出**；後續 `analysisCaseId`/`analysisBidVersionId` 空白，`quantity-analysis` 和 `major-price-analysis` 均回 404。此為客戶端前置問題，不代表 SRC parse 或模型演算法 FAIL；`ExpectedItems=446` 僅為 truth 預期值，不是 API 已解析數量。
- 修復驗證方式：Windows PowerShell 先明確執行 `Add-Type -AssemblyName System.Net.Http`，重新在同一視窗建立新的 HttpClient/multipart（不要沿用 null 變數），逐步檢查 HTTP 成功、JSON 有正整數 ID、`itemCount` 與 truth 比對，只有上傳成功才執行兩種分析；以 try/finally 釋放連線與 form，避免把空白 ID 誤判為可用。
- 無正式 DB/history seed 修改、Migration 或刪除；僅記錄使用者實測失敗與下一個正確檢查點。SRC baseline E2E 仍 NOT RUN（請待成功上傳後更新實際結果）。


### 2026-11-21 — SRC 第一份正式 baseline Excel 446/446 通過；基準已有鋼筋及價格紅燈；新增 13 件成對 API 批次工具
- 使用者上傳本機完整 PowerShell 回報（`貼上的文字 (1)(4).txt`），`feature/1d-api-integration` 上 `http://127.0.0.1:5262`，先以 `Add-Type -AssemblyName System.Net.Http` 修正 Windows PowerShell 5.1 缺少 HTTP 型別後，`SYN-00030` **SRC 未注入基準標單**成功 `POST /api/analysis-cases/from-upload`：`analysisCaseId=1`, `analysisBidVersionId=1`, `itemCount=446`，與 truth 446 完全一致（解析筆數 PASS）。`quantity-analysis` 成功建立 `analysisRunId=1`；`major-price-analysis` 成功建立 `analysisRunId=2`（API 完整流程 PASS）。
- 數量結果：totalGroupCount=2、analyzedGroupCount=2、insufficientDataCount=0、skippedGroupCount=0；**混凝土** observed=4237.490 m³，區間 3738.14～4938.99，Green/Normal，參考8案、有效樣本數7.11；**鋼筋** observed=448230.732 kg，區間500496.85～596353.22，Red/Low，參考8案、有效樣本數6.94。這個 **Red 出現在未經注入的 baseline**，不得當成特意注入異常的命中。
- 價格結果：結構體 observedAmount=98,123,625.84，區間104,691,527.43～150,234,001.55，observedPricePerPing=20,630.34，區間22,011.23～31,586.47，Red/Low，參考8案、有效樣本數7.37。**未注入基準本身即價格紅燈**，可能來自合成生成器自然差異/原有5.5% group_anomaly、歷史同結構相似性/樣本分布/估算方法等；不能在未核算來源前斷定「錯誤」或「造假」，也不能用新注入紅燈數字直接稱精準度 PASS。
- GitHub `main` 的 `tools/graduation-synthetic/generate_full_project_dataset.py` 每一群工項 `group_anomaly = project_rng.random() < .055`，遇到時會以 `.45` 或 `1.8` 乘量，證明合成基準「未額外注入」**不等於無任何變異/刻意異常**；三個來源基準不能作為模型誤報率的無污染獨立真值。必須進一步整理層級標籤、用更多正常 holdout 做正式回測。
- 新增 `tools/graduation-synthetic/audit_paired_anomaly_api.py` commit `2ace863c`：先以 test-only + XLSX SHA 驗證13件 (3 base + 10 variants)，預設 dry-run 無 HTTP/DB 寫入，僅 `--execute` 對 local 5262 逐件呼叫上傳、數量分析、價格分析；readiness 檢查、非本機地址拒絕、回傳 IDs+解析筆數守門、失敗保留 partial；輸出 JSON `results` 及同來源案 `paired_deltas`，不動歷史交易/Scope/DB Schema。此批次執行**會建立 AnalysisCase/AnalysisRun**（target pipeline），不是純唯讀。
- 新增 `tools/graduation-synthetic/test_paired_anomaly_api.py` commit `a9a68d01`：純離線測試基準已 Red 仍可正確計算注入前後 delta、只摘要核准類別、不誤納鋼骨為正式 category。 `tools/graduation-synthetic/README.md` commit `1975db2d`：記錄 Windows dry-run/execute/成對報告查看方式。
- **新增腳本/tests 尚未執行 Windows unittest 或正式 13 件 API 批次，NOT RUN**。不修改後端/前端/DB/Migration，不重跑 train seed，不清除 volume。下一驗證點：檢查 git scoped 檔案無本機未提交更動 → 更新工具腳本 → unittest → dry-run13件 SHA / test-only → execute API → 比對實際觀測值變化、燈號變化、原始基準已紅的混淆情形，決定是否要重修合成資料的 clean-baseline 定義。正式鋼骨/模板/土方候選仍禁止直接紅綠燈。


### 2026-11-21 — 新成對 API 測試工具在 Windows 未取得：Git fetch origin/main ref 競態；21 舊測試 PASS
- 使用者在 `feature/1d-api-integration` 執行 `git fetch origin main`，遠端物件下載完成但最後出現 `error: fetching ref refs/remotes/origin/main failed: incorrect old value provided`。此為遠端追蹤 ref 更新衝突（可能同時有另一 Git 程序更新；原因尚未證實），**不能假設 FETCH_HEAD 指向最新 commit**。
- 其後 `git restore --source=FETCH_HEAD --worktree -- tools/graduation-synthetic/audit_paired_anomaly_api.py tools/graduation-synthetic/test_paired_anomaly_api.py` 均回 `pathspec ... did not match any file(s) known to git`；本機未取得新檔。此次 `python -m unittest ...` 仍是 **21 tests OK**，為舊測試，**沒有**跑到新 `test_paired_anomaly_api.py`。執行新的 dry-run、`--execute` 都回 `No such file or directory`，故尚未建立 13 筆新分析案件；報告 JSON 不存在屬連帶結果。
- 重新以 GitHub connector 驗證遠端 `main` 確實含 `audit_paired_anomaly_api.py`、`test_paired_anomaly_api.py`。下一修復採 **`git fetch --no-tags --refmap= origin main`**（空 refmap 避免依 config 同步更新衝突的 `origin/main`，只取得 FETCH_HEAD），然後明確檢查 `git cat-file -e "FETCH_HEAD:tools/..."`（兩檔都存在）才 `git restore --source=FETCH_HEAD --worktree -- <兩個檔案>`。不要使用 `reset --hard`、`clean -fd`、強制更新 `origin/main`、刪資料庫或重 seed。
- 下一驗證點：兩個腳本在 Windows `Test-Path` 成功 → unittest 預期新增 2 測試（共 23）→ dry-run 13 SHA/test-only 驗證 → 用既有 localhost:5262 + 55433 API 進行成對分析。若仍報 fetch ref 競態，先輸出 `git remote -v`、`git rev-parse FETCH_HEAD`、`git status -sb` 查核再採更精確修復，不覆蓋 feature 工作樹。
- 此輪僅因真實使用者回報修訂交接；未修改應用程式、資料庫或 Migration。


### 2026-11-21 — Git refmap 修復後，成對 API 稽核腳本 23/23 單元測試與 13/13 dry-run PASS
- 使用者於 Windows `feature/1d-api-integration` 執行 `git fetch --no-tags --refmap= origin main`，成功只下載 FETCH_HEAD，避免先前 `origin/main` 遠端追蹤參照競態。由 FETCH_HEAD 執行 `git cat-file -e` 與 `git restore --source=FETCH_HEAD --worktree -- tools/graduation-synthetic/audit_paired_anomaly_api.py tools/graduation-synthetic/test_paired_anomaly_api.py`，`Test-Path` 兩檔均 `True`。使用者回報 status scoped 沒有既有未提交修改。
- `python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v` **Ran 23 tests in 19.459s，OK，23/23 PASS**；新增 `test_paired_delta_keeps_same_project_and_existing_baseline_red` 和 `test_summary_tracks_only_approved_quantity_categories` 均 OK。其間 `test_refuse_overwrite` 觸發 `argparse ... output directory must be empty` 是刻意的安全測試，整體 OK。
- `python tools/graduation-synthetic/audit_paired_anomaly_api.py --suite .\\graduation-anomaly-holdouts --api http://127.0.0.1:5262 --output .\\graduation-src-paired-api-report.json` 未加 `--execute` 回傳 `{"status":"DRY_RUN","verifiedXlsxAndTruth":13,"api":"http://127.0.0.1:5262","next":"run again with --execute to create 13 analysis cases"}`。**13份標單/test-only真值/XLSX SHA 存在及一致性檢查 PASS**，此階段沒有 HTTP 上傳/DB 寫入。注意 DRY_RUN 也沒有驗證 HTTP API 實際仍連接指定 55433 DB。
- 下一階段在明確確認 `5262` 連接本機新 `55433/bidding2a_graduation_src` 後，執行相同腳本加 `--execute`；此步將建 13 個 AnalysisCase 與最多 26 個 AnalysisRun（target pipeline），不能重跑或在正式庫執行。完成後從 `graduation-src-paired-api-report.json` 讀 paired_deltas，區分 baseline 原有 Red、注入後 status 變化，避免錯算為獨立樣本或準確率；中途錯誤保留 `.partial.json` 做復原。嚴格模式鋼骨仍不納入正式紅綠燈。
- 無程式碼 / Migration / DB schema 更動；本次僅更新實際測試結果交接。


### 2026-11-21 — 13/13 heldout workbook 實際 ASP.NET API 批次 E2E 完成，26/26 分析成功；尚非準確率 PASS
- 使用者在 Windows `feature/1d-api-integration` 呼叫 `audit_paired_anomaly_api.py --output .\\graduation-src-paired-api-report.json --execute`，API 為預設 `http://127.0.0.1:5262`（指向之前已驗證的整合測試資料庫）。真正回傳 `{"status":"API_COMPLETED_NOT_ACCURACY_PASS","baselines":3,"variants":10,"report":"graduation-src-paired-api-report.json"}`。13件依序通過 `SYN-00029` RC baseline 409、`SYN-00030` SRC baseline 446、`SYN-00026` SC baseline 368，及 10個不同量價變體；每案都產生有 ID 的 Quantity/Price AnalysisRun（共26次，run IDs 3～28），上傳明細數均與對應基準一致。這是**實際 API 完整流程 PASS**，不是量價模型準確率 PASS。另先前 SRC `SYN-00030` 已獨立上傳過 case1、run1/2，勿把衍生案視為獨立觀測或無條件重跑匯入腳本。
- 使用者從 `paired_deltas` 摘要貼出10案的 `priceObservedDelta` 與價格燈號：`design_concrete_high` RC +19,936,369.06 Green→Green；`design_concrete_low` SRC -6,451,452.19 Red→Red；`design_rebar_high` RC +64,169,368.15 Green→Red；`design_rebar_low` SRC -24,119,919.24 Red→Red；`design_steel_high` SC +14,394,046.96 Red→Green；`design_steel_low` SRC -10,915,153.98 Red→Red；`structural_price_high` SRC +197,265,261.17 Red→Red；`structural_price_low` RC -72,923,249.10 Green→Red；`combined_concrete_high_price_high` SRC +241,929,179.40 Red→Red；`combined_concrete_low_price_low` RC -74,651,069.54 Green→Red。
- **解讀限制**：上述是結構體**總價**燈號，不是量異常燈號；數量修改必然影響金額，但不能據此判定是單價異常。原始 SRC baseline 的鋼筋數量/結構總價 Red 已在上一輪確認；基準本身可能有自然/注入前生成器變異。Red→Red 情境無法單從 TrafficLight 判斷是否由 Low 跨越到 High；Red→Green 也可能是原本偏低的金額進入區間。不能將上述數列算為召回率、誤報率或準確度，不得使鋼骨正式 quantity red/green 解禁。
- 下個驗證點：**不再重跑 `--execute`**，利用現有 `graduation-src-paired-api-report.json` 比對每案同來源 baseline 與 variant 的**鋼筋/混凝土 observedQuantity、lower/upperQuantity、analysisStatus、trafficLight**及價格 **observedAmount、lower/upperAmount、analysisStatus**，確認 high/low 方向與經驗合理性，列出既有 baseline Red 與真正跨燈情況；若要正式評估誤報/漏報率，另需更大且獨立、確定正常的 holdout 標單及設計/單位標籤檢查。
- 本次沒有修改應用/資料庫 schema 或 migration，僅記錄 E2E 真實結果；最終資料庫維持單一 RC/SC/SRC 方案，原 55432 比對用 DB 仍不得刪除。


### 2026-11-21 — 13 案量價成對回測完成狀態方向診斷（真實 Windows API）
- 使用者從 `graduation-src-paired-api-report.json` 完整提供 13 案**正式數量**與**結構體總價**的 `observed/lower/upper/analysisStatus/trafficLight`，此輪比上輪僅燈號摘要多了 Low/Normal/High 方向。三來源 baseline：RC `SYN-00029` 混凝土 3885.108（3463.0～6146.12）Normal/Green；鋼筋496658.648（567635.47～1055828.46）Low/Red；結構總價91153245.71（69512978.87～148060696.08）Normal/Green。SRC `SYN-00030` 混凝土4237.49（3738.14～4938.99）Normal/Green；鋼筋448230.732（500496.85～596353.22）Low/Red；總價98123625.84（104691527.43～150234001.55）Low/Red。SC `SYN-00026` 混凝土1382.831（1183.63～1628.52）Normal/Green；總價14892313.82（27552456.71～39099327.08）Low/Red；SC 無正式鋼筋量項、鋼骨未核准數量紅綠燈。
- 數量變化與合理區間：RC `design_concrete_high` 混凝土3885.108→9712.773，Normal→High；SRC `design_concrete_low`4237.49→1483.122，Normal→Low；RC `design_rebar_high`496658.648→1191980.756，Low→High；SRC `design_rebar_low`448230.732→179292.293，Low→Low（原本已低，不能當新跨燈命中）；SRC `combined_concrete_high_price_high` 混凝土4237.49→10593.727，Normal→High；RC `combined_concrete_low_price_low`3885.108→1359.788，Normal→Low。僅改單價時，鋼筋/混凝土數量與燈號維持一致；鋼骨 high/low 未產生正式鋼骨數量偵測類別（依嚴格模式）。
- 結構總價方向：`design_concrete_high` RC 91,153,245.71→111,089,614.77 Normal→Normal；`design_concrete_low` SRC 98,123,625.84→91,672,173.65 Low→Low；`design_rebar_high` RC 91,153,245.71→155,322,613.86 Normal→High；`design_rebar_low` SRC 98,123,625.84→74,003,706.60 Low→Low；`design_steel_high` SC 14,892,313.82→29,286,360.78 Low→Normal；`design_steel_low` SRC 98,123,625.84→87,208,471.86 Low→Low；`structural_price_high` SRC 98,123,625.84→295,388,887.01 Low→High；`structural_price_low` RC 91,153,245.71→18,229,996.61 Normal→Low；`combined_concrete_high_price_high` SRC 98,123,625.84→340,052,805.24 Low→High；`combined_concrete_low_price_low` RC 91,153,245.71→16,502,176.17 Normal→Low。所有同來源案對應上下界不變，觀察到的方向符合注入倍率。**不能把用量增加造成總金額升高說成單價過高**，也不能將 Low→Low 或原本 Red→Green 誤報成異常新增命中。
- 判讀：正式 API 流程與注入變化方向驗證 PASS；沒有統計上足夠的獨立基準正常樣本，RC/SRC 的鋼筋與 SRC/SC 的總價 baseline 已紅，不足以報告 precision/recall/false-positive rate 或實際工程性能。生成器已知有 `group_anomaly` 5.5% 情境及其他變異，未修改的 baseline ≠ 嚴格正常標籤。下一研究工作應先建立清潔/明確標記的獨立 holdout，按結構型別分層、分別分析量與價格及工程設計影響，不要現在就提高門檻或擴大鋼骨紅綠燈。
- 沒有程式碼、DB Schema、Migration 或 Seed 更動；此為使用者 Windows 實測的分析交接。


### 2026-11-21 — 2A 正式轉入專案收斂/驗收階段（scope freeze）
- 使用者明確決定「慢慢收斂專案」，不再優先增加任意新功能或新資料庫。已在 `main` 新增 `docs/PROJECT_CLOSURE_PLAN.md`，commit `3e46177c579e75f9a7d5d4e5cf553adea0a88793`；內容固定核心範圍（RC/SC/SRC 單庫、鋼筋/混凝土數量、結構體大項價格、標單上傳/分析紀錄）、現有 Windows 測試證據、P0 模型有效性/feature-main 整合門檻、P1 備份還原/部署交接，以及風險和禁止重跑/刪除事項。
- 目前已驗證：Windows Python unittest 23/23；城市/基地面積相似度敏感度；獨立 SRC DB train-only 210 案/101166交易；13份 API Excel/26分析成功；受控量價高低方向大體符合注入邏輯。但 3 baseline 中鋼筋或大項價格原已 Red，故正常誤報率、實際準確性、外部有效性**尚無足夠證據**。不拿 10 個衍生情境當 10 個獨立建案。基準乾淨與可追溯標籤是下一個最先處理的證據缺口。
- `main` / `feature/1d-api-integration` 差異尚未整合，Vue UI 全流程、build/CI、跨裝置 DB 恢復等仍需驗證。保留既有 `55432` 和 `55433`；不重 seed、不 `down -v`、不修改正式 Supabase。嚴格模式下未核准鋼骨/模板/土方仍不上線。
- 此次僅新增驗收/收斂文件與同步交接；**未修改應用/資料庫/模型，未執行新的 Python/.NET/E2E 測試**。下一執行點：先建可靠正常 holdout 標籤及明確異常真值，再依現有不變報告檢驗誤報/漏報；不要再重做已 PASS 的 13 案 API 批次。


### 2026-11-21 — 收斂 P0：已實作不破壞既有資料的 test holdout 來源倍率稽核（待 Windows 驗證）
- 使用者要求直接繼續專案收斂。檢視 `main` `generate_full_project_dataset.py` 發現**已有逐交易 synthetic_label**：生成器對每個工項群組以約5.5%機率將數量乘 0.45 或 1.8，同群組產生的歷史交易列註記 `injected_anomaly`，其餘為 `baseline`；`project_boq_rows.csv` 可以由 `synthetic_transaction_id` 與歷史列連接。這提供可追溯的「有無生成器刻意倍率」證據，不必重建 300 筆建案或重 seed；**但它不是獨立工程設計合理性真值**。
- 新增 `tools/graduation-synthetic/audit_holdout_provenance.py` commit `62a7415d`：read-only SHA256、筆數、交易/BOQ 映射與 split 驗證；按 RC/SC/SRC 及 concrete/rebar/structural_price 彙整 test-only 來源筆數、群組倍率列數、各來源建案可用 scope；可選同時讀取現有 `graduation-src-paired-api-report.json` 的 3 基準 status，映射其群組倍率證據。不適用類別（如 SC rebar）不硬算；`engineeringNormalCertified=false`、`readyForFalsePositiveRate=false`，不能把沒有 group shock 當正常樣本或把未注入基準 Red 當正式 false positive。
- 新增 `tools/graduation-synthetic/test_holdout_provenance.py` commit `f67a4548`：離線 mock CSV 測試 scope/shock/non-normal 声明、hash 篡改拒絕、baseline Red 不推導誤報，預期單元測試從 23 增加至 26。新增 README 使用方法 commit `7d03bb3c`，更新 `docs/PROJECT_CLOSURE_PLAN.md` commit `689ffbe1`。
- **未執行**使用者 Windows `python -m unittest`（26預期）、未讀取當地來源產生 provenance report、未新增 API/DB/Migration/模型參數或更動歷史資料。下一驗證點：`git fetch --no-tags --refmap= origin main` + 只還原新增兩工具（先確認未提交同名檔案） → unittest 26/26 → 只讀 `audit_holdout_provenance.py --source .\\graduation-src-extreme-v3 --paired-report .\\graduation-src-paired-api-report.json --output .\\graduation-src-holdout-provenance.json`；以實際輸出確認 contamination 分布，再規劃合成乾淨驗證集或業界工程設計標籤。
- 收斂期間不再重做 13 件 `--execute` 或清空 `55433/55432`，亦不擴大鋼骨正式紅綠燈。舊 `main`/feature API 差異待 P0 下一項整合。


### 2026-11-21 — P0 正式 Windows Holdout Provenance 稽核結果：26/26 PASS、45 Test、3 Baseline
- 使用者在 `feature/1d-api-integration` 成功自 GitHub `main` 取得 `audit_holdout_provenance.py`、`test_holdout_provenance.py`。在 Windows `python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v`：**Ran 26 tests in 19.041s, OK**，包含 3 個新增正常標籤/篡改保護測試全部 PASS。刻意的 `test_refuse_overwrite` 測試列印錯誤訊息但 unittest 狀態仍 OK。
- 實際執行 `audit_holdout_provenance.py --source .\\graduation-src-extreme-v3 --paired-report .\\graduation-src-paired-api-report.json --output .\\graduation-src-holdout-provenance.json` 回傳 `{"status":"PROVENANCE_AUDITED_NOT_ACCURACY","testProjectCount":45,"pairedBaselines":3,"readyForFalsePositiveRate":false}`。只讀 CSV SHA 與 label，不寫 DB、不呼叫正式 API。
- Test RC18（非 extreme16）：混凝土 18 無 shock／0有、鋼筋 17／1、結構大項15／3。SC17（非 extreme15）：混凝土15／2、鋼筋不適用、結構大項11／6。SRC10（非 extreme8）：混凝土10／0、鋼筋10／0、結構大項7／3。這些是**生成器記錄倍率變化篩檢**，不是正常值保證，也不是模型準確率。
- 3 Baseline 交叉：RC `SYN-00029` 混凝土 Normal/0、鋼筋 Low/0、結構總價 Normal/0；SRC `SYN-00030` 混凝土 Normal/0、鋼筋 Low/0、結構總價 Low/0；SC `SYN-00026` 混凝土 Normal/0、結構總價 Low/**4 筆原始生成器 shock**。因此 RC/SRC 的基準紅燈不是已記錄的 group shock 直接引起；SC shock 可能與總價有關但未驗證因果。均 `engineeringNormalCertified=false`，不可用來宣稱誤報率。
- 已更新 `docs/PROJECT_CLOSURE_PLAN.md` commit `de881d5d3e4c9b5e33d073886f69ea24fb8de8c0`。下一點：對 3 baseline Low 根據正式 API 的對照組成與相似建案資訊做只讀離線分析，確認是否為合成結構分布/材料 scope/區間邊界之問題，再決定如何建立經審定正常測試真值。不要重跑13次 `--execute` 或重 seed、改模型門檻；沒有程式碼/資料庫/Migration 變更。


### 2026-11-21 — P0 第二階段：3 baseline 已保存 AnalysisRun 的唯讀參考組診斷工具（待 Windows 驗證）
- 使用者表示「可以繼續」，在原 26/26 Python 單元測試、45 test provenance 已 PASS 的基礎上，開始對三個 baseline 原有 Low 做診斷，不重跑已完成的13案、26次分析。
- 已從 GitHub `main` 和 `feature/1d-api-integration` 查核最新交接/README、`AnalysisCaseController.cs`、`AnalysisCaseService.cs`、`ProjectSimilarityService.cs`；feature 路由確實存在 `GET /api/analysis-cases/{analysisCaseId}/runs/{analysisRunId}`，回傳已保存的 `AnalysisRun.ResultJson`。本次只讀此路由，禁止 POST 和改資料庫。
- 已在 `main` 新增 `tools/graduation-synthetic/diagnose_baseline_low.py` commit `c5d572f1`，查 `graduation-src-paired-api-report.json` 的 3 個 baseline run IDs，與 `graduation-src-holdout-provenance.json` 交叉驗證，按 concrete/rebar/structural_price 計算 observed 對上下界偏離比例、參考建案數/結構/分數/年距/權重及來源群組 shock。從 `graduation-src-extreme-v3/projects.csv` 的 SHA 驗證來源專案結構。任何跨結構參考僅標記為待查，不直接宣稱 bug；任何「無 shock」不算工程正常真值或可計誤報率。
- 新增 `tools/graduation-synthetic/test_diagnose_baseline_low.py` commit `459a70d7`，4 個純離線測試涵蓋下界/上界距離、跨結構參考標記及非法範圍拒絕；README 新段落 commit `a6a00b3a`、`docs/PROJECT_CLOSURE_PLAN.md` commit `38b3078c`。
- **未執行**使用者 Windows 新 unittest（預期 30 項）、GET 保存分析紀錄或輸出 JSON；未修改 ASP.NET/Vue/DB/EF Migration/生成器或估算參數。下一驗證點：安全 fetch 新兩工具到當前 feature 分支 → Windows `python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v` 30/30 → 執行 `diagnose_baseline_low.py --api http://127.0.0.1:5262 --source .\\graduation-src-extreme-v3 --paired-report .\\graduation-src-paired-api-report.json --provenance .\\graduation-src-holdout-provenance.json --output .\\graduation-src-baseline-diagnosis.json` → 使用實際數據決定是否需要模型/相似度修正。不要重 seed、不要刪 55432/55433、不要再跑13件 --execute。


### 2026-11-21 — 基準 Low 唯讀診斷 30/30 PASS；開始 main/feature 分支整合風險稽核
- 使用者 Windows `git fetch --no-tags --refmap= origin main` 成功，`git restore` 取得 `diagnose_baseline_low.py` 與對應 test；`python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v` **Ran 30 tests in 19.065s, OK**（含最新4項 PASS）。
- `python tools/graduation-synthetic/diagnose_baseline_low.py --api http://127.0.0.1:5262 --source .\\graduation-src-extreme-v3 --paired-report .\\graduation-src-paired-api-report.json --provenance .\\graduation-src-holdout-provenance.json --output .\\graduation-src-baseline-diagnosis.json` 回傳 `READ_ONLY_DIAGNOSTIC_NOT_ACCURACY`、`baselines=3`、`createdRuns=0`，代表讀取既有 GET 成功而無新分析紀錄。
- 診斷結果：RC SYN-00029 鋼筋 Low 距下限 12.504%、generatorShockedLines=0；SRC SYN-00030 鋼筋 Low 10.443%/0，結構體總價 Low 6.274%/0；SC SYN-00026 結構體總價 Low 45.949%/4。其餘混凝土均 Normal。輸出 CrossStructureReferences 空白，不代表已驗證所有參考案 lineage（需查 unresolvedReferenceIds）。沒有無異常工程真值，不能直接算誤報率或為消紅燈調模型。
- GitHub 正式比較 `main...feature/1d-api-integration`：`status=diverged`、feature ahead=619/behind=167、171 file diff；反方向 main ahead=167/behind=619、41 file diff。共同 merge base `b18e99dacb44476a60dd13101695e643c52ecc38`。Feature 有目前運行且經 E2E 的 ASP.NET API/Vue 案件 UI/DB SQL；main 有最新 synthetic 工具與 `docs/AI_HANDOFF.md`、收斂計畫；兩側 README 均不同。不能直接大合併或覆蓋已驗證 feature 工作樹。
- 已新增 `docs/BRANCH_INTEGRATION_AUDIT_2026-11-21.md` commit `52ddbc3c`，逐群組記錄差異/整合風險/阻擋條件/驗證計畫。**這次沒有真的合併、沒有 PR、沒有修改 ASP.NET/Vue 或 DB/EF Migration，沒有執行 build/部署**；下一個驗證點是在本機現用 feature branch 取得 `git status -sb`、`git rev-parse HEAD`、兩側 head，於獨立整合分支逐步解衝突，先跑 dotnet/npm/Python，保留 55432/55433 現有資料與已通過報告。


### 2026-11-21 — 本機 feature HEAD/未提交工作檢查，遠端 13 commits 差異已由 GitHub 驗證（尚未合併）
- 使用者 Windows `git status -sb`：本機分支 `feature/1d-api-integration`，`HEAD=97f7cfdcd04cd6d3b2eb489bf3512ed24323535f`，落後 `origin/feature/1d-api-integration` 13 commits，且 tracked 檔案 `compose.fault-test.yaml`、`frontend/src/components/WorkbookSheetPicker.vue` 有未提交修改；未追蹤包括 `compose.fault-test.local-backup.yaml`、`tools/`、`tests/fault-fixture/`、多個 `graduation-*` CSV/Excel/報告、`schema-review.sql`。不得隨意 pull、clean、hard reset、stash -u 或 checkout 覆蓋。
- GitHub connector 對 `97f7cfd...feature/1d-api-integration` 比較**確定遠端較本機多 13 commits，0 behind，共 12 個差異檔案**：`frontend/src/components/WorkbookSheetPicker.vue`（與本機未提交變更直接重疊，最高風險）、`frontend/src/views/AnalysisCaseDetailView.vue`、`崴正/Services/ProjectSimilarityService.cs`、`tools/graduation-synthetic/SimilarityContract/Program.cs`、`.../SimilarityContract.csproj` 及 7 個 docs/交接資料。沒有發現遠端 13 commits 更動 `compose.fault-test.yaml`，但本機修改來源仍待確認。
- `tools/` 整個目錄未追蹤可能含重要 README/tests/scripts，後續與遠端同路徑工具衝突風險不能忽視；測試產物不得直接 git add、push 或公開。下一步只讀 `git diff -- compose.fault-test.yaml frontend/src/components/WorkbookSheetPicker.vue` 與 `git ls-files --others --exclude-standard tools/`，再 `git fetch --no-tags --refmap= origin feature/1d-api-integration` + `git log --oneline HEAD..FETCH_HEAD` + `git diff --name-status HEAD..FETCH_HEAD`，確認本機與遠端同名程式衝突；先備份兩個 tracked 變更與未追蹤測試資料、獨立 integration worktree 才能安全整合。此次**未合併、未修改/刪除本機資料、未執行 build**。


### 2026-11-21 — 收斂版本整合：本機 WorkbookSheetPicker 修改疑似不完整；SRC DB/新參考組政策列為 P0
- 使用者提供完整 Windows 本機 diff/log：`compose.fault-test.yaml` 本地改動加入 `tests/fault-fixture/bidding2a-fault-schema.sql` 的唯讀 initdb mount、api_access bridge network；應先保留和獨立驗證隔離安全性，不能覆蓋。 `frontend/src/components/WorkbookSheetPicker.vue` 有新增未辨識工作表選取功能，但 diff 同時插入未完成的 `<button type="button"...>` 與 `<span...`，疑似模板語法缺陷；**沒有跑 npm build，不能當成語法確定 FAIL**。遠端 feature 已有 `7e27271` 相關完整修正，不應直接 pull 覆蓋本機。
- `git ls-files --others --exclude-standard tools/` 共有 30 餘個不追蹤 synthetic tool scripts、GraduateSeed、測試等；大量 `graduation-*` 檔、`tests/fault-fixture/` 與 `schema-review.sql` 均在本機未追蹤；禁止 `git clean -fd`、`git reset --hard`、`git add .`、無選擇性 stash -u 或原地強制更新。
- `git fetch --no-tags --refmap= origin feature/1d-api-integration` 成功；`git log HEAD..FETCH_HEAD` 驗證 13 commits，遠端頭 `86c068b`；最後 `86c068b fix(2a): prevent SRC similarity from using RC/SC historical cohorts`，以及 `8263675` 相似度 C# 測試、`b355b28` SimilarityContract 測試工具、`7e27271` 手動選取0筆工作表與其他UI/文件修正。遠端差異共12個檔案；與本機唯一直接 tracked overlap 是 `WorkbookSheetPicker.vue`。
- **SRC 問題必須區分**：(A) 55433 `bidding2a_graduation_src` 已有整合 test train-only 210 案/101166歷史交易且SRC E2E 已通過，但這是**合成測試資料庫，不等於正式 Supabase 已有 SRC approved 歷史資料**；(B) 本機老 `97f7cfd` 尚無最新 `86c068b` 僅 SRC cohort 相似度修正，因此先前13件 API E2E 的結果**不能當作新 SRC 嚴格參考選取政策已通過**。若正式 DB SRC 歷史不足，須顯示不足而非借RC/SC給紅燈。
- 新增 `tools/graduation-synthetic/check_src_db_readonly.sql` commit `bd077327`，只查隔離 55433 project_master 各結構/核准/有效面積、historical_transaction 各結構建案/交易列數/未連標單列、SRC approved count，`BEGIN TRANSACTION READ ONLY`；未執行 Windows SQL。新增 `docs/SRC_DB_AND_BRANCH_INTEGRATION_2026-11-21.md` commit `70624dd2`，提供 safe worktree 與 read-only SQL 路徑；無 model/EF/DB 變更。
- 下一驗證點：確保新 `../BiddingSystem-2A-integration` 路徑及 `integration/src-closure` branch 未存在，重新 fetch 遠端 feature → `git worktree add -b integration/src-closure ../BiddingSystem-2A-integration FETCH_HEAD`，在新 worktree 先建置 `dotnet build`、`npm ci && npm run build`、SimilarityContract；在原 worktree 使用 fetch main 的唯讀 SQL 對 55433 採樣。**新工作樹/DB 均尚未動工**，待使用者實際 Windows 日誌；保留55432/55433 volumes、所有報告，不重 seed、不再用 --execute 重跑13案。不能直接合併 main 或宣稱已修完 SRC DB。


### 2026-11-21 — 新 worktree 最新 feature 建置及 SRC SQL 指令阻礙實測
- 使用者已在本機成功建立獨立 worktree `C:\\Users\\User\\Desktop\\BiddingSystem-2A-integration`，分支 `integration/src-closure` HEAD=`86c068b`。再次 `git worktree add -b integration/src-closure` 因分支已存在而拒絕，**非失敗的初始化**，不要重做/刪目錄。
- `dotnet build .\\崴正\\崴正.csproj -c Release` **PASS**（6.7s），1 個 `TenderImportInspectionService.cs:135 CS8604` 的可能 null Unit 警告，待下一組小範圍修復。
- `dotnet run -c Release --project .\\tools\\graduation-synthetic\\SimilarityContract\\SimilarityContract.csproj` **PASS**（9 項 SRC/RC/SC 結構、城市、基地面積 C# contract checks）；有 `Microsoft.EntityFrameworkCore.Relational` 10.0.4 vs 10.0.5 `MSB3277` 版本衝突警告。屬需要整理 NuGet 引用一致性的警告，不可稱已解決；此合約離線執行不等於新 SRC 嚴格 DB/API E2E。
- 使用者在 `...\\BiddingSystem-2A-integration\\frontend` 執行 `npm ci` 和 `npm run build`，被未簽署的 `npm.ps1` PowerShell policy 阻擋，**未真正開始 Vue 安裝/編譯**，應在同資料夾用 `npm.cmd ci`、`npm.cmd run build`，不需修改全域 execution policy。
- 在 `...\\integration\\frontend` 執行 `docker compose -f tools/graduation-synthetic/compose.graduation-src-db.yaml ...` 因**相對路徑被解讀到 frontend/tools**，回找不到檔案；**未接觸 DB，更未執行 SQL**。正確做法先 `Set-Location 'C:\\Users\\User\\Desktop\\BiddingSystem-2A'` 回到原工作樹（其中 `tools/graduation-synthetic/compose.graduation-src-db.yaml` 存在），確認 `docker compose ... ps` 指向 `graduation-src-postgres` 和 55433；再於原工作樹 `git fetch --no-tags --refmap= origin main` 取得 SQL 並 `git show FETCH_HEAD:tools/graduation-synthetic/check_src_db_readonly.sql | docker compose -f tools/graduation-synthetic/compose.graduation-src-db.yaml exec -T graduation-src-postgres psql -v ON_ERROR_STOP=1 -U graduation -d bidding2a_graduation_src`。若 compose 需要 `GRADUATION_SRC_DB_PASSWORD`，只在本機設已知測試環境密碼，絕不貼出。
- 新結果與狀態：worktree/latest-feature .NET build PASS with warning、C# offline contract PASS with warning；Vue **NOT RUN**、SRC DB read-only cohort query **NOT RUN**；未動 DB/migration/seed，原本 55432/55433 保留。等使用者實際回傳 Vue build 和 DB counts 後才判斷 SRC DB 問題，不直接宣稱正式 Supabase 已有 SRC。


### 2026-11-21 — 最新 feature Vue 建置 PASS，SRC Compose 因缺測試環境變數尚未查 DB
- 使用者在獨立 `BiddingSystem-2A-integration/frontend` 以 `npm.cmd ci` 成功安裝 49 個套件（audit 50 套件），輸出 **3 high severity vulnerabilities**；尚未蒐集 `npm audit` 逐項資訊，不能盲目 `npm audit fix --force`（可能跨主版破壞穩定性）。
- `npm.cmd run build` 真正執行 `vue-tsc --noEmit && vite build`，Vite 7.3.6，**66 modules transformed、built in 1.73s、PASS**，包含 `WorkbookSheetPicker.vue` 的遠端完整版本。這不代表 UI 實際操作 E2E 已驗收。
- 從新 worktree frontend 跳回舊 `C:\\Users\\User\\Desktop\\BiddingSystem-2A` 後，`docker compose -f .\\tools\\graduation-synthetic\\compose.graduation-src-db.yaml ps` 因新 PowerShell 無 `GRADUATION_SRC_DB_PASSWORD` 環境變數而在 YAML interpolation 階段拒絕；**沒有觸及 DB、不代表資料庫未啟動或損壞**。下一步可使用 Docker 既有 compose labels 找出 `graduation-src-synthetic` / `graduation-src-postgres` 容器，核對 port 55433/5432 後，`git show FETCH_HEAD:tools/graduation-synthetic/check_src_db_readonly.sql | docker exec -i <verified-container-id> psql -v ON_ERROR_STOP=1 -U graduation -d bidding2a_graduation_src`（從容器本地 socket 執行只讀 SELECT），不依賴 compose 變數、不詢問/記錄 DB 密碼。不符合 label/port 條件應停下核實，禁止盲目猜容器。
- 目前 `dotnet build` PASS（CS8604 1 warning）、`SimilarityContract` PASS（MSB3277 EF Relational 10.0.4/10.0.5 conflict warning）、`npm ci` PASS with 3 high advisories、`vue-tsc && vite build` PASS、SRC DB 資料查核 **NOT RUN**；禁止 `down -v`、重 seed、改既有 55433/55432。待結果後再做新版嚴格 SRC API/DB E2E，不得以舊 run 宣稱新 cohort PASS。


### 2026-11-21 — 55433 SRC 歷史資料唯讀 SQL PASS、npm 依賴安全性 3 high 待修復
- 使用者在舊工作樹透過 `docker ps` 以 compose project/service label 唯一辨識現有容器 `bfee2f6e84bc`，並確認 `docker port <id> 5432/tcp` 為 `127.0.0.1:55433`。從 GitHub main 的 `tools/graduation-synthetic/check_src_db_readonly.sql` 管線至 `docker exec -i <container> psql -v ON_ERROR_STOP=1 -U graduation -d bidding2a_graduation_src` 真正成功執行，SQL `BEGIN TRANSACTION READ ONLY` 至 `COMMIT` 正常結束。
- `project_master` 未刪除/Approved：RC78、SC55、SRC77，全部有正總樓地板及基地面積，共210。 `historical_transaction` 非刪除：RC 39,453（78個有交易建案）、SC 24,209（55案）、SRC 37,504（77案），合計101,166筆，`final_bid_item_id IS NULL` 在上述各結構都是0。**SRC 77核准案及37,504交易存在、連接完整；不是 SRC 歷史零筆**。但這是`55433` synthetic isolated DB，**不能推出正式 Supabase 有 SRC 來源、已通過外部工程真值或最新版 `86c068b` SRC-only policy E2E**。
- 使用者獨立整合工作樹 `integration/src-closure`（feature HEAD `86c068b`）已 `npm.cmd ci` 成功（49 packages, audit 50），`vue-tsc --noEmit && vite build` PASS（Vite 7.3.6，66 modules，1.73s）；`npm.cmd audit --omit=dev` 與 `npm.cmd audit` 均報 **3 high**（Vue 3.5.22 / @vue/server-renderer <=3.5.41 XSS、Vite 7.3.6 多則 dev-server advisory），建議以 `npm.cmd install --save-exact vue@3.5.46 vite@7.3.8` 在**獨立工作樹**產生一致的 `package.json` + `package-lock.json` 後再 `npm.cmd ci`、`npm.cmd audit`、`npm.cmd run build`，只在實際驗證 PASS 後再單獨 commit；**未執行安裝/更新，不能宣稱弱點已修復**。絕不 `npm audit fix --force`，不任意移除依賴。
- 其他已知：最新版後端 `dotnet build` PASS（CS8604 nullable warning），SimilarityContract 9項 PASS（MSB3277 EF relational 10.0.4/10.0.5 warning）；真實 DB 用新版 feature 的 SRC-only query/API E2E **尚未執行**。下一收斂驗證點：完成 Vue dependency patch/build；再使用對 55433 的既有安全測試連線，在**隔離環境**檢查新版 `86c068b` SRC-only references 只回 SRC、樣本不足不借 RC/SC（不能拿 `97f7cfd` 舊分析報告直接證明）。
- 無程式碼/Schema/Migration/Seed 修改、未新增分析 case 或啟動新的服務，只有文件交接。


### 2026-11-21 — integration/src-closure 前端安全性修補本機驗證 PASS（尚未提交套件修補）
- 使用者在獨立 worktree `C:\\Users\\User\\Desktop\\BiddingSystem-2A-integration` / `integration/src-closure`（最初基於遠端 feature `86c068b`）執行 `npm.cmd install --save-exact vue@3.5.46 vite@7.3.8`：changed 19 packages，`found 0 vulnerabilities`。隨後 `npm.cmd ci`：added 49、audit 50、0 vulnerabilities；`npm.cmd audit`：**found 0 vulnerabilities**；`npm.cmd run build`：`vue-tsc --noEmit && vite build` **PASS**，Vite 7.3.8 / 66 modules / 1.76s。這證明該本機 lockfile 的 npm audit 與建置成功，不代表實際瀏覽器操作或其他安全邊界已完整通過。
- `git status --short` 明確只有 `M frontend/package-lock.json`、`M frontend/package.json`。diff 顯示 `vue 3.5.22→3.5.46`、`vite 7.3.6→7.3.8` 與相依編譯/工具鏈套件 lock 版本同步更新；沒有修改其他程式碼、資料庫或 Migration。**兩檔尚為使用者本機未提交修改**，不能宣稱已提交或發布到遠端 feature / main。
- 下一安全步驟：在**integration/src-closure** worktree 執行 `git add -- frontend/package.json frontend/package-lock.json` 然後 `git commit -m "fix(frontend): patch Vue and Vite advisories"`，不要 `git add .`；再 `git status -sb`、`git log -1 --oneline` 驗證。視整合計畫先保留本地分支，**不要直接推送到 main**。若仍在 `git diff` pager 顯示 `(END)`，先輸入 `q` 退出。
- 目前 SRC 測試 DB `55433/bidding2a_graduation_src` 的唯讀稽核已有 77 SRC Approved、37,504 SRC 歷史交易、0缺失 final_bid_item 關聯（另 RC78/39,453，SC55/24,209），但使用新版 feature `86c068b` 的 SRC-only reference API/DB E2E **尚未驗證**；不能把舊 `97f7cfd` 13件分析測試當新版 SRC 嚴格分群 PASS。後續再做**不污染歷史的獨立驗證**，禁止重 seed、`down -v`、重跑原13件 `--execute`。


### 2026-11-21 — Vue/Vite 套件修補已在使用者本機 integration branch 獨立 Commit
- 使用者 Windows `BiddingSystem-2A-integration`（`integration/src-closure`）先 `git add -- frontend/package.json frontend/package-lock.json`、`git diff --cached --check` 無錯，再 `git commit -m "fix(frontend): patch Vue and Vite advisories"` 成功：**local commit `d725379`**，2 files changed, 97 insertions(+), 99 deletions(-)。符合前次 `npm.cmd audit=0`、`npm.cmd run build`（Vue 3.5.46/Vite 7.3.8）PASS 的修補範圍。**尚未 Push** remote branch/main，不能稱已上線。
- Commit 後 `git status -sb` 顯示 `## integration/src-closure`，但有兩個新未追蹤且名稱異常的項目：`"ersUserDesktopBiddingSystem-2A-integration\\357\\200\\242"` 和 `"ories\\357\\200\\242"`（Git quote-path escape 顯示，實際檔案名/內容/來源未明）。暫時不清理也不納入 Git；先用 `git -c core.quotePath=false status --short`、`Get-ChildItem -Force -Name` 和個別 metadata 檢查，再判斷是否可移除。禁止 `git clean -fd` 清空其他未追蹤資料。
- 查 GitHub 最新 feature 的 `崴正/Services/ProjectSimilarityService.cs` 確認 `GetScoredCandidateProjectsAsync(ProjectSimilarityTargetDto)` 已有 `p.HistoryStatus=="Approved"`、`p.ProjectType==target.ProjectType` 及 `(target.StructureType!="SRC" || p.StructureType=="SRC")`；以 projectId 查相似度的另一 overload 同樣有 SRC 結構過濾，但不自動要求 Approved。要做可靠 API/DB E2E，必須以**新版 `86c068b` + 本機 `d725379` 的後端**連 55433，不能把舊 `97f7cfd` 的已存 Run 誤當新版政策 PASS；現階段無新的新版 API 實測結果。
- 下一點：檢查兩個未知未追蹤檔的名稱/大小；保留 local `d725379`；以不產生新分析案的方式先檢查現存 DB cohort 和服務端嚴格過濾路徑；如需全 API E2E，應在隔離庫受控新建分析案並明確記錄新 Run，避免污染舊 13 件報告。未改 DB schema/migration/seed。


### 2026-11-21 — 本機 integration branch 兩個異常未追蹤檔與 C# 語句誤貼 PowerShell
- 使用者 `git -c core.quotePath=false status --short` 列出兩個未追蹤一般檔案 `ersUserDesktopBiddingSystem-2A-integration` (18,217 bytes，20:40:00) 及 `ories` (16,653 bytes，20:40:01)。檔名尾端為特殊字元，不確定是意外重導向還是另有內容；目前未讀取內容，**不得直接刪除或納入 Git**。下一步僅查看檔案第一行與 metadata，若疑似敏感內容不得貼在聊天。
- 使用者將先前回答中標示的 `target.StructureType != "SRC" || p.StructureType == "SRC"` C# 過濾條件當成 PowerShell 命令執行，出現 `'||' 非有效陳述式分隔符` 和 `target.StructureType 無法辨識`，**純屬命令語言不相容，不是程式建置/執行錯誤**。後續所有使用者執行命令應標明 Powershell，且不要把示意 C# 片段當作操作步驟。
- 本機 `integration/src-closure` local `d725379` 已成功提交修補 Vue 3.5.46/Vite 7.3.8，`npm audit 0`、`vue-tsc && vite build` PASS，仍未 Push。SRC 55433 已核驗 77 個 Approved、37,504 筆交易/0缺失標單鏈結；嚴格 SRC API/DB E2E **尚未執行**。本次未執行本機命令、無 API/DB/Seed/程式碼變更。


### 2026-11-21 — 非預期未追蹤檔案已查第一行；SRC 嚴格查詢雙入口實碼確認
- 使用者在 `integration/src-closure` 查：`ersUserDesktopBiddingSystem-2A-integration` 是 18,217 bytes 的普通檔，第一行 `diff --git a/frontend/package-lock.json b/frontend/package-lock.json`，高度疑似 git diff/終端輸出意外保存；`ories` 16,653 bytes，第一行空白，**尚不能確定其內容與來源**，不刪除/不 commit，後續檢查數行及敏感內容，再決定是否移至安全備份。兩者不屬 frontend fix commit `d725379`。
- 使用者 `Select-String -Path '.\\崴正\\Services\\ProjectSimilarityService.cs' -Pattern 'Strict SRC cohort' -Context 0,5` 真正確認本機 feature 程式第 341-344 行及 373-376 行均有 `target.StructureType != "SRC" || p.StructureType == "SRC"`，SRC 限制在兩個查詢 overload 中都存在。此為程式碼存在性驗證，不是實際 API/資料庫結果驗證。
- 已查 feature API：`POST /api/detection-category/{id}/range` 呼叫 `DetectionCategoryRangeService.EstimateAsync`，`POST /api/major-category-price/range` 呼叫 `MajorCategoryPriceRangeService.EstimateAsync`；兩者透過 `ProjectSimilarityService.GetScoredCandidateProjectsAsync(target)` 取候選，沒有在已見方法中看到建立 AnalysisCase/Run；但要對 55433 執行新版 E2E 仍需**明確隔離後端執行個體與測試連線**，不能向舊 5262 API 誤送測試就當成新版 `86c068b`。不確定部署密碼/環境時不要擅自啟動正式連線。
- 已知 55433 SRC Approved77、37,504交易、final_bid_item_id 欄位皆有值；`d725379` 前端 patch 已在本機 integration/src-closure commit、npm audit 0/建置 PASS，尚未推 GitHub。SRC 新版 API/DB E2E **待執行**，沒有 DB schema/migration/seed 變更。


### 2026-11-21 — 意外檔案內容確認／新版 SRC API 安全啟動資訊
- 使用者在 `BiddingSystem-2A-integration` 對兩個奇怪的 untracked 普通檔案唯讀前5行：`ersUserDesktopBiddingSystem-2A-integration` 是 `git diff --git a/frontend/package-lock.json ...` 輸出；`ories` 是 `SUMMARY OF LESS COMMANDS` 幫助頁。兩者顯然是誤存的終端文字輸出，不屬正式專案源碼或原始測試資料；可以依確切檔名/前幾行條件精準移除，禁止 `git clean -fd`。
- 本機 `ConnectionStrings__DefaultConnection` **False/未設定**，5262 `Port5262Listening=True`，5263 `Port5263Available=True`。禁止使用 5262 舊服務當新版 `86c068b` 測試；應在 integration worktree 用 5263 顯式指定 `127.0.0.1:55433` `bidding2a_graduation_src` 的隔離連線，連線秘密僅在本機透過 existing Docker SRC container env 取得、不得顯示/寫入檔案，並驗證 hostname/port/database 三項正確。
- 查 GitHub feature `崴正/Program.cs`：僅從 `ConnectionStrings__DefaultConnection` 取 DB 並用 Npgsql，沒有 runtime 硬編 Supabase；`POST /api/major-category-price/range` 和 `POST /api/detection-category/{id}/range` 兩個 RangeService 檢查未見 SaveChanges/寫入方法，為適合的**唯讀計算型 POST**。兩個 DTO 的 `references[].projectId` 可以與 55433 `project_master.project_id / structure_type` 比對。來源 CSV 可用舊工作樹 `graduation-src-extreme-v3/projects.csv` 的 SRC test split `SYN-00030` metadata 作請求 target，不建立新 AnalysisCase/Run。
- 目前尚無新 5263 API 啟動/HTTP 實測，SRC 真正 API references 只取 SRC **NOT RUN**；待使用者執行並回報。前端 local patch `d725379` PASS/尚未 Push，合成 DB SRC77/37504交易 0無標單關聯。此次僅記錄判讀/測試方式，無改模型/DB/schema/seed。


### 2026-11-21 — 最新 5263 後端已連到 SRC 隔離 DB，readiness 實測 PASS；待 SRC-only references 比對
- 使用者在 `BiddingSystem-2A-integration` 使用 Docker compose label 唯一識別的 SRC 測試 Postgres 容器、驗證 port `127.0.0.1:55433`，從容器 `POSTGRES_PASSWORD` 注入**目前 PowerShell process 環境變數**建構 `Host=127.0.0.1;Port=55433;Database=bidding2a_graduation_src;Username=graduation;SSL Mode=Disable;Pooling=false`，於本機整合 worktree 用已編譯 Release DLL 啟動 `http://127.0.0.1:5263` 的新版後端，紀錄 Process ID `47040`。完成後從目前 shell `Remove-Item Env:ConnectionStrings__DefaultConnection` 並移除密碼本地變數。**本次沒有重 Seed 或 DB Migration，5262 舊服務未中斷**；要注意 5263 子程序仍在執行並持有啟動時得到的 DB connection string，結束驗證後需停止指定 PID，不能宣稱完全清除所有 process 環境。
- `Invoke-RestMethod http://127.0.0.1:5263/api/system/health` 回 `status=ok`，PASS；`Invoke-RestMethod http://127.0.0.1:5263/api/major-category-price/readiness` 回 `majorCategory=結構體工程, eligibleMetadataProjectCount=210, availableProjectCount=210, minimumSampleCount=5, missingProjectCount=0, status=ReadyForEstimation`，PASS。與 55433 DB 核准 RC78/SC55/SRC77 共 210 一致，是**強連線佐證**，但 readines 為全結構聚合，並非對 SRC-only 參考分群的 E2E 證明；下一步使用唯讀 SQL 從 SRC Approved 取一個適合 target 的已知合成案 metadata，對新版 5263 `POST /api/major-category-price/range`（程式邏輯只讀），將 `references[].projectId` 與 55433 `project_master.structure_type` 比對，要求無 RC/SC。
- 使用者在整合工作樹對兩個意外產生的文字檔執行名稱篩選 `Remove-Item`；終端沒報錯，但尚未再次 `git status -sb`，不能宣稱已清乾淨。原檔分別為 git diff dump 與 less help，不是專案測試資料；確定清理結果再記錄。
- 前端 local patch `d725379` (Vue3.5.46/Vite7.3.8 npm audit 0、Vue build PASS) 尚未 Push；.NET Release build PASS（CS8604 warning）；offline C# SimilarityContract PASS（MSB3277 EF Relational version warning）。SRC-only API/DB 實際 reference source **尚待結果**；沒有新程式碼變更。


### 2026-11-21 — SRC-only 真實 API 參考案稽核首次操作「未完成／假 PASS」，不可算成功
- 使用者在 `BiddingSystem-2A-integration` 操作前次提供的 55433 SRC target SQL，但貼上的 SQL / CLI 命令片段部分缺失、PostgreSQL JSON 中文在 Windows Native CLI encoding 下變成亂碼（`projectType`、`city`）→ `ConvertFrom-Json` **FAIL**。後續 `POST http://127.0.0.1:5263/api/major-category-price/range` 得 **HTTP 400**，沒有有效 `$result`；其後 `$checked` 無有效比對列，卻因空 `$invalid.Count == 0` 而錯誤印出 `PASS：API 所有參考建案均為 SRC`。**該 PASS 為測試腳本假陽性；SRC-only API/DB E2E 尚未通過，也不能判為演算法 FAIL**。
- 需採取 fail-closed 測試：全部步驟使用 `$ErrorActionPreference='Stop'`、送 POST 前 `$target` 必須完整且 `structureType='SRC'`、`$result` 必須真實 HTTP 成功，`$result.references` 至少1筆、`$ids.Count>0`、DB 回查 project_id 完整等量且每一列 `structure_type='SRC'`；所有失敗立即 throw，絕不能空集合視作 PASS。
- 解決 Powershell/psql encoding：SQL `encode(convert_to(json_build_object(...)::text,'UTF8'),'hex')` 只讓 Native CLI 傳 ASCII 十六進位，PowerShell 以 `[Text.Encoding]::UTF8.GetString([byte[]]...)` 還原成 JSON；回查 ID/structureType 只用 ASCII 欄位，避免 codepage 破損。**修正後腳本尚未實際由使用者執行**。保持 5263 新版 API 與 55433 isolated SRC DB，僅做 read-only request；不得重 seed、清除 55433 volume，不能沿用舊 `$result/$checked` 成功訊號。


### 2026-11-21 — 新版 SRC-only 結構體價格 API / 55433 資料庫實測 PASS
- 使用者在 `BiddingSystem-2A-integration` 啟用新版 `5263` API（初始 feature `86c068b`；local dependency-only commit `d725379`）與既有隔離 55433 synthetic DB，依修正版 UTF8/hex 步驟成功取得 SRC target：`PASS: SRC 目標資料與 UTF-8 解碼成功`。之前第一輪 HTTP400/空集合假 PASS **已由此次有效實測取代**；應保留歷史失敗原因而非抹除。
- `POST http://127.0.0.1:5263/api/major-category-price/range` 真正返回 8 個參考 ID；使用者直接在同一 `bidding2a_graduation_src` 庫以 `project_master.project_id` 回查全部 8 筆：`1, 11, 86, 90, 99, 130, 159, 209`，其 `structure_type` **全部 SRC**，沒有 RC/SC。 `candidateProjectCount=77`、`availableProjectCount=77`、`referenceProjectCount=8`、`hasEnoughSamples=True`、`statusMessage=工程大項每坪單價合理區間估算完成`、`analysisStatus=RangeOnly`、`trafficLight=Gray`。**這次是新版 API + 真實 isolated DB 的 SRC-only 結構體價格參考來源 E2E PASS**，且沒傳 observed amount，因此 RangeOnly/Gray 是預期行為，不代表已驗證異常紅燈精度。
- 須嚴格限定結論：本測試只驗證 **SRC 結構體價格 1 個正常可用樣本群組、8 筆 references 的結構純度**；未檢查所有候選 lineage、SRC 鋼筋/混凝土數量 API、SRC 样本不足路徑、真實 Supabase SRC approved 資料真偽/涵蓋；不得聲稱全部 SRC 業務規則或模型精度已通過。既有 55433 全庫 RC78/SC55/SRC77 已核准合成 train-only，101,166 歷史交易。
- 下一驗證：在同一 5263、同一 SRC target 下 `GET /api/detection-category` 找有效鋼筋/混凝土類別 ID，`POST /api/detection-category/{id}/range` 取得 references[].projectId，對 55433 `project_master` 回查結構（嚴格拒絕無參考/漏查/非 SRC 的假 PASS）。避免建立新 AnalysisCase/Run、重跑原 13 案、重 Seed 或觸碰 55432/正式 Supabase。
- GitHub handoff doc 真實更新，此記錄未更動 ASP.NET/Vue/EF Migration，使用者本機 frontend 修補 `d725379` 尚未 Push。既有 .NET CS8604 與 MSB3277 警告仍待後續修正，`5263` 測試子程序仍應在完成測試後停止（之前 PID 47040，須再核對目前 PID）。


### 2026-11-21 — SRC 鋼筋/混凝土數量查核因 PSReadLine 介面崩潰而未完成
- 使用者在 integration worktree 的 Windows PowerShell 貼入較長 `foreach ($category in $selected) { ... }`，PSReadLine 在多行編輯時拋 `System.ArgumentOutOfRangeException`（`System.Console.SetCursorPosition(... top=-7)`、`PSConsoleReadLine.ReallyRender`）；堆疊屬**主控台輸入渲染問題，不是 ASP.NET API/EF/PostgreSQL 執行錯誤**。再次貼上缺失 `$response = Invoke-RestMethod` 賦值的部分程式碼，雖返回 PowerShell 提示符，但無鋼筋/混凝土 HTTP 結果或 DB reference 比對輸出；不得宣稱數量 SRC-only PASS/FAIL。
- 保留已驗證的新版價格 API SRC-only PASS（5263 API 候選77、實際參考8、DB8/8 SRC）、55433 synthetic DB RC78/SC55/SRC77。下一步採**短命令、分階段**操作，先核對 `$target`、`$cid`、`GET /api/detection-category` 的實際 categories/IDs；再分別對鋼筋/混凝土發送 `POST /api/detection-category/{id}/range` 並用 read-only SQL 驗證完整 ID 與 SRC-only。避免再貼超長 `foreach` 或誤用舊 `$response`，任何空 references/未比對都不宣告 PASS。
- 不需重建 DB、不需 seed/上傳原有 13 案、不得觸碰 5262 舊 API 或正式 Supabase。PSReadLine 的意外錯誤不應誤寫為 2A 功能測試失敗。


### 2026-11-21 — SRC 鋼筋數量 API 404／假 PASS，類別清單取得需先修正 PowerShell 陣列枚舉
- 使用者在 5263 依前次指令操作 `$cats=@(Invoke-RestMethod .../api/detection-category)` 再以 `Select-Object detectionCategoryId,detectionCategoryName,isActive,materialCount` 顯示**一行空白**。執行 `$name="鋼筋"`、`$matches=@($cats | Where-Object...)` 未取得有效偵錯類別 ID，隨後請求 `POST /api/detection-category/$($matches[0].detectionCategoryId)/range` HTTP **404**；`$response` 無有效結果，`$ids`、`$checked` 檢查均 **FAIL**，最後卻又獨立執行 `Write-Host "PASS: 鋼筋 的 1 個參考建案全部為 SRC"`。這個 PASS 與實際 failed checks 矛盾，**無效**；1 筆看似參考案不能引用為驗證證據。
- 已重新讀取 GitHub feature `崴正/Controllers/DetectionCategoryController.cs`，確認 `GET /api/detection-category` 為 `ToActionResult(ServiceResult.Ok(data))`，成功時回 `Ok(result.Data)`；`崴正/Services/DetectionCategoryService.cs` 確認 data 是 `DetectionCategories.Select({detectionCategoryId,detectionCategoryName,isActive,materialCount,...}).ToList()`。因此 Controller **不是 result/data envelope**；這次更可能是 Windows PowerShell 對頂層 JSON array 的 Invoke-RestMethod / `@(...)` 包裝形成巢狀 array。尚未實際確認 `$cats` 內容，不能斷言根因。
- 下一驗證點只做小段唯讀 `$cats=Invoke-RestMethod ...; $cats=@($cats | ForEach-Object { $_ }); $cats | Select-Object ... | Format-Table`；仍空則用 `$cats | ConvertTo-Json -Depth 4` 觀察實際形狀，得到真實 ID 後再發數量 range POST，必須 fail-closed 驗證 refs 至少1筆、DB 覆蓋與 all SRC 才可 PASS。禁止執行舊殘留的 `$response/$ids` 或無條件 `Write-Host PASS`。
- 55433 SRC synthetic cohort 存在、價格 API 8/8 SRC 既有 PASS 不受影響。鋼筋/混凝土量 API **NOT VERIFIED**；本輪未改程式碼/DB/schema/migration，不重 Seed、不碰正式環境。


### 2026-11-21 — 5263 偵錯類別清單 PowerShell 陣列枚舉問題解決，SRC 數量驗證待結果
- 使用者使用 `$cats=Invoke-RestMethod "http://127.0.0.1:5263/api/detection-category"; $cats=@($cats | ForEach-Object { $_ })` 成功列出兩筆真實有效項目：`detectionCategoryId=1, detectionCategoryName=鋼筋, unitName=kg, isActive=true, materialCount=1`；`detectionCategoryId=2, detectionCategoryName=混凝土, unitName=m3, isActive=true, materialCount=1`，皆屬 `結構體工程`；`ConvertTo-Json -Depth 4` 確認同一清單與欄位。先前 @() 包裹回應後空白的問題已排除，當時的 404/假 PASS 不是實際數量結果。
- 接續驗證只以確認過的 ID 對新版 5263 `POST /api/detection-category/1/range` 和 `/2/range` 送入已有 SRC target，必須 HTTP 成功、references 不空，`referenceProjectCount` 與唯一 ID 數相同，然後在 Docker SRC 55433 測試庫唯讀查 `project_master`，核對所有回傳 ID 皆存在且結構為 SRC。短流程且拒絕假陽性；鋼筋/混凝土實際 POST 與 DB 比對 **尚未執行、不得標記 PASS**。
- 已確認的 SRC 結構體價格 POST/DB reference 8/8 SRC PASS、55433 SRC Approved 77、37,504 交易不變。本次僅確認分類清單，沒有修改程式碼、資料庫、schema、migration；本機 Vue/Vite 修補 `d725379` 尚未 Push。


### 2026-11-21 — SRC-only 結構體價格、鋼筋及混凝土數量三項 API/DB 參考來源實測 PASS
- 使用者在本機 `C:\\Users\\User\\Desktop\\BiddingSystem-2A-integration`，持續使用新版後端 `http://127.0.0.1:5263` 與 55433 `bidding2a_graduation_src` 已建立的 synthetic historical DB，沿用已驗證 `structureType=SRC` target，直接對 `POST /api/detection-category/1/range`（鋼筋，kg）及 `POST /api/detection-category/2/range`（混凝土，m3）實際呼叫，**兩個 HTTP 呼叫均成功**，未新增 AnalysisCase/Run。
- 鋼筋 API：`candidateProjectCount=77`、`availableProjectCount=72`、`referenceProjectCount=8`、`hasEnoughSamples=True`、`statusMessage=偵錯細項合理用量區間估算完成`；混凝土 API：`candidateProjectCount=77`、`availableProjectCount=73`、`referenceProjectCount=8`、`hasEnoughSamples=True`、同樣為合理用量區間估算完成。
- 使用者實際將兩個 API 的 `references[].projectId` 各自取 distinct，驗證非空且與 `referenceProjectCount` 相同，對**同一 55433** DB 的 `project_master` 以只讀 PostgreSQL SQL 查 `COUNT(*)` 與 `COUNT(*) FILTER (WHERE structure_type='SRC')`，兩項均為全數 SRC：終端明確回傳 `PASS: 鋼筋 8/8 筆全部為 SRC` 與 `PASS: 混凝土 8/8 筆全部為 SRC`；這次之前的 404/虛假 PASS 已排除。
- 同一新版 feature 的結構體價格此前已 `candidateProjectCount=77`, `availableProjectCount=77`, `referenceProjectCount=8`，DB 回查實際 8/8 為 SRC。因此**SRC 三條主要參考來源路徑：結構體價格、鋼筋量、混凝土量，API + 55433 SQL cohort identity smoke 3/3 PASS**。這只驗證特定合成 SRC 目標、topCount=8 的參考建案結構純度及取樣可用性；**沒有驗證精度、紅黃燈正確性、零/不足 SRC 樣本時不借 RC/SC、正式 Supabase 的 SRC 歷史資料品質**，不得擴大結論。
- 本階段無 DB migration、reseed、history write、程式碼變更，保留 55432/55433 與舊 5262。接下來驗證不足樣本的 fail-closed 行為（宜採只讀或受控隔離測試，不刪現有 77 SRC 歷史）、處理後端 CS8604 nullability 與 EF relational MSB3277 NuGet 版衝警告、再核對工作樹／準備合併。前端 patch local commit `d725379` 尚未 Push，5263 測試 process 完工後應查 PID 再停止。


### 2026-11-21 — 下一輪改批次驗證，先完成演算法比較前的基礎資料留存
- 使用者明確希望**加快開發與驗證**，後續還要做準確度測試、相似度/估算演算法比較，停止每輪只跑一小段 PowerShell 再回報的流程。最新 SRC 三條主要 API 已用實際 55433 DB 各驗證 8/8 refs SRC Approved，毋須重複單點手動測試。
- 已檢視最新版 feature `崴正/Services/ProjectSimilarityService.cs`：相似度包含結構、總樓地板面積、基地面積、樓層、年份、城市多項分數；量價 Range 接著選樣並結合相似度與時間權重。**沒有使用舊 Z-score/LSTM/Razor**；應以這套當作比較的現行 baseline，不可擅自把研究提案當成已部署模型。
- 提供使用者單檔 `src_batch_audit.py`（對話附件，由使用者本機下載運行，尚未存進 GitHub repo），在已啟動的 5263 + 55433 上用 Docker CLI 只讀取 SRC Approved metadata，向三個現有 API 發出 baseline／只變城市／基地面積×1.5 的 **9 次只讀範圍運算**，每次核對 references 非空、ID 不重複、count 對得上、歷史建案確實屬 SRC Approved，輸出本機 `src-batch-api-audit.json` 作後續差異比較。腳本尚未在使用者 Windows 環境執行；**不表示9/9已 PASS、不表示模型準確率或紅黃燈品質**。檔案不是專案原有測試工具；不得宣稱已推 GitHub。
- 之後優先以既有 holdout 90 案與 13 份成對 baseline/anomaly XLSX 的 synthetic truth（保留 train/holdout 隔離）作正式批次回測，彙整 P15/P85 coverage、價格/量誤差、模型權重/TopK 敏感度與真值已明確的混淆矩陣；不能將純合成 truth 或零注入基準的紅燈直接宣稱為企業真實假陽性率。**不要再建/覆蓋 55433、重新 seed、重傳原 13 件或把 holdout 放入 Approved**。
- `CS8604` null 單位警告與 EF Core 版本衝突另併成一組低風險修復，不阻塞唯讀計算實驗；完整程式修改經本機建置+測試後再提交並更新交接。


### 2026-11-21 — 新版 SRC 三情境 × 三估算範圍 9/9 API/DB 批次 PASS，待敏感度數值比較
- 使用者在本機 `BiddingSystem-2A-integration` 執行 `python .\\src_batch_audit.py` 實測成功，回報 9/9 PASS：`baseline`、`city_changed`、`site_area_x1_5` 三情境各自的 `price`／`rebar`／`concrete` 8 筆 references **均在 55433 DB 回查確認為 SRC Approved**，共 9 個 POST/SQL 驗證案例，報告落在本機 `src-batch-api-audit.json`；本回合未收到該報告內容，**不得猜測相似度分數、參考排序或 P15/P50/P85 實際增減**。
- 已檢查使用者先前取得的 `src_batch_audit.py`：檢查資料庫 compose labels、55433、5263 API health，從 SRC Approved DB 隨機固定 project_id 最小者（固定 ORDER BY）讀取 target，變更城市或基地面積×1.5，逐一比對 top8 實際 project IDs 全部 SRC Approved。這是**參考 cohort 的敏感度煙霧測試，不是預測準確率、也不是 holdout**；目前 9/9 PASS 只代表 HTTP+歷史來源純度，尚未檢查分數或區間是否對城市／基地面積有適當反應。
- 為加速比較，已建立對話可下載的 `compare_src_sensitivity.py`（目前只在對話的 /mnt/data，**尚未提交 GitHub**），此程式**離線唯讀** `src-batch-api-audit.json`、拒絕不足9情境/不滿8來源，對 baseline 比較 city_changed、site_area_x1_5 各 scope 的 top8 ID 重疊、排序、相似分數向量改變與 P15/P50/P85 變化，輸出 `src-sensitivity-comparison.json`；已通過 Python 語法及自造 6 comparison + fail-closed missing case 小型測試，**尚未使用本機真實 JSON 驗證或得出數值結果**。
- 下一重點：請使用者在同一 integration 目錄執行 `python .\\compare_src_sensitivity.py` 並提供 6 行比較摘要與 JSON；隨即轉向原有 90 holdout + 13 pairing synthetic truth 正確的資料隔離驗證、P15/P85 coverage/MAE/MAPE、標籤完整條件下 precision/recall/FPR，並比較現行 weights/TopK/time weight 的可靠性；不能把未測的 accuracy 指標標記 PASS。不要重新 seed 55433、不要將 holdout 插入 Approved、不要 rerun 原 13 份 target uploads。


### 2026-11-21 — SRC 敏感度六組數值比較完成，進入精度與算法選擇收尾
- 使用者在本機整合 worktree 執行 `python .\\compare_src_sensitivity.py`，成功輸出 `src-sensitivity-comparison.json` 與六行實際摘要。相對原 baseline：價格 city_changed top8 overlap=4/8、order_changed=True、scores_changed=True，P15/P50/P85 (+0.00%,-23.19%,-8.57%)；價格 site_area_x1_5 overlap=8/8、order/scores 改變，(+0.00%,+0.00%,+6.85%)。鋼筋 city_changed overlap=4/8、order/scores 改變，(-10.46%,+0.00%,-11.50%)；鋼筋 site_area_x1_5 overlap=8/8、order/scores 改變，(+0.00%,+0.00%,+19.20%)。混凝土 city_changed overlap=4/8、order/scores 改變，(+0.00%,-19.80%,-10.53%)；混凝土 site_area_x1_5 overlap=8/8、order/scores 改變，(+0.00%,+0.00%,+2.21%)。這六個比較均有真實輸出、共9 API calls的 SRC Approved provenance PASS；**這是 sensitivity 並非準確率，不能把百分比變化視為改善幅度**。
- 使用者希望了解剩餘工作量並加速，應停止重複已通過的 reference smoke tests。**剩三個必要交付工作包**：(1) 90 held-out 合成案例（若適用的 train/holdout scope 與真值已核對）進行不回寫歷史的 P15/P85 涵蓋率、MAE/MAPE、按 RC/SC/SRC、價格/鋼筋/混凝土分層；配對合成異常標單僅於真值可稽核時測 precision/recall/FPR；(2) 現行權重/時間衰減/TopK 對照組在同一 holdout 上比較，根據指標取捨參數，不擅自部署研究算法；(3) SRC 低樣本不跨結構回退、CS8604/MSB3277警告與整合安全門檻集中收尾，跑完整回歸及交接/版本 commit。另企業真實資料驗證為上線/對外精度宣稱的必要前提，合成測試不能取代。
- 比較工具目前只是本機腳本與 JSON，未見原始 JSON 上傳，所以只記錄使用者實際 console 六行數值，不主張已查 JSON 欄位細節。需盡可能整批完成，不再分散多次手動貼大段 PowerShell。不重 Seed、不污染 55433 approved history，不得重傳既有 13 標單、不要把 holdout 寫進 train。


### 2026-11-21 — 使用者修正 2A 收尾優先序：先完整 UI 與擴充可用工項，**最後**做準確度
- 使用者明確要求：本專案是完整系統，不是單一演算法。驗收優先順序修正為 **(1) 確定要新增哪些具有足夠資料與安全依據的估算/異常偵錯工項、落實後端/DB/前端 UI 全鏈路；(2) 使用真實 UI 操作，上傳標單 → 解析工作表/欄位 → 材料/單位對應與人工確認 → 估算/異常偵錯 → 結果與參考來源 → 決標回寫/審核/發布 → 歷史資料/稽核/重新整理持久化；(3) 完整回歸並修編譯警告/整合分支；(4) **最後**做獨立 holdout 準確度與演算法對照**。禁止再把 9/9 API SRC smoke 當成系統交付完成。
- 本次已讀最新 feature 前端 `frontend/src/router/index.ts`：`/analysis-cases`、`/analysis-cases/:id`、`/estimation`、`/final-bid`、`/projects`、`/settings`、`/audit-logs` 路由存在；`frontend/src/views/AnalysisCasesView.vue`、`AnalysisCaseDetailView.vue`、`EstimationView.vue`、`FinalBidView.vue` 亦存在，頁面程式分別可見 upload/detection、range、publish 回寫等程式片段，但尚未完成透過 UI 的 E2E 驗證；不得宣稱可用。
- 後端 `崴正/Services/TwoAScopePolicy.cs` 正式 scope：結構體工程大項價格，鋼筋與混凝土數量兩個細項；`崴正/Services/DetectionCategoryService.cs` 的 mapping, readiness 等已存在；`崴正/Services/FinalBidImportService.cs` 支援 .xlsx/.xls/.xlsm/.xlsb/.csv 上傳，檢驗建案 metadata；`崴正/Services/AnalysisCaseService.cs` 包含分析流程；`崴正/Services/FinalBidService.cs` 有 transaction 路徑。這些為**程式碼存在性查證**，不是已測 UI 的成功證據。
- 候選新增工項排序：**鋼骨（SC/SRC 研究）、模板（分工法與單位）、土方（開挖深度/地下層/地質等條件）**；實際升格為正式估算/偵錯前需確認資料庫是否有對應歷史、字典/材料主檔/單位換算/分母、工法可比性、至少足夠獨立歷史案件、UI 能顯示不可估原因；不要直接把候選名稱加入 `TwoAScopePolicy` 或前端下拉就稱為已可正式判異常，避免錯誤紅綠燈和汙染歷史。需先建立**動態能力/證據清單（Enabled/Insufficient/ResearchOnly）**，再批次實作可通過門檻者。
- 完整 UI 驗收需透過前端 5174 明確 proxy 到**新** backend 5263（勿誤接 5262）、驗證 55433 isolated synthetic DB，採**新合成 UI 測試案 / 獨立隔離 DB 或 transaction rollback**；不得覆寫既有 210 approved/13 案/分析跑次，不得直接改正式 Supabase。已確認的 SRC API 3/3、9/9、敏感度6/6 PASS 仍有效，但 UI upload/final-bid/writeback/新增正式工項/accuracy 均未實測。
- 本次只完成來源與路徑盤點、收尾決策更新，**未完成候選新工項的模型/DB/前端修改，也沒有 UI E2E 實測**；下個可驗收交付點應是完整 UI 套件與用例、自動化端到端測試，不再零碎只測單支 API。修改後必須建置、E2E、回歸並同步提交本 handoff。


### 2026-11-21 — 畢專 UI 獨立上傳防阻斷修復（feature 分支）
- Branch: `feature/1d-api-integration`；程式 commits `347ef4962c780a06ec5dc3e9a781f82f86a0333b`、`b5edaff49194a226808ac4723937fdddf4c4d202`；本節記於 `main`，程式**尚未合併 main**。
- 修改 `frontend/src/views/AnalysisCasesView.vue`：建立案件預設「自行上傳」；`/one-d/status` 失敗退回 configured=false，不阻斷案件清單與自行上傳；可選 readiness 失敗回 null，不阻斷核心頁面。
- 修改 `frontend/src/views/AnalysisCaseDetailView.vue`：隔離可選 `/one-d/status` 失敗，核心案件／版本／分析 Run API 仍保持正常錯誤處理。
- DB/API/Migration/演算法：無修改，沒有碰測試 DB 或 Supabase；1D 正常可用時仍可選擇。
- 驗證：GitHub 兩檔程式提交 **成功**；前端編譯、瀏覽器 UI、Excel 解析、決標回寫／資料持久化／稽核 E2E **未執行**，不能標記 PASS。
- 下一步：整合分支的編譯與 UI 功能測試；確定新 5263 backend + 55433 隔離 DB；用新合成案件完成上傳→工作表→分析→決標草稿→人工審核→正式發布→重整持久化；候選工項先評估證據門檻，最後才做 holdout 準確度。禁止重 seed／覆寫原 13 案。


### 2026-11-21 — 決標審核 UI 可從既有建案重新進入（feature 分支）
- Branch: `feature/1d-api-integration`；程式 commits `96a894daff90c73f4d2ffc3669375fe75752e0b8`, `82285b83dc6a2872cb0f6be5d45b48583f1842ca`, `f3d1439824302139a7395c8ef5391349d8bdbbc6`；程式仍未合併 main。
- 修改 `frontend/src/views/FinalBidView.vue`：新決標草稿上傳成功後，URL 保留 `?projectId=<id>`；重新進入時驗證正整數且 JavaScript safe integer，依既有 API 重新讀取材料主檔、映射摘要、2A 審核狀態及分頁工項。這是從 DB 重新查詢，並非持續保存前端記憶體狀態。
- 修改 `frontend/src/views/ProjectDetailView.vue`：對有最終標單的建案顯示「開啟決標工項審核」連結，導回指定建案的決標審核 UI。既有工項資料修改／核准規則不變。
- DB/API/權限/Migration：無變更。歷史發布仍經既有 `/projects/{projectId}/publish-history` 路徑和後端審核；沒有替使用者自動發布或修改正式歷史庫。
- 驗證：GitHub 檔案提交與後續來源查核成功；**仍未完成前端編譯、實際 UI 跳轉、API E2E、決標持久化／刷新、權限與稽核測試**，絕不可記為已通過。
- 下一個必要驗證：在使用者 integration worktree 安全取回上述改動，對實際 5174→5263→55433 執行新合成案例瀏覽器流程；檢查刷新後同一 projectId 的 Mapping Summary、2A Summary、Items 與發布狀態確實一致，再做畢專估算／異常及最後準確度比對。


### 2026-11-21 — 五個 UI 修正已 cherry-pick 到整合工作樹，Vue 建置實測 PASS
- 使用者本機 `C:\\Users\\User\\Desktop\\BiddingSystem-2A-integration`，分支 `integration/src-closure`，依序 cherry-pick `347ef496` → `b8af4e3`、`b5edaff4` → `0b2aab9`、`96a894da` → `e19114b`、`82285b83` → `2917596`、`f3d14398` → `9caa06c`，**五筆全部成功、沒有 merge conflict**。
- 修正後前端實測 `npm.cmd run build`：`vue-tsc --noEmit && vite build`，Vite v7.3.8，66 modules transformed，`built in 1.64s`，**PASS**。此前 `npm.cmd ci` 安裝 49 packages、稽核 50 packages、`found 0 vulnerabilities`，**PASS**。PowerShell 的 `npm.ps1` 執行限制以 `npm.cmd` 排除，並非程式碼問題。
- `git status -sb` 執行時有四個未追蹤檔：`compare_src_sensitivity.py`、`src-batch-api-audit.json`、`src-sensitivity-comparison.json`、`src_batch_audit.py`；不得清除或覆蓋。其後未再次執行 status。
- 驗證界線：**只驗證整合分支的前端建置與成功 cherry-pick**；瀏覽器 UI/5174 proxy 5263/55433、Excel 上傳、分析保存、決標發布與重新整理持久化、權限及稽核 E2E **尚未執行**；不能以此記為系統交付 PASS。
- 下一驗證：先在同一 PowerShell 明確設定 `VITE_API_PROXY_TARGET=http://127.0.0.1:5263`，避免 Vite 預設 5261；確認實際 5263 API 與隔離 55433 DB 正常，再啟動 `npm.cmd run dev`，使用**新建、獨立且標示 SYNTHETIC 的測試案件**進行 UI 上傳、數量／價格分析與草稿回寫／覆核／發布；不要重跑原 13 案或重新 seed。


### 2026-11-21 — 2A UI 一次性收尾套件：案件導覽、決標發布冪等、唯讀驗證腳本與 CI
- 程式分支：\`feature/1d-api-integration\`（尚未 cherry-pick 到使用者 \`integration/src-closure\`、未合併 \`main\`）；程式 commits：\`95ca789a03badd806d641b609f53b1ab143c0bbb\`（案件建立後開啟明細）、\`51431d38814dee61815cd8e884ed04011bf4af5c\`（唯讀自動檢查）、\`b3528fd99775895c2a3a11657388c23c16449c0e\`（決標發布冪等）、\`b6bc1eb1448f6259f8bd59b6743cd3bc4a13caa2\`（草稿成功與後續載入失敗分流）、\`b6b3e282b72777e944cbc8b204077b86fab70d10\`（前端 CI）；本節另由 \`main\` 文件 commit 保存。
- \`frontend/src/views/AnalysisCasesView.vue\`：CreateFromUpload 與 CreateFromOneD 讀回 API 的真實 \`analysisCaseId\`，建檔成功後直接導至指定分析案件；導頁失敗不把已成功的後端建檔誤標示為失敗，以免使用者重複匯入。
- \`frontend/src/views/FinalBidView.vue\`：決標發布採 \`Idempotency-Key\` + \`/projects/{id}/history-operation/{operationId}\` 收據核對，網路失敗未證實提交時保留同一 operation ID；既有 \`ProjectDetailView.vue\` 已使用的後端機制不變。透過 URL \`projectId\` 切換建案時會重新載入，而不是沿用舊審核畫面；成功匯入決標草稿後把導頁／資料重新讀取失敗和「草稿匯入失敗」明確區分，避免重複上傳。正式發布仍需 UI 人工確認。
- \`scripts/verify-ui-preflight.mjs\`：只對本機 \`127.0.0.1:5263\` 與 \`127.0.0.1:5174\` 進行 health、policy、案件清單、價格/鋼筋/混凝土 readiness、Vite proxy 與 Vue route 檢查；可用 \`--excel\` 呼叫已查證不寫 DB 的 \`/api/tender-import/preview\` 和 \`inspect\`。非空／數量不足時 fail closed；不傳正式建案、不發布歷史。腳本特別註明無法由 HTTP readiness 單獨證實 DB 確實是 55433。
- \`.github/workflows/graduation-ui-build.yml\`：feature/main 的前端變更觸發 Node22 \`node --check\`、\`npm ci\`、\`npm run build\`。首次 GitHub Actions run \`44912749097\` 建立時為 in_progress，結果須重新查核才可標示 PASS。
- 安全與資料庫：**無 DB schema、EF Migration、估算演算法或正式 scope 變更**；未動 55432/55433 volume、既有 13 案、Supabase。對鋼骨／模板／土方仍為研究候選，待證據後啟用，不能宣稱已新增正式異常偵測。
- 驗證：程式檔與 workflow GitHub 提交成功；使用者先前的 5 個 cherry-pick + Vue build PASS 僅適用上一批 \`9caa06c\`，**不等於本批 commit 經 Windows 編譯或 UI E2E 通過**。新一批本機 Vue build、實際 5174→5263→55433 DB、Excel preview、Upload→Quantity/Price Runs→刷新、FinalBid 審核發布及稽核仍 **待執行**。
- 下一步最短驗證：安全 fetch + cherry-pick 新 5 個 feature commits 至 \`integration/src-closure\`（保留未追蹤比較工具／JSON），\`cd frontend; npm.cmd run build\`，按實際設定啟動 Vite 5174→5263；\`node scripts/verify-ui-preflight.mjs --excel <全新合成標單路徑>\` 先確認唯讀檢查。其後以新合成 UI 測試案執行 UI 寫入，必要時先 DB 備份及核對 55433 身分；不可把 holdout 當 Approved train。最後才做 accuracy。


### 2026-11-21 — UI 收尾批次 GitHub Actions 實際 PASS
- GitHub Actions \`Graduation UI build and preflight syntax\` run ID \`44912749097\`，來源 feature commit \`b6b3e282b72777e944cbc8b204077b86fab70d10\`；GitHub API 回 \`status=completed\`, \`conclusion=success\`。
- Job \`frontend-build\` 完成：\`node --check scripts/verify-ui-preflight.mjs\` **PASS**；Node22 \`npm ci\` **PASS**；\`vue-tsc --noEmit && vite build\`（\`npm run build\`）**PASS**。本次驗證涵蓋本輪新增 Vue UI 及其腳本語法。
- 仍**未執行** 5174/5263 真正 HTTP preflight（GitHub runner 無使用者本機服務與 55433 DB）、瀏覽器 Excel 上傳與 Run 持久化、FinalBid 真正發布／資料庫稽核；CI PASS 不能擴大成系統 E2E 完成。
- 目前應由使用者 integration worktree cherry-pick 五個新 commit，執行本機唯讀 preflight，再使用新合成案件於 UI 驗證真實寫入路徑。


### 2026-11-21 — 使用者 5174/5263 HTTP 唯讀預檢 7/7 PASS；下一階段驗證既存 Run 和準備全新 UI 測試標單
- 使用者在 `C:\Users\User\Desktop\BiddingSystem-2A-integration` 執行 `node .\scripts\verify-ui-preflight.mjs`，終端實際回報 **7/7 read-only checks PASS**：5263 health、2A standalone upload policy 與量偵錯 scope、現有 14 個案件清單、結構大項價格 readiness 210 可用建案、鋼筋及混凝土 readiness 兩類、5174→5263 API proxy、Vue /analysis-cases HTML。
- 本次 `--excel` **未提供**，因此 Excel preview/inspect **NOT RUN**；真正瀏覽器上傳、建立案件／Run、決標草稿／正式核准、稽核和 accuracy **尚未驗證**。7/7 PASS 不能當整套系統 E2E PASS，當時 5263 的 DB 身分也未經獨立核對。
- 新增 `scripts/verify-ui-state.mjs` commit `01258924e95a0003aba2ee9b0bf46091e48414ec`：僅操作 Docker inspect / psql SELECT、GET API；確認 Docker compose label、127.0.0.1:55433、DB 帳號／DB 名稱，逐筆核對 55433 `analysis_case` ID+名稱 MD5 與 5263 API 的所有案件，再核對 Approved 歷史建案數及既存版本／Run 讀回。此為強來源對照，**不是正式連線字串證明**；零 DB 寫入、零 run 重算、零 publish，若任一步空值或不符則 FAIL。
- 新增 `tools/graduation-synthetic/prepare_ui_case.py` commits `8370dab895b2f5f759d00cfff41d88a1f0c88da3`, `37bed2b1d749d2ad6541a17a4bfe38d92aca1175`：檢查原本 v2 synthetic 來源 manifest 和 SHA，透過 5263 現有案件清單排除已見案例名稱及先前 3 個已測基準 SYN-00026/29/30，優先選尚未使用的 test split SRC/RC 案；從同源資料產 Excel + JSON truth，列出完整 UI 建案欄位；拒絕覆寫，僅供待偵錯 Target Pipeline，不得當最終決標並發布歷史庫。
- 修改 `.github/workflows/graduation-ui-build.yml` commits `4f16e3738d4dabaea57dcb94263668cabb09ee3d`、`5e69dda39ea79f205c4ac0697cd98f8b9ca54cbe`：Node22 `node --check` 兩個 script、Python `py_compile` fixture generator、Vue `npm ci && npm run build`；GitHub run 44913149083 已 `completed/success`（檢查 Node state script 和 Vue build），較新 run 44913252777 提交當時 `in_progress`，需後續查證。
- 實測界線：新增 DB identity/readback 工具與 fixture script **已提交 GitHub、尚未在使用者 Windows 執行**，不能宣稱 DB 4/4 PASS 或 UI 業務流程通過。沒有更改 DB／Migration／估算算法／歷史資料；目前仍僅正式支援鋼筋／混凝土量和結構體大項價格，鋼骨／模板／土方未開正式異常燈。
- 下一驗收：在既有 `integration/src-closure` 保留 untracked JSON 與 Python 腳本；先 cherry-pick 上述5個 commits，執行 `node scripts/verify-ui-state.mjs`，只有 DB 強對照成功後再生成新 Excel；`node scripts/verify-ui-preflight.mjs --excel <新檔>` 先跑 preview/inspect，最後用瀏覽器 UI 自行上傳並產生 Run、刷新再查證；決標發布僅在另外備份且可回滾的隔離測試資料庫執行，禁止將 test holdout 寫入 Approved 歷史。


### 2026-11-21 — 新版 DB 身分比對／UI 測試資料準備器之 GitHub CI PASS
- GitHub Actions `Graduation UI build and preflight syntax` run `44913252777`，head `5e69dda39ea79f205c4ac0697cd98f8b9ca54cbe`；實際 API 確認 `status=completed`，`conclusion=success`。
- Job `frontend-build`：兩個 Node 檢查腳本 `node --check`、`prepare_ui_case.py` Python `py_compile`、Node22 `npm ci`、Vue `npm run build` **全數 PASS**。
- 此 CI 不具備使用者本機 Docker 55433／API 5263，僅屬靜態語法和前端建置驗證。兩支新工具的實際 DB 比對、合成 Excel 產出及透過 UI 操作仍 **NOT RUN**。


### 2026-11-21 — 修正本機 UI 驗收 Excel 產生器缺檔，永久 CI 收斂

- 使用者 2026-11-21 本機 `integration/src-closure` 原始實測：`verify-ui-preflight.mjs` **7/7 PASS**；`verify-ui-state.mjs` **4/4 PASS**（Docker compose SRC 資料庫 55433、5263 API 14 件 AnalysisCase ID/名稱摘要一致、210 Approved train 一致、14 versions/28 runs 中14份保存結果可讀回）。以上是唯讀驗證，**不是 UI 新上傳或準確度測試**。
- 真實失敗：`prepare_ui_case.py` 因 `ModuleNotFoundError: No module named 'export_full_project_excel'` 終止；後續 `verify-ui-preflight.mjs --excel` 的 `ENOENT` 為 XLSX 沒產生的連帶失敗。兩個錯誤已獨立記錄，不能假稱 API/Excel parser 故障，也不可當成功。
- 原因及修復：feature 分支原本僅有 `tools/graduation-synthetic/prepare_ui_case.py`，沒有它引用的 main 既有 `export_full_project_excel.py`，而 exporter 也需要 `generate_irregular_excel.py`。已從 `main` 完整加入兩檔至 `feature/1d-api-integration`，commits `97558ce`、`3235696`。新增永久的 `test_prepare_ui_case.py` commit `0aca507`，以最小離線合成資料驗證 SHA 檢查、test-only 欄位、XLSX zip、JSON truth、拒絕覆寫及不選 train，防止再次發生「語法通過、執行時缺模組」。
- 使用者明確要求**不要臨時 CI/CD**。已更新長期 `.github/workflows/2a-build.yml`（commit `9f68b3a`）：保留後端 .NET 10 build + WorkbookParser Tests、前端 Vue TypeScript/Vite build，加上兩支長期唯讀檢查工具 node --check 和 Python 合成 Excel 產出回歸；刪除功能重複的 `.github/workflows/graduation-ui-build.yml`（commit `71738d9`），保留既有 `2a-handoff-review.yml`，不清除使用者未追蹤 Python/JSON、測試 Excel、Docker volume 或 PostgreSQL。
- feature 同步文件 `README.md`、`docs/CURRENT_HANDOFF_2026-11-11.md`、`docs/RESEARCH_TRACEABILITY_2026-11-11.md`、`docs/WORKLOG.md`、`docs/HANDOFF_CHANGE_REVIEWS.jsonl` 均已提交；Guardian review commit `a5d749c3df9b293f5ba17211515057d55e5434c1`，本批 10 檔 compare 已核對。
- GitHub Actions run `44914054847` （head `71738d9`）已 completed/success；更近一次 head `a5d749c` 的 run `44914199526` 已由 GitHub API 最終核實 `completed/success`，frontend-build、synthetic-fixture-contracts、backend-build **三個 Job 全部 PASS**，包括實際產生 XLSX 的永久 Python 回歸測試；此 CI 不等於已在 Windows 跑 UI/DB E2E。
- **仍未完成**：使用者 Windows 復跑匯出器、實際 Excel preview/inspect、UI 從上傳→量價分析→跨頁刷新 Run、決標回寫人工審核／發布、權限及稽核 E2E；最後才是獨立 holdout accuracy。沒有資料庫 Migration、演算法與正式工項增加，test holdout 不得發布成 Approved history。
- 下一步最短路徑：`git fetch origin feature/1d-api-integration`；在 `integration/src-closure` 依序 cherry-pick `97558ce 3235696 0aca507 9f68b3a 71738d9 cfcce98 76b845f 3644d48 923b7d2 a5d749c`（若有本機變更或衝突立即停）；不重新 seed／不刪已存在的工作檔。之後 `python tools/graduation-synthetic/prepare_ui_case.py --source graduation-src-extreme-v3 --output graduation-ui-1121-01.xlsx --truth graduation-ui-1121-01-truth.json`、`node scripts/verify-ui-preflight.mjs --excel graduation-ui-1121-01.xlsx`；再進瀏覽器跑真正 UI 寫入。


### 2026-11-21 — 修正 Windows 合成 UI 來源路徑缺失，正式改為無資料夾即可匯出

- 使用者已成功 cherry-pick 上輪永久 CI 批次，Python `python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v` **2/2 PASS**；但其 Windows 工作目錄沒有 `graduation-src-extreme-v3/manifest.json`，以先前指定 `--source .\graduation-src-extreme-v3` 的命令執行時真實 **FAIL**（`[Errno 2] No such file or directory`），後續 `verify-ui-preflight --excel` 的 `ENOENT` 是 XLSX 未產生之連帶結果。原有 API 7/7、DB 4/4 唯讀 PASS 不受此錯誤影響。
- GitHub `feature/1d-api-integration` 修正 `tools/graduation-synthetic/prepare_ui_case.py` commit `de6a4f52fd3510164021216082bff0f21e729042`：`--source` 現在是可選；**省略** `--source` 時直接使用既有 `generate_irregular_excel.make_xlsx` 產生可上傳的 SRC/RC 多工作表示例 Excel、逐列 quantity/price/amount、SHA256、建案資料 JSON，完全不讀取任何外部 CSV、DB 或 API；如顯式指定不存在的來源則回報正確缺失，不默默轉用其他資料。
- 新離線 fixture 是 `SYNTHETIC_UI_ONLY_NOT_HOLDOUT_OR_HISTORY`；`eligible_for_training_history=false`、`is_accuracy_ground_truth=false`，含鋼筋、混凝土、模板、鋼骨和基礎示意數字；只能走 `/analysis-cases` 自行上傳功能，不得走正式決標回寫並批准歷史。**不代表真實工程參考值或任何模型準確度。**
- `test_prepare_ui_case.py` commit `29b2a1d092dd951b097b9dca40a1626f2d9084f6` 加入真正 subprocess 呼叫 Python CLI 的測試：無來源夾離線產 Excel/JSON、ZIP integrity、數量與金額、RC/SRC、no-overwrite／安全 metadata。執行於既有永久 `2a-build.yml`，沒有再建立 Workflow。CI run `44914577394` 在此提交上 `completed/success`；CI 測試成功仍**不能**等同 Windows 本機的 Excel 預覽、瀏覽器上傳、Run 計算或審核發布成功。
- 已同步 feature 分支 `README.md`、`docs/CURRENT_HANDOFF_2026-11-11.md`、`docs/RESEARCH_TRACEABILITY_2026-11-11.md`、`docs/WORKLOG.md` 以及 `docs/HANDOFF_CHANGE_REVIEWS.jsonl`，此輪 review commit `4602a38976a75e6f0d1e9d5724f65b9e3cc3c669`。
- **下一輪 Windows 命令**：先 cherry-pick `de6a4f52 29b2a1d0 a0c11e7 9e621c8 985602a 76fbed9 4602a389`；`python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v`；`python .\tools\graduation-synthetic\prepare_ui_case.py --output .\graduation-ui-1121-01.xlsx --truth .\graduation-ui-1121-01-truth.json`（刻意**不帶 --source**）；`node .\scripts\verify-ui-preflight.mjs --excel .\graduation-ui-1121-01.xlsx`。若 XLSX 或 truth 已存在，必須使用新的輸出檔名；不可刪除舊成果。預期之前的 7/7 加上 Excel preview/inspect 2 PASS，但以使用者實際終端為準。
- 仍待本機真實 UI 流程：從 5174 建立此新 synthetic AnalysisCase→V1→執行數量和結構體大項價格→28+ 新 Run 跨刷新驗證；決標審核／發布須與 heldout 分離的可回滾測試環境；最後才做模型成效比較。無 DB schema/Migration/產學正式資料或演算法變更。


### 2026-11-21 — 本機 Excel 9/9 PASS；Windows 測試輸出 CP950/UTF-8 修正，UI Run 保存核對

- 使用者本機 `integration/src-closure` 成功 cherry-pick 前一組 `de6a4f52`～`4602a389` 7 commits（本機 `e6af81a`），實際生成 `graduation-ui-smoke-1121-02.xlsx` 及 truth JSON：SYNTHETIC_UI_ONLY_NOT_HOLDOUT_OR_HISTORY、住宅 SRC／臺中市、12,000 m²、基地 950 m²、12F/B2、2025-01，3 worksheets、15 detail rows。**這份 fixture 是純 UI smoke，不是 holdout 標準答案或可上傳 Approved History 的決標資料**。
- 使用者執行 `node scripts/verify-ui-preflight.mjs --excel graduation-ui-smoke-1121-02.xlsx`：5263 health、standalone policy、14 cases、210 參考案、鋼筋/混凝土 readiness、5174 proxy、Vue route、Excel preview 3 sheets/2 detail、Excel inspect 15 candidates/4 warning rows，實際 **9/9 read-only checks PASS**。4 warnings 需核對內容，不能標示資料無缺點。API/DB 身分 4/4 PASS 是之前使用者獨立執行的結果。
- 同輪 `python -m unittest discover -s tools/graduation-synthetic -p test_*.py -v`：**3 項中 2 PASS / 1 ERROR**。Windows 3.12 child process stdout 使用本地 CP950，test parent 設 `encoding=utf-8` 解碼，導致 background reader `UnicodeDecodeError: invalid continuation byte` 和 `completed.stdout=None` 後續 `json.loads(None)` TypeError。**不是 XLSX 產生或 API 解析錯**，因使用者單獨直接產生 Excel 和 API preview/inspect 均成功。
- 在 GitHub `feature/1d-api-integration` 修正 `tools/graduation-synthetic/test_prepare_ui_case.py` commit `79af4b822b523d8843d353f5b6fcd5144381f473`：只對該測試子程序 `env=os.environ.copy(); env["PYTHONIOENCODING"]="utf-8"`，與 parent `encoding="utf-8"` 對齊，不修改使用者 Windows 全機環境、正式程式碼或 DB。
- 為接續完整 UI 驗收，不建立臨時腳本，改擴充原本永久 `scripts/verify-ui-state.mjs` commit `d6b3fb1463111e7c502d31f4657c47455e8d8d5a`：新 `--case-id <ID>` 會在 55433 Docker/DB/API 四項唯讀核對後，額外要求 `SYNTHETIC-UI-驗收-` 開頭、目前版本同時具備 Completed Quantity 及 MajorPrice:結構體工程 Runs，並逐筆用 GET 讀回結果；成功為 5/5。零 POST、零 DB 寫入、不能取代人類瀏覽器操作或模型準確度驗證。
- feature 分支已同步 `README.md`、`docs/CURRENT_HANDOFF_2026-11-11.md`、`docs/RESEARCH_TRACEABILITY_2026-11-11.md`、`docs/WORKLOG.md`、`docs/HANDOFF_CHANGE_REVIEWS.jsonl`；最新審查 commit `0316939010def4b71a870e2a347ad3e1f8df4b9c`。沒有新 CI/CD Workflow；仍沿用 `.github/workflows/2a-build.yml`，新測試修正與 state script 的 CI runs `44914884628`、`44914939982` 在該各自 head **completed/success**，最後的文件 head `031693901` run `44915011422` **已最終 completed/success**；永久 `2A Build Validation` 前端／後端／Python synthetic fixture contracts 均通過。GitHub CI 通過不等於 Windows 本地 Python 修正已實測或 UI 操作通過。
- 下一步：使用者安全 fetch/cherry-pick 最新 7 commits `79af4b82 d6b3fb14 3097c017 64814953 4129942d 4ef8edcf 03169390`；只需重跑 Windows Python unittest，**不用重建 Excel、也不必重跑 9/9**。直接進 5174 `/analysis-cases`，點「新增待偵錯案件 → 自行上傳」，填上述原 fixture metadata，明確只勾 `結構體明細` 與 `基礎及地下工程`，先人工檢查4條警告，再「建立案件與 V1」。成功後執行「數量偵錯」及大項「結構體工程 → 價格異常」，刷新並檢視歷史 Run，再以 `node scripts/verify-ui-state.mjs --case-id <新案件ID>` 驗證**同一 DB 持久化**。這個 write E2E 還未執行；決標草稿／發布與 Audit 仍未驗收，準確度最後進行。不得將 pure UI smoke workbook 發布成歷史。


### 2026-11-21 — Case #15 真正 UI 上傳和量價分析畫面已驗證；四筆材料相關排除需人工釐清
- 使用者提供 `http://127.0.0.1:5174/analysis-cases/15` 實際瀏覽器截圖，明確證實建立 `SYNTHETIC-UI-驗收-graduation-ui-smoke-1121-02`、自行上傳 V1、**15 筆工項**，數量偵錯（鋼筋/混凝土）結果顯示、結構體工程價格分析結果顯示並提示已保存 `Run #31`。SRC、住宅、臺中市、12,000m² 樓地板、950m² 基地、12F/B2 與輸入 fixture 一致。**已完成 UI 畫面層實際操作**，但尚未由獨立 5/5 API+DB 唯讀腳本驗證這兩筆 Run 的刷新持久化。
- UI 價格結果：結構體工程 29,268,000 元、每坪 8,062.81，對應合成歷史 P15/P50/P85 每坪 28,591.65／28,718.02／39,487.84，8 參考案、ESS 6.25、Low。結構體測試標單 10 行逐列 quantity×unitPrice 加總正好 29,268,000，靜態對照**大項價格金額一致**；Low 不是舞弊或真實公司估算精度證據。
- UI 數量結果：鋼筋 38,000kg、混凝土 3,650m³，兩類各 8 參考案。原 `prepare_ui_case.py` 示意明細鋼筋總計 130,000kg、混凝土 5,450m³。根據目前實際 `TenderImportInspectionService.ClassifyQuantityCandidate`、`TwoAPreprocessService.BuildItem`、`TwoAQuantityAnalysisService`，**三筆含「加工／組立／綁紮／工資」的鋼筋行（42,000+32,000+18,000kg）及一筆「預拌混凝土澆置」（1,800m³）被排除**。故納入量=38,000kg/3,650m³，正好對應之前 Excel inspect 的 4 warning rows。這是保守名稱分類，不是加總算術錯誤；但複合工項未證實一定只有勞務，且部分「鋼筋施工」「泵送與搗實」目前仍被納入，**需要材料／服務語義確認及 UI 清楚呈現排除理由**，避免錯把納入量當整案總材料量。不擅自為消掉 Low 調模型或放寬關鍵字。
- 此輪程式碼與 DB **未修改**；已實際提交 feature `docs/CURRENT_HANDOFF_2026-11-11.md`、`docs/RESEARCH_TRACEABILITY_2026-11-11.md`、`docs/WORKLOG.md` 與 `docs/HANDOFF_CHANGE_REVIEWS.jsonl`，審查 commit `9952a38d8aa766aa98037d3248448270b14db27c`。
- **下個唯一必要驗證點**：使用者從專案根目錄，在現有 5263→隔離 55433 DB 活躍的情況執行 `node .\scripts\verify-ui-state.mjs --case-id 15`；只讀、預期若確有已儲存之 Quantity+MajorPrice:結構體工程 同版本 Run 則回報 5/5。若失敗保留原始 Run 與錯誤；不得重傳已建立的 V1 或重 seed。再從頁面「歷次偵錯」重開結果／刷新確認。決標及正式來源認證、真實精度另驗。


### 2026-11-21 — 使用者 Case #15 隔離資料庫實測 5/5 PASS；增加唯讀工項排除原因介面
- **使用者 Windows 真實結果**：`node scripts/verify-ui-state.mjs --case-id 15` 全部 **5/5 isolated read-only checks PASS**：唯一 Docker SRC isolated database (127.0.0.1:55433)、5263 API 對應 55433 全部 **15 案** ID／名稱指紋一致、Approved 合成歷史參考群 **210 案**不變、DB **15 版本／32 Runs** 可查、抽讀 **15** 份 Run payload；指定 synthetic UI #15 同版本 Quantity 與 MajorPrice:結構體工程 Run 皆能實際 GET 讀回。加上使用者提供的 UI V1 自行上傳、15筆工項、數量／價格結果畫面，**本輪 UI 上傳與分析保存的 E2E 已有實證**。但此 5/5 結果不是瀏覽器刷新點擊自動化、決標正式發布、權限或模型真實工程準確度。
- **已確認的解釋性風險**：case #15 15筆示意標單，原鋼筋名義 130,000kg、混凝土 5,450m³，但系統的 `TenderImportInspectionService.ClassifyQuantityCandidate` 與 `TwoAPreprocessService.Build` 依複合工項字樣排除3筆鋼筋(92,000kg)+1筆混凝土(1,800m³)；正式量分析僅納入 **38,000kg／3,650m³**；大項價格仍計入全部結構體工項，總價 **29,268,000元**。這是分類策略而非加總漏算，**並不代表被排除工項一定沒有材料**。資料源是人工合成 smoke，不是 holdout 或真實工程異常證據。
- **新相關修改，均在 GitHub `feature/1d-api-integration`，尚未 cherry-pick 本機整合分支／未 merge main**：
  - `崴正/Services/AnalysisCaseService.cs` commits `eae57e4d31cae064f1b352b94471b7602b9ce007`、`2bfb31664328c80d987ce32d47dd5758b6af736a`：只讀 `GetVersionQuantityScopeAsync`，從版本保存工項依與正式 `RunQuantityAnalysisAsync` 完全共用的 `ToPreprocessSourceItems` → `TwoAPreprocessService.Build`，以既有工項 ID、Excel sheet/row、quantity/unit、Included／Excluded／Pending、reason/normalized unit 回覆。畫面使用**現行規則重算**而非改寫已保存 Run 的快照，絕不當作舊 Run 的完整歷史判定依據。
  - `崴正/Controllers/AnalysisCaseController.cs` commit `3819ad1028129a04442c4933b241646fae17bca7`：唯讀 GET `/api/analysis-cases/{id}/versions/{versionId}/quantity-scope`，不 SaveChanges、不建新 Run、不觸碰 Approved history。
  - `frontend/src/views/AnalysisCaseDetailView.vue` commit `4f6ccc025c50fea4559cfff4465eff1583c5b172`：新增工項納入範圍摘要及可展開的排除／待確認清單、原 Excel sheet/row、數量/單位、排除理由；顯示材料相關名稱被排除筆數與人工審核提醒。API 無法連線時呈非致命警告，不影響載入既有 V1、已保存分析。
  - `tests/WorkbookParserTests/Program.cs` commit `3be3a5bb96840cc1214904b8c2a70fa44b07b640`：永久測試同源15列、4條 Excluded 候選、38,000kg／3,650m³、結構體價格 29,268,000元及排除理由。`scripts/verify-ui-state.mjs` commit `9c7ed7c85ecc0acf85001450047c0fa613c3057e`：既有 `--case-id` 再驗新 GET 的逐項合約與 #15 精確量，**新版後端才可取得預期 6/6**；單純執行不帶 ID 仍為4/4。無新暫時 CI/CD。
- **GitHub CI 實際驗證**：永久 `2A Build Validation` run `44918762420`（head `9c7ed7c85e`），`completed/success`；backend Release build + WorkbookParserTests、frontend vue-tsc + Vite、Node read-only scripts check、Python synthetic fixture contracts 三個 Job **PASS**。**未在使用者 Windows 重啟新 C# 後端，也未執行新 `quantity-scope` URL 或 6/6 本機驗證**，不得將 CI 通過冒充新 UI 已實測。
- **最新 CI 再核實**：正式 feature 最後交接審查 head `a27f850d356aa46b0f201dc2e672af3341e9014c` 的永久 `2A Build Validation` run `44918895956` 已 `completed/success`；無新臨時 Workflow。此結果仍不代表本機已重新啟動 5263 或已實際取得 6/6。
- **交接**：feature 已同步 `README.md`、`docs/CURRENT_HANDOFF_2026-11-11.md`、`docs/HANDOFF_CHANGE_REVIEWS.jsonl`（最新交接審查 commit `a27f850d356aa46b0f201dc2e672af3341e9014c`）；main 本檔為實際提交的跨分支交接索引。
- **下個驗證**：在使用者本機 `integration/src-closure` 安全 fetch + cherry-pick `eae57e4d 3819ad10 2bfb3166 4f6ccc02 3be3a5bb 9c7ed7c8 c155baad 9c94381a a27f850d`（若衝突停，保留 untracked 和 existing DB）。切勿直接 `git pull` 重置本機；因修改 C# **必須停止舊的 5263 進程、重新啟動相同 55433 isolated DB 的新版後端**，保持 5174 Vite proxy 5263；刷新原 `/analysis-cases/15` 並查看新卡片與四筆候選排除，執行 `node scripts/verify-ui-state.mjs --case-id 15` 期待 **6/6**，以前 5/5 不代替此新版檢查。不可重跑既有 32 Runs、重新上傳 V1、重 Seed、碰正式 DB；決標審核／稽核、真實模型準確度仍是未完成事項。


### 2026-11-21 — Windows 新版 quantity-scope 整合遇到 Git 文件衝突及後端 exe 鎖定
- 使用者交付本機完整 PowerShell 終端紀錄：在 `integration/src-closure` 依序 cherry-pick 新功能批次，**程式碼及測試檔前 7 筆成功**：`6f0d9ea`(AnalysisCaseService)、`9772d0f`(Controller)、`bb061d2`(說明修正)、`01688a3`(Vue UI)、另有 WorkbookParserTests 的測試 commit、`0e96db2`(永久 state checker)、`0b1119a`(README)。**第 8 筆 `9c94381`** 在 `docs/CURRENT_HANDOFF_2026-11-11.md` 遇到文字內容合併衝突；Git 停留在 cherry-pick，最後第 9 筆 `a27f850d` 未確認完成。不可直接再次執行同批 cherry-pick、`git reset --hard`、`--abort` 或無條件採用 --ours/--theirs（可能遺失交接）。
- 使用者在 cherry-pick 未完成時執行 `dotnet build 崴正/崴正.csproj -c Release`，真正 **FAIL** 主因：本機舊後端進程 **崴正.exe PID 53748** 持續占用 `崴正/bin/Release/net10.0/崴正.exe`，`MSB3026` 持續重試，最後 `MSB3027`、`MSB3021`，不是 C# compilation errors；另可見 `TenderImportInspectionService.cs:135 CS8604` nullable warning，未改、不視為本次致命問題。
- 同輪 `cd frontend; npm.cmd run build` 真實 **PASS**：Vue `vue-tsc --noEmit && vite build`，Vite 7.3.8，66 modules，1.65s；這驗證已 cherry-pick 的新版 UI build，**不能推論新 backend 已在 Windows 建置成功**。
- 已有 Case #15 上一輪 `verify-ui-state.mjs --case-id 15` 真實 **5/5 PASS**、15 cases、15 versions、32 Runs、210 歷史合成 approved；新版 `quantity-scope` API 的 Windows `6/6` **尚未執行**，不得誤報已驗收。
- **修復策略**：只解 `docs/CURRENT_HANDOFF_2026-11-11.md` 的 append-only 衝突、保留 ours 與 incoming Section 28，`git add` + `git cherry-pick --continue`，確認 sequencer 清空；以使用者已開啟之舊後端視窗 Ctrl+C 停止 PID 53748，或驗證 exe 路徑後精準停止該進程，再重新 `dotnet build -c Release`；在原已驗證的 55433 隔離 DB 連線環境重啟 5263，`node scripts/verify-ui-state.mjs --case-id 15` 目標 **6/6**，刷新 5174 查看新排除清單。不要重建、重 Seed 或上傳 synthetic V1。無需新增臨時 CI/CD。


### 2026-11-21 — 公開 Repository 前的 GitHub 分支盤點與 POC PR 清理
- 2026-11-21 由連接 GitHub 實際讀取 repository metadata：`YI-CHENLIN1204/BiddingSystem-2A` **當時 visibility=public**，default branch=`main`；僅有遠端三分支：`main`、`feature/1d-api-integration`、`feature/1d-detection-integration`。**公開狀態已存在，並非本次工具設定。**
- PR #1 `POC: visualize project-wide 1D to 2A data flow`（`feature/1d-detection-integration` → `main`）屬 2026-10-06 展示用的**非正式**隱藏 POC／不寫資料庫，改動只有 `frontend/src/router/index.ts`、`frontend/src/views/OneDIntegrationView.vue`；本次已**實際將 PR #1 關閉**（closed、unmerged），保留 GitHub PR 歷史以利追查。**遠端 POC 分支尚未刪除**；可透過 GitHub Branches UI 或由使用者執行 `git push origin --delete feature/1d-detection-integration`，不可將關閉 PR 說成刪分支。
- PR #2 `feature/1d-api-integration` → `main` 仍 **open、unmerged**，而且 `main` vs feature 有大量分岔（查詢時 feature ahead 671 commits、behind 208 commits、177 檔差異）。這個 feature 保存主要 Vue/ASP.NET 2A 系統開發，**絕不可把它當無用舊分支刪除或未經測試直接強制覆蓋 main**。本機 `integration/src-closure` 曾有交接文件 cherry-pick 衝突，最新是否已解決需先在使用者本機 `git status` 查實。下一階段將穩定整合功能到 main、跑永久 CI 及隔離 DB E2E，再刪 feature。
- 公開安全檢查進度：在 `main` 和 feature Git tree 中均存在 `course-version.zip` 舊版程式封存檔（約 1.9MB），**目前無法證明封存檔內沒有敏感資料**；另外存在開發設定、`.env.example`、容器測試配置、歷史 handoff/研究紀錄。只是初步路徑／部分現行文字檔掃描，**不是完整原始碼、壓縮包與全 Git 歷史的密碼／合作資料掃描**。刪 branch 或刪檔不會自動清除所有 Git 歷史和既有 public exposure；公開前需取得合作資料授權、檢查全歷史／ZIP、必要時撤回公開並旋轉已暴露之憑證。給組員看程式碼可改用 private repo 邀請 Collaborators；若需公開作品集，以去識別、單一乾淨提交的新展示 Repo 比原始產學 Private 開發歷史安全。
- 已完成：唯讀盤點與關閉舊 POC PR #1。未完成：遠端 branch deletion（目前 GitHub 連接工具不具該操作）、主功能 PR #2 merge、已公開歷史安全查核、任何 visibility 變更。本次**沒有**刪除遠端分支、覆寫 main 或修改資料庫與正式系統。


### 2026-11-21 — 工作區實際修復及 Windows 驗證；現有 5263 新端點仍 404
- GitHub 開發交接提交：`92eedf7ef19396b7d79eccb8e79adef01af4b8bf`，審查紀錄 LF 格式保全提交 `97e582cb971cb60f71ae1ca7b9b44556df426dd7`，分支 `feature/1d-api-integration`。更新主交接、研究回溯、新對話入口、WORKLOG 與 JSONL 審查；未合併開發程式到 main。
- **分清三種狀態**：桌面原 `integration/src-closure` HEAD `dadc7aa`，仍在最後 `a27f850d` 的 JSONL 衝突。另於可寫工作區 clone 重現並完成修復，recovery branch 本機 commit `ab32e344ef76f67e8c0134f0592ba5b74f136a1f`，完整保留 19 筆有效、無重複紀錄；此修復 commit 尚未發布 GitHub，也不代表桌面原工作樹已完成。
- Windows .NET SDK 10.0.301：API Release build **PASS，0 errors**；WorkbookParserTests **全部 PASS**（含 case15 工項/排除量/價格契約）；Python 3.12 合成 UI 回歸 **3/3 PASS**；兩個永久 Node 工具 syntax check **PASS**。核心來源與 a27f850d 相同；前端沿用整合副本已安裝 node_modules，未執行 npm ci。
- 前端標準 build 的 vue-tsc 通過，但預設 Vite config loader 因工作區上層目錄權限失敗；改 `npm.cmd run build --prefix frontend -- --configLoader native` 後 **PASS**（Vite 7.3.8，66 modules）。沒有修改正式設定/套件/CI。保留既有 CS8604 與 Parser build 的 EFCore.Relational MSB3277 警告，尚未修正。
- 本輪直接 GET：`/api/analysis-cases/15` **200**，currentVersionId=15；`/api/analysis-cases/15/versions/15/quantity-scope` **404**。永久 checker 因 Docker named-pipe 及 config 權限失敗，**新版 6/6 未完成**。不能把副本 build PASS 或前輪 5/5 套用到目前服務。
- 交接 gate `python scripts/check_handoff_review.py --base a27f850d356aa46b0f201dc2e672af3341e9014c` 在本機文件提交 **PASS（4 files/1 review）**；遠端舊 19 筆審查紀錄逐筆比對完整保留。
- 下個驗證：以使用者權限完成桌面最後提交、同一已核實 55433 隔離連線重啟新版 5263；確認新 GET 200 後跑永久 checker 6/6。DB、Migration、既存 Run、Approved history、演算法皆未改動。決標/稽核/實際模型準確度仍未完成。

- **本輪 CI 最終核實**：永久 `2A Build Validation` run `44921528280`（head `97e582cb971cb60f71ae1ca7b9b44556df426dd7`）已 `completed/success`。`backend-build`、`frontend-build`、`synthetic-fixture-contracts` 三個 Jobs 全數通過，含 Linux 乾淨 `npm ci` + 標準 `npm run build`；沒有新增 Workflow。此 CI 不改變桌面原工作樹衝突、5263 新端點 404 或本輪 Docker 權限阻塞，6/6 仍未完成。


### 2026-11-21 — 組員報告版本分支指定及雙 README 公告
- 根據 GitHub 現況：Repository 已 Public，default `main`，但 PR #2 `feature/1d-api-integration → main` 仍 Open／未合併；先前比較顯示兩者明顯分岔（feature 比 main ahead 673 / behind 210 commits）。組員要根據目前 2A 程式碼撰寫專題報告，**指定共享的 `feature/1d-api-integration` 分支**，不是現階段尚未整合最新功能的預設 main。此為「報告閱讀／開發分支」而非已釋出的 production release；正式報告應記錄固定 Commit SHA。
- 已**實際修改並提交** `main/README.md`（commit `c159f85669d4e98504764312a3fbcb5219e5b9a8`）、`feature/1d-api-integration/README.md`（commit `42eac3eeeb668f6c5c531167e6a2955025fc982d`）的最上方醒目提醒與分支超連結。 `main` 原 README 中「此專案維持 Private」與 GitHub 現況衝突，已改為公開風險注意事項。feature `docs/CURRENT_HANDOFF_2026-11-11.md` 另行同步記錄。
- 本輪是分支導覽／文件決策，**沒有**合併主程式碼、動過資料庫或新增 CI；未做新的 Windows/E2E 驗證。 `course-version.zip` 和完整歷史對外公開安全性仍未核准，須與合作方確認授權；GitHub Public 狀態非本輪修改。


### 2026-11-22 — 桌面最後 cherry-pick 已完成；目前對話尚未載入新增資料夾權限
- 使用者已將桌面 integration 及主 repo 加入專案資料夾。本輪直接讀取原 `C:\Users\User\Desktop\BiddingSystem-2A-integration`：分支 `integration/src-closure`、HEAD `5c0ae5c`（最後審查紀錄提交）、`git status --short` 只剩原有 6 個 untracked 測試／比較檔，沒有 UU；`CHERRY_PICK_HEAD` 不存在。**前述「原桌面仍在最後衝突」已過時，不得再重新 cherry-pick 或修同一衝突。**
- 目前工作階段 cwd 仍是原本的 Codex 暫存工作區。對原桌面 `docs/HANDOFF_CHANGE_REVIEWS.jsonl` 與主 repo `.git/worktrees/BiddingSystem-2A-integration/index` 嘗試只開 ReadWrite handle（未寫入任何位元組），兩者均 Access denied。新增資料夾尚未在本段對話提供可用寫權；不能宣稱可直接改原檔。
- HTTP 實測：5263 case15 GET=200；同版本15 quantity-scope GET=404。新版 API 重啟、Docker/DB 6/6 仍未完成；沒有啟動／停止原程序或修改 DB。
- 下一步：從已附加兩資料夾的本機專案建立新 Local／This computer 對話，integration 設為主要目錄；先核對實際寫權、Git HEAD/status 和 DB 身分，再停止舊 5263 並以原 55433 隔離連線重啟新版後端。保留既有 V1/Run/資料庫與未追蹤測試檔，不重 seed、不重算、不發布 smoke。


### 2026-11-22 — 合成準確度回測與144組對照實際完成；保留正式參數，兩項編譯警告已修
- 使用者本輪重新指定三個必要收尾工作包，**本次優先實作數值回測及對照**，不再受舊「accuracy永遠最後」段落限制。不重跑已完成的3條SRC來源API、9組source smoke或6組城市/基地敏感度，不重seed、不重傳既有13標單、不發布holdout。
- GitHub開發提交：`ab565eaaca905eb75f2bf2bdbdd764574ad2b61c`，`feature/1d-api-integration`；本輪12檔（11個受審查變更＋JSONL），2筆新審查紀錄且遠端原紀錄無刪除。**未merge main、未套回桌面或重啟原5263。**
- 已提交可讀報告 `docs/HOLDOUT_EVALUATION_2026-11-22.md` 與機讀摘要同名JSON；新增永久 `tools/graduation-synthetic/evaluate_live_holdout.py`、測試。來源既有SYNTHETIC v2／300建案／143700對齊明細，SHA與lineage核對；實際210train/45validation/45test，不是90個獨立test。正式5263 range API完成237個project/scope之Top8/完整池配對（474次只讀range POST），均通過reference ID／source train／SRC-only／數值/日期/面積及區間重現檢查。沒有建立cases/runs/history或修改settings，API case/settings前後快照不變；不是新Docker連線認證。
- **SRC test未注入非極端子集**：鋼筋n8，現行MAPE17.245%、coverage75%；混凝土n8，9.923%/75%；結構體價格n5，14.563%/60%。僅代表合成數值誤差；「未注入」不等工程正常，n小且不是時間順序預測。3個test案曾被舊paired/UI診斷，另提供排除後42案補充結果，不稱全新盲測。
- **參數對照**：六因子3組×TopK5/8/16/32×半衰期1/3/6/無×相似指數0/1/2，共144組；只用validation的相對interval score宏平均選擇，再評test。選出的40/25/10/15/5/5、K32、半衰3、指數1，使SRC鋼筋MAPE降至13.195%、混凝土7.937%，但SRC價格升至24.331%、coverage降至20%。**決策：維持正式25/30/10/15/10/10、K8與既有設定，不部署全域替代組，不依test再挑參數。** 替代組是已對齊API baseline的離線counterfactual，不是將144組寫入settings。
- **Precision/Recall/FPR不可識別**：13個paired XLSX/truth的hash已唯讀通過，只來自3案。全部variant仍manual_review_required，其cause_kind為可能合理的設計變異、非高報價證明的單價壓力或兩者混合；baseline無工程正常認證，不能自動當TN。JSON三指標維持null，這是標籤定義缺口，不能靠多跑API補出。
- **同批警告修復**：`崴正/Services/TenderImportInspectionService.cs` 對InspectionRow Unit null顯式轉空字串，保留分類與缺單位警示（CS8604）；`崴正/崴正.csproj` 顯式Relational10.0.5對齊既有EFCore，避免測試project採到10.0.4（MSB3277）。無Migration/schema/估算算法變更。
- **實測PASS**：Windows API Release **0 warnings/0 errors**；WorkbookParser全通過；Python **11/11**；兩個永久Node syntax PASS。交接gate首跑因Git quotePath將中文路徑轉義而FAIL，已改`scripts/check_handoff_review.py`使用NUL分隔並加入真實temp Git/core.quotePath=true測試；最終本機整批gate `--base 853e98d48c592e68b524c4fb4eee5a36a95e350e` **PASS：11 files/2 reviews**，遠端相對e644edb0的變更與review coverage亦核對一致。
- **永久CI最終PASS**：`2A Build Validation` run `44927779220`，head `ab565eaaca905eb75f2bf2bdbdd764574ad2b61c`，completed/success；backend-build、frontend-build、synthetic-fixture-contracts三Job全部success。不新增臨時workflow。
- **三包剩餘界線**：(1)合成數值回測已交付，分類Precision/Recall/FPR受真值缺口阻塞；(2)對照已交付並作不換正式參數的決策；(3)兩項編譯警告及交接工具問題已修、回歸與CI通過，但原機寫權/Docker、新5263 quantity-scope 6/6、既定完整系統整合/PR#2與最終版本仍未完成。不得寫「企業準確率通過」或「整個系統已結案」。


### 2026-11-22 — Supabase 現況唯讀盤點與跨裝置資料保存
- 使用者已確認目前 Supabase 畫面 `public.project_master` 的建案皆為模擬用途資料，不能以 Supabase UI 「PRODUCTION」標籤認定為崴正真實建案。使用者 Windows 先確認本機 55433 PostgreSQL 持久層為 Docker named volume `graduation-src-synthetic_graduation_src_pgdata`（`/var/lib/postgresql/data`），並使用 `pg_dump -Fc --no-owner --no-acl` 及 `pg_restore --list` 成功，另以 `docker cp` 實際保存備份 `bidding2a_graduation_src_20261122_014156.dump` 到 Windows 專案外 `Desktop/BiddingSystem-2A-backups`，檔案 5,907,064 bytes；尚未執行完整還原演練，SHA256 指令已下但結果未提供。**不可將備份上傳 Public GitHub。**
- 2026-11-22 透過已連接 Supabase 直接執行**純 SELECT 計數**確認現有雲端 Supabase 專案（與本機 55433 分開）：`public.project_master=10`，`public.analysis_case=1`，`public.analysis_run=24`，`public.final_bid_item=634`，`public.historical_transaction=23`。這是雲端即時讀取結果，**不同於**此前使用者本機 55433 read-only 5/5 的 `210 synthetic Approved / 15 analysis cases / 15 versions / 32 runs`。兩者資料並未自動同步。使用者另表示 Supabase 10 個建案皆為模擬資料；是否有來源與權限資訊需獨立審核，不據畫面自行推斷真實工程資格。
- 跨裝置需求：正式授權歷史與合成專題測試必須物理或明確邏輯隔離。**尚未選定、建立、還原或同步任何雲端新資料庫**。建議先備份現有 Supabase、比對雙方 EF/SQL schema 與來源資格，再由使用者授權選擇建立獨立 Supabase 測試 Project 或受控測試環境；本機 55433 `.dump` 應在隔離空環境做還原驗證，不能直接 `pg_restore` 覆寫當前雲端主分支，也不可把合成 Approved 視為實際已鑑真的歷史。多裝置共用應走後端 API + 私密 DB connection，禁止在公開 Vue/GitHub 放資料庫憑證。
- 這一輪沒有修改任何 PostgreSQL/Supabase 資料、Migration 或分析程式；是既有資料位置與備份狀態的唯讀盤點。11/25 交件期限仍需追查實際平台與上傳內容。


### 2026-11-22 — 2A Supabase 全量測試資料搬遷 DRY RUN 停止：實際 Schema 不同
- **E1 使用者 Windows 原始回報**：舊搬遷工具 `01-check-only.cmd` 真實檢查，來源 `bidding2a_graduation_src_20261122_014010.dump` 已通過 SHA256 `8C30A97A66B067191A5AAD17B7C34774C2601A33F75AB3423471AEC4E8816F2F` 與 `pg_restore --list`；雲端舊版 `public` 已成功備份至使用者 Downloads 下 `BiddingSystem-2A-migration-backups/supabase_before_replace_20261122_021719.dump`，備份 SHA256 `999761CC6613C47859DA0D47F290DB3EDF86AC4DFA7FCADD52C84A8A245A3866`。因 Schema mismatch 停止、exit status 1；**未修改雲端任何資料**，絕不可宣稱已完成 Supabase 搬遷。
- 檢查兩者都是 **17 張 2A public 資料表**；直接檢視使用者提供的 PostgreSQL Custom Dump TOC DDL，來源共 **165 個欄位**（許多 `text`、無精度 `numeric`）；經連接 Supabase 純 SELECT 核對現有雲端共 **173 個欄位**，其中8個雲端獨有且 nullable/defaulted：`analysis_bid_item.source_category1/2/3`、`analysis_bid_version.source_1d_project_id`、`final_bid_item.category1/2/3`、`item_dictionary.created_at`。另外雲端 `varchar(n)`、`numeric(p,s)` 對本機 `text`/較寬鬆 `numeric` 存在**真實相容性風險**，不能把第一輪錯誤當純格式假陽性或取消安全檢查。使用者明確同意**替換**現有 Supabase 2A 測試資料，不是合併；不可牽涉 Auth/Storage/其他非2A資料。
- 已在此對話提供新的 **`bidding2a-cloud-migration-compatible.zip`**（僅對話附件，未提交 Public GitHub）包含同一份 SHA256 已確認來源備份、重寫的 `migrate-bidding2a-cloud-schema-compatible.ps1`、`01-check-only.cmd`、`02-APPLY-OVERWRITE.cmd` 及 README。新增 whitelist 僅允許上述8個 nullable 雲端額外欄位；來源 text→雲端 varchar 前逐欄檢查值長度、numeric→限精度前逐欄核對不會改值；額外驗證原始 dump Schema 表/欄及 17 張 TABLE DATA；只有經再次確認、已備份雲端及輸入 `REPLACE-2A` 後才用 public 業務表單一 Transaction 執行 data-only COPY/驗證 count。新 SQL 排除非交易性的 `setval()`，改用 Transaction 內 `ALTER SEQUENCE RESTART`，對 Auth/Storage 不做變更。
- **驗證界線**：已檢查 ZIP 完整性、來源 dump 雜湊、與現有 Supabase schema 的8個欄位來源；**新版腳本尚未在 Windows Docker + Supabase 真實執行，DRY RUN/Apply 均 NOT RUN，並未修改雲端資料**。如新檢查仍 FAIL，繼續 fail-closed，須診斷實際長度／精度／FK/Check，不得直接略過。覆蓋後才重新核對 `project_master` 等全17表筆數、API 讀回，且可回復原雲端備份。
- 連接 Supabase `list_tables` 同時回報全部17張 public 業務表 **RLS disabled**（高風險）；本輪未擅自啟用 RLS，以免破壞現有 API，後續需釐清 Supabase Data API 權限及使用者 AuthN/AuthZ 後再訂政策。不能讓正式敏感資料在未授權狀態存入可供 anon/authenticated 存取的資料表。


### 2026-11-22 — Supabase 搬遷工具第三輪：空表 COPY 被誤判為缺表
- Windows 第三輪一鍵搬遷工具 `bidding2a-ONE-CLICK-existing-supabase.zip` **實際執行失敗**：來源檔 SHA256 `8C30A97A66B067191A5AAD17B7C34774C2601A33F75AB3423471AEC4E8816F2F` 與 `pg_restore --list` 通過；來源備份列數 `project_master=210`、`analysis_case=15`、`analysis_run=34`（這份最新備份的 run 數較前輪驗證 32 高，不能用舊數覆蓋現值）；雲端原資料再次成功備份 `supabase_before_replace_20261122_025648.dump`（SHA256 `EB7E1680E9B4791FA00C8D6C5D8F0CCED5A6C6C40C1BC70BAA732C1FBADC3AC2`）；COPY 欄位對應通過；Python 驗證函式卻回報 `Incomplete COPY data or missing public tables`。未到輸入 `REPLACE-2A` 與 transaction，雲端沒有變更。
- 實際檢查附屬 `validate_2a_copy_values.py` 找到確定程式邏輯錯誤：它只在讀到某表非空資料列時增加 `rows`，最後用 `len(rows)!=17` 判定缺表，導致合法空表即誤報。修正為**每看到合法 COPY header 就初始化 rows[table]=0**，另用 `copy_tables` 集合嚴格檢查 17 張、缺表、重複表，不取消 varchar 長度/numeric 精度檢查。修正程式已通過自造17表含14空表、259非空資料列的測試，也通過刻意缺表拒絕測試；Python 語法檢查、ZIP 完整性及內附原始 dump SHA256 通過。
- 修正包 `bidding2a-ONE-CLICK-FIXED-empty-table.zip` 已在聊天中提供，內含來源 dump + 修正後的 Python 值驗證 + 原 PS 搬遷程式 + CMD。**修正版尚未由使用者在 Windows 執行，因此雲端覆蓋仍是 NOT RUN / 未驗證，不能宣稱雲端現在有210筆。** 目前使用連接 Supabase 唯讀查證仍為10 projects、1 case、24 runs、634 final bid items。
- 本修正只在對話附件，未修改應用程式 GitHub 版控的 API/Vue/Schema 或 Migration；更新本交接是為防止之後重複錯誤／誤以為成功。雲端17張 public 表 RLS 尚未啟用，須後續進行權限風險修正。


### 2026-11-22 — 已驗證 Supabase 合成歷史搬遷成功；回寫字典、單位與連線待驗
- 使用者提供 Supabase Table Editor 截圖看到 210 records（`project_master` SYNTHETIC 案）。本輪**透過已連接 Supabase 即時純 SELECT** 獨立確認：`project_master=210`、其中 `history_status='Approved'=210` 且名稱含 `SYNTHETIC` 或「合成」=210；`final_bid_item=101166`、`material_id IS NOT NULL=101166`、`mapping_status='Manual'=101166`、`historical_transaction=101166` 且涵蓋210建案；`material_master=100`、`item_dictionary=0`、`unit_master=11`、`unit_category=11`、`analysis_case=15`、`analysis_bid_version=15`、`analysis_run=34`、`audit_log=15`。比先前雲端10個案、1個case、24 runs 明顯已替換。**資料已在 Supabase，無需重建或重新回寫這210案；但本輪沒有驗證使用者 Windows 搬遷程序結尾成功碼，也尚未確認 ASP.NET API 已改連線。**
- 所有100種材料已有合成歷史交易，來源決標明細 `mapping_status=Manual` 是既有合成資料標記；因 `item_dictionary=0`，**絕不能解釋為真實已逐筆人工確認或已學會下次自動對應**。程式（feature/1d-api-integration）`崴正/Services/FinalBidImportService.cs` 只靠 `ItemDictionary.Keyword` 在工項名稱／規格 substring 命中並以長字優先，沒有字典時新標單進 `Pending/NO_DICTIONARY_MATCH`；`MaterialMappingService.UpdateMaterialMapping` 人工對應會建立／更新 `item_dictionary` 並記 audit；`StandardMaterialService` 可建立標準材料；`UnitConversionService` 使用資料庫主檔，只允許同單位，或同一 allow_conversion 類別有倍率的換算。
- 唯讀細查現有單位皆為 `SYN-*` 類別、`allow_conversion=false`，11個具名單位分別為 `只、式、座、組、處、樘、點、kg、m、m2、m3`。目前不支援新來源單位 `噸、公斤、㎡、m²、立方公尺` 等自動換算／同義用語（需依單位主檔實際新增及測試，避免改寫現有單位ID/造成101166筆FK影響）。目前 `detection_category=2`、`detection_category_material_map=2`，對應鋼筋／混凝土；`material_master` 已存在「結構鋼骨」但尚未列為正式 detection_category map，因此不表示鋼骨正式估算功能已開啟。
- 以 `lower(btrim(item_name))` 粗規則檢查來源 `final_bid_item`：共41373個不同名稱及41373個(名稱,材料ID)配對，同名多材料衝突0（僅此合成資料內，不保證外部資料無衝突）；不能把41373個包含樓層／設計條件的原工項長名稱全數直接灌字典，會造成高基數過擬合及廣義contains誤判。建議先在隔離測試環境審閱少數穩定關鍵字＋規格＋單位，改善有信心/人工確認工作流，驗證新標單 auto/pending 效果後才導入字典。
- 遠端 `feature/1d-api-integration` `崴正/Program.cs` 使用 `ConnectionStrings__DefaultConnection` 讀取 DB 連線，現階段暫用 `Pooling=false`；`compose.yaml` 透過 `DB_CONNECTION_STRING` 注入。前端 Vue 不應存資料庫密碼，後端 API 才能連 Supabase。**切換 Windows 5263 API 之前**需確認既有啟動程序仍是本機 55433，停止舊程序，從正確工作樹以 session env 注入 Session Pooler 5432 的雲端連線，啟動後用 `GET /api/projects`／個案／估算唯讀驗證。沒有本機 API 重啟證據就不可聲稱已切換。
- 系統設計仍為上傳最終標單→Draft→字典預對應/人工審查、單位及數據品質→PublishHistory Approved→寫入歷史與稽核；已 Approved 不得直接改對應，需 Reopen。**尚未進行實際瀏覽器的決標回寫/UI E2E，也未以雲端後端重跑驗證**。建議回寫 UI 驗收、標準材料/單位資料品質盤點、字典可重用性測試，隔離回歸後再正式提交/新增建案；防止真實資料混入合成 Approved training pool。雲端17張 public 2A表先前 RLS Disabled 的權限問題仍未修，使用者需在對外部署前處理。
- 本輪只做 Supabase `SELECT`、GitHub 源碼與手動交接文件更新；**沒有修改雲端任一筆資料、程式碼或 Migration**，沒有重新 seed 或重傳合成資料。


### 2026-11-22 — Windows 5264 API 實際連上 Supabase 的讀取驗證
- 使用者提供 Windows PowerShell 實測截圖：以 `ConnectionStrings__DefaultConnection` 連線現有 Supabase Session Pooler，`ASPNETCORE_URLS=http://127.0.0.1:5264`，`dotnet run --project .\\崴正\\崴正.csproj --no-launch-profile` 成功啟動，console 顯示 `Now listening on: http://127.0.0.1:5264` 且 EF Core Database Command SELECT project_master/final_bid_item/historical_transaction 成功；另一個 PowerShell 以 `(Invoke-RestMethod "http://127.0.0.1:5264/api/projects").items.Count` 實際收到 **210**。這是**後端讀取驗證 PASS**，與 Supabase 上已透過直接 SELECT 確認210個合成Approved專案相符。
- 前端 Vite 預設 `5174`、`frontend/vite.config.ts` 可用 `VITE_API_PROXY_TARGET=http://127.0.0.1:5264` 開發期代理新API；**尚未實際重啟前端、確認瀏覽器操作、執行正式決標回寫／材料字典／單位 E2E、核對估算結果或跑完6/6 quantity-scope checker**。新5264目前只是暫時工作階段連線，非持久部署；不表示舊5263已關閉或切換。
- 此驗證使用現有 Windows 版本，啟動可能仍顯示 `TenderImportInspectionService.cs` 的 CS8604 警告；此前 GitHub feature 已有警告修正，但不能因為遠端CI通過就宣稱桌面版已同步。
- 本輪僅記錄已驗證事實，無Schema/Migration/資料或API原始碼變更。繼續保留現有 Supabase 資料的合成來源標記、回寫與使用者正式資料隔離、密碼不得進前端與版控，及17張 public 業務表 RLS未啟用的安全待辦。


### 2026-11-22 — 前端 Vite 5184 → 後端 5264 → Supabase 210 筆讀取路徑 PASS
- 使用者在 Windows `C:\\Users\\User` 的 PowerShell 實際執行 `(Invoke-RestMethod "http://127.0.0.1:5184/api/projects").items.Count`，回傳 **210** 且無錯誤。此證據與前一輪 `5264/api/projects=210`、透過 Supabase 直接 SELECT `project_master=210` 互相核對，確定**開發期 Vite 代理讀取路徑** 5184 → 5264 → Supabase PASS；Vite 在 5174 衝突後以 5184 啟動。未修改任何雲端資料或 210 個合成 Approved 專案。
- 驗證界線：這僅證明代理 GET／API 連線；**尚不等於瀏覽器 UI 畫面全部通過，也未測試估算、異常、決標上傳、字典、單位、Draft/Publish/Reopen、稽核權限或完整 E2E**。下步先在 `http://127.0.0.1:5184` 唯讀驗收建案管理、待偵錯案件、估算頁面、決標回寫頁面與操作紀錄；避免在共有的 Supabase 對既有 Approved 合成訓練資料直接做破壞性寫入，寫入 E2E 須建立可回復、明確隔離測試案件並核對前後資料數量。
- 遠端 `feature/1d-api-integration` 的 `frontend/vite.config.ts` 支援 `VITE_API_PROXY_TARGET`，測試時設定為 `http://127.0.0.1:5264`；目前是 Windows 當前終端暫時設定，**並非持久部署或固定前端 API 連線**。不要將含密碼的 DB connection string 寫入前端、公開 GitHub 或示例。
- 本輪僅更新交接文件，沒有程式碼、資料庫、Migration 或回寫模型更動。Supabase 17 張公開 schema 表 RLS disabled 的安全待辦仍未處理。
