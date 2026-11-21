# 崴正營造 2A — 收斂及驗收計畫

> 2026-11-21；狀態：收斂計畫已建立，未宣稱最終驗收完成。
> GitHub `main` / `feature/1d-api-integration` 為不同程式碼版本。以下測試證據依實際 Windows 執行紀錄為準。

## 目標及範圍凍結

此輪收斂不再以新增類別、增加更多生成資料、修改模型參數為主要工作。優先完成現有功能的**可驗證性、部署可移轉性、前後端與分支一致性、交接文件**。

- 核心交付：已分類最終標單上傳與驗證、建案基本條件相似度、結構體工程**鋼筋／混凝土數量合理區間與異常方向**、**結構體工程大項總金額與每坪價格合理區間**、人工查核解釋與分析紀錄。
- 整合支援：RC／SC／SRC 進**同一資料庫**，而非每結構分庫；1D 為可選來源，2A 必須可以獨立接受符合格式的 Excel。
- 暫不擴大正式紅綠燈：鋼骨、模板、土方與其他材料只有研究候選，不因合成資料充足就啟用。不得將大項總價異常描述為材料**單價**異常，更不能將紅燈稱為違法或舞弊證據。
- 不以舊設計方案（如 Z-score、LSTM、Razor）描述目前實作；以對應提交的程式碼為準。

## 已有的實測證據

| 項目 | 已驗證結果 | 限制 |
| --- | --- | --- |
| 合成資料來源 | 300 專案（RC110/SC88/SRC102），143700 歷史交易和標單明細，train/test/validation 各 6 極端 | 模擬，不是合作方真實工程資料 |
| train-only seed | 210 專案，101166 歷史交易和同數 FinalBidRows、100 材料主檔、排除 90 holdout | 只證明隔離測試庫成功匯入 |
| Python 單元測試 | 23/23 PASS（Windows） | 不能代替 API E2E 或真實工程準確率 |
| 相似度敏感度 | SYN-00002 城市、基地面積調整後參考分數/排序變化，PASS | 不等於預測準確 |
| 真實 API E2E | 3 baseline + 10 variants，共 13/13 完成、26 個 quantity/price runs | 10 variants 僅基於 3 個來源建案，統計不獨立 |
| 數量方向 | 混凝土高/低對應 High/Low；鋼筋高可 Low→High；低用量基準原已 Low | 未做全體正常 holdout 誤報率 |
| 大項價格方向 | 單價 x3 對應 Low→High；單價 x0.2 對應 Normal→Low；量增導致總價變更 | 不能將總價紅燈等同單價紅燈 |
| 基準狀態 | RC/SRC 鋼筋 baseline Low，SRC/SC 價格 baseline Low | 未修改 ≠ 嚴格正常；生成器包含約5.5% 群組層級量值倍率變化 |

## 待完成及退出條件（依優先順序）

### P0 模型驗證可解釋

- [ ] 在現有 300 專案中明確區分 train/validation/test，對測試樣本的**合成設計偏差與原有 group anomaly** 建立可追溯真值。若做不到可靠正常標籤，另建立清潔 holdout，不更改原資料與已匯入的 seed。
- [ ] 按 RC/SC/SRC 和鋼筋/混凝土/結構體總價分層彙整：真正正常例數、紅燈率、異常高低方向、P15/P50/P85 涵蓋情況、缺樣本與灰燈率。明確排除不適用的鋼筋/鋼骨型別，不能硬湊完整組別。
- [ ] 分開報告：**資料/流程正確性**、**受控注入偵測方向**、**標註正常值的誤報**、**真實工程外部有效性（目前未知）**。沒有足夠獨立證據不宣稱 accuracy、precision、recall 已達標。
- [ ] 僅在找出可重現的邏輯缺陷後修改演算法；每次修改都要保留原版/新版本結果比較，不用測試集調到過擬合。

### P0 程式分支及整合

- [ ] 列出 `main` vs `feature/1d-api-integration` API、相似度、模型、EF Migration 差異，查清先前 SRC E2E 實際跑的 commit；預備最小範圍 PR，避免無條件覆蓋 main。
- [ ] 在可重建的測試 DB 中完成 Vue UI：Excel 上傳、案件版本、數量分析、價格分析、紅綠燈理由、錯誤處理與未對應/不支援說明（以實際存在的 API 為準）。
- [ ] `dotnet build`、前端 `npm ci && npm run build`、Python unittest、主要 API/E2E 逐項 PASS；測試失敗或未測分開列示。

### P1 單一可攜資料庫與部署

- [ ] 整理「一個 RC/SC/SRC 整合測試資料庫」的 Docker Compose / .env.example / 版本化 Migration，不新增每個結構專用 DB。現用 `55433` 為**整合測試庫**，不是正式合作資料庫。
- [ ] 制定並演練本機測試庫備份/還原（包括既有 AnalysisCase/AnalysisRun），有 checksum 與敏感檔案排除；新裝置 `git clone` 不會攜帶 Docker volumes。
- [ ] `55432` 保留舊回退資料，不得任意 `down -v`、重 seed 或直接合併同代號 SYN 建案。完成新環境還原及 API smoke test 後才能討論停用。

