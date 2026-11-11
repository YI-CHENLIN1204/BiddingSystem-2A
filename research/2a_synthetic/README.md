# 2A Synthetic Research Sandbox / 模擬資料研究區

> **專題研究允許模擬資料；產學合作正式基準只能採用經核准的真實歷史資料。**
> 本區所有數字都是**假設的生成參數**，不代表台灣工程用量標準、真實價格、正式決標標單或崴正營造內部施工規範。
> This directory has no DB connection and no production database write path.

## 研究目的
廠商約 20 件原始歷史工程，按「住宅／廠房 × RC／SC／SRC」區分，部分組合只有 1～3 件。這裡不是要把真實樣本虛增為幾百件，而是以**已知生成機制**比較估算方法在小樣本與分布偏移下的行為。

- 6 個建案用途／結構組合，每個合成建案產生「鋼筋 kg」和「混凝土 m³」兩筆**示意用量**。
- `stable`：小變異；`high_variance`：大變異；`company_shift`：虛構企業群相對外部群有偏移；`outliers`：偶發極端值。
- 建案的樓地板面積、樓層、地下室、基地面積、年份、縣市只用來演示權重與分層，並非經結構設計或數量計算軟體求得。
- `compare.py` 在每個「用途 × 結構 × 材料」群內留下目標案，使用其餘案例中的固定小批參考，報告 MAPE、描述區間涵蓋率與 ESS。
- 此比較結果是**模擬研究指標，不是正式工程的預測準確率或信心度**。

## 執行（Windows PowerShell；Python 3.10+，不需額外安裝套件）
從 GitHub 專案根目錄：

```powershell
python research/2a_synthetic/generate.py --seed 20261111 --cases-per-combo 30 --scenario company_shift
python research/2a_synthetic/compare.py research/2a_synthetic/output/synthetic_company_shift_seed20261111.csv --train-size 3
python -m unittest discover -s research/2a_synthetic -p "test_*.py" -v
```

其他情境：
```powershell
python research/2a_synthetic/generate.py --seed 20261111 --cases-per-combo 30 --scenario stable
python research/2a_synthetic/generate.py --seed 20261111 --cases-per-combo 30 --scenario high_variance
python research/2a_synthetic/generate.py --seed 20261111 --cases-per-combo 30 --scenario outliers
```

輸出：
- `output/synthetic_<scenario>_seed<seed>.csv`：逐案／逐材料研究數據
- `output/synthetic_<scenario>_seed<seed>.manifest.json`：亂數種子、情境、SHA256、合成標記與禁止匯入的表名
- `output/synthetic_<scenario>_seed<seed>.comparison.json`：分組比較結果

`output/` 建議排除版本控制，因為生成資料可以用固定種子重現。

## 與正式系統隔離
- **不可**將模擬 `CSV` 匯入 `project_master`、`historical_transaction`、`final_bid_item`。
- `is_synthetic=true`、`dataset_type=synthetic_research`、`reference_scope=SIMULATED_MATERIAL_TOTAL_NOT_FINAL_BID` 是強制的來源提示。
- CSV 中的金額只是 `合成材料數量 × 假設單價`，**並非結構體工程大項完整決標金額**。不可用來證明正式價格模型準確率。
- `company_group=崴正風格_虛構` 是**情境標籤**，完全不反映崴正的實際特殊規範，也不是已擬合公司差異。
- 正式系統的權重、異常燈號、最低樣本門檻在本次研究中都不變。

## 方法限制（必要解釋）
1. 產生器的 baseline 和噪音大小完全由研究者設定；若測試顯示某算法較好，只能表示它**在此假設世界**中較好。
2. `compare.py` 的 train-size 是每次預測可使用的少量觀測數，**不是**實際崴正已擁有的有效案件數。
3. 對每一筆目標案皆排除該案後才挑選訓練資料；不同材料來自同一建案，不能被誤算為兩件獨立建案。
4. 評估是同群的固定小樣本比較；目前**沒有**驗證時間前推、工程範圍完整性、跨結構換算或公司真實偏差。
5. 常態模型示意採用平均數 ± 一個樣本標準差，**不是統計校準後的預測區間**；不能以其涵蓋率作為 68% 的預測保證。
6. 未完成本機執行與回歸測試之前，不得宣稱結果已驗證。

