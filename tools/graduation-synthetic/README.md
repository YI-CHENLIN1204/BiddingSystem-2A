# 畢業專題合成資料生成器（獨立、離線）

本工具只輸出 CSV 與 manifest，不會連線資料庫或匯入 1A 資料。使用 Python 3.10+ 標準函式庫，無外部套件。

## 執行

從儲存庫根目錄：

```powershell
python tools/graduation-synthetic/generate.py --count 300 --seed 20261119 --output .\graduation-output
python -m unittest discover -s tools/graduation-synthetic -p test_generate.py -v
```

輸出：`projects.csv`、`historical_transactions.csv`、`manifest.json`。`--output` 須為不存在或空的資料夾，不會覆寫既有內容。請不要把輸出資料或企業真實標單上傳到 GitHub。

## 模型與限制

- 合成建案類型暫為住宅，示意結構 RC／SC；產生面積、地上地下樓層、日期與縣市。
- 每個建案產生兩種示意材料交易，含數量、虛構價格、來源標記 `SYNTHETIC`、已知異常標籤。
- `train` / `validation` / `test` **按建案**切分，禁止同建案不同材料跨訓練與測試。
- 使用可重現亂數種子、SHA-256 結果校驗；不包含企業工程數據、真實名稱、真實行情。
- **示意材料係數與價格是人造數值，不是工程用量標準或真實價格**；只適用資料流程、資料結構與受控異常檢測，不能宣稱實務準確率。
- 目前輸出是**中介 CSV**，不是正式 DB SQL seed：`synthetic_project_id`、`material_code` 不等同正式外鍵。必須先建立獨立畢專 DB、主檔映射、可回滾匯入器才可進資料庫。
- 尚未與 `TenderExcelParser` 上傳介面執行 E2E；目前不生成最終標單 Excel。

## 下一個驗證點

1. 建立與正式庫隔離的畢專 PostgreSQL（連線憑證不提交）。
2. 畢專專用匯入器支援 dry-run、防重複與回滾。
3. 生成可上傳 Excel，驗證最終標單匯入和異常分析。
4. 合成資料可做受控實驗，但真實適用性須由合法公開工程資料獨立驗證。


## 產生給現有標單解析器使用的測試 Excel

先建立原始合成 CSV，再從 **test 切分**選取一個建案匯出：

```powershell
python tools/graduation-synthetic/generate.py --count 300 --seed 20261119 --output .\graduation-output
python tools/graduation-synthetic/export_demo_excel.py --source .\graduation-output --output .\graduation-demo.xlsx
python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v
```

指定建案可加上 `--project-id SYN-00003`（需存在於輸入資料）。命令會輸出對應建案的類型、結構、縣市、面積、地上與地下樓層、日期，供手動輸入系統；產物只包含測試工項。

- 工作表名稱為「材料明細」，表頭為「項次、項目名稱、規格、單位、數量、單價、金額」，對應 `崴正/Services/TenderExcelParser.cs` 的欄位辨識。
- 會檢查 SHA-256、來源標籤與建案關聯，不允許覆寫現存 xlsx。
- **此 Excel 是功能測試資料，不是企業最終標單或合法的實際工程用量基準。**
- `export_demo_excel.py` 只輸出檔案，不呼叫 API、不寫入資料庫、不建立材料主檔；是否能在 UI 成功匯入，仍需在隔離畢專資料庫上實際驗證。
- 不要把 **合成異常樣本** 未經處理便當成正常歷史基準；正式回測必須先分離訓練與驗證資料，並排除有植入異常的歷史基準樣本。


## v0.3 不規則多工作表標單（建議先用這個驗證解析器）

在 **Windows PowerShell、GitHub 專案根目錄**執行：

```powershell
git pull --ff-only
python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v
python tools/graduation-synthetic/generate.py --count 300 --seed 20261119 --output .\graduation-data
python tools/graduation-synthetic/generate_irregular_excel.py --source .\graduation-data --output .\graduation-irregular.xlsx --truth .\graduation-irregular-truth.json
```

注意：`--output` 指定的檔案不可已存在。重新執行請使用不同輸出檔名，不要刪除不確定來源的檔案。

