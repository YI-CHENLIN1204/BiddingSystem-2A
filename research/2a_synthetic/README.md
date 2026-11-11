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
