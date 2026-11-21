# 2A 研究證據、版本回溯與實驗追蹤（2026-11-11）

> 目的：新對話不靠模糊記憶，而能用**檔案、命令、Git commit、數據來源、驗證狀態**追回每一步。真實工程的準確率與模擬結果分開報告。

## A. 各證據等級（每次回報必須分類）

- **E1：有使用者完整終端輸出／可重現的結果 CSV／CI 日誌**，可確認程式執行及本輪測試情況。
- **E2：使用者口頭回報「沒有錯誤」**，視為執行未發現錯誤，但不代表結果數值、邊界情況或驗收全部通過。
- **E3：GitHub commit 和程式碼可查**，只確認檔案已提交，不代表執行或效果。
- **E4：研究假設／設計提案／待實作**，不能寫為「完成」。
- DB 查詢皆有「查詢時間」限制，不可假設永久一致；`Approved` 代表系統狀態，不代表可證明材料數量真實、完整、已經過現場驗收。

## B. 研究時間線與可回溯 commit（來源：先前會話操作＋當前分支檔案）

| 時點 | SHA 縮寫 | 交付／目的 | 實際證據 |
|---|---|---|---|
| 2026-10-12～15 | 詳 `docs/WORKLOG.md` | 待偵錯案件、V1/V2/V3、已核准歷史分流、1D 選配、鋼筋／混凝土、結構體價格 | 既有程式與 docs |
| 2026-10-15 | `DEMO_2A_20261015`（資料批次 ID，**不是 SHA**） | 8 件示範歷史案、示範分析，`docs/DEMO_DATA_20261015.md` | 開發演示，不是工程驗證 |
| 2026-11 初 | `36e94c0` | UI 調整「數量偵錯／價格異常」通用名稱 | GitHub 實作 |
| 2026-11-10 | `85bc279`／`318503f` | 常態分布適用性診斷（描述性 CV、偏態；不更動判定）及前端顯示 | 提交，未校準為正式常態模型 |
| 2026-11-10 | `b5b1c5f` | 外部資料研究／公司歷史優先決策記錄 | docs/WORKLOG.md |
| 2026-11-10 | `80fa288` | 全面模型審查（來源、結構、權重、價格基期、分位、回測） | docs/2A_MODEL_AUDIT_2026-11-10.md |
| 2026-11-10 | `053bf749`／`4b7ebc9`／`39df16a`／`a37f800`／`eeeb1e4`／`986065e` | ESS／最大單案權重占比，數量與價格服務及 UI | 程式已提交 |
| 2026-11-10 | `40ff682` | 審查文件補 Run ESS 示例 | DB Run #18/#19 查詢 |
| 2026-11-10 | `7f559e1` | 公開資料集可行性／資料隔離 | docs/2A_PUBLIC_DATASET_FEASIBILITY_2026-11-10.md |
| 2026-11-11 | `94267fb` | `generate.py` 固定 seed 合成六組類型 × 兩材料 | 程式提交 |
| 2026-11-11 | `fd71391` | `compare.py` 小樣本對照 | 程式提交 |
| 2026-11-11 | `e70c73a` | `test_research.py` 初版自動化測試 | 程式提交 |
| 2026-11-11 | `56eab92` | 同建案兩材料共用城市等專案欄位 | 程式修正 |
| 2026-11-11 | `40d169f`／`eac75b8` | 分位數精確索引 bug 修正及回歸測試 | 程式提交 |
| 2026-11-11 | `474fc03` | 模擬研究第一輪結束，.gitignore 排除輸出 | 已拉取／可查 |
| 2026-11-11 | `75bf7d5` | 參考案抽樣改確定性 SHA256，避免固定前幾筆偏差 | 程式提交 |
| 2026-11-11 | `8e6b7b2` | `batch.py` 多種子／情境／樣本數／模型 | 程式提交 |
| 2026-11-11 | `7f37a5e` | 批次穩定性測試 | 程式提交 |
| 2026-11-11 | `3986920` | 批次 README 及第二輪整體提交節點 | **使用者實測** `5760 group runs, 96 summary rows` + 原有 **7 tests OK** |
| 2026-11-11 | `ee81ea0` | `production_weight_review.py` 重現 6 因子、結構 gate 對照 | 程式提交 |
| 2026-11-11 | `98e1757` | 結構 gate、分數、批次可重現等新增測試 | 程式提交 |
| 2026-11-11 | `68e8e45` | 第三輪模擬權重比較文件 | 使用者之後**回報「沒有錯誤」**（未附 CSV 結果） |
| 2026-11-11 | `aa0ec994` | 最新主交接檔 | 本交接工作 |