## 下一輪
1. 在固定生成條件下比較 train-size = 3、5、8，觀察模型穩定性和權重 ESS。
2. 測試異結構混入、來源群體偏移及極端值時，現行加權相似度是否造成更差結果。
3. 以「真實／外部／模擬」三組資料隔離開發；真實工程留案回測是產學判定採用的必要條件。
4. 每一組量價模型有獨立驗收標準，不能因為模擬數據量大就替正式系統解除最低真實樣本數限制。

## 批次敏感度實驗（第二輪）

第一版單次比較會固定挑選前面幾件候選案，可能讓特定順序／虛構公司比例影響結果。現在已改成**基於目標案例與候選案例 ID 的確定性 SHA256 排序抽樣**，同一個待估案對所有候選方法使用相同訓練案例，避免順序偏誤。仍然不是時間前推驗證，僅是可交換樣本條件下的模擬測試。

從專案根目錄執行：

```powershell
python research/2a_synthetic/batch.py --first-seed 20261111 --seeds 10 --cases-per-combo 30 --train-sizes 3 5 8
python -m unittest discover -s research/2a_synthetic -p "test_*.py" -v
```

這會在 `research/2a_synthetic/output/` 建立：
- `batch_detail.csv`：逐組合／情境／種子／樣本數／模型的指標。
- `batch_summary.csv`：依情境、樣本數、材料及方法彙整跨種子的平均 MAPE、覆蓋率、ESS 與 MAPE 跨組變異。
- `batch_manifest.json`：清楚註記全部是模擬研究、方法與亂數種子。

一輪預設涵蓋 4 情境 × 10 種子 × 6 用途結構組合 × 2 材料 × 3 小樣本數 × 4 方法 = **5760 組方法測試**；並非產生 5760 件真實建案，也不代表完成 5760 次獨立真實驗證。

**解讀準則：**
- 模擬 MAPE 只是「在研究者假設的資料生成機制下」之預測誤差。
- `normal` 採平均值 ±1 個樣本標準差，`median` 用插值 P15/P85，`weighted` 用階梯加權 P15/P85：各方法的區間**定義不同**，覆蓋率僅供敏感度觀察，**不能直接據此比較哪種區間較準確**。下一階段要做同一目標覆蓋率的公平區間評比。
- `weighted` 是現行加權方法的**簡化版**（面積、樓層、城市、時間），尚未重現正式 2A 所有相似度欄位，不能宣稱已驗證正式權重最優。
- 種子彼此可重現，但不代表與真實世界獨立抽樣條件一致；真正產學模型仍需真實歷史建案、工程範圍與適用條件驗證。

## 第三輪：對照正式 2A 六因素相似度（尚不修改正式系統）

`production_weight_review.py` 是獨立的唯讀研究腳本，參照 `ProjectSimilarityService` 及 `DetectionCategoryRangeService` 的設計，包含結構 (25)、總樓地板面積 (20)、基地面積 (10)、樓層 (10)、年份 (0)、縣市 (5) 的六因素分數，面積／基地面積差異 50% 截斷、地上／地下樓層每層 3%／10% 扣分、時間三年半衰期、相似度平方、按相似度優先選 Top K，最後計算加權 P15/P50/P85。

三組固定待比較設定：
- `current_no_structure_gate`：現行權重，**允許不同結構候選**（與生產程式相同的風險）。
- `current_with_structure_gate`：維持原權重，但只保留相同結構。
- `no_site_area_with_gate`：相同結構條件，將基地面積權重 10 改為 0，面積 20 改為 25、樓層 10 改為 15，測試是否因規模重複加權使誤差增加。

```powershell
python research/2a_synthetic/production_weight_review.py --first-seed 20261111 --seeds 5 --cases-per-combo 30 --train-sizes 3 5 8
python -m unittest discover -s research/2a_synthetic -p "test_*.py" -v
```