本版模擬的格式變異：

- 四張工作表：`工程總表`（摘要）、`結構體明細`、`基礎及地下工程`、`樓層材料明細`。摘要表預期由現有解析器略過。
- 每張明細表有不同表頭文字、起始列、欄位排列、附註、分類列和結尾合計列。
- 同材料分布在多張明細表，為同一母資料交易的**不同分攤數量**，並非重複計數；但缺數量列應被標記而不可自行補完。
- 材料名稱使用示意變體，例如 `SD420W D19 竹節鋼筋加工及組立`、`預拌混凝土 280kgf/cm²（含泵送）`、`H型鋼梁柱構件製作及吊裝`，並非只有「鋼筋／水泥」。
- 各材料生成 `complete`、`missing_unit_price`、`missing_quantity` 測例；`graduation-irregular-truth.json` 含工作表、列號、正確材料類別、案例類型與應見值，可用來人工核對解析結果。

**重要**：目前 v0.3 只建立解析器壓力測試資料。命名變體與資料缺漏是示意測試規則；先前原始企業 Excel 在此輪無法取得可讀位元組，因此**尚未逐份驗證為真實企業檔案的格式分布**，不能宣稱完全相同。全部不完整測例不得作為已核實歷史成交資料；未驗證之材料用量係數不代表工程標準。絕不可把 v0.3 測試標單匯入產學正式 DB。

從應用程式做手動測試：在**已確認隔離的畢專資料庫**環境開啟標單匯入頁面，上傳 `graduation-irregular.xlsx`，比對 `graduation-irregular-truth.json`，確認各工作表是否有被解析、命名變體是否誤配、缺單價／缺數量是否被擋下或列為待審。**不可只因 HTTP 200 就判為 PASS。** 後端 `TenderExcelParser` 的數字位置推斷需額外留意缺數量列被誤讀。


### v0.3.1 補充：42 筆混合工項驗證

目前每份 v0.3.1 測試標單有 4 張工作表，3 張明細表合計 **42 筆測例**：
- 6 筆核心材料分拆數量（每種材料完整列／缺單價列／缺數量列各一）。
- 36 筆額外虛構工程工項（假設、土方、模板、防水、門窗、水電、裝修、弱電等），包含未對應工項與缺單位／缺單價案例。
- 物料名稱使用長名稱、工法、規格、施工位置及不同描述方式，請依 `graduation-irregular-truth.json` 核對工項分類與缺漏，不要只看名稱相似度。
- 36 筆額外工項**只存在標單測試檔**，不會被自動寫進合成歷史成交 CSV；測試初始狀態可合理預期列入未對應清單。是否正確處理仍需隔離環境 E2E。
- 3 張明細表採三種表頭排列，包含不同起始列、分類列、空行、備註及合計列。此格式是**參考解析器現行能力設計的合成不規則案例**，尚不等於真實企業範本的逐張分布。
- 所有數量與價格都是示意數值，存在故意缺漏；不可以把它當成已確認的決標標單或真實工程行情。


## v0.4 大型工程標單（取代上方 v0.3.1 的 42 筆預設值）

使用者指出 42 筆工項遠少於實務詳細標單，因此此版將 **一份標單的明細筆數** 與「歷史合成建案總數」拆開控制。**`--count 300` 是歷史建案數；`--items 360` 是這一份 Excel 裡的工程項目列數**，兩者不是同一回事。

在 Windows PowerShell，先從 GitHub main 取得新版 `tools/graduation-synthetic`（如果目前在 `feature/1d-api-integration`，不用切換分支）：

```powershell
git fetch origin main
git restore --source=origin/main --worktree -- tools/graduation-synthetic
python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v
```

若前一階段 `graduation-data` 已存在，**沿用即可，不必重新產生**。以下生成新的 Excel 名稱，以避免覆寫先前的版本：

```powershell
python tools/graduation-synthetic/generate_irregular_excel.py --source .\graduation-data --output .\graduation-irregular-v04.xlsx --truth .\graduation-irregular-v04-truth.json --items 360
```

可將 `--items` 設為 60–2000；建議先用 360，觀察解析器結果後再加大至 600。

