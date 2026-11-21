# 崴正營造 2A｜完整開發交接主檔（2026-11-11）

> **此文件是新對話的 CURRENT SOURCE OF TRUTH（目前狀態入口）**。歷史 docs/HANDOFF.md、docs/NEXT_CHAT_HANDOFF.md 的 2026-10-15 快照是歷史紀錄，**不可蓋過本檔**。
>
> 原則：GitHub 最新程式碼／當前 DB 唯讀查核／使用者明確測試回報，優先於歷史文字；對未查證內容明確標注，不得宣稱「百分百完成／正式準確率」。

## 0. 30 秒摘要

- 專案：雲科大 × 崴正營造 2A「量價估算與異常偵測」。GitHub `YI-CHENLIN1204/BiddingSystem-2A`；**主要分支 `feature/1d-api-integration`，不得自行合併 main**。
- **正式功能範圍**：鋼筋／混凝土「數量偵錯」、結構體工程大項「價格異常」。UI 標題用通用名稱，適用範圍另外描述。不得恢復 Z-score、LSTM 作正式模型或已淘汰通用估算服務。
- 系統兩條路：`AnalysisCase → AnalysisBidVersion (V1/V2/V3) → AnalysisRun` 為待偵錯工作／快照；`ProjectMaster Draft → FinalBidItem 人工審核 → Publish History Approved → HistoricalTransaction` 才是歷史基準。**AnalysisCase ≠ ProjectMaster**。1D 匯入可選，2A 可自行上傳。
- 現行正式演算法：六因子建案相似度、相似度平方、時間半衰期、Top K、P15/P50/P85；正式數量按每 m² 用量再乘本案面積；正式價格按結構體工程每坪金額；詳見第 3 節。
- **2026-11-10 讀取 DB 快照**：10 件未刪除 ProjectMaster（8 Approved 全是住宅 RC，2 Draft）；其中 7 件 Approved 名稱辨識為 DEMO／模擬／測試。廠商表示另有約 20 件原始建案（住宅／廠房 × RC／SC／SRC），**不能當作已匯入的 20 件核准歷史樣本**。資料庫即時狀態須重新 SELECT 查證。
- 教授同意**專題報告使用模擬資料**；產學版模擬資料只供研究，**不可混入正式歷史基準**。資料生成器故意使用假設數值與虛構企業偏差，絕非真實施工規範。
- 研究目前已實作 `research/2a_synthetic/`，包含 `generate.py`、`compare.py`、`batch.py`、`production_weight_review.py`、`test_research.py`。
- **已由使用者貼出實測**：`batch.py --seeds 10 --cases-per-combo 30 --train-sizes 3 5 8` 正常完成 **5760 組模擬方法測試／96 列彙總**，原有 **7/7 unittest 通過**，曾合併到 GitHub commit `3986920`。
- **最後一次使用者回報（本次交接）**：「沒有錯誤」：針對其後新增的 `production_weight_review.py` + 新增測試，**使用者口頭表示通過；未收到完整新執行輸出及實際 CSV**，因此結果數值尚未供本對話分析；不可自行捏造哪組模型獲勝。
- **下個開發任務：先讀取／檢查 `production_weight_summary.csv` 和 `detail.csv`，再強化真正公平的模型比較與回測，不先改正式產學演算法。**

## 1. 文件、程式優先閱讀順序

1. `docs/CURRENT_HANDOFF_2026-11-11.md`（本檔，最新）
2. `docs/RESEARCH_TRACEABILITY_2026-11-11.md`（證據來源／版本／實驗可重現／未驗證）
3. `research/2a_synthetic/README.md`
4. `research/2a_synthetic/generate.py`、`compare.py`、`batch.py`、`production_weight_review.py`、`test_research.py`
5. `docs/2A_MODEL_AUDIT_2026-11-10.md`
6. `docs/2A_PUBLIC_DATASET_FEASIBILITY_2026-11-10.md`
7. `README.md`、`docs/2A_CURRENT_ARCHITECTURE.md`、`docs/DECISIONS.md`、`docs/API.md`、`docs/DATABASE.md`
8. `docs/DEVELOPMENT.md`、`docs/LOCAL_TEST_GUIDE.md`、`docs/VENDOR_QUESTIONS.md`、`docs/WORKLOG.md`
9. 舊的 `docs/HANDOFF.md` 和 `docs/NEXT_CHAT_HANDOFF.md` 為**歷史狀態**，只供查舊決策與舊操作；遇到衝突本檔優先。

## 2. 產品架構、功能與限制

### 技術
- Backend：ASP.NET Core Web API、.NET 10、EF Core、Npgsql；`崴正/`。
- Frontend：Vue 3、TypeScript、Vite、Bootstrap；`frontend/`；Nginx Docker 正式入口。
- DB：PostgreSQL（Supabase）。使用 Docker Compose `compose.yaml`，frontend 暴露 `http://localhost:8081`，backend 容器內 8080。
- 必要 `.env`：`DB_CONNECTION_STRING`（映射 `ConnectionStrings__DefaultConnection`）；選配 `ONE_D_API_BASE_URL`；**不得放密碼、TOKEN 在對話／GitHub 文件**。
- 本機 Windows PowerShell，工作目錄例如 `C:\Users\lyich\OneDrive\桌面\BiddingSystem-2A`。不能假設新裝置同路徑。
- GitHub 最新內容在 `feature/1d-api-integration`；GitHub 預設 main 不等於此分支。