如 GitHub SHA 檢索與上表摘要有差異，以實際 `git show` 的變更為準，不要猜 commit 內容。GitHub 個別檔案可追歷史與 Blame，未儲存的本機輸出不會出現在 commit。

## C. 已有實驗的重跑與資料位置

先 `git status`，確認目前分支與修改狀態。研究使用 Python 標準函式庫（無需安裝 NumPy）。

```powershell
# 原第二輪，已在 2026-11-11 由使用者完整確認成功：
python research/2a_synthetic/batch.py --first-seed 20261111 --seeds 10 --cases-per-combo 30 --train-sizes 3 5 8
# 原輸出：Simulation-only benchmark completed: 5760 group runs, 96 summary rows

# 第三輪，使用者口頭確認「沒有錯誤」，CSV 尚未分析：
python research/2a_synthetic/production_weight_review.py --first-seed 20261111 --seeds 5 --cases-per-combo 30 --train-sizes 3 5 8

# 測試（原 7/7 有完整成功記錄；新增測試的總數要以最新輸出為準）
python -m unittest discover -s research/2a_synthetic -p "test_*.py" -v
```

本機 `research/2a_synthetic/output/` 是 **gitignore 排除的產生檔**。要新對話精準銜接，請保留或傳入：`batch_summary.csv`、`batch_detail.csv`、`batch_manifest.json`、`production_weight_summary.csv`、`production_weight_detail.csv`、`production_weight_manifest.json`。

**重跑不等於新貢獻**，同 seed 相同程式理應重現結果；只有模型或資料修改才需要重新做全面回歸。

## D. 最重要的錯誤與風險登記（追蹤而不是暗改）

| ID | 風險 | 狀態 | 何時可以結案 |
|---|---|---|---|
| R-01 | DB 裡 Approved 仍含示範案（7 件名稱指向 DEMO） | **未結案** | 正式來源欄位和資料隔離、清理方案經明確驗收 |
| R-02 | 歷史 RC／SC／SRC 只扣相似分數、未強制相容 | **研究中** | 模擬 + 真實不同材料／不同用途可比性審核 |
| R-03 | 權重 25/20/10/10/0/5、TopK 8、半衰期 3 年主觀 | **研究中** | 有留案誤差／敏感度對照與清楚採用理由 |
| R-04 | 資料不足，只有 20 件原始企業案，分類後更少 | **未解** | 至少確認每案每材料完整性與可比性；資料不足時保留人工審核 |
| R-05 | P15～P85 不保證工程合理上下限／校準涵蓋率 | **未解** | 同目標覆蓋率公平比較 + 真實留案檢驗 |
| R-06 | 歷史價額未統一價格基期 | **未解** | 同口徑價格資料與適用指數驗證 |
| R-07 | 模擬數量／虛構企業差異可能自我證實研究結論 | **已標注、持續注意** | 使用多情境／真實資料交叉比較，不把模擬結果當實務準確率 |
| R-08 | 演算法回測可能使用未來年份資料（對稱 abs） | **未解** | 新增時間前推、不看未來案例之 holdout |
| R-09 | 公開資料集真實用途與授權未完成實檔驗收 | **未解** | 下載實檔、欄位／口徑／授權／年份核實 |
| R-10 | 研究 Python 與 .NET 正式公式可能不完全等價 | **待驗證** | 建立相同輸入的等價測試，記錄差異 |
| R-11 | 研究區間定義不同（normal ±1SD vs weighted P15/P85） | **待修** | 同等目標涵蓋率／相同實驗樣本公平比 |
| R-12 | 產學公司資料加權尚未實作 | **待研究** | 企業-only、external-only、部分池化對照，不能任意設倍率 |