v0.4 輸出 **7 張工作表**（1 張摘要、6 張不同工程類別明細），預設 **360 筆詳細工項**。不同明細表有表頭偏移、表頭用詞與欄位順序差異；工項名稱加入工程部位、棟別、樓層與規格。部分工項故意缺單價、缺數量、缺單位，部分為預期未對應材料；另保留分類、空白、備註、合計列。JSON 會記錄工作表、列號、預期數量／單價、缺漏情境，以及各工作表工項計數。不可把缺漏資料當正常歷史交易。

**限制與界線：** 為測試解析器承載能力所建立的**合成壓力資料**，不是透過企業真正標單逐案抽樣學到的格式分布；名稱、數量、單價仍未經專家校準。因此 360 筆不等於 360 種不同材料，也不等於企業工程估算的可信度。未完成畢專隔離 DB 前，不能將測試 Excel 上傳正式資料庫。


## v0.5：2026-11-25 交付前資料真實性、可讀性及比較測試

原始 v0.3 / v0.4 說明仍保留作版本紀錄，**此節優先於前述版本的輸出格式敘述**。

- 標單明細欄位已加寬，項目名稱/規格/備註的儲存格增加換行樣式，列高提高；請以新檔名重新產生 Excel，舊版檔案不會被自動修改。
- 生成器讀取 `basement_floor_count` 與 `floor_count`，無地下室時過濾部分地下室、連續壁、擋土等工項，並避免生成超過實際地上樓層的樓層代碼。**這是初步建案條件限制，不代表完整的建築設計、規範或工料係數模型**。
- 保留其他工程類別明細，包括假設、土方、基礎、結構、泥作、防水、門窗、電氣、消防、空調、弱電、裝修、電梯與景觀等；但不能稱作已涵蓋所有建築工項。
- 新增 `evaluate_synthetic_holdout.py`：以 `train` 歷史建案預測 `test` 合成建案，隔離植入異常的測試交易，比較加權分位數與加權常態基準；依材料分別報告有物理單位的 MAE、區間寬度與評分，避免混算 kg/m² 和 m³/m²。
- 這個評估是 **OFFLINE_PROXY_NOT_API / SYNTHETIC**，相似度分數是簡化代理，不等於 C# 後端真正端到端精度。不可將輸出的 MAPE 宣稱為真實建案或企業精準度。

```powershell
git fetch origin main
git restore --source=origin/main --worktree -- tools/graduation-synthetic
python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v
python tools/graduation-synthetic/generate_irregular_excel.py --source .\graduation-data --output .\graduation-irregular-v05.xlsx --truth .\graduation-irregular-v05-truth.json --items 360
python tools/graduation-synthetic/evaluate_synthetic_holdout.py --source .\graduation-data --output .\graduation-synthetic-holdout-v01.json --top-k 8 --min-sample 5
```

所有輸出檔都拒絕覆寫，若名稱已存在請更換檔名。2026-11-25 教授交付的資料來源區分、真實精度驗收規則，請閱讀 `docs/GRADUATION_DELIVERY_2026-11-25.md`。


## v2.0 重要變更：建案專屬、多類別且歷史明細與標單 1:1 對齊

**此方案取代 v0.1 的「每個建案只有兩個材料交易」作為 11/25 畢專測試的主要資料集。**
原始 `generate.py`、`generate_irregular_excel.py` 保留作舊版 2 材料功能測試，不可把 v0.1 的合成精度套用到 v2.0。

v2 依建案結構 RC/SC、總樓地板面積、地上地下樓層、基地限制（合成情境）、外牆工法與裝修等級，決定採用哪些工項模板、樓層、工程位置與數量。模板除了結構工程，也涵蓋假設、土方、基礎、泥作、防水、屋頂、門窗、金屬、外牆、室裝、給排水、電氣、消防、空調、弱電、電梯、景觀、雜項等。資料筆數因建案特徵而改變，不強制固定 360 筆或相同 7 張表。

輸出：
- `projects.csv`：建案基本資料＋ v2 合成建築特徵。
- `historical_transactions.csv`：**每筆工程明細有對應同一案歷史交易**；交易欄位仍保持既有 CSV 相容。
- `project_boq_rows.csv`：每個建案的詳細工程列，記錄來源交易 ID、材料代碼、工程類別、單位、價格、數量與位置。
- `manifest.json`：來源標記、3 份檔案 SHA-256 與總數。

