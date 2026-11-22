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
