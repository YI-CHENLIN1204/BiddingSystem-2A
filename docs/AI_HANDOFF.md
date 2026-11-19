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