輸出 Excel 直接取同案 `project_boq_rows.csv`，不再額外憑空附加 360 筆來源不明的假明細。為測試缺漏，部分 Excel 列會故意遮蔽單價／數量／單位，**但歷史來源保留完整合成數值**，並在 truth JSON 逐列記錄；缺漏測例不能直接寫入正常歷史庫。

### Windows PowerShell 操作

**不刪除先前 `graduation-data`，新建 v2 資料夾。** 如果已在 feature 分支，僅同步工具路徑即可。

```powershell
git fetch origin main
git restore --source=origin/main --worktree -- tools/graduation-synthetic
python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v
python tools/graduation-synthetic/generate_full_project_dataset.py --count 300 --seed 20261119 --output .\graduation-data-v2
python tools/graduation-synthetic/export_full_project_excel.py --source .\graduation-data-v2 --output .\graduation-v2-project.xlsx --truth .\graduation-v2-project-truth.json
python tools/graduation-synthetic/evaluate_full_project_holdout.py --source .\graduation-data-v2 --output .\graduation-v2-holdout.json --top-k 8 --min-sample 5
```

選擇特定建案可於匯出時附上 `--project-id SYN-00002`，未指定時自動從 `test` 案件中選一個。三種輸出若已存在，拒絕覆寫。

### 資料完整性與研究限制

- 同案 `historical_transactions.csv` 與 `project_boq_rows.csv` 以 `synthetic_transaction_id` 1:1 對應、金額／數量一致；保留 `train / validation / test` **依建案**切分。
- 重複樓層的相同材料應先**同建案同材料碼彙總**，再形成估算樣本；不可把一案 50 列算成 50 個獨立建案。
- `evaluate_full_project_holdout.py` 比較加權經驗分位數與加權常態代理基準，逐材料輸出 MAPE／涵蓋率／區間評分。**不是正式 C# Service 的 E2E 結果，也不是企業實際精準度**。
- 工項名稱與類別是合成示例／工程常見名詞，**不是經過公開標單逐列抽取與統計校準**；單位面積係數、數量與單價也不是工程設計標準。仍須以合法取得的真實住宅標單核對工項組成、數量分布、工法與設計條件。
- v2 暫以住宅 RC/SC 為範圍，不把商辦、廠房混入未校準的住宅模板；住宅內部建案條件與工項內容已有差異，但**尚不涵蓋所有建案類型**。
- 雖然輸出含廣泛工程類別，但正式 C# 數量異常模型只對受控的可估算偵錯細項判定。**標單完整明細≠所有類別都已具有效估算模型**。
- `SYN-WORK-*`、`SYN-REBAR` 等是合成材料代碼，不是系統正式 MaterialMaster 外鍵；禁止在產學合作正式 DB 匯入，須先做隔離畢專 DB 和主檔映射。


## v2.1：獨立畢專資料庫與圖形證據（不可連產學正式 DB）

**三個重要輸出：** A) `graduation-data-v2` 全部建案與同源歷史資料，B) test-only `graduation-v2-project.xlsx` 待估算標單，C) 可列印的 `graduation-v2-evidence.html` 圖形報告。模型數字都是合成資料離線代理回測。

### 階段 A：先驗證檔案，不連 DB

```powershell
git fetch origin main
git restore --source=origin/main --worktree -- tools/graduation-synthetic
python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v
python tools/graduation-synthetic/generate_full_project_dataset.py --count 300 --seed 20261119 --output .\graduation-data-v2
python tools/graduation-synthetic/export_full_project_excel.py --source .\graduation-data-v2 --output .\graduation-v2-project.xlsx --truth .\graduation-v2-project-truth.json
python tools/graduation-synthetic/evaluate_full_project_holdout.py --source .\graduation-data-v2 --output .\graduation-v2-holdout.json --top-k 8 --min-sample 5
python tools/graduation-synthetic/build_evidence_report.py --source .\graduation-data-v2 --benchmark .\graduation-v2-holdout.json --output .\graduation-v2-evidence.html
python tools/graduation-synthetic/prepare_training_seed.py --source .\graduation-data-v2 --output .\graduation-v2-train-seed.json
Invoke-Item .\graduation-v2-evidence.html
```