### 人的操作流程
**待偵錯案**：建立案件（1D 選配匯入／2A 上傳 Excel、工作表手動選擇）→ 填建案基本條件 → V1/V2/V3 版本快照 → 選擇數量偵錯或價格異常 → 儲存分析 Run → 可查歷史結果與參考來源。此路線不得直接寫入歷史交易。

**正式歷史案**：上傳**真正商務定案的最終標單** → 唯讀預覽與選工作表 → 建 `ProjectMaster` Draft + `FinalBidItem` → 核對來源列號、工項、材料與單位、數量／價額、完整性 → 人工 Publish → Approved → 正式 `HistoricalTransaction`。Approved 修改需 Reopen Review。原始分類 Category1/2/3 保留，人工覆寫 `major_category_override`。

### 分析範圍／呈現
- 數量偵錯只支援鋼筋（kg）與混凝土（m³）細項；依歷史 **單位樓地板面積用量** 計算區間，再乘本案面積為總量。不要錯把 kg/m² 顯示成 kg。
- 價格異常目前只支援「結構體工程」大項。歷史價格用 `FinalBidItem.Amount` 或 `Qty×UnitPrice` 加總，以每坪價計算。與 `HistoricalTransaction` 的數量職責分開；**跨年行情未校正**。
- 模型結果 P15～P85 是參考歷史範圍，不等於工程法定上下限、準確率 70%、信心度或正式預測保證。
- 已有 ESS 與單案權重占比於新結果顯示；舊 Run 快照不會自動補欄位。
- 先前展示 V3 模擬 Run #18 數量（鋼筋偏高、混凝土正常）、Run #19 價格（結構體偏高）；這是**模擬環境功能測試，不是工程有效性證明**。
- 2026-11-10 唯讀 SQL 由 Run #18、#19 計算 8 筆參考案 ESS 7.34、最大單案權重 18.30%；這反映權重集中度，不證明來源真實。
- `docs/DEMO_DATA_20261015.md`、`scripts/cleanup_demo_2a_20261015.sql` 有演示樣本紀錄及清理腳本；**不要未確認依賴與權限就自動刪資料**。

## 3. 正式演算法／參數（來自 2026-11-10 的 DB 唯讀快照，不代表現在必然一樣）

六因子線性加權相似度：

```text
structure 25; floor area 20; site area 10; ground/basement floors 10; project year 0; city 5
Total = 70, S = 100 × sum(partial scores)/70
面積差距及基地面積差距 >= 50% 時該子分數為 0；
地上每差一層扣該樓層權重的 3%，地下每差一層扣該權重的 10%；
縣市相同加 5 分，不同加 0；結構相同加 25，不同加 0。
```

候選先按建案類型一致選取（住宅／廠房），`MinimumSimilarityScore=0`，**RC／SC／SRC 不同只有扣分，並非硬排除**。由個別材料可用的歷史案按相似度排序、再按最終權重排序，取 TopK=8；最低有效案名義數 5。

```text
similarity_weight = (similarity_score / 100) ^ 2
time_weight = 0.5 ^ (abs(project_date - history_date) / (3 years))
final_weight = similarity_weight × time_weight
P15/P50/P85 = weighted empirical quantile over historical usage/m² (or price/ping)
ESS = (sum w)^2 / sum(w^2)
largest_reference_share = max(w)/sum(w)
```

**注意**：相似度的取值、權重與 TopK 不是經真實資料校準的最優設定。結構異質、基礎面積雙重計數、歷史價格基期、日期 `abs` 在時間回測引發未來資訊洩漏、只有計數門檻無完整來源品質門檻，是未解問題。正式生產現行程式中 ESS 只顯示，不影響異常燈號。

## 4. 數據現實與研究倫理

廠商據稱提供約 20 件原始歷史建案，分類為住宅／廠房 × RC／SC／SRC，各組很可能只有 1～3 件。不能拿數百筆工項當數百件獨立建案。

三類資料：

| 類別 | 保存位置／用途 | 是否正式基準 |
|---|---|---|
| 崴正真實完整核准歷史標單 | 正式 ProjectMaster/FinalBidItem/HistoricalTransaction，明確 Publish | 只有來源、範圍、核准合格時才是 |
| 外部公開／研究資料（台灣建研所、FutuRaM 等） | 另建來源追蹤、獨立研究資料集；授權／口徑需核實 | **否**，只供研究與另行驗證後外部參考 |
| 自行生成模擬資料（專題允許） | `research/2a_synthetic/output/`，專題情境評測、算法驗證 | **否**；不能匯入生產歷史表 |

RMS 台灣建案統計平台需購買／登入，沒有帳號，**已排除立即可用資料集**。其他公開來源並非已驗證可直接取得完整台灣 RC/SC/SRC 住宅／廠房最終決標標單，詳見資料集可行性文件。

模擬資料能讓專題演示、研究運作不受「只有 20 件」限制，**不能**解除真實工程可靠度驗證所需的資料限制。不可拿模擬結果聲稱「實務準確率 93%」。

## 5. 模擬研究：腳本、輸出、參數、用途

所有腳本使用 Python 3.10+ **標準函式庫**，與正式 DB 無連線：

