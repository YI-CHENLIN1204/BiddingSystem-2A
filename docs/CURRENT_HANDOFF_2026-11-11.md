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
