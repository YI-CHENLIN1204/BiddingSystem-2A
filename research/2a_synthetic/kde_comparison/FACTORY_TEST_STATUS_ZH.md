# 廠房實驗完成狀態

2026-11-22。以原晚間測試計畫為準。第一版結案範圍為合成廠房探索性比較。

| 編號 | 狀態 | 證據或缺口 |
|---|---|---|
| T00 | 部分完成 | 廠房來源雜湊核對。固定候選池 1,272 筆選案與權重、477 個分位對照通過。住宅原始快照和隔離 API 尚缺。 |
| T01–T02 | 部分完成 | 已見名稱、字典學習曲線與安全變體完成，陌生名稱自然樣本及正式字典來源尚缺。 |
| T03 | 情境推估完成 | 已計建置、維護及覆核成本。真人計時尚缺。 |
| T04 | 部分完成 | 單位、工程口徑及缺項或零量案例已有離線驗證。 |
| T05 | 廠房完成 | 70% 主分析、90% 敏感度與日期限制比較均已封存。住宅重播尚缺。 |
| T06 | 部分完成 | 780 筆固定區間擾動及 6 個缺項或零量案例完成。工程異常真值、混合擾動尚缺。 |
| T07 | 部分完成 | 解析器回歸通過。隔離資料庫決標發布與重開待驗。 |
| T08 | 部分完成 | 已測離線字典查找。解析器及 KDE 專項計時、記憶體待測。 |
| T09 | 部分完成 | 三份比較 CSV 重跑一致。本輪 Python 18/18 與 21/21，既有 Release 解析器輸出 78 條 PASS 並正常結束。正式資料庫前後計數未重驗。 |
| T10 | 部分完成 | 廠房章節 TF-IDF 只有已見名稱樣本。 |
| T11 | 廠房完成 | B1 依文字重建，B2 面積分母消融完成。 |

本輪以 `python -m unittest discover -s research/2a_synthetic -p test_research.py`、`python -m unittest discover -s tools/graduation-synthetic -p 'test_*.py'`、`dotnet run --no-build -c Release --project tests/WorkbookParserTests` 驗證。最後一項使用現有編譯結果。舊報告的 79 行計數包含最終成功摘要，本表計 78 條以 PASS 開頭的檢查。

端到端驗證需可使用的隔離 PostgreSQL。獨立驗證需可追溯資料及實際可用日期。論文中的估算結論目前限於已封存合成資料。

## 2026-11-22 補充驗證

T06 已完成四組同案量價混合擾動。T08 已完成 Python KDE 批次微基準，完整解析效能及正式 API 尚待驗證。

## 2026-11-22 Excel 解析效能

T08 已補三份完整測試標單的 C# 解析量測，163–167 列，每份 20 輪。大標單與完整 API 負載尚待驗證。詳見 `FACTORY_PARSER_BENCHMARK_ZH.md`。

## 2026-11-22 T08 大規模解析

1,000／10,000／50,000 列各 20 次解析完成，列數一致。詳見 `FACTORY_PARSER_BENCHMARK_ZH.md`。完整 API 負載仍待隔離環境。

## 2026-11-22 B1／B2 金額分母

已補 26 案配對、結構分層 bootstrap。詳見 `FACTORY_DENOMINATOR_BOOTSTRAP_ZH.md`。

## 2026-11-22 T10 未見名稱補測

781 列合成留出測試，Top-1 262、Top-3 553。詳見 `FACTORY_C1_UNSEEN_ZH.md`。

## 原計畫逐項覆核

T00 至 T11 的狀態以本表和各次補測為準。T05 的「廠房完成」指探索性比較已執行。原計畫正式 API 等價門檻尚未通過，所以結果不能升格為完整正式驗證。住宅仍缺原始資料。

