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