- `generate.py`：六組（住宅／廠房 × RC／SC／SRC），鋼筋／混凝土雙材料列；固定 seed 可重現。四情境 `stable`、`high_variance`、`company_shift`、`outliers`。虛構公司 12% 差異是研究者假設，基準數量與單價也是研究假設，不是實際工程參考值。每組 30 案預設 180 獨立合成建案／360 材料行。
- `compare.py`：中位數、描述性常態平均±1 SD、加權百分位、面積-only 加權，目標案 holdout，確定性 SHA256 抽選參考資料，不是時間前推驗證。
- `batch.py`：跨四情境、10 seeds、每組 30 案、可用參考數 3/5/8、四方法。**使用者實測完成** 5760 group runs / 96 summary rows；現有 7 unittest 全過（貼出完整 PowerShell 輸出）。
- `production_weight_review.py`：第三輪，對應正式六項因子公式的研究版、時間權重、TopK、加權 P15/P50/P85；三設定 `current_no_structure_gate`、`current_with_structure_gate`、`no_site_area_with_gate`，包含 ESS、跨結構引用率。**使用者回報「沒有錯誤」**，但尚未提供詳細 console log 或實驗產出的數字。不能說其模型比較結果已完成分析。
- `test_research.py`：先前 7 測試；後續增加正式權重重現／結構門檻與批次重現性測試。當前**新增測試實際通過數**未由完整輸出確認。

執行（從 repo 根目錄）：

```powershell
git status
git fetch origin
git merge --ff-only origin/feature/1d-api-integration

python research/2a_synthetic/batch.py --first-seed 20261111 --seeds 10 --cases-per-combo 30 --train-sizes 3 5 8
python research/2a_synthetic/production_weight_review.py --first-seed 20261111 --seeds 5 --cases-per-combo 30 --train-sizes 3 5 8
python -m unittest discover -s research/2a_synthetic -p "test_*.py" -v
```

資料輸出在 `research/2a_synthetic/output/`（`.gitignore` 排除 Git）；`batch_detail.csv`、`batch_summary.csv`、`batch_manifest.json`；`production_weight_detail.csv`、`production_weight_summary.csv`、`production_weight_manifest.json`。**GitHub 不含本機研究輸出**，若要評估實際 MAPE 優劣，必須請使用者提供 CSV／將匿名研究彙總檔上傳。

模型與區間公平性限制：中位數插值分位、常態平均±1 SD、加權階梯分位**不是相同的預測涵蓋準則**，不得只比較涵蓋率就說哪個較好；下一步需相同目標覆蓋率／更好的真實回測。模擬資料的結構用量差異是預設值，硬門檻模型若在此情境較佳可能是自我設定使然。

## 6. 已驗證／尚未驗證清單

| 項目 | 證據 | 認定 |
|---|---|---|
| GitHub 功能提交 | commits 見追溯文件 | 已提交（非等於部署） |
| `batch.py` 5760 組與 96 彙總 | 使用者完整終端日誌，2026-11-11 | **已實際執行通過** |
| 當時 `unittest` 7/7 | 使用者完整終端日誌 | **已實際執行通過** |
| 第三輪正式權重研究腳本 | 程式已提交，使用者回報無錯誤 | 已回報通過；**詳細輸出未附** |
| GitHub 主系統分析 UI + ESS 程式 | commits `053bf749` ～ `986065e` 等 | 已提交；本對話尚無當前 Docker UI 完整驗收日誌 |
| 2026-11-10 DB 條件與 Run #18/#19 ESS | 唯讀查庫 | 只限查詢當時，不代表永久不變 |
| 真實工程估算準確率／信心度 | 沒有足夠可比真實 holdout | **未完成** |
| 公開資料與正式資料整合 | 尚未實作 | **未完成** |
| 模擬資料與正式表的隔離 | 目前 Python 研究工具根本不連資料庫 | 已在研究工具隔離；**正式 DB 仍留早期 DEMO 測試案，需另處理** |
| 產學正式交付驗收 | 尚缺真實資料代表性與結果驗證 | **未完成** |

## 7. 下一步（按次序，避免重複／無止境優化）

**P0 第一件**：取得 `production_weight_summary.csv`、`production_weight_detail.csv`，先回答模擬的三組權重差在哪、跨結構參考比例多少、ESS 是否穩定、在哪些情境會失敗。不以只跑通作為研究已完成。
**P0 第二件**：全面審查 Python 研究版與 C# 正式演算法是否等價（6 因素、缺漏欄位、TopK 排序、時間、P15/P85、參考來源、數值精度），寫「差異列表＋單元測試」，不要直接改正式服務。
**P1**：公平比較相同目標涵蓋率的預測區間，加入樣本數與權重敏感度。跨結構納入是否允許應按鋼筋／混凝土分別決策。
**P1**：20 件廠商原始真實建案建立用途 × 結構 × 工項可用性矩陣，核對是否完整最終標單、單位與價格範圍。不能宣稱目前已完成匯入。
**P1**：實際下載／核實台灣建研所 PDF 附錄與 FutuRaM Excel 真正的欄位、授權與口徑。RMS 付費平台先排除。
**P2**：研究公司差異的分層／收縮估計；企業優先權重不能任意固定乘倍率。研究停止條件是簡單方法已達成事先約定驗收，而不是必須寫完 LSTM 或所有模型。
**交付**：專題可展示模擬研究結果（標示假設與限制）；產學版必須隔離資料源、確保真實歷史、樣本不足時僅提供參考／人工複核，未校準不顯示信心百分率。

