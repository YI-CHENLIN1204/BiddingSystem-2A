# Factory Fairness V2 最終交付驗收

- [Word](Factory-Fairness-Report-ZH.docx)
- [完整證據包](Factory-Fairness-V2-Evidence.zip)。解壓後在 `figures/` 取得 14 組 300 dpi PNG 與 SVG，在根目錄取得逐案與彙總資料及重現說明。

| 驗收 | 狀態 | 證據 |
|---|---|---|
| 嵌圖與圖說 | PASS | OOXML 關聯及 8 個媒體已核對。 |
| 兩種圖檔格式 | PASS | 14 組 PNG/SVG 與原 manifest 雜湊相符，PNG 解析度欄位為 11,811 像素／公尺。 |
| 證據包 | PASS | ZIP CRC 與 64 個成員清冊核對通過，包含封存逐案輸出與逐檔 SHA256。 |
| 研究結論與範圍 | PASS | E1 比較區間估計器，E2 比較 B1-K 控制變體，E3 比較全群 B1 的研究重建。結論限於合成資料。 |
| Word 自動渲染 | FAIL | 啟動 Word COM 時回報 80070520。 |
| Word 逐頁目視 | NOT RUN | 未產生可目視檢查的頁面。 |
| 重跑 E1／E2／E3、正式 API 與 Supabase | NOT RUN | 依本輪交付範圍沿用封存結果。 |

E2 金額 70% Interval Score 的點估計支持 A0-K，K5 與 K8 共同可估分別為 18 與 15 案。K8 的混凝土和鋼筋各只有 1 個共同可估案。E1 開工日限制版 A1 改善約 3.1%，差異區間跨零，低於原訂 5% 採用門檻。目前無法判定獨立實際工程上的整體勝者。
