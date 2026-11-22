# 合成標單量價區間壓力測試

固定既有 78 個案項的 70% 區間，僅將標單觀測值乘以固定倍率，兩方法共 780 筆。每行分母為 78，表中方向與擾動相對應。

| 觀測值倍率 | 計數方向 | A0 | A1 |
|---:|---|---:|---:|
| 0.50 | 低於下界 | 78 | 78 |
| 0.75 | 低於下界 | 62 | 60 |
| 1.00 | 區間內 | 45 | 50 |
| 1.25 | 高於上界 | 50 | 51 |
| 1.50 | 高於上界 | 67 | 65 |

同案項衍生情境相依。觀測倍數是合成擾動，工程異常真值仍未知，計數只描述區間反應。

在本目錄執行 `python factory_interval_stress.py evening_results/factory_a0_a1_pairs.csv factory_interval_stress.csv`。[逐筆結果](evening_results/factory_interval_stress.csv)

本環境 Docker daemon 拒絕存取，本機未找到 PostgreSQL 可執行工具。固定候選池的 C# 核對已有紀錄，隔離 API 與決標回寫端到端仍待驗證。