**不要重做**：已通過的 5760 批次和原 7 個測試，除非比較方法、輸入或關鍵程式碼有變；下一輪應集中分析結果、測未測差異。不要動 `main`，不要刪 Supabase 或示範資料來「修正」成功率。

## 8. 安全、Git 和可回溯

- Git commit 是不可變的程式快照；保留 commits 的 SHA 與對應文件。要回溯先 `git log --oneline --max-count=30`、`git show <SHA>`，不要使用 `reset --hard` 破壞本機資料。
- 新對話不能只憑摘要假設使用者工作目錄乾淨；先 `git status`。先看最新 `docs/CURRENT_HANDOFF_2026-11-11.md`，再看程式與 docs/WORKLOG。
- 本機研究 CSV 未 push 且被 gitignore 排除；若要跨對話保存實驗結果，上傳 CSV，或在新對話同種子重跑。
- `git merge --ff-only origin/feature/1d-api-integration` 是避免非快轉覆蓋的優先方式；如有衝突先回報，勿覆蓋。
- 不接觸正式 DB 寫入，不自動刪除示範資料；變更生產公式要先有實驗與使用者明確同意。
- 不分享或記錄資料庫密碼、密鑰或個人資料。

## 9. 新對話可以直接貼的指示（完整版本在 docs/NEW_CHAT_START_2026-11-11.md）

```text
請直接接手 GitHub YI-CHENLIN1204/BiddingSystem-2A 的 feature/1d-api-integration。
先讀 docs/CURRENT_HANDOFF_2026-11-11.md、docs/RESEARCH_TRACEABILITY_2026-11-11.md、
research/2a_synthetic/README.md、docs/2A_MODEL_AUDIT_2026-11-10.md，
並以最新程式碼核對，勿沿用 10/15 的舊交接假設。
最近已完成批次模擬 5760 組／96 彙總、7/7 舊測試通過；
第三輪 production_weight_review.py 與新增測試使用者回報沒有錯誤。
請先分析本機 production_weight_summary.csv 與 detail.csv（若未提供，先執行固定命令產生）
及程式對照差異，不要重新設計系統或重做已完成研究。
專題模擬資料與產學真實歷史必須分離；不改正式 DB／權重，不合併 main。
```


## 10. 2026-11-11 新增專案 Skill：變更後必審交接

- 唯一 Skill 識別：`waizheng-2a-handoff-guardian`；檔案 `.agents/skills/waizheng-2a-handoff-guardian/SKILL.md`；全 repo 代理入口 `AGENTS.md`。
- **每次修改**（程式、模型、研究、設定、資料庫、文件）完成後，必須重新評估交接影響，並在 `docs/HANDOFF_CHANGE_REVIEWS.jsonl` 新增 JSON 審查紀錄。若改動使架構／現況／決策／測試與下一步不同，應實際更新本主交接、`docs/RESEARCH_TRACEABILITY_2026-11-11.md`、`docs/WORKLOG.md`、`docs/DECISIONS.md` 或其他受影響文件；即使不必更新也需記原因。
- 參考可執行檢查：`python scripts/check_handoff_review.py --base <該輪變更前 SHA>`，確認本輪所有檔案都被新增審查記錄涵蓋。GitHub PR（目標為 feature 分支）有 `.github/workflows/2a-handoff-review.yml` 自動 gate，另可手動觸發；直接 push 不會被 PR gate 自動攔截，操作方仍必須遵循 Skill。
- Skill 是版本化的 repo 規則，不可誇稱已自動安裝至所有 ChatGPT 對話。新對話必須明確讀取 `AGENTS.md` 與 Skill 檔案。
- 此項治理機制不改任何正式量價計算或研究數據；已提交程式與 CI 規則仍需由 GitHub Actions／本機驗證執行情況，不等於已由 CI 完整測過。


## 11. 2026-11-11 第四輪工具：第三輪權重結果自動成文

- 新增 `research/2a_synthetic/report_weights.py`，讀取本機 `production_weight_summary.csv` 與 `production_weight_manifest.json`，輸出 `production_weight_report.md`。
- 報告必須以**實際 CSV** 為根據，按情境、樣本數、鋼筋／混凝土比較三組六因素權重，統計 MAPE 改善／惡化組數、ESS、混入不同結構的參考比例。缺少資料、資料不是明確標記模擬或某組設定遺漏，拒絕產生報告。
- **尚未取得使用者本機的第三輪 CSV**，所以目前無權重優勝者、實際 MAPE 數值的研究結論；此輪只交付研究分析工具與單元測試程式。
- 下一步：`python research/2a_synthetic/report_weights.py`，上傳 `research/2a_synthetic/output/production_weight_report.md`，再由新對話判讀結果並確認 Python／C# 模型等價性。正式 DB／生產模型權重不變。


## 12. 2026-11-11 最新研究結論與下一個驗證點