## E. 跨對話的可追溯協議

每一輪要在 `docs/WORKLOG.md` 或獨立研究紀錄記錄：
1. 日期、任務目的、欲挑戰的假設與前一輪根據。
2. 變更路徑、commit SHA、精確執行命令、環境／seed。
3. 輸入來源與權利（real-company / external-public / synthetic），含版本／授權、樣本數與條件。
4. 實際輸出檔及 SHA256／成功或失敗（只貼 console 無 CSV，不可自行推斷 MAPE）。
5. 結果（同時記優勢與失敗、區間涵蓋、ESS），與資料生成假設的限制。
6. 接受／拒絕／待驗證決策及可回退 commit，不得只保留最成功的一組。
7. 下一個具體驗證點，避免新對話重做既有的實驗。

若研究報告想宣稱**跨真實工程準確率**，一定要有真實 holdout 和資料完整性審查；模擬實驗再多仍不能替代。

## F. 新對話與環境安全

- 分支固定 `feature/1d-api-integration`；勿擅自合 main。
- 不使用 `git reset --hard`、`git clean -fd` 等破壞性命令，可能有個別未提交修改或本機輸出。
- PostgreSQL／Supabase 查詢需先確認目標專案及 DB；執行研究無需讀取或修改 DB。
- `output/` 有完整可重建資料才可要求使用者重跑；若要復原原始輸出，使用精確 seed+commit。


## G. 2026-11-11 新增 Handoff Guardian 追蹤制度

- Skill：`.agents/skills/waizheng-2a-handoff-guardian/SKILL.md`，識別名 `waizheng-2a-handoff-guardian`。
- 操作指令：`AGENTS.md`；機械檢查器：`scripts/check_handoff_review.py`；審查記錄：`docs/HANDOFF_CHANGE_REVIEWS.jsonl`；GitHub PR gate：`.github/workflows/2a-handoff-review.yml`。
- 每輪必記：起始 SHA、修改路徑、是否更新交接、真實更新文件、原因、實際驗證。GitHub 的每檔提交 SHA 可透過 change_id 與前後版本重建變動。
- **審查紀錄不會自動證明結論正確**；Python script 只檢查 changed_paths 和審查 JSON 新增行的一致性，人工仍要判定交接內容是否準確。PR CI 只在符合事件條件時執行，不涵蓋所有直接 push。
- 當次建立治理文件屬 E3（GitHub 程式已提交）；若未見本機命令或 GitHub Actions log，不得宣稱 E1 gate 已通過。


## H. 2026-11-11 第四輪：本機第三輪權重 CSV 實證報告

- 新增 `research/2a_synthetic/report_weights.py`；新增 `test_research.py` 的「實際彙總轉報告」及「不完整配置拒絕」測試。
- 資料來源是本機 `production_weight_summary.csv`，並要求 manifest 標明 `SIMULATED_RESEARCH_ONLY`；沒有可讀結果檔就不能推論模型表現。
- 明確輸出 `production_weight_report.md`（本機忽略 Git）；可透過固定亂數種子與同一 Git 版本回溯，或由使用者上傳報告／資料。
- 狀態：GitHub 已提交研究工具（E3）；**第三輪實際數值尚未取得／分析，新增測試待本機輸出確認**。不能把這份腳本提交當作真實工程統計結果。


## I. 2026-11-11 第三輪結果收到與第五輪因子拆解

