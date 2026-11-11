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