- **已收到使用者上傳的 `production_weight_report.md`**：24 組情境 × 三方案（72 筆彙總比較），A 17.10%、B 17.07%、C 16.91%（24 個已四捨五入 MAPE 的簡單平均）；平均差約 0.03 與 0.19 個百分點。正式審查：`docs/2A_SYNTHETIC_WEIGHT_RESULT_REVIEW_2026-11-11.md`。
- A 模型跨結構參考比例平均僅 0.18%，所以本次無法有效評估跨結構混用；且 C 同時改變 3 個權重與硬門檻，不能推論基地面積「不重要」。
- **最新研究新增** `research/2a_synthetic/factorial_weight_review.py`（六方案拆因子配對），擴充 `production_weight_review.py` 可由研究呼叫端注入設定，保留原本 A/B/C 舊比較。請以最新 `research/2a_synthetic/README.md` 執行新命令，等待本機跑出的 `factorial_weight_report.md` 分析。
- 最近使用者曾貼出 **11／11** 研究測試通過，交接守門審查亦通過。這個證據**發生在本輪新增因子拆解腳本之前**，所以不能宣稱新增測試已通過。
- 下一優先：產生並分析因子拆解配對報告；其後才設計同結構只有 0～3 案的壓力測試、真正時間前推回測及公平區間對比。**不調整正式產學模型、資料庫及主分支**。


## 13. 2026-11-11 第五輪使用者實測／第六輪研究新階段

- 使用者在本機 `C:\Users\User\Desktop\BiddingSystem-2A` 執行第五輪：`factorial_weight_review.py --first-seed 20261111 --seeds 5 --cases-per-combo 30 --train-sizes 3 5 8`，終端顯示 **4320 group runs、144 summaries、192 pairwise rows**；13/13 tests OK；交接審查 9 files / 1 review OK。這些是**程式執行驗證**，不是對實驗配對 MAPE 勝負的判定，因 `factorial_weight_report.md` 仍未提供。
- 新增第六輪 `research/2a_synthetic/sparse_structure_review.py`，刻意測試每個相同結構只有 0/1/2/3 件可比較歷史的情形。區分數學上能輸出結果、達到名義最低 5 件門檻、跨結構混用比例、模擬 MAPE。新工具／新增測試已提交，**尚未取得用戶實測輸出**。
- 下一輪請先保留並上傳 `output/factorial_weight_report.md`，及新 `output/sparse_structure_report.md`；完整指令見研究 README。交接資料與研究結果只用於專題，正式 .NET 判定與 Supabase 均未修改。


## 14. 2026-11-11：第五／六輪報告已收到，正式結論轉向「可比性」

使用者已上傳 `factorial_weight_report.md` 與 `sparse_structure_report.md`，詳細數據及限制**已永久記錄**在 `docs/2A_FACTORIAL_SPARSE_RESULTS_2026-11-11.md`。

- 第五輪：權重重新分配（固定其他條件）在模擬中平均改善約 0.165～0.180 MAPE 百分點；基地面積直接去權重約改善 0.102～0.131 百分點；結構硬門檻在同結構資源充裕情況僅改善約 0.029 百分點。均不足直接修改產學正式權重。
- 第六輪：同結構歷史僅 0～3 件時，現行無硬門檻在假設其他結構案例充足的模擬條件下，仍 100% 通過名義最低五案，但跨結構參考占比分別 100%、87.5%、75%、62.5%。以模擬情境四組簡單平均，無硬門檻 MAPE 約 73.82%、59.67%、47.90%、35.03%；硬門檻 0/1 件無可計算，2/3 件僅技術可算，**沒有一組達到五件門檻**。
- 工程實務不可直接使用上述 MAPE；生成器先人為設定不同結構有不同材料強度，且其他結構每類有 20 件虛構候選。結論是「名義樣本數可合格而同結構可比性不足」，**不是**「已證明正式 2A 跨結構錯誤率」。
- **新開發優先順序**：先研究可比性／同結構樣本資格診斷與跨結構材料差異敏感度，其次時間前推／同涵蓋率公平比較。第五／六輪結果已分析完畢，不要重複做一模一樣的權重實驗。產學 .NET 及 Supabase 不變。


## 15. 2026-11-12：研究版歷史參考資格診斷

- 使用者 2026-11-12 凌晨貼出 `git merge --ff-only` 更新到 `cedf7df` 成功，以及 `python scripts/check_handoff_review.py --base 705efe3` 正常輸出 **6 個修改檔／1 筆新增審查紀錄**。這只確認文件完整性檢查通過，不代表第五／六輪研究已在真實公司案驗收。
- 第七輪新增 `research/2a_synthetic/eligibility_review.py` 與三個測試方法，於研究區驗證來源／用途／結構／材料／單位／工程範圍／審核狀態的可比性與正式資料資格。即使模擬 `Approved`，其 `source_kind=synthetic` 仍永遠不符合 production eligibility。
- 「hypothetical eligibility（僅研究）」、「production eligibility（真實來源審核）」與「樣本數門檻」分開呈現。程式不存取 DB，**尚未連接真正 2A 歷史表，也不會修改生產判定**。
- 下一任務：執行 `python research/2a_synthetic/eligibility_review.py --seed 20261111 --cases-per-combo 12 --minimum-count 5` 和 `python -m unittest discover -s research/2a_synthetic -p "test_*.py" -v`；回報實際輸出，再檢查研究規則是否可映射到正式 DB 的現有欄位。新對話必須先讀 Handoff Guardian Skill。


## 16. 2026-11-12 第七輪報告收到及資料模型對照