- 使用者正式上傳本機 `production_weight_report.md`（第三輪、72 筆三設定比較）：以報表 24 個已四捨五入的 MAPE 行平均 A 17.10%、B 17.07%、C 16.91%。改善約 0.03／0.19 個百分點；跨結構參考 A 平均 0.18%，B/C 0%；報告記載 B 18/4/2、C 20/4/0 改善／惡化／持平。**這是 E1 的輸出結果文件，不是原始 detail CSV，也不是實際建案工程驗證。**
- 審查報告 `docs/2A_SYNTHETIC_WEIGHT_RESULT_REVIEW_2026-11-11.md` 記錄計算方式、三方案的混雜因素以及因為第三輪同結構樣本充裕而未測出風險。
- 新增 `research/2a_synthetic/factorial_weight_review.py`，擴充 `production_weight_review.py` 的呼叫端可注入六方案設定，不改原 A/B/C CLI；`test_research.py` 新增對照設計與配置完整性測試。
- 新的 2×3 配置能分離硬門檻效果與兩種基地面積權重調整方式，但仍依賴人為合成工程，且同結構資料尚未稀疏化。下一輪要再測少樣本及時間前推。
- **目前驗證狀態**：上輪使用者 PowerShell 11/11 OK，新增第五輪腳本／測試尚未得到用戶本機輸出；不能將 Git commit 當成已實驗通過。


## J. 2026-11-11 第五輪已驗證、第六輪稀疏樣本壓力實驗

- **第五輪（E1、使用者終端完整證據）**：`factorial_weight_review.py` 執行成功 4320 group runs、144 summaries、192 paired rows；`python -m unittest discover -s research/2a_synthetic -p "test_*.py" -v` 實際 13/13 OK；`python scripts/check_handoff_review.py --base 2acf10d` 顯示 9 files / 1 new audit record、handoff updated。原先 2160 的前瞻估算已修正為 4320。
- 尚未取得 `factorial_weight_report.md` 和配對原始 CSV，因此此時**不能**斷言在控制因素後哪種方法誤差較低。
- **第六輪**：新增 `sparse_structure_review.py`，以 0～3 件同結構資料、不同結構候選多的反差場景，評估無 gate 與有 gate 的名義最低 5 件門檻、技術上可算性、跨結構混入、模擬誤差。新增單元測試。此為刻意壓力情境，其他結構參考數量**不是**崴正歷史樣本實況。
- 目前 E3：提交程式與文件；第六輪實際通過與輸出內容待使用者本機證據。不可聲稱樣本稀少時安全放行任何正式異常燈號。


## K. 2026-11-11：第五／六輪本機結果已收到

- 使用者上傳 `factorial_weight_report.md` 與 `sparse_structure_report.md`，屬 E1：可檢視**模擬研究輸出報告**，不是原始 detail CSV，更不是企業實務工程驗證。
- 原始報告要點及限制已固化 `docs/2A_FACTORIAL_SPARSE_RESULTS_2026-11-11.md`。第五輪部分配對改善約 0.1～0.18 MAPE 百分點，但不保證真實增益；第六輪少同結構樣本時雖然無硬門檻顯示 100% 達五筆名義門檻，參考案最高 100% 不同結構，顯示數量門檻與可比性門檻需分開審查。
- **不得宣稱** 73.82%／47.90% 代表崴正真實工程錯誤率；模擬生成器設置結構不同用量基準，研究設計還特意提供大量其他結構候選；報表不含各 seed 完整差異。
- 此次僅完成分析、證據與交接文檔（E3 GitHub commit），沒有變更 C# 生產服務、正式設定或 DB。下一研究方向為可比性／資料資格檢查和跨結構差異敏感度的獨立實驗。


## L. 2026-11-12：可比性／歷史資料來源資格研究

