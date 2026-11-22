# 廠房合成資料 v1：生成與交付（2026-11-22）

識別 `factory-dataset-v1-20261122`。遠端基線 `5e01a37463d3b27c70edc1879d6134e9e11eb7cb`；本機基線 `e0eef47917f7fb15b92c867ec1121ebb9589c166`。本批生成器、素材型錄、封裝器、測試及交接以此識別回查 Git commit。機讀結果與全部來源雜湊見同名 JSON。

## 已生成的獨立資料

| 廠房結構 | Train | Validation | Test | 合計 |
|---|---:|---:|---:|---:|
| RC | 42 | 9 | 9 | 60 |
| SC | 42 | 9 | 9 | 60 |
| SRC | 42 | 9 | 9 | 60 |
| 合計 | 126 | 27 | 27 | 180 |

生成器 `factory-1.0.0`，CSV schema `2.0.0`，種子 20261122，批次 `FACTORY_20261122_V1`。獨立 `FAC-20261122-*` 建案 ID；本批不將住宅改名或覆寫。批次與 ID 是固定的重現身分，改 seed 不代表可當作新批次再次匯入，匯入前須檢查批次／ID 重複。

- 最終基準：23,980 筆 BOQ 與一對一歷史 CSV；數量、單位、單價與金額完整、正量正價，金額按小數規則計算。
- 每案 39–59 種工項；型錄 104 種（原合成型錄100＋4新模板），其中基準實際使用67種，不能把型錄數當作每案工項數。
- 新模板：廠房金屬鋼板屋面、金屬外牆板、工業捲門、耐磨地坪。原型錄保留既有名稱與單位；DB ID 只作比對提示，不能當跨資料庫固定 FK。四種新模板尚未建立任何雲端主檔。
- 原住宅來源 300 案 CSV／原 Holdout 不變；原 manifest 三份 CSV SHA 全部核對相符。Supabase 本輪最後純 SELECT 仍為住宅210、廠房0，沒有資料寫入或自動 Approved。

## 情境與假設

六種情境為單層作業區、倉儲附辦公、多層生產、地下室生產、耐磨地坪作業區、裝卸場。結構、地上／地下層數、基地／建築投影／樓地板面積、跨度、載重類別、屋面／外牆／地坪與有無辦公區共同決定工項，再依明示假設算量。

例如本情境 RC 不配置主體鋼骨，SC/SRC 配置；地下專用工項只在有地下層數時出現；辦公隔間／空調等依有無辦公區出現；每案只取一種土方描述及一種模板描述，避免把替代描述重複計量。**這些是合成情境規則，不是任何真實工廠的工程適用性認證。**

`assumptions.json` 明列幾何關係、用量假設、擾動與價格生成。所有用量係數、載重效果與單價皆未用真實工程校準；價格年度因子也不是官方物價指數。資料是所選工種的標單格式示例，不是完整結構工程合約，不包含完整製程設備或結構安全設計。

不能用生成器假設本身的可預測性宣稱企業準確率；本輪沒有做 Holdout 演算法比較、訓練正式模型、重選權重或建立工程正常／異常標籤。

## 缺項真值分開保存

`project_material_applicability.csv` 共18,720列（180案×104型錄項），與模型輸入分開：

| 狀態 | 語意 |
|---|---|
| ApplicablePresent | 情境有此工項，基準標單有正量列 |
| NotApplicableAssumed | 此合成情境明確未配置；不認證真實工程不適用 |
| OutOfScope | 本批未模擬，不能理解為未施作或零量 |

未出現的工項 expected_quantity 留空，從不補0。對獨立 test 原案另做三結構×兩種變體，共6個配對診斷：

- MissingRequiredItem：刻意移除原本適用的混凝土列，屬資料缺漏測試。
- ExplicitRecordedZero：保留列但明確填零與零金額，屬零量輸入測試。

