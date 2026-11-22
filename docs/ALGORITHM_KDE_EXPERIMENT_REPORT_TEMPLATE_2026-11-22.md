# 加權分位數 vs KDE 測試報告模板（目前空白，尚未實測）

> 研究報告數據必須由研究輸出 CSV/JSON/manifest 產生；禁止預填勝負、杜撰百分比或用示意圖充當結果。

## 摘要
研究問題：固定參考池與權重時 KDE 是否改善合理區間？完整階層分群是否有額外效果？
實際日期／Git SHA／資料集版次與 SHA／輸出 run-id：**待測**。
正式系統 5184／5264／Supabase 前後狀態：**待驗**。
研究結論與採用建議：**待測**。

## 研究方法
- 住宅 210 train／45 validation／45 test；廠房 126／27／27 第二批（須確認來源可讀）。
- A0 正式加權分位；A1 同參考同權重 KDE；A2 同參考等權 KDE；B1 報告階層分群＋KDE；B2 價格面積口徑。
- 主比較同 P15～P85 名義70%；附P5～P95 名義90%。不跨不同單位平均MAE。
- train/validation/test 與候選防洩漏、樣本不足規則、區間計分與原始值來源。

## 結果總表（只由真實程式結果寫入）
| 用途 | 結構 | 項目 | Split | n獨立建案 | 方法 | 相對 Interval Score↓ | Coverage | 相對寬度 | P50 MAPE | 棄答數 | ESS |
|---|---|---|---|---:|---|---:|---:|---:|---:|---:|---:|
| 待測 | 待測 | 待測 | test | 待測 | A0/A1 | 待測 | 待測 | 待測 | 待測 | 待測 | 待測 |

## 報告插圖（真實測試後才生成）
1. 圖1 **模型相對 Interval Score 對照**：figures/01_interval_score.png/svg；說明相同涵蓋率與n。
2. 圖2 **Coverage 與平均區間寬度**：figures/02_coverage_width.png/svg；解讀是否只因放寬界限而多涵蓋。
3. 圖3 **P50 MAPE 分組比較**：figures/03_mape.png/svg；按鋼筋、混凝土、結構價格及結構類型。
4. 圖4 **實際值與估算區間案例圖**：figures/04_actual_vs_bounds.png/svg；不可只選勝出案例。
5. 圖5 **加權分位數與KDE分布曲線**：figures/05_empirical_vs_kde.png/svg；明示例案、帶寬及權重。
6. 圖6 **樣本充足度與棄答**：figures/06_sample_heatmap.png/svg；各群 n。
7. 選作圖7 **配對 Interval Score 改善／退步**：figures/07_paired_delta.png/svg。

圖說統一：「合成資料研究；住宅／廠房、train/validation/test、估算 scope、單位、n、比較方法、名義區間；不代表崴正企業真實估算準確率。」PNG須300 dpi，可貼 Word；SVG 可縮放。每張圖後寫一段解讀，不可只貼圖。

## PASS/FAIL/NOT RUN 與真實測試紀錄
| 驗證項目 | 狀態 | 直接證據 |
|---|---|---|
| A0 正式參考ID與分位數重現 | NOT RUN | |
| manifest/hash/訓練與留出集隔離 | NOT RUN | |
| KDE CDF/分位/同值/極端數值回歸 | NOT RUN | |
| validation固定參數、test只跑凍結方案 | NOT RUN | |
| 70%公平比較與90%敏感度 | NOT RUN | |
| 研究頁無 Supabase/DB 寫入路徑 | NOT RUN | |
| 正式 settings、project/case/run 前後唯讀計數一致 | NOT RUN | |
| 圖表與逐案CSV可重算一致 | NOT RUN | |
| Word報告含正式圖表與圖說 | NOT RUN | |

## 結論（待實驗結果）
逐範圍報有證據的改善與惡化，與預先確定的採用門檻比較；本研究不自動更換正式模型。Precision／Recall／FPR 若缺經核准真值標為「不可識別」。