- 使用者上傳 `eligibility_report.md`，內容顯示 35 件合成候選、同結構限制有、假設來源真實時合格 1 件、真正具備正式來源資格 0 件；35 件 `unverified_source`，25 件 `wrong_structure`。各拒絕原因可能同案重複。**這是研究輸出，不是正式 DB 的合格案數。**
- 已新建 `docs/2A_REFERENCE_ELIGIBILITY_MAPPING_2026-11-12.md`：核對 C# 模型與研究資格欄位。現有 `ProjectMaster.ProjectType`、`StructureType`、`HistoryStatus`、`HistoricalTransaction.MaterialId` 等可支援基礎檢查；**已讀模型無可靠來源真實性／整體工程範圍完整性認證欄位**。
- 已新增 `research/2a_synthetic/reference_schema_readonly.sql`，下一步應由使用者在正確 Supabase 開發 DB 執行純 `information_schema.columns` 查詢，檢查真實 schema 再決定可否設計實務資格審查 UI／migration。**SQL 尚未執行，也未修改正式 DB。**
- 正式模型的權重、最少參考數、已核准歷史查詢、異常燈號全部保持現況。下一個對話先取 schema 結果，避免重跑第七輪同一批合成測試。


## 17. 2026-11-12：真正 PostgreSQL 欄位 Schema 已核對（最新）

使用者上傳在 Supabase 執行 `reference_schema_readonly.sql` 後的 `information_schema.columns` JSON 結果。**已直接驗證六張資料表共 86 個欄位**：`project_master` 17、`final_bid_item` 27、`historical_transaction` 8、`material_master` 5、`unit_master` 7、`estimation_settings` 22。

根據真實 Schema：有用途／結構／Approved／材料／單位／來源檔案 hash 等基本欄位，但**沒有獨立且具證據的 source provenance certification／整份結構體工程 scope completeness 欄位**。尤其 `historical_transaction.project_master_id` 與 `final_bid_item_id` 可為 NULL，而 `project_master.project_type`／`structure_type`／`total_floor_area` 也可為 NULL。這不能僅依 C# Model 推論所有資料品質達標；亦不能因 Approved 代表真實可比較。

詳見 **`docs/2A_VERIFIED_DB_SCHEMA_GAPS_2026-11-12.md`**。下一個操作點是研究用 **`research/2a_synthetic/reference_quality_counts_readonly.sql`**，在確認正確 Supabase 開發資料庫後，由使用者自行執行四組**唯讀聚合**查詢，查看用途 × 結構的獨立建案數、Approved 欄位缺漏與交易 FK／材料數量品質。**目前僅驗證存在的資料庫欄位，尚未讀取／查證當前實際建案筆數，新的聚合 SQL 亦尚未執行。**

此次不修改任何正式 Schema、C#、參數或歷史判定。轉新對話時不應再要求重貼同一份 86 欄清單，應改分析新聚合結果。

## 18. 2026-11-12：Q4 真實材料歷史聚合結果

使用者提供 `reference_quality_counts_readonly.sql` 的 Q4 輸出：**住宅 × RC** 有 material_id=1 的 **8 件獨立 Approved、未刪除、非異常且數量為正的歷史建案，11 筆交易、11 個不同連結工項**；material_id=2 同樣 **8 件獨立建案，10 筆交易、10 個連結工項**。不是 11／10 件工程，也不能判定 8 件皆為真實可用標單。尚未確認 material_id=1/2 名稱或 DEMO 資料是否仍在；Q1～Q3 尚未取得。

保存於 `docs/2A_APPROVED_MATERIAL_COHORT_2026-11-12.md`。下一步優先執行唯讀聚合 `research/2a_synthetic/reference_material_source_audit_readonly.sql` 確認材料名稱、來源疑似 DEMO 分組與同案同材料多筆交易；並補提供先前的 Q1～Q3 結果。**SQL 尚未執行，不能當成已完成核驗。** 正式 C#／DB／模型權重保持不變。


## 19. 2026-11-12：已取得 Q1～Q4 真實資料品質完整聚合

使用者已貼出 `reference_quality_counts_readonly.sql` **四組全部**查詢結果（不再只是 Q4）。詳細表格、證據範圍與不確定性見 **`docs/2A_HISTORICAL_DATA_QUALITY_Q1_Q4_2026-11-12.md`**。

- Q1：`project_master` 10 件，全部住宅 RC：8 Approved、2 Draft，沒有任何建案標記刪除。
- Q2：8 Approved 有 7 件缺少 `source_file_hash`（87.5%）；用途、結構、總樓地板面積、基地面積、日期、來源檔名、ApprovedAt/By 均無本次查詢定義的缺漏／不合法情況。這仍未證明來源真實與工程範圍完整。
- Q3：23 筆歷史材料交易；2 筆已標記刪除、2 筆連到非 Approved 或已刪除建案；全部有有效的 ProjectMaster／FinalBidItem FK，且無跨案工項錯連。**刪除與非 Approved 兩組是否重疊尚未確認，不能直接相加為四筆問題交易。**
- Q4：有效 Approved 歷史交易回傳住宅 RC、material_id=1 的 8 獨立建案／11 交易／11 工項、material_id=2 的 8 獨立建案／10 交易／10 工項；材料 ID 名稱與 DEMO 歷史來源仍待驗證。

`research/2a_synthetic/reference_material_source_audit_readonly.sql` 現已包含 Q5 材料名稱／標準單位、Q6 疑似 DEMO 來源聚合、Q7 同案同材料多筆交易、**Q8 歷史交易刪除狀態×關聯建案核准狀態交叉表**。下一步先執行 Q5～Q8 的唯讀 SQL，回報聚合，不要重查已完成 Q1～Q4，不得因此更改正式模型權重或把 DEMO 當真實。