- 使用者提供 2026-11-12 PowerShell 完整輸出，`git merge 705efe3..cedf7df` 成功，`check_handoff_review.py --base 705efe3` 顯示 **Reviewed 6 files with 1 new audit record**；屬 E1 交接機械完整性驗證。
- 新增 `research/2a_synthetic/eligibility_review.py`（研究-only），把來源真實性、同用途、同結構、材料、標準單位、工程範圍與 Approved 審核分開列拒絕原因。
- 所有生成資料都帶 `source_kind=synthetic`；`hypothetical_eligible` 只用於隔離其他規則的研究影響，**不會授予任何假資料正式可用資格**。
- 新增三項測試：全 synthetic 都不得成為生產基準；同案、自身與多錯誤防護；結構限制調整不會繞過來源驗證。**本輪新增的 Python 程式與測試尚未在使用者本機執行；不得記成 E1。**
- 這些研究欄位並不證明真實 DB 有工程範圍或驗證來源欄位；日後若要對接生產，需先唯讀檢查資料表結構、資料品質及使用者授權。


## M. 2026-11-12 第七輪結果與正式資料模型缺口

- **E1 合成研究輸出**：使用者上傳 `eligibility_report.md`，35 筆候選、hypothetical_eligible=1、production_eligible=0，25 筆 `wrong_structure`、35 筆 `unverified_source`；不是資料庫實際來源查核。
- **E3 靜態欄位對照**：查閱現行分支 `崴正/Models/ProjectMaster.cs`、`FinalBidItem.cs`、`HistoricalTransaction.cs`、`MaterialMaster.cs`、`UnitMaster.cs`、`崴正/Data/ApplicationDbContext.cs`；結果在 `docs/2A_REFERENCE_ELIGIBILITY_MAPPING_2026-11-12.md`。
- `source_kind` 與 `scope_quality` 是**模擬研究欄位**；在已讀實際 ProjectMaster 等模型中未見等價的可靠資格標記，不能斷言 DB 絕無其他欄位。
- 新增 `research/2a_synthetic/reference_schema_readonly.sql` 供唯讀檢查資料庫 schema；**尚未實際連線執行，不可稱為 DB 驗證通過**。本輪無 C#、DB、正式參數變更。


## N. 2026-11-12：真實六張表 86 欄 Schema 證據

- **E1（使用者本機 SQL editor 輸出）**：上傳 `information_schema.columns` JSON，逐筆包含 table_name、column_name、data_type、is_nullable；實際 86 列，涵蓋 `project_master` 17、`final_bid_item` 27、`historical_transaction` 8、`material_master` 5、`unit_master` 7、`estimation_settings` 22。這是 Schema 存在性驗證，**不是資料列或資料來源的證實**。
- **特別缺口**：六張表內沒有正式來源真實性證明／工程範圍完整性的專用欄位；`ProjectMaster.HistoryStatus=Approved` 不代表完成實體標單鑑真。ProjectMaster 類型、結構、樓地板面積 nullable；HistoricalTransaction 對 ProjectMaster/FinalBidItem 的參考可 null。
- 建立 `docs/2A_VERIFIED_DB_SCHEMA_GAPS_2026-11-12.md` 保存經核對的資料表與限制；建立 `reference_quality_counts_readonly.sql` 作為下一步**未執行**的聚合式歷史品質查核。查詢不得讀取個別建案、檔名、hash、標單內容。
- 過去文件中的 2026-10-15「0 案」為當時快照；**本輪沒有 COUNT 查核，不得沿用成今日結論**。此輪只修改 research/docs，不涉及正式資料庫 DML/DDL。

## O. 2026-11-12：Q4 獨立 Approved 材料建案數（使用者實際 DB 聚合）

E1：使用者貼出 Q4 兩列：住宅 RC、material_id=1：8 獨立建案／11 交易／11 linked bid items；material_id=2：8 獨立建案／10 交易／10 linked bid items。SQL 限定 Approved、未刪除、非 Outlier、正數量。**未能確認來源真實、各工項外鍵品質或兩個 material ID 的名稱**；其他用途/結構未出現在這份有效歷史材料 Q4 聚合。Q1～Q3 結果尚未收到，不能當成已檢查。