兩者均須資料複核，但工程異常真值保持 null；不等於不適用或施工錯誤。這6個變體來自3個既有 test 案，不是6個新獨立建案；未混入基準 BOQ、歷史 CSV 或 train 候選。

## 可用交付內容

`factory-synthetic-v1-20261122.zip`，2,110,357 bytes；SHA256：

`b6d4d69188d7713ab8d86624e3f4eaa015ec4d1b51910ddf558d4ba7f53f4e4e`

包內28個檔案包含：

- 基準 projects／BOQ／history、material_catalog、適用性表、診斷資料與真值、assumptions、manifest。
- `delivery/import_candidate/`：只含126 train案與16,786配對來源列，以及待對應材料／環境／範圍的 preflight；不是自動執行 SQL，不帶憑證，不會自動核准。
- `delivery/workbooks/train/`：RC/SC/SRC各1份完整 Excel，取自本批 train 的地下室情境。
- `delivery/workbooks/test/`：RC/SC/SRC各1份完整 test Excel。不得發布為 Approved 歷史，避免汙染保留測試。
- 每份 Excel 都有來源真值 JSON；CSV與封裝都有雜湊。生成資料包已在使用者對話交付，不把大型可重現 CSV 灌入 Git。

## 修改程式與重現指令

- `tools/graduation-synthetic/generate_factory_dataset.py`：獨立廠房情境、算量、適用性、配對診斷、分割及拒覆寫。
- `factory_material_catalog.json`：合成型錄快照及4個新模板。
- `prepare_factory_delivery.py`：檢查來源雜湊、分離train候選、產生6份完整工作簿、封裝並拒覆寫。
- `export_full_project_excel.py`：支援schema版本欄位及三個明確大項，新增 `--complete`；舊預設故意缺欄測試保留。
- `test_factory_dataset.py`：10項新回歸。
- `tests/WorkbookParserTests/FactoryWorkbookTests.cs`、`Program.cs`：可選的實際 Excel 解析器逐列核對；不更改正式 API 或估算服務。

```powershell
python tools/graduation-synthetic/generate_factory_dataset.py --output research/2a_synthetic/output/factory-new
python tools/graduation-synthetic/prepare_factory_delivery.py --source research/2a_synthetic/output/factory-new --output factory-synthetic.zip
python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v
dotnet run --project tests/WorkbookParserTests/WorkbookParserTests.csproj -c Release -- --factory-workbook <xlsx> <truth.json>
```

## 實測結果與未完成項目

- Python **21/21 PASS**（原11＋新廠房10），涵蓋分割、真值、缺項／零量、單位、價格、Lineage、型錄／實際數區分、可重現、拒覆寫、竄改拒絕與train-only交付。
- API／Parser測試專案 Release **0 warnings / 0 errors**。
- 原78個 Parser／大宗回歸通過；6份完整廠房 Excel 逐列數量／單價／金額／工程分類全部匹配：train RC160、SC164、SRC170列；test RC167、SC164、SRC163列。
- ZIP CRC及全部封裝檔SHA通過；住宅來源SHA未改；Supabase僅SELECT、沒有雲端新增廠房或材料。
- 初版辦公區套用過廣，已增加無辦公區情境；初版將型錄104與實際BOQ材料數混用，已區分catalog_material_count=104與distinct_material_codes=67。發布前重新生成、測試及封裝；中間檔不作交付版。
- **未做**：雲端匯入／新材料對應、DB／瀏覽器 E2E、參數比較、真實工程準確率。CI提交後另查，不預先宣稱。

下一步先確認隔離合成測試目標、材料名稱／單位／範圍，準備只新增126 train的匯入與回滾；不覆寫住宅，不匯入validation／test／診斷，不自動把未審核內容當正式歷史。工項／資料整合完成後做完整UI、集中回歸，最後才進行獨立準確度驗收。沒有Schema變更；本批程式回滾為revert提交，已交付檔案及雲端資料無須資料遷移回滾。
