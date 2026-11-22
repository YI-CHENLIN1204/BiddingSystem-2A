# 廠房估算方法比較實驗紀錄

2026-11-22。合成廠房 126 件訓練、27 件驗證、26 件主測試。排除先前診斷的 FAC-20261122-SC-0052。沿用 [B1 凍結設定](factory_b1_freeze.json) 與既有 B1 結果。

| 比較 | 區間 | 主要結果 | 逐案紀錄 |
|---|---:|---|---|
| A0 對 B1 | 70% | 混凝土與鋼筋可估案數各為 26 對 9，金額皆為 26。共同可估案件中，A0 三項的平均相對區間分數皆較低，金額命中為 13 對 17。 | [CSV（含驗證及測試集）](evening_results/factory_a0_b1_pairs.csv) |
| A0 對 A1 | 70% | 宏平均區間分數 0.5302 → 0.5026，涵蓋率 58.3% → 64.8%，寬度增加 7.1%。成對差 95% bootstrap 區間 −0.071 至 +0.014。 | [CSV（78 筆測試案項）](evening_results/factory_a0_a1_pairs.csv) |
| A0 對 A1 | 90% | 宏平均區間分數 0.6943 → 0.6664，涵蓋率 84.9% → 87.7%，寬度增加 11.2%。成對差 95% bootstrap 區間 −0.111 至 +0.035。 | [CSV（78 筆測試案項）](evening_results/factory_a0_a1_90pct_pairs.csv) |

A0 是依正式程式與參數快照獨立重播的近似基線，尚待正式 API 逐案驗證。A1 共用 A0 參考案、權重與面積，以驗證集挑選高斯 KDE 帶寬係數 0.125，兩種名義區間沿用同一係數。A0 對 A1 屬探索性消融，結果未達既定採用門檻。B1 屬外部報告重建，保留原作者未公開參數的限制。研究結果適用於這批合成案的回溯比較。

## 重跑順序

1. 使用 [廠房來源與重建說明](FACTORY_EVENING_RESULTS_ZH.md) 執行 B1 並取得逐案 CSV。
2. 執行 `python replay_factory_a1.py <廠房來源目錄> <B1輸出目錄> .3`，同時產出 A0／B1 與 70% A0／A1 逐案檔。
3. 將最後參數改為 `.1` 產出 90% 逐案檔。

## T05 上下界漏覆蓋

固定 26 案 × 3 工項。依序列出低於下界、區間內、高於上界的案項數。70% A0 為 9／45／24，A1 為 6／50／22。90% A0 為 2／66／10，A1 為 0／68／10。計數逐筆與原命中欄位一致。此為合成資料逐案項描述，三工項可能同案相依。90% 涵蓋增加全部來自低於下界的案例減少，高於上界仍有 10 筆。

重跑 `python factory_interval_tails.py evening_results/factory_a0_a1_pairs.csv evening_results/factory_a0_a1_90pct_pairs.csv`。

## T05 逐案涵蓋轉換

26 合成廠房、78 案項。70% 區間兩法皆命中 43，僅 A1 命中 7，僅 A0 命中 2，兩法皆漏 26。90% 對應為 65／3／1／9。已有的預測與門檻沿用，此為描述性補充，案項在案內相依。結構體金額 90% 雖兩法各命中 21，仍有一進一出。機讀分項見 `evening_results/factory_coverage_transitions.csv`。重跑 `python factory_coverage_transitions.py evening_results/factory_a0_a1_pairs.csv evening_results/factory_a0_a1_90pct_pairs.csv`。

## T05 涵蓋率與寬度的配對不確定性

26 案、78 案項，三結構各以整案重抽 2,000 次（seed 20261122），結構與工項等權。70% 與 90% 的涵蓋率差各為 +6.48 與 +2.78 個百分點，95% 重抽區間均跨零；相對寬度差各為 +0.02584 與 +0.05939，兩區間均高於零。相對寬度為預測區間寬除以真值。此為同一合成資料的描述性補充。[重抽結果](evening_results/factory_coverage_uncertainty.csv)

## T05 逐案留一敏感度

固定 70% 主分析預測，輪流移除 26 件測試案各一件並重算九層等權分數。KDE 的相對改善落在 2.20%–6.94%，26 次均為正，16 次達 5% 點估計門檻。這是已檢視資料的影響度診斷，仍以原定 bootstrap 與採用門檻判斷，維持 A0。研究目錄執行 `python factory_leave_one_out.py evening_results/factory_a0_a1_pairs.csv`。輸出封存於 `evening_results/factory_leave_one_out.csv`。

## A0／B1 成對區間分數重抽樣

名義 70%，按結構分層，以共同可估建案重抽 2,000 次。B1 減 A0 的區間分數差在混凝土、鋼筋、結構金額各為 +0.228、+0.697、+1.337，95% 區間各為 [0.121,0.320]、[0.592,0.832]、[0.833,2.004]。共同可估案數為 9、9、26；兩個數量結果僅適用於共同可估子集。分數越低越好。這是已檢視合成資料的探索性補充，B1 依報告重建。金額涵蓋 B1 17/26、A0 13/26，採用判斷仍保留 A0。

重跑 `python factory_a0_b1_uncertainty.py evening_results/factory_a0_b1_pairs.csv`。結果存於 `evening_results/factory_a0_b1_uncertainty.csv`。

## A0／B1 的 90% 敏感度分析

以相同測試案項與真值配對。B1 減 A0 的相對區間分數差，混凝土 +0.460（95% 區間 +0.200 至 +0.834）、鋼筋 +0.979（+0.624 至 +1.480）、結構體金額 +1.878（+1.231 至 +2.623）。共同可估案數為 9、9、26。方向與 70% 主分析一致。此補測使用已檢視的合成案。重跑 `python factory_a0_b1_uncertainty.py <factory_b1_pairs.csv> evening_results/factory_a0_a1_90pct_pairs.csv`。結果見研究目錄的 `evening_results/factory_a0_b1_90pct_uncertainty.csv`。

## A0／B2 同面積分母金額比較

26 合成廠房以相同真值配對，兩法均用總樓地板面積。70% 區間相對區間分數 A0 0.460、B2 1.551，差 +1.091（結構分層 2,000 次重抽樣 95% 區間 +0.880 至 +1.312），涵蓋 13/26 與 14/26。90% 差 +0.817（+0.573 至 +1.041），涵蓋 21/26 與 24/26。A0 分數較低的方向在消除分母差異後維持，但兩法仍有選案、權重和區間算法等差異。結果限於已檢視的合成資料。重跑 `python factory_a0_b2_price.py <factory_b1_pairs.csv> evening_results/factory_a0_a1_pairs.csv evening_results/factory_a0_a1_90pct_pairs.csv`。

## T05 A2 等權 KDE 次要比較

A2 保留 A0 選案與面積口徑，固定 A1 的帶寬係數 0.125，把權重替換為等權。78 案項全可估。70% 九層等權區間分數／涵蓋／相對寬度為 0.4900／68.83%／0.3832；90% 為 0.6777／88.89%／0.5981。70% A2 點估計低於 A0 與 A1，90% A1 分數低於 A2。A2 是同資料次要分析，未作獨立選參或採用門檻檢驗。重跑將 A1 入口改為 `factory_a2.py`，輸出 `factory_a2_70.csv` 與 `factory_a2_90.csv`。
