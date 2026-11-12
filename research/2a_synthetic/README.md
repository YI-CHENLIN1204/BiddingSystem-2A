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


## 第六輪：稀疏同結構歷史案壓力測試（2026-11-11）

第五輪 `factorial_weight_review.py` 使用者完整實測：**4320 group runs / 144 summaries / 192 pairwise rows**，且當時 `unittest` **13/13 OK**；`check_handoff_review.py --base 2acf10d` 已成功，9 檔完整審查。原先預估的 2160 次有誤，正確計算為 4 情境 × 5 seeds × 6 用途結構 × 2 材料 × 3 參考數 × 6 配置 = 4320。

新的 `sparse_structure_review.py` 不更動正式模型，針對**每組 0/1/2/3 件相同結構歷史**的特定困境，保留其他結構的候選案例，並分開報告：
- `technical_prediction_available`：數學上是否能算加權分位
- `reference_count_gate_pass`：是否滿足現行最低參考案數 5 的名義門檻
- `cross_structure_reference_percent`：參考中的跨結構比例
- 模擬 MAPE、ESS、跳過及可計算個數

```powershell
python research/2a_synthetic/sparse_structure_review.py --first-seed 20261111 --seeds 5 --reservoir-per-combo 20
python -m unittest discover -s research/2a_synthetic -p "test_*.py" -v
```

輸出：`output/sparse_structure_detail.csv`、`sparse_structure_summary.csv`、`sparse_structure_report.md`、`sparse_structure_manifest.json`。預計 4 情境 × 5 seeds × 6 類型 × 2 材料 × 4 稀疏度 × 2 結構 gate = **960 組比較**，彙整 4×4×2 = 32 行。

此設計的人為「其他結構約 20 件候選」並非崴正實際情形；缺少真實同質案的研究結果只能幫助**辨識資料不足時的演算法行為**，不能證明 5 件門檻是足夠的真實統計可靠度或跨結構估算可用。正式產學 DB 不會讀研究輸出。


## 第五／六輪結果回收（2026-11-11）

第五輪 `factorial_weight_report.md` 與第六輪 `sparse_structure_report.md` 已由使用者上傳並分析。完整原始表的摘要、解讀、可比性風險、不可外推限制及後續研究優先序，已整理到 `docs/2A_FACTORIAL_SPARSE_RESULTS_2026-11-11.md`。

重要觀察：五輪權重微調平均改善約 0.1～0.18 MAPE 百分點；六輪同結構僅 0～3 案時，無硬門檻的參考資料可大量跨結構混用，雖通過總數最低 5 案門檻，卻不能證明符合工程口徑。**下一輪應優先驗證資料來源與可比性門檻，而非再繼續盲目調權重。** 目前不變更生產服務、DB 或既有模擬腳本。


## 第七輪：來源與工程可比性資格診斷（2026-11-12）

第六輪結果顯示，名義歷史樣本數達門檻，**仍可能因結構異質而不可比較**。為避免把「參考筆數」當成來源真實性的替代，新增 **`eligibility_review.py`**，僅在獨立研究區試驗來源、用途、結構、材料、標準單位、整體工程範圍、審核狀態的資格規則。

```powershell
python research/2a_synthetic/eligibility_review.py --seed 20261111 --cases-per-combo 12 --minimum-count 5
python research/2a_synthetic/eligibility_review.py --seed 20261111 --cases-per-combo 12 --minimum-count 5 --allow-cross-structure
python -m unittest discover -s research/2a_synthetic -p "test_*.py" -v
```

輸出 `output/eligibility_detail.csv`、`eligibility_summary.json`、`eligibility_report.md`。兩種執行使用**同一個預設輸出名稱**，第二次會覆寫第一次；若需比對兩者，請提供不同 `--output` 前綴（如 `.../eligibility_strict` 與 `.../eligibility_cross`）。

**嚴格隔離**：
- 所有來源仍是 `source_kind=synthetic`。即使模擬了 `review_status=Approved`，也不能變成企業正式歷史。
- `hypothetical_eligible` 只表示「若暫時假設來源可驗證，其他條件是否通過」，僅供研究規則；`production_eligible` 對全部合成資料必須為 `false`。
- `reference_count_gate_pass` 只代表名義最低 5 案，**不是**工程品質保證或可以直接使用於正式異常偵測的充分條件。
- 故意注入不同結構、錯誤單位、不完整範圍、Draft、錯誤材料／建案用途等情況，用來確認所有拒絕原因可追溯；可能有多個原因同時成立。
- 這是**資格規則的研究樣品**，不是從資料庫讀出的真實 `ProjectMaster`／`HistoricalTransaction`，亦未與正式 .NET 的單位換算、結構類別、工項完整性校對。
- 不需要新增任何測試資料到 Supabase；正式異常判定與資料庫不受影響。