新建 `docs/2A_APPROVED_MATERIAL_COHORT_2026-11-12.md` 與只查聚合、不揭露檔名和建案名稱的 `research/2a_synthetic/reference_material_source_audit_readonly.sql`。只提交未執行；先前 DEMO Approved 風險仍未結案。


## P. 2026-11-12：Q1–Q4 真實資料品質完整證據

E1（使用者直接貼出 SQL 四組聚合）：Q1 10 件住宅 RC（8 Approved、2 Draft，未刪除皆 0）；Q2 8 Approved 中 **7 件缺 source_file_hash**，其他被查欄位缺漏均為 0；Q3 23 歷史交易，標記刪除 2、連到非 Approved／已刪除建案 2，全部 FK 有值且有效、工項不跨案；Q4 材料 ID=1／2 各涵蓋 8 個獨立住宅 RC Approved 專案，分別 11／10 筆有效交易。**Q3 兩組 2 件可能重疊，尚未證實交集。**

歸檔 `docs/2A_HISTORICAL_DATA_QUALITY_Q1_Q4_2026-11-12.md`；補強 `reference_material_source_audit_readonly.sql` Q8，供同一 SQL Editor 唯讀交叉檢查。仍未取得 Q5～Q8 輸出、material_id 對應名、正式來源證明、工程範圍認證、實際可用真實公司歷史案數。未動 C# 或 DB。不得把 DB Approved 數推論成真實材料資料集。


## Q. 2026-11-12：Q5～Q8 使用者真實 DB 聚合完成

E1：Q5 確認材料 1=鋼筋 kg、2=混凝土 立方公尺；Q6 8 Approved 中 7 件名稱疑似展示／測試，1 件未核實；2 Draft 中 1 疑似、1 未核實；Q7 鋼筋 7×1+1×4=11 行、混凝土 7×1+1×3=10 行，與 Q4 一致；Q8 刪除的 2 行恰好都是連非 Approved/刪除建案，**不存在四筆不同異常**。Q2 7件缺hash與Q6 7件疑似Demo交集仍未知。詳細數據固化於 `docs/2A_HISTORICAL_DATA_QUALITY_Q5_Q8_2026-11-12.md`。

**來源認證現況**：此輪未取得原始完整定案文件、真實工程範圍核證，故「目前已證實真實完整的來源數」為 0 件，但不能推論只有 0 件真案；單憑檔名不能確認是假案。Q1～Q8 數據盤點已完成，下一步是來源驗證流程設計，不再盲目微調權重。


## R. 2026-11-12：Verified Reference 規格草案（設計 E3，非實作）

- 使用者已確認上輪 GitHub 更新／Guardian 命令「沒有錯誤」；這是操作回報，並未提供另外完整終端截圖，不能當做真實 DB 來源人工審核證明。
- `docs/2A_VERIFIED_REFERENCE_DESIGN_V1_2026-11-12.md` 新增獨立來源資格與 per-purpose（鋼筋／混凝土／結構體價格）審核設計，Draft/Approved 業務發布流程不變，舊 Approved 暫不取得 Verified Reference。狀態、角色、稽核、重開、撤銷、API、資料模型與 VR-01～VR-14 測試均屬**提案**。
- 依據：Q1～Q8 真實 DB 聚合，8 Approved 但 7 名稱疑似展示、1 來源尚未核實；不得由此斷言已有 0 件真案，只能說已提供證據中尚無完成鑑真的真實完整標單。不能據此擅改生產查詢。
- 研究下一步：對照最新 C# Published History／權限／模型查詢實際實作，建立變更影響清單並由業務窗口確認；這批只改文件，無程式／DB／模型驗證。


## S. 2026-11-12：Verified Reference 接入路徑審查（E3 靜態證據）