所有檔案均不覆寫。若已建立，**保留既有結果並更換新的檔名**；若 v2 資料先前由舊版本產生，應用全新 `graduation-data-v2b`，並修改後續 `--source`。

### 階段 B：本機隔離 PostgreSQL（需 Docker 與 .NET 10）

**本工具尚未通過 Windows 實測與 C# 編譯；先完成階段 A 後再進行。** 以 PowerShell 在專案根目錄操作：

```powershell
$env:GRADUATION_DB_PASSWORD = "<請設定僅供本機使用的強密碼>"
docker compose -f tools/graduation-synthetic/compose.graduation-db.yaml up -d
docker compose -f tools/graduation-synthetic/compose.graduation-db.yaml ps

$env:GRADUATION_DB_CONNECTION = "Host=127.0.0.1;Port=55432;Database=bidding2a_graduation;Username=graduation;Password=$env:GRADUATION_DB_PASSWORD"
dotnet run --project .\tools\graduation-synthetic\GraduationSeed\GraduationSeed.csproj -- --seed-json .\graduation-v2-train-seed.json
```

`GraduationSeed` 僅允許本機 `127.0.0.1:55432`（或 localhost）、資料庫 `bidding2a_graduation`、帳號 `graduation`；會用 EF `EnsureCreated` 初始化**全新空白**畢專 DB，任何已有資料的 DB 都拒絕種入。匯入 train-only 的 `ProjectMaster`、`MaterialMaster`、`UnitMaster`、`FinalBidItem`、`HistoricalTransaction`、偵錯細項對應與測試估算參數。其 `HistoryStatus=Approved` 僅用於**隔離合成測試環境**，不適用實務審核；`EnsureCreated` 亦不能取代正式 Migration。

**安全與回滾：** 禁止把 `GRADUATION_DB_CONNECTION` 指向 Supabase；DB 以獨立容器/Volume 運作，C# 端再次檢查資料庫名稱、host、port、username；不會修改既有 `compose.yaml`、`ConnectionStrings__DefaultConnection` 或企業 DB。若要重建，只在使用者明確確認後處理畢專專用 volume，不會自動執行 `down -v`。

### 階段 C：正式系統 API（尚未驗收）

以 **`ConnectionStrings__DefaultConnection=$env:GRADUATION_DB_CONNECTION`** 啟動本機 ASP.NET 後端，並以 `AnalysisCaseController` 的 `POST /api/analysis-case/from-upload` 上傳 test-only 的 `graduation-v2-project.xlsx`。必要參數包含建案類型、結構、縣市、總樓地板面積、基地面積、地上地下樓層、日期及手動選擇的工作表。比對回傳的估算和 `graduation-v2-project-truth.json`。

目前正式數量偵錯政策僅支援「結構體工程／鋼筋、混凝土」；其他工程工項可進歷史與完整明細供解析與未來擴充，但**不可宣稱已能正式預測每個工程類別的合理用量**。

**驗收順序：** unittest → v2 資料生成 → train-only seed → PostgreSQL 匯入 → test Excel 上傳 → 正式 API P15/P50/P85 → 圖表/回測。每個檢查點必須留下實際執行結果；沒有結果就保持「未驗證」。

> 精準度和可信度：任何帶有 `SYNTHETIC` 標籤的 MAPE 與涵蓋率都只能證明模型在**受控合成條件**下的表現，不能作為真實工程預測能力的外部驗證。


## 2026-11-21：SRC + 極端條件嚴格模式（新隔離資料集）

本階段新增 **實驗性 SRC 核心材料（鋼骨／鋼筋／混凝土）**與六組刻意設計的極端條件（超小低樓層、超大型高樓、深地下室、狹窄基地、大基地低樓層、偏遠且舊年份）。每種情境分別配置於 **train / validation / test**，以建案為切分單位，且寫入 `extreme_scenario` 欄位以便分層回測。這些不是實際施工設計或建材用量規範。

