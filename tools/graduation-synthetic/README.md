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