T05 補報 70% 主分析的 P50 平均絕對百分比誤差與有效樣本數。26 案 × 3 工項全數成對可估，RC／SC／SRC 每工項為 9／8／9 案。九層等權 P50 MAPE 為 A0 12.080%、A1 12.084%，兩法共用相同權重，平均 ESS 為 7.147／8。KDE 中位數以同一條件分布的 P50 計算。帶寬沿用驗證集選定值。[分層 CSV](evening_results/factory_t05_metrics.csv)

仍有可執行的既有資料項目，包括等權經驗分位基準與部分單位、規格邊界。跨用途、真人計時、隔離資料庫回寫及獨立建案仍各有前置缺口。

## T05 等權基準

70%／90% 各 78 筆已完成。見 `FACTORY_EQUAL_WEIGHT_ZH.md`。

## T04 單位驗證

正式換算服務的 18 項合成主檔案例全通過。詳見 `FACTORY_UNIT_CONTRACT_ZH.md`。

## T08 字典規模補測

已完成 12 組查找測試，500 詞為合成壓力配置，正式 67 詞快照仍待驗證。詳見 `FACTORY_DICTIONARY_SCALE_ZH.md`。

## T10 覆核負擔補測

已記錄前三候選順位及另查比例。詳見 `FACTORY_C1_UNSEEN_ZH.md`。

## T02 訓練字典 67 詞補測

合成測試正確 1742／3386，正式字典來源待核。詳見 `FACTORY_DICTIONARY_REPORT_ZH.md`。

## T01／T02 建案宏平均補測

每案先算正確列／明細列，再等權平均。驗證 27 案、測試 26 案，按建案重抽 2000 次，種子 20261122。測試 50／67／100 詞為 39.86%／53.41%／72.52%，67 詞的 95% 區間為 49.49–57.30%。只涵蓋凍結的合成已見名稱。從研究目錄執行 `python factory_dictionary_macro.py <mapping_per_item.csv> <output.csv>`。機讀結果位於 `evening_results/dictionary_macro.csv`。

## T02 字典擴充同案配對

50→67 與 67→100 詞在 27 驗證案與 26 測試案均逐案改善。測試宏平均分別 +13.55 與 +19.11 個百分點，案級配對 bootstrap 95% 區間分別為 [12.56,14.54]、[17.81,20.48] 個百分點（2,000 次，種子 20261122）。沿用同一凍結資料及巢狀精確名稱字典，結果只適用於已見名稱；未納入正式字典。既有 `factory_dictionary_macro.py` 指令同時產生 `dictionary_paired.csv`。

## T01 材料留出對照

沿用 C1 的訓練材料碼留出與正規化名稱剔除規則。精確與最長包含詞兩法均將 850 驗證列、781 測試列全部轉待審，無自動命中或錯配。自動精確率因零自動命中而無分母。此情境同時留出材料碼與名稱，只支持合成留出下的結果。同材料的新別名與陌生廠商真實資料仍待驗證。

重跑 `python factory_dictionary_unseen.py <source> <output.json>`，機讀結果為 `evening_results/factory_dictionary_unseen.json`。

## T01 同材料新名稱合成測試

固定訓練字典及材料碼，驗證 162、測試 156 筆混凝土／鋼筋來源列。原名稱兩法全對。人工加前綴保留原詞時，最長包含詞全對、精確比對全待審。以預拌砼／鋼質補強筋替換名稱後，兩法全待審。兩類改寫皆為合成，同材料真實廠商別名仍缺。研究目錄執行 `python factory_alias_stress.py <source> <output.csv>`；逐列證據見 `evening_results/factory_alias_stress.csv`。

## T01 字典分層比較

以固定廠房快照重跑零字典、精確名稱、最長包含詞與保守規則，按單位、規格、章節與工料範圍輸出分母、正確、錯配、待審數。測試集純材料 207 列均通過保守規則，含工帶料 3179 列均轉人工。精確及包含詞兩法皆正確對應全部 3386 列材料碼。此結果限於已見合成名稱及指定規則。未驗證正式字典或跨用途。沿用 replay_factory_safety.py 原重跑指令，新增輸出 factory_mapping_strata.csv，已收錄於研究目錄 evening_results。

