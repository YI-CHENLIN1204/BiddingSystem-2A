## 2026-11-23 GitHub Actions 實測與隔離權限修正

[Run 45049455364](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45049455364) 已核實，測試提交為 `b3bceb90b2d8775ba7006ac146bed3d0ecffbc27`。本次修改從 `e0109ade56e34f225efd001022ba3745bcbde838` 接續，提交訊息為 `research: enforce isolated audit roles and reconcile acceptance evidence`。

- T04 隔離 EF 換算 PASS。T07 隔離 Draft 讀取、人工映射、Publish、Reopen、重複提交與稽核核對 PASS。兩項保留原本完整驗收門檻。
- T08 隔離建案讀取 30 次，p50 19.04 ms、p95 26.33 ms。正式全流程效能仍待測。
- T09 原 Run FAIL，匿名稽核 HTTP 200。研究 runner 編譯時加入隔離授權，門檻為匿名 401／403、無效憑證 401、一般使用者 403、主管 200 且讀回 T07 稽核紀錄。修正後 Actions 結果待核實。正式角色整合仍待驗收。
- Word 渲染 job PASS，產出 8 頁 PDF 與 PNG。逐頁視覺仍待取得可開啟的頁面。渲染狀態及雜湊見[執行查核](RESEARCH_ACTIONS_RUN_VERIFICATION_2026_11_23.md)。
- 本批範圍是 `research/acceptance-ci/` 與研究交接文件。隔離 runner 使用一次性 PostgreSQL，無 Migration。下一步核實新 Run 角色矩陣並完成八頁視覺判讀。研究算法與 A0 維持既有版本。

## 2026-11-23 T04 安全修正與 T07／T08／T09 補測

基準 `cbf7cbeaae27ea3a8b754bc34cbc0f1ada2c6372`。研究 T04 安全副本通過 23/23，原版同一套為 13/23。由基準的 83 個 GitHub 原始碼 blob 乾淨重建 Parser，0 警告／0 錯誤、78 條 PASS。相同 K5 的 A0-K／B1-K 已補離線選案、群聚、帶寬、單案項與批次計時。隔離資料庫 E2E、API p50/p95 和 Word 視覺仍未執行，正式分支及資料庫未修改。證據、各項驗收狀態與正式移植風險見[本輪補測](../research/2a_synthetic/kde_comparison/t00t11-followup-20261123/README.md)。

## 2026-11-23 T00～T11 優先補測

以 `3eef29061e8ab46a12da62c2a4b390f5031bfdba` 為研究基準。T04 服務 fixture 13 案中 10 通過、3 失敗（零或負係數）。T08 K5 共同可估的 30 案項預測計時完成。T07／T09 解析器既有二進位 78 條 PASS，Python 18/18。隔離 Docker daemon 拒絕存取，未執行資料庫寫入流程或角色測試。Word 通過封裝與嵌圖核對，視覺驗收仍未執行。完整狀態、重現命令及缺少的資料欄位見[補測紀錄](../research/2a_synthetic/kde_comparison/t00t11-acceptance-20261123/README.md)。下輪需先取得隔離 PostgreSQL 或可操作的 Word 渲染器，再補資料與人工驗收。

# T00～T11 原始實驗全完成驗收門檻（2026-11-23）

研究基準 `b38ea147ecd5d7209608e6f344385a99e6cec5ed`；研究分支 `research/kde-comparison-20261122`。狀態依實際證據，不用「已交報告」冒充「全測完成」。最新補測見 [合成 TRAIN DB 唯讀實測](../research/2a_synthetic/kde_comparison/LIVE_TRAIN_AUDIT_2026_11_23_ZH.md)。

| ID | 已取得的證據 | 尚未完成（**全關門需達成**） | 目前 |
|---|---|---|---|
| **T00** 來源／防洩漏／A0重現 | 廠房180案來源及split檢查、固定候選池C# 1272順位／477分位、**正式126 TRAIN／16786明細唯讀一致性 PASS** | 住宅原始CSV及獨立A0重播、正式API SQL候選資料的端到端等價、獨立有時點資料 | **PARTIAL** |
| **T01** 材料字典自動對應 | 合成已見詞與別名/衝突/規格/單位負例、正式67詞 TRAIN 內部一致性 | 正式67詞對獨立未見不同廠商標單、真值人工核對、safe-auto/false-auto與真實spec/工料 | **PARTIAL** |
| **T02** 字典規模學習曲線 | 訓練字典0/10/30/50/67/100/193及新名稱合成擾動，建案macro CI | 真正 train-only 的獨立新來源詞彙，跨住宅/廠房測試；正式67詞因出自合成TRAIN不當盲測 | **PARTIAL** |
| **T03** 人工節省工時 | 100/500/1000列保守/中性/樂觀、首次/攤提與負效益，情境公式完成 | 真人AB/BA交叉計時，至少兩份難度配對標單、人工核錯與重工時間、簽收量測表 | **SCENARIO_ONLY** |
| **T04** 單位與工項安全 | 隔離 EF 換算 PASS（Run 45049455364）；正式 C# 服務合成主檔18個契約、正式DB 11類11單位／0類可換算確認 | 正式 DB 的拒絕/允許實際行為，規格與工料真值、完整上傳與已核准案件流 | **PARTIAL** |
| **T05** A0/A1 KDE | 70%/90%相同8 refs；廠房E1、日期限制、建案bootstrap；採用門檻未通過 | 住宅原始來源獨立 replay、全新獨立工程與真正as-of時間盲測；若要宣稱實務勝出需另驗 | **SYNTHETIC_PASS** |
| **T06** 異常壓力與漏判 | 780筆固定區間擾動、4組同案量價混合、缺/零情境 | 工程師標註的正常/異常真值、跨規格/新場景測試，方能報precision/recall/FPR | **PARTIAL** |
| **T07** Excel與決標回寫 | C# parser回歸、1k/10k/50k列解析測試；隔離 DB Draft→人工映射→Publish→Reopen→Audit PASS | 正式API唯讀回應等價 | **PARTIAL** |
| **T08** 效能穩定性 | 1k/10k/50k parser、字典0/10/100/500、Python KDE微基準；隔離 HTTP 30 次 p50 19.04 ms／p95 26.33 ms | 同工作量比較 A0/B1、正式API整體p50/p95與RSS、上傳＋資料庫端到端 | **PARTIAL** |
| **T09** 回歸／權限／證據 | 既有Python/C#回歸、SHA、CSV互核、GitHub handoff審查；原 Run 匿名稽核 200（FAIL） | 修正後的隔離角色矩陣、完整API/DB隔離測試、角色權限與交接gate、研究只讀展示頁若做則需資安與無proxy驗證 | **PARTIAL** |
| **T10** TF-IDF候選 | 合成章節未見名Top1/Top3、材料TF-IDF候選另列且人工覆核 | 真實未見同義詞與工程師正確標籤、人工實際審查速度；TF-IDF未自動改材料 | **SYNTHETIC_PASS** |
| **T11** 階層分群KDE | B1重建、B2面積消融、E2等K5/8、E3全群比較及敏感度報告 | 作者原始實作/參數無法聲稱exact match；跨真實工程資料與新blind test方能最終勝負 | **SYNTHETIC_PASS** |

