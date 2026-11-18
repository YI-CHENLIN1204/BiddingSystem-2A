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