## T05 上下界漏覆蓋

固定 26 案 × 3 工項。依序列出低於下界、區間內、高於上界的案項數。70% A0 為 9／45／24，A1 為 6／50／22。90% A0 為 2／66／10，A1 為 0／68／10。計數逐筆與原命中欄位一致。此為合成資料逐案項描述，三工項可能同案相依。90% 涵蓋增加全部來自低於下界的案例減少，高於上界仍有 10 筆。

重跑 `python factory_interval_tails.py evening_results/factory_a0_a1_pairs.csv evening_results/factory_a0_a1_90pct_pairs.csv`。

## T05 逐案涵蓋轉換

26 合成廠房、78 案項。70% 區間兩法皆命中 43，僅 A1 命中 7，僅 A0 命中 2，兩法皆漏 26。90% 對應為 65／3／1／9。已有的預測與門檻沿用，此為描述性補充，案項在案內相依。結構體金額 90% 雖兩法各命中 21，仍有一進一出。機讀分項見 `evening_results/factory_coverage_transitions.csv`。重跑 `python factory_coverage_transitions.py evening_results/factory_a0_a1_pairs.csv evening_results/factory_a0_a1_90pct_pairs.csv`。

## T05 涵蓋率與寬度的配對不確定性

26 案、78 案項，三結構各以整案重抽 2,000 次（seed 20261122），結構與工項等權。70% 與 90% 的涵蓋率差各為 +6.48 與 +2.78 個百分點，95% 重抽區間均跨零；相對寬度差各為 +0.02584 與 +0.05939，兩區間均高於零。相對寬度為預測區間寬除以真值。此為同一合成資料的描述性補充。[重抽結果](evening_results/factory_coverage_uncertainty.csv)

## A0／B1 成對區間分數重抽樣

名義 70%，按結構分層，以共同可估建案重抽 2,000 次。B1 減 A0 的區間分數差在混凝土、鋼筋、結構金額各為 +0.228、+0.697、+1.337，95% 區間各為 [0.121,0.320]、[0.592,0.832]、[0.833,2.004]。共同可估案數為 9、9、26；兩個數量結果僅適用於共同可估子集。分數越低越好。這是已檢視合成資料的探索性補充，B1 依報告重建。金額涵蓋 B1 17/26、A0 13/26，採用判斷仍保留 A0。

重跑 `python factory_a0_b1_uncertainty.py evening_results/factory_a0_b1_pairs.csv`。結果存於 `evening_results/factory_a0_b1_uncertainty.csv`。

## A0／B1 的 90% 敏感度分析

以相同測試案項與真值配對。B1 減 A0 的相對區間分數差，混凝土 +0.460（95% 區間 +0.200 至 +0.834）、鋼筋 +0.979（+0.624 至 +1.480）、結構體金額 +1.878（+1.231 至 +2.623）。共同可估案數為 9、9、26。方向與 70% 主分析一致。此補測使用已檢視的合成案。重跑 `python factory_a0_b1_uncertainty.py <factory_b1_pairs.csv> evening_results/factory_a0_a1_90pct_pairs.csv`。結果見研究目錄的 `evening_results/factory_a0_b1_90pct_uncertainty.csv`。

## A0／B2 同面積分母金額比較

26 合成廠房以相同真值配對，兩法均用總樓地板面積。70% 區間相對區間分數 A0 0.460、B2 1.551，差 +1.091（結構分層 2,000 次重抽樣 95% 區間 +0.880 至 +1.312），涵蓋 13/26 與 14/26。90% 差 +0.817（+0.573 至 +1.041），涵蓋 21/26 與 24/26。A0 分數較低的方向在消除分母差異後維持，但兩法仍有選案、權重和區間算法等差異。結果限於已檢視的合成資料。重跑 `python factory_a0_b2_price.py <factory_b1_pairs.csv> evening_results/factory_a0_a1_pairs.csv evening_results/factory_a0_a1_90pct_pairs.csv`。