舊 `graduation-data-v2` 與 `127.0.0.1:55432/bidding2a_graduation` **保留不動**。新版採不同資料夾、不同 DB、不同 Port、獨立 Docker Compose Project + Volume。請從專案根目錄執行：

```powershell
git fetch origin main
git restore --source=FETCH_HEAD --worktree -- tools/graduation-synthetic
python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v
python tools/graduation-synthetic/generate_full_project_dataset.py --count 300 --seed 20261119 --include-src --include-extremes --output .\graduation-src-extreme-v3
python tools/graduation-synthetic/prepare_training_seed.py --source .\graduation-src-extreme-v3 --target-database bidding2a_graduation_src --output .\graduation-src-train-seed.json
python tools/graduation-synthetic/export_src_holdout_suite.py --source .\graduation-src-extreme-v3 --output-dir .\graduation-src-holdouts
python tools/graduation-synthetic/evaluate_full_project_holdout.py --source .\graduation-src-extreme-v3 --output .\graduation-src-offline-proxy.json
python tools/graduation-synthetic/audit_candidate_scope.py --source .\graduation-src-extreme-v3 --output .\graduation-src-candidate-audit.json
```

所有產生器均不覆寫既有輸出。使用者應先確認測試結果無錯誤，並查看 `manifest.json` 的 `structure_type_counts`、`extreme_cases_per_split`（預期 train/validation/test 各 6）、train seed 筆數與 `graduation-src-holdouts/suite_manifest.json`（1 個普通 SRC test + 6 個極端 test，每個 Excel 附 truth）。

建置並啟動 SRC **新的隔離** PostgreSQL（絕不連正式 Supabase，勿回傳密碼）：

```powershell
dotnet build .\tools\graduation-synthetic\GraduationSeed\GraduationSeed.csproj -c Release
$secure = Read-Host "新的 SRC 測試資料庫密碼" -AsSecureString
$env:GRADUATION_SRC_DB_PASSWORD = [System.Net.NetworkCredential]::new("", $secure).Password
docker compose -f tools/graduation-synthetic/compose.graduation-src-db.yaml up -d
docker compose -f tools/graduation-synthetic/compose.graduation-src-db.yaml ps
$env:GRADUATION_DB_CONNECTION = "Host=127.0.0.1;Port=55433;Database=bidding2a_graduation_src;Username=graduation;Password=$env:GRADUATION_SRC_DB_PASSWORD"
dotnet run --no-build -c Release --project .\tools\graduation-synthetic\GraduationSeed\GraduationSeed.csproj -- --seed-json .\graduation-src-train-seed.json
```

匯入器會檢查目標 JSON 的 `target_database` 與連線的名稱、埠一致，並拒絕已有資料的 DB。只可在**全新**且本機隔離的 SRC DB 執行一次；不要對既有 55432 DB 重跑 Seed，也不要使用 `docker compose down -v` 破壞資料。

用目前含 SRC／基地面積實作的 **`feature/1d-api-integration`** 版本 ASP.NET 連接新資料庫（需關掉舊埠的後端或另選未使用埠），再做**唯讀**城市與基地面積敏感度檢查：

```powershell
$env:ConnectionStrings__DefaultConnection = $env:GRADUATION_DB_CONNECTION
dotnet run -c Release --project .\崴正\崴正.csproj --urls http://127.0.0.1:5261
# 在另一個 PowerShell 視窗：
python tools/graduation-synthetic/verify_similarity_sensitivity.py --source .\graduation-src-extreme-v3 --api http://127.0.0.1:5261 --output .\graduation-src-city-site-audit.json
```

`verify_similarity_sensitivity.py` 僅呼叫 `POST /api/major-category-price/range`，不建立 analysis case、不寫歷史資料；控制同一 SRC 測試建案的城市、基地面積變化，比較參考建案 ID / 相似度分數。它若回報 `INVESTIGATE`，先核實後端使用的 Git 分支、城市 / 基地值與 SRC 歷史資料，不可把未通過當作已驗證。

### 嚴格工項上線門檻