## 20. 2026-11-12：Q5～Q8 真實資料庫結果已取得（最新）

使用者已提供 `reference_material_source_audit_readonly.sql` 的 Q5～Q8：**material_id=1 鋼筋/kg，2 混凝土/立方公尺**；8 Approved 中有 7 件專案名稱或來源檔名符合 DEMO/TEST/模擬/測試字樣、1 件未命中且**尚未核實**。2 Draft 中 1 件疑似展示/測試、1 件未核實。這是文字篩查，不能當真偽認證。Q2 的 7 件缺 Hash 與 Q6 的 7 件疑似 Demo 是否為同一批**未知**。

有效 Approved 同案同材料交易分布：鋼筋 7 案×1筆 + 1 案×4筆 = 11筆；混凝土 7案×1筆 + 1案×3筆 = 10筆。可能是不同來源工項，**不能直接當重複輸入或多個獨立建案**。Q8 證實 Q3 的 2 筆已刪除與 2 筆連至非 Approved 是**同一批兩筆**，其餘 21 筆未刪除交易連至 Approved、未刪除建案。

完整原值與證據限制見 **`docs/2A_HISTORICAL_DATA_QUALITY_Q5_Q8_2026-11-12.md`**。Q1～Q8 聚合結果已全部收到，**下輪不要再重查這些基本聚合**。優先事項：提出可稽核的來源／工程範圍認證設計；需區分業務 Approved 與模型 Verified Reference。未經核實不得聲稱有 8 件可靠真實公司最終標單，也不應擅自刪除展示資料。未改正式程式／DB。


## 21. 2026-11-12：Verified Reference 來源／範圍資格設計草案（尚未核准）

使用者回覆上一輪 GitHub 同步與 `python scripts/check_handoff_review.py --base ee3ff3e`「沒有錯誤」，作為**使用者口頭操作通過**證據。Q1～Q8 實際資料庫聚合已完整取得，主要問題是 8 Approved 當中 7 件名稱疑似 DEMO、另 1 件未核實，且不能將 Approved 當作來源真實完整之證明。

新建 **`docs/2A_VERIFIED_REFERENCE_DESIGN_V1_2026-11-12.md`**，是尚待產學窗口／指導教授確認的**設計草案**，非已核定需求與現有功能。核心：保留既有 `Draft/Approved` 發布與鎖定流程，增設獨立 `Unreviewed/InReview/Verified/Rejected/Revoked` 來源審核及各估算目的（鋼筋、混凝土、結構體價格）的適格資格；審核須有可追溯來源版本、工程範圍、標準單位／材料口徑、主管權限、事件紀錄、撤銷及重審機制。

草案包含欄位／資料表方案、API/UI 草案、角色矩陣、14 項驗收測試矩陣與舊 Approved 預設 Unreviewed 的遷移方向；未經決策**不能**建立 Migration、改 DB、改正式估算查詢、取消現有 Approved、或刪 DEMO。下一階段先針對現有 Authorization/History Publish/Reopen 的實際程式做接入影響清單，再與業務確認規格，才可進入實作。


## 22. 2026-11-12：Verified Reference 正式後端接入點靜態審查

已讀最新 `Program.cs`、`ProjectsApiController`、`ProjectManagementService`、`ProjectSimilarityService`、`DetectionCategoryRangeService`、`MajorCategoryPriceRangeService`、`AnalysisCaseService` 等，報告：**`docs/2A_VERIFIED_REFERENCE_INTEGRATION_AUDIT_2026-11-12.md`**。

**P0 兩項實際程式風險**：`Program.cs` 未看到 Authentication/Authorization 配置，Publish/Reopen/Delete/Audit API 無 `[Authorize]`，不能假設只有主管能執行；ProjectSimilarityService 的 `GetScoredCandidateProjectsAsync(int)` 缺 Approved 限制，DTO overload 有 Approved，但兩者用途應先辨清。外部部署網路限制尚未核對，不可宣稱實際遭攻擊。

PublishHistory 已有 advisory transaction lock／Idempotency receipt／SyncHistoricalTransaction／Audit；Reopen 會標記現有交易刪除、改 Draft；數量模型會按 ProjectId 加總多交易再計獨立建案、價格模型由 FinalBidItem 大項彙總並另有 Readiness。將來 Verified Reference 必須分目的在候選選擇器一致套用，不能只改其中一條 query，也不能假設已有 Verified 欄位。

本輪**僅文件審查**，無正式程式或 DB 變更、沒有 dotnet build 或部署權限驗證。下一步是確認實際部署身份／權限模式、前端入口，與崴正／老師核定審核規則，再走具回退的分期實作。


## 23. 2026-11-13：Verified Reference 前端／Nginx 靜態安全接入審查

使用者確認上一輪 GitHub 同步、Guardian 無錯並要求繼續。已完成對最新 Vue 前端 `api.ts`、`router/index.ts`、`App.vue`、`ProjectDetailView.vue`、`ProjectsView.vue`、`AuditLogView.vue`、`AnalysisCaseDetailView.vue`、以及 `frontend/nginx.conf`、`frontend/Dockerfile`、後端 `Program.cs` 的靜態查核，完整結果：**`docs/2A_VERIFIED_REFERENCE_FRONTEND_SECURITY_AUDIT_2026-11-13.md`**。