已直接閱讀目前分支後端程式碼，詳細文件 `docs/2A_VERIFIED_REFERENCE_INTEGRATION_AUDIT_2026-11-12.md`。有兩個需先處理的 P0：`Program.cs` 無內建 Authentication/Authorization 中介設定且 Projects/Audit 等敏感 Controller 無 `[Authorize]`；ProjectSimilarityService 以 int ProjectId 取候選的 overload 缺 Approved 限制，而 DTO overload 有 Approved。需辨別一般展示與正式模型使用，不能盲目改邏輯。

另外證實 Publish/Reopen 已採交易與冪等識別，數量模型依 ProjectId 聚合合格交易，價格模型依 FinalBidItem 工程大項彙總，正式最少樣本依 EstimationSettings 判定。**本輪靜態查核，不等於實際部署安全驗證、執行測試或新 Verified 功能上線**。下一步先完成部署身份機制與前端接入點清單、業務審核政策核准。


## T. 2026-11-13：前端與 Nginx 靜態入口查核

E3：從 GitHub 開發分支讀取 `frontend/src/services/api.ts`、`frontend/src/router/index.ts`、`frontend/src/App.vue`、`ProjectDetailView.vue`、`ProjectsView.vue`、`AuditLogView.vue`、`AnalysisCaseDetailView.vue`、`frontend/nginx.conf`、`frontend/Dockerfile`、`崴正/Program.cs`。分析已收錄 `docs/2A_VERIFIED_REFERENCE_FRONTEND_SECURITY_AUDIT_2026-11-13.md`。

核對到：Vue 無角色/登入 route guard，側欄直接可見 AuditLogs；`api.ts` 未設 token，Nginx 配置只有 proxy_pass 而無 auth_request／allow/deny；ProjectDetailView 使用 Idempotency-Key + receipt 防失敗重送；分析頁已做 DEMO 名稱警告。部署主機、外部 Gateway/VPN/ACL 不在 GitHub 範圍，**本輪沒有做實際對外 API 測試、登入測試或建置測試**，不得推論正式服務一定對公眾可達。Verified Reference 仍為未核定設計，未做 API/DB 變更。


## U. 2026-11-21：UI 資料鏈相依修復與測試可信度界線

- **E1 使用者實測：** 7/7 5263／5174 API 預檢 PASS，4/4 Docker/SQL/API 唯讀比對 PASS，14 個案件、14 個版本、28 筆 Run、210 個歷史建案；只證實可查與讀回，未證實新 UI 上傳、模型結果準確或真實公司資料資格。
- **E1 失敗紀錄：** `prepare_ui_case.py` 執行遇到 `ModuleNotFoundError: No module named 'export_full_project_excel'`；後續 `ENOENT` 為沒有產生 XLSX 的結果。保留失敗證據，不應算 parser 缺陷。
- **E3 程式修正：** 本分支加入 main 已有來源驗證與 test-only XLSX 產生器及相依的 OOXML 產生器（`97558ce`、`3235696`），並建立 `test_prepare_ui_case.py`（`0aca507`）驗證 XLSX/JSON 產出、來源完整性、test-only 及拒絕覆寫。
- **E3 CI 治理：** 更新 `2a-build.yml`（`9f68b3a`），刪除重複的 `graduation-ui-build.yml`（`71738d9`），不修改量價模型或 DB。**CI 結果須查 Actions 實際狀態，不得只憑提交宣稱通過。**
- **未測：** 本地重新產出新 Excel、透過 UI 逐筆上傳／生成 AnalysisRun、正式歷史回寫與 Audit；不得把 holdout 加入 Approved 當新訓練歷史。後續準確度仍待獨立真值。


## V. 2026-11-21：UI 測試標單來源缺失的正式消除方案