`audit_candidate_scope.py` 只產生 **OFFLINE_PROXY_NOT_REAL_CSHARP_API** 研究資訊，列出鋼骨、模板及土方等候選工項及各 RC／SC／SRC 資料量、離線預測誤差／涵蓋率。**不會修改 `TwoAScopePolicy`、`DetectionCategories`、`DetectionCategoryMaterialMaps`，亦不會啟用新紅綠燈。**

正式納入任何新工項需獨立驗證：可比的計量單位／穩定材料定義、足夠的每結構樣本、獨立 test／極端建案回測、正式 ASP.NET API 的 P15/P50/P85 覆蓋與誤報/漏報評估，以及業界設計依賴性審查。所有條件通過並取得明確決策後才可以另行修改 scope 與 DB mapping。**SRC 只有新增資料實驗，不能將鋼骨誤認成既有核准的鋼筋類別。**

本工具產出的數字全屬 SYNTHETIC，不能用來宣稱真實營造工程準確率。注意：GitHub `main` 的相似度與 `feature/1d-api-integration` 尚有不同步之處，實際正式環境版本應先按交接文件核對；不要只因 CLI PASS 就認定不同分支功能相同。


### 設計／量價異常的獨立標單壓力測試（2026-11-21）

除了樓層、基地、城市等**建案條件極端值**，現在還能為現有 **test-only** RC／SC／SRC 建案製作**標單量價異常**，不修改 train、validation、歷史交易 CSV、既有 Seed 或 DB。建議在上述 `graduation-src-extreme-v3` 已產出後執行：

```powershell
python tools/graduation-synthetic/export_anomaly_holdout_suite.py --source .\graduation-src-extreme-v3 --output-dir .\graduation-anomaly-holdouts
```

結果是 **10 份 Excel + 10 份 truth JSON + anomaly_suite_manifest.json**，包含：
- `design_concrete_high/low`：混凝土數量 2.5 倍／0.35 倍。
- `design_rebar_high/low`：鋼筋數量 2.4 倍／0.4 倍。
- `design_steel_high/low`：鋼骨數量 2.5 倍／0.35 倍（**鋼骨目前不屬於正式量異常紅綠燈範圍**）。
- `structural_price_high/low`：結構體工程各列單價 3.0 倍／0.2 倍，數量不變。
- `combined_concrete_high_price_high/low_price_low`：混凝土數量與整個結構體工程單價同時增／減，驗證系統對總價變動的解釋與獨立量價偵錯能力。

以上比例為**受控測試倍率**，不是一般工程合理幅度或真實建築師設計決策；名稱中的「設計」代表要模擬設計複雜度可能造成的量差，**並非已核實的設計因果資料**。任何一個異常情境都保留 `injection.changed_lines` 的原始／注入後數量、單價、倍率，並在各行的 `baseline_quantity`、`baseline_unit_price`、`injection_label` 顯示變更。採用每行 `金額=注入數量×注入單價` 的一致性規則。

**請勿混淆異常標籤與模型判定**：已知注入價格或數量倍率，只證明人為介入的偏差，不能保證正式 API 必須輸出紅燈；尤其混凝土增量也會使**總金額**上升，這不是單價過高。正式檢測需拿 `quantity-analysis` 的材質用量與 `major-price-analysis` 的整個結構體價格分別比對，再做**基準未修改標單**與每種變體的成對比較。單一基準建案可被多種情境重複使用，統計評估時不得當成多個彼此獨立的真實建案。

預設不使用 `--anomaly-scenario` 時，原 `export_full_project_excel.py` 的行為不改變。可用單次 `--project-id` 加 `--anomaly-scenario` 產生一份新檔，但**僅限 test split**，其餘 split 直接拒絕。所有來源 CSV SHA-256 保持不變、輸出檔拒絕覆寫，`anomaly_suite_manifest.json` 明確標記 `HELDOUT_ONLY_NOT_TRAINING_HISTORY`。

此版本尚未在使用者 Windows / 隔離 SRC DB 中完成正式 ASP.NET API E2E。請先跑 Python 測試、確認真值 JSON 和 Excel 金額一致，再用新的 55433 隔離資料庫測量量價檢出與誤報，**絕不把標籤反寫訓練歷史或放行新的紅綠燈類別**。