風險：前端沒有可辨識的登入／角色路由守衛，操作紀錄列在一般側欄，`api.ts` 不帶專屬 bearer token；目前 Nginx 只有將 `/api/` 代理到 `backend:8080`，未在這個檔案配置 API 認證。後端亦無 ASP.NET Core AuthN/AuthZ。這是程式層的安全缺口；**實際部署可能有外部代理、VPN、內網、防火牆，尚未核實，不能聲稱外部可直接存取。**

現有 ProjectDetailView Publish/Reopen 有保留 sessionStorage Idempotency-Key 的失敗對帳；Verified Reference 接入必須保留此機制，且將「Approved 已納入模型」的既有文案與「來源 Verified」區分。AnalysisCaseDetailView 現已有對名稱可辨識 DEMO 的提醒，**只能作為疑似標記**。

本次未修改正式前後端或資料庫；下一步須由部署管理員確認真正入口的防護方式、企業用戶與角色來源，據此設計後端授權及 401/403 行為，然後才可開始建立新的核准 API。


## 24. 2026-11-21：永久 CI 收斂、合成 Excel 測試鏈相依修復

使用者本機 `integration/src-closure` 於 `5263`／`5174` 的唯讀 API 預檢實際 **7/7 PASS**；進一步 Docker/SQL/API 唯讀比對 **4/4 PASS**，確認 `127.0.0.1:55433` 的 `bidding2a_graduation_src` 容器，API 與 DB 的 **14 個案件 ID/名稱指紋一致、210 個 Approved 歷史案一致、14 個版本／28 筆 Run 中抽讀 14 筆成功**。這不是新 UI 上傳、重算、發布或實際準確度驗證。

接著首次執行 `prepare_ui_case.py` 時因整合分支缺少 `export_full_project_excel.py` 發生 `ModuleNotFoundError`；後續 `verify-ui-preflight.mjs --excel` 的 `ENOENT` 是檔案尚未產生的連帶結果。**已針對根因**把 main 現有正式 `export_full_project_excel.py` 和 `generate_irregular_excel.py` 加入 feature 分支；新增真正執行 XLSX/JSON 輸出的永久 `test_prepare_ui_case.py`，檢查 test-only、SHA 來源一致、ZIP 可讀和拒絕覆寫。

依使用者明確決策：不再保留一次性、重複的 CI。已整併到 `.github/workflows/2a-build.yml`（backend Release、Parser Regression、Vue Build、Node 唯讀腳本檢查及 Python 產生器契約測試）；**刪除** `.github/workflows/graduation-ui-build.yml`，保留真正有用的 CLI 與 `2a-handoff-review.yml`，不刪使用者未追蹤 Python/JSON 或任何測試資料。使用方法見 README 第 19 節。

此輪沒有 DB/Migration/API/正式估算 Scope 變更。後續：同步 feature 本輪 5 commits、測試 GitHub Actions 實際結果；使用者再跑 `python prepare_ui_case.py` 及 `node verify-ui-preflight.mjs --excel`，後續才開始 UI 建案／兩種異常分析／Run 跨刷新保存。決標正式發布必須另用可回滾測試來源，禁止讓 test holdout 進歷史。


## 25. 2026-11-21：修復 UI 測試資料來源路徑缺失

使用者本機重新同步上一批永久 CI 修改，`python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v` **2/2 PASS**；但 `graduation-src-extreme-v3/manifest.json` **不存在**，先前指令強制帶入 `--source` 導致 `prepare_ui_case.py` 無法產生 XLSX，隨後 `verify-ui-preflight --excel` 的 `ENOENT` 僅是檔案不存在。不可推論 parser/API 或模型出錯，也不能將該次 Excel 檢查記為 PASS。

在 `feature/1d-api-integration` 修正 `tools/graduation-synthetic/prepare_ui_case.py`：新增**離線、無來源資料庫**的預設 UI smoke workbook 模式（不帶 `--source`），輸出有總表、結構體、基礎地下三工作表的合成明細，以及 Excel SHA／逐列數量單價／完整 UI metadata 的 JSON；所有資料明確標註 `SYNTHETIC_UI_ONLY_NOT_HOLDOUT_OR_HISTORY`、`eligible_for_training_history=false`、`is_accuracy_ground_truth=false`，**不依賴 5263/55433，也不准發布成 Approved History**。保留原有明確 `--source` 模式及 SHA 驗證；指定不存在來源時仍然 fail closed。

新增 `test_prepare_ui_case.py` 的**真 CLI 子程序**離線測試、Excel OOXML／明細／數量×單價一致性、來源標記、防重覆輸出和 RC/SRC 切換。更動 commits `de6a4f52fd3510164021216082bff0f21e729042`、`29b2a1d092dd951b097b9dca40a1626f2d9084f6`。僅使用既有永久 `.github/workflows/2a-build.yml`，沒有新 workflow；暫無 .NET、DB、Migration、正式模型或檢測類別變更。

**下一步**：同步兩筆 commit，改用 `python tools/graduation-synthetic/prepare_ui_case.py --output graduation-ui-1121-01.xlsx --truth graduation-ui-1121-01-truth.json`（**不帶 --source**），接著 `node scripts/verify-ui-preflight.mjs --excel graduation-ui-1121-01.xlsx`。唯讀解析成功後才做 UI 寫入＋Run 持久化；歷史發布只能使用可回滾、與 holdout 完全分開的測試庫。不能宣稱新功能 E2E 已成功。