輸出至 `research/2a_synthetic/output/`：
- `production_weight_detail.csv`：各組各情境的模擬誤差、區間涵蓋、ESS、混入不同結構的參考比例；
- `production_weight_summary.csv`：按情境、樣本數、材料、設定比較；
- `production_weight_manifest.json`：固定參數與研究限制。

注意：
- 雖參照正式程式的六因素公式，**Python 腳本仍是研究版重現，不是 ASP.NET API 的端到端驗證**；可能存在計量精度、欄位缺失、交易資料整合上的差異。
- `cases-per-combo=30` 是模擬宇宙的每組建案數，`train-sizes 3 5 8` 模擬每案可使用的參考數。全部生成資料不等於企業真實資料。
- 生成器的不同結構具有研究者預設的不同用量，因此「結構硬門檻是否更好」的模擬結果**可能受到設計假設主導**；這是敏感度實驗，不是建築工程的事實結論。
- 模擬年份目前可包含待估案年份後的歷史參考，以重現現行相似度計算的對稱年代差異。**不可把這份實驗冒稱真正的時間前推回測。**
- 這一輪不處理正式結構體工程大項完整決標價，合成單項材料金額也不等於大項價格。


## 第四輪：從第三輪結果 CSV 產生可查證報告（2026-11-11）

第三輪權重研究已能成功執行，但在未提供輸出 CSV 前，**不能推斷哪個模型比較好**。新增 `report_weights.py`：直接讀取第三輪 `production_weight_summary.csv` 與標記 `SIMULATED_RESEARCH_ONLY` 的 manifest；檢查欄位、有效數值、每組三種設定完整性，再產生 Markdown 報告。

```powershell
python research/2a_synthetic/report_weights.py
python -m unittest discover -s research/2a_synthetic -p "test_*.py" -v
```

輸出：`research/2a_synthetic/output/production_weight_report.md`，包含不同用途／材料／樣本數的三設定模擬 MAPE、相對差異、區間覆蓋、ESS、跨結構混用及跳過數；不改正式參數，不連任何 DB。

**請上傳這個 Markdown 報告或 CSV 給下一個對話，才有真實數值可解讀**；`output/` 被 gitignore 排除，不會自動出現在 GitHub。CSV 若缺漏、重複配置或來源標記不對，程式將停止並報錯，不會虛構得分。此分析不是產學真實工程驗證。


## 第五輪：第三輪實測結果判讀與因子拆解（2026-11-11）

已收到使用者上傳的 `production_weight_report.md`。來源摘要與限制已保存於 `docs/2A_SYNTHETIC_WEIGHT_RESULT_REVIEW_2026-11-11.md`：24 個模擬情境組的平均 MAPE A（現行）約 17.10%、B（結構硬門檻）17.07%、C（調整面積／樓層與硬門檻）16.91%；現行跨結構引用平均僅 0.18%，C 對 A 的改善約 0.19 個百分點，不能說實務一定比較好。

**重大研究設計修正：** C 同時改動多個因子，無法獨立證明是基地面積權重還是結構門檻帶來的改善。新增 `factorial_weight_review.py`，沿用第三輪研究基底，建立六種條件的交叉／配對對照：A／B（原權重、無／有硬門檻）、C／D（重新分配面積／樓層權重、有／無門檻）、E／F（只移除基地面積權重、有／無門檻）。原第三輪的 A/B/C 不會被覆寫；新結果另存 `factorial_weight_*.csv`。

```powershell
python research/2a_synthetic/factorial_weight_review.py --first-seed 20261111 --seeds 5 --cases-per-combo 30 --train-sizes 3 5 8
python -m unittest discover -s research/2a_synthetic -p "test_*.py" -v
```

新輸出：`factorial_weight_detail.csv`、`factorial_weight_summary.csv`、`factorial_weight_paired.csv`、`factorial_weight_report.md`、`factorial_weight_manifest.json`（均在 output/），整輪預期 4 情境 × 5 seeds × 6 類型 × 2 材料 × 3 樣本數 × 6 配置 = **2160 組配置測試**，加上 24 組情境 × 8 種配對 = **192 行配對結果**。這些都是合成研究指標，沒有真實工程校準，且仍未覆蓋每結構僅 0～3 件可比案例的情形；正式產學資料庫及演算法保持不變。