**另外：** Word 已由 Actions 渲染為 8 頁 PDF／PNG，逐頁視覺待覆核；5185獨立研究展示頁原計畫屬「有餘力」非T00～T11必備驗證，不應拿UI缺少否定研究實驗結果。是否製作5185另行決定。

## 全完成的必要區分

- `synthetic-phase-complete`：廠房合成E1/E2/E3的封存測試、報告、可追溯輸出已完成，可供**探索性**專題章節引用。
- `original-plan-all-tests-complete`：只有在本表各T00～T11的**全關門條件有實際證據**後才能改PASS。人員/真實資料/獨立資料庫缺席時應如實停在PARTIAL／NOT RUN，不能以合成數據虛構。
- `production-validated`：獨立真實工程、已核准來源時間、正式功能/權限與使用者驗收通過才可標記，與合成phase不同。
- 單位測試正式主檔目前**0種允許類別換算**，這不是 C# 演算法測試FAIL；代表測試「允許跨單位換算」要先在隔離DB設好合法類別與因子，正式DB一律不動。
- main舊交接說126廠房Draft已過時：現場 `history_status=Approved` 126/126，任何重播都應以目前DB狀態並附快照時間為準。

## 下一批可立即執行而不需企業原始資料

1. Codex建立**全新隔離**PostgreSQL/權限資料庫（不可連正式Supabase），跑T07完整流程與T09角色/回歸；有DB寫入前先在測試日誌記錄 DB host 對象為本地隔離。若本機無PostgreSQL，就標`NOT RUN`及明確環境錯誤。
2. 從現有研究原始CSV和產生器做T04負例擴增、T06已完成的邊界補測、T08 A0/B1同硬體/同案/同次數效能；原CSV不要覆寫，採新run_id及SHA。
3. 使用本次唯讀SQL把T00 TRAIN字典/單位一致性加入自動驗收，重跑後用當時 timestamp 防止把舊時點當現況。
4. Word/PDF版面驗收可用 LibreOffice headless或人工Word；完成逐頁截圖/檢查表，不要再改算法結論來迴避排版。

## 需要外部投入才能完成的測試

- **真實／未見建案盲測**：合法取得、去識別化且有工程來源日期及估算時點標單；工程專業核對材料規格與工料scope；保持train/validation/test隔離。不得用合成TRAIN的126案當真實外部test。
- **工程異常真值**：至少一名具工程判讀能力的審查者獨立標出正常／異常／不確定與原因；若標準判讀有歧義需雙人覆核。模板：`research/2a_synthetic/kde_comparison/input_templates/expert_anomaly_labels_TEMPLATE.csv`。
- **真人工時**：至少2組難度相近、無重複工項、匿名化標單；受測人員AB/BA配平；實際設定、對應、覆核及修正秒數與品質驗收。模板：`research/2a_synthetic/kde_comparison/input_templates/human_time_trials_TEMPLATE.csv`。
- **正式資料庫字典**：目前67詞全來自合成 TRAIN 且工項名稱與合成型錄相依；盲測必須由另一來源提供已核定真值，避免資料洩漏。

## 驗收交接要求

對每批產出測試程式/來源SHA/逐案CSV/總結/日期/環境及pass/fail/not_run。**只要有未證明的步驟，不能叫「全部完成」**。每次GitHub修改更新 `docs/AI_HANDOFF.md`、`docs/CURRENT_HANDOFF_2026-11-11.md`、`docs/RESEARCH_TRACEABILITY_2026-11-11.md`、`docs/WORKLOG.md`、`docs/HANDOFF_CHANGE_REVIEWS.jsonl`；驗證 `python scripts/check_handoff_review.py --base <start_sha>`。