**下個驗證點**：先執行此腳本與單元測試，檢查合成候選的 `production_eligible=0` 與拒絕理由。其後研究「從真實 DB 唯讀欄位建立可比性摘要」時，必須先確認企業歷史原始來源、工程範圍資訊是否真的存在，不能推測資料庫已具備來源完整性標記。


## 第七輪輸出解讀及正式欄位靜態對照（2026-11-12）

使用者上傳 `eligibility_report.md`：35 個模擬候選，同結構限制開啟；假設來源真實後其他條件合格 **1** 件，實際正式來源合格 **0** 件；每案可能有多個拒絕理由（跨結構 25 件，來源未驗證 35 件）。此結果僅說明模擬資格邏輯，不是正式 2A 已有來源驗證機制。

完整對照見 `docs/2A_REFERENCE_ELIGIBILITY_MAPPING_2026-11-12.md`：C# `ProjectMaster` 有用途／結構／Approved、`HistoricalTransaction` 有 MaterialId、`MaterialMaster` 和 `UnitMaster` 支援標準單位資訊；但是已讀模型中缺少可直接確認真實來源認證、整體工程範圍完整性的權威欄位。**不能只憑檔名、Hash、Approved 就說資料是真實完整的最終標單。**

新增 `research/2a_synthetic/reference_schema_readonly.sql` 供使用者在確認正確開發 DB 後執行，只查 `information_schema.columns`，不含工程資料列、機密或更新；尚未執行，不能宣稱 DB schema 已驗證。此輪未修改正式 C# 或資料表。


## 2026-11-12：已取得正式資料表 Schema，進入唯讀品質聚合

使用者已執行 `reference_schema_readonly.sql` 並提交 PostgreSQL 的 86 欄位查核資料。研究證據與正式欄位缺口：`docs/2A_VERIFIED_DB_SCHEMA_GAPS_2026-11-12.md`。

下一步在核實正確 Supabase 開發 DB 後，開啟 `research/2a_synthetic/reference_quality_counts_readonly.sql`，查詢四組**聚合**結果：Approved/用途/結構案數、缺失重要基礎資料或來源檔案欄位案數、歷史交易連結品質、各用途/結構/材料的**獨立案數**。它是 `BEGIN READ ONLY` 且只輸出匯總、不輸出建案名稱或單價明細，也不會寫 DB。**腳本尚未執行，任何準備率與合格案數仍未驗證。**

`project_master` 之 `project_type`、`structure_type`、`total_floor_area` 可為 NULL；`historical_transaction.project_master_id`、`final_bid_item_id` 可為 NULL；`Approved` 不證明企業來源真實。資料品質聚合結果即使全部通過，仍不是工程範圍完整性與來源真實的正式證明。


## 2026-11-12：實際 DB 的 Q1–Q4 品質盤點已完成

使用者提供真實 Supabase 聚合的完整 Q1–Q4。結果詳見 `docs/2A_HISTORICAL_DATA_QUALITY_Q1_Q4_2026-11-12.md`：10 件住宅 RC（8 Approved、2 Draft），7/8 Approved 缺來源 Hash，23 歷史交易中 2 已刪除、2 連非 Approved／已刪除（不一定為互異交易）；有效 material_id=1 有 8 件獨立建案／11 筆交易、material_id=2 有 8 件獨立建案／10 筆交易。每筆交易 FK 與 ProjectMaster/FinalBidItem 的關聯未見異常；仍不能據此證明資料來源真實。

下一份 **`reference_material_source_audit_readonly.sql`** 共有 Q5～Q8：材料名稱、疑似 DEMO 來源聚合、同案同材料交易數、交易刪除×關聯 Approved 狀態交叉表。只輸出主檔名稱和聚合數值、不回傳任何真實建案名稱、標單內容或來源檔案名稱。尚未執行 Q5～Q8；只用於偵查，不能作為已核准正式模型改動。