- **E1 失敗證據**：使用者 Windows `test_prepare_ui_case.py` 2/2 PASS，產生器因 `graduation-src-extreme-v3/manifest.json` 不存在而失敗；預檢的 `ENOENT` 為連鎖錯誤，Excel 真正預覽未執行。
- **E3 提交修正**：`prepare_ui_case.py` 新增省略 `--source` 的獨立 UI smoke 工作簿，完全不需要大型來源 CSV/manifest，並保留原 SHA/test split 模式。資料包含示意鋼筋、混凝土、模板、鋼骨與基礎工項，數量／單價只是操作測試值，**不參與模型訓練，也不是獨立 holdout 的準確度真值**。
- **E3 永久回歸**：`test_prepare_ui_case.py` 新增真正 subprocess 執行 CLI 的測試和 XLSX SHA/ZIP、逐列 amount 及 metadata 安全條款；利用既有 `2a-build.yml`，不再新增臨時 CI。
- **未驗證**：使用者本機修正版匯出 Excel／HTTP preview+inspect、瀏覽器建立案件與 Run、決標回寫／Audit。


## W. 2026-11-21：Windows Python 子程序 CP950 與 UI 實際 Excel 9/9

- **E1 使用者實測 PASS**：已建 `graduation-ui-smoke-1121-02.xlsx`＋truth JSON，SRC 3 sheets、15 工項。5174/5263 唯讀 API 實際 9/9 PASS，Excel preview 3 sheets (2 detail)、inspect 15 rows/4 warnings；原有 4/4 隔離 DB 14 案／28 runs 已另有實際證據。**不可**把 9/9 當作 UI 真實上傳、審核或模型準確率。
- **E1 使用者實測 FAIL**：Python 3.12 Windows `test_offline_standalone_ui_fixture_without_source_dataset`，子程序使用 CP950 而父用 UTF-8 解碼，發生 `UnicodeDecodeError` 及 `TypeError`，3 項測試中 2 PASS / 1 ERROR。錯誤與已成功生成的 Excel/API 無因果。
- **E3 代碼修正**：`test_prepare_ui_case.py` 對 subprocess child 顯式使用 `PYTHONIOENCODING=utf-8` commit `79af4b822`，父 decoder UTF-8 不變；`verify-ui-state.mjs` 擴充 `--case-id` 嚴格 synthetic case + same-version Quantity/MajorPrice Runs 與 payload GET，commit `d6b3fb146`。兩者僅修改永久驗證工具，零 DB schema、model、授權/標準範圍變更。
- **仍待**：Windows Python 回歸 PASS、瀏覽器實際自行上傳並保存 2 Runs、跨刷新成功、決標審核與稽核 E2E。4 warning rows 必須讀實際警告內容，不臆測工程缺陷。


## X. 2026-11-21：Case #15 UI 成功與工項排除統計對照
- **E1 使用者截圖**：17? 不採猜測。正式證據為 `/analysis-cases/15` 的 V1 自行上傳 **15 條**、價格分析 `Run #31` 已顯示保存成功，實際大項總價 **29,268,000 元**／每坪 **8,062.81**，相對人工合成歷史區間顯示 Low；數量分析顯示鋼筋 **38,000kg**／混凝土 **3,650m³** 並列出各 8 個參考案。尚未用 `verify-ui-state --case-id 15` 對照 DB 保存 payload，故「API 重新讀回 5/5」**未驗證**。
- **E3 原始 fixture／分類程式核對**：結構體原始鋼筋共 130,000kg，混凝土共 5,450m³；`ClassifyQuantityCandidate` 將前 3 條鋼筋加工、組立、綁紮／工資與第一條預拌混凝土澆置標為 `Excluded`，合計被排除 92,000kg、1,800m³，符合 preflight 的 4 警示；量估納入 38,000kg/3,650m³。價估計所有結構體 10 行共 29,268,000 元，與畫面一致。只是關鍵字分類策略，**不代表已證實複合項目無材料，也不代表工程數量/價錢推估有外部真實有效性**。
- 下個驗證點：對已存在 #15，執行永久 script `node scripts/verify-ui-state.mjs --case-id 15`（read-only），再按歷次偵錯查看刷新保存；分類邊界／排除理由 UI 顯示列為待查，不改模型閾值或資料庫。