### P1 交付與展示

- [ ] 完成固定範圍 README、安裝/啟動/備份還原 SOP、資料字典、最小操作手冊、API 測試操作、匿名合成展示檔、模型限制及已知風險。
- [ ] 一次完整展示：建案基本資料 → 上傳合法標單 → 數量/價格合理區間 → 低/正常/高 → 參考建案與稽核說明 → 保存分析版本；不對未核准項目顯示正式紅燈。
- [ ] 以可重現的 commit、test log、DB fixture、人工操作紀錄作最終驗收，不靠聊天紀錄或憑印象宣稱 PASS。

## 近期工作順序與停止規則

1. **先做不寫資料庫的證據整併與正常 holdout 標籤設計**；不要再次執行完成過的 13 件 `--execute`。
2. 補足不混淆量與價格的模型驗證，再決定是否真的需要改演算法。
3. 針對實際有缺陷的部分一次一組修復，跑完整回歸；無關功能暫停開發。
4. 固定並整合 feature/main；最後完成部署可攜性和文件。

收斂成功的定義是「**固定核心功能可運作、有誠實的測試與限制說明、可在新裝置恢復、可交接**」，而不是所有紅燈都吻合人為假設或新增更多類別。

## 風險與回滾

- 不修改原有 `graduation-src-extreme-v3`、`graduation-anomaly-holdouts` 和 `graduation-src-paired-api-report.json`，不清除 `55433` 或 `55432` volumes。
- 生成資料與訓練歷史不可混入 test；分析案件只能寫 Target Pipeline。
- 涉及 DB Schema 變動時先備份、列 Migration 與降級策略，並在隔離庫先驗證。
- 本文件為**收斂計畫/技術決策**，不表示上述待完成項目已執行。


## P0 第一個落地工具：合成 holdout 來源稽核（2026-11-21）

已在 `main` 新增 `tools/graduation-synthetic/audit_holdout_provenance.py` 與 `test_holdout_provenance.py`，用歷史 CSV 的 `synthetic_label` 串接同一 transaction 的 BOQ，在現有 test split 逐建案列出三種評估 scope 中是否有生成器刻意倍率變化，並對既有 3 份 baseline API 結果加上來源警示；完全不重建建案、不重跑 API、不改動 DB。

- 來源唯一有證據的標籤是**已記錄的群組倍率變化**，不是工程正常/異常的正式 ground truth。
- `noRecordedGeneratorShock` ≠ 工程正常；`readyForFalsePositiveRate` 一律 false。後續若要正式誤報/漏報率，必須先擬定獨立工程情境與正常標記的驗收方法。
- **狀態**：GitHub 實作已提交、Windows 單元測試與實際 CSV 報告**尚未執行**。下一次驗證點請直接依 `tools/graduation-synthetic/README.md` 執行新 3 tests + 既有 23 tests，再產生 provenance JSON；未通過前不可勾選上方 P0 的最終驗收項目。


### 2026-11-21 實際 Windows 來源稽核驗證

- 使用者 `git fetch --no-tags --refmap= origin main`、還原兩個新稽核工具後，在 Windows 跑 `python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v`，結果 **Ran 26 tests in 19.041s / OK**，新增三項全 PASS。
- 既有 `graduation-src-extreme-v3` + `graduation-src-paired-api-report.json` 的**唯讀來源稽核 PASS**，輸出 `graduation-src-holdout-provenance.json`：45 test 建案，3 baseline 可對照，`readyForFalsePositiveRate=false`。
- RC：18 test（16 非 extreme），混凝土無生成器 shock 18/18、鋼筋 17/18、結構大項 15/18；SC：17 test（15 非 extreme），混凝土 15/17、鋼筋不適用、結構大項 11/17；SRC：10 test（8 非 extreme），混凝土 10/10、鋼筋 10/10、結構大項 7/10。
- 三 baseline：RC `SYN-00029` 鋼筋 Low/shockedLineCount=0，結構總價 Normal/0；SRC `SYN-00030` 鋼筋 Low/0，結構總價 Low/0；SC `SYN-00026` 結構總價 Low/shockedLineCount=4（混凝土 Normal/0）。**RC、SRC 的基準紅燈不能用已記錄生成器 shock 直接解釋**；SC 四筆 shock 可能影響總價，但尚未證明因果。注意生成器未加 shock 不等於合格正常標單。
- 收斂下一段：先對無 shock 的 RC/SRC 基準 Low 做**離線診斷**（同結構篩選、參考建案特徵與範圍成分、生成器係數的量級差異），不要直接為了消除紅燈調整量價門檻。若要取得可靠正常誤報率，需另定具工程情境、獨立分布的正常 holdout 並驗證正式 API。
