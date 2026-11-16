# 2A 資料覆蓋率與估算樣本診斷（2026-11-17）

> 僅含彙總數字，不含企業原始標單、建案名稱、單價或連線憑證。資料來源：已連接之 Supabase PostgreSQL，2026-11-17 唯讀 SELECT；程式碼依 GitHub main。此文件是**診斷基線**，非功能驗收或準確率報告。

## 1. 資料庫彙總結果

| 指標 | 結果 |
|---|---:|
| 建案總數／未刪除建案 | 10 / 10 |
| 未刪除且具完整主要建案特徵（類型、結構、縣市、面積、樓層、地下樓層、日期） | 10 |
| 具至少一筆初步有效歷史成交紀錄的建案 | 8 |
| 歷史成交紀錄總數 | 23 |
| 未刪除、未標記異常、數量 > 0、具有建案連結的紀錄 | 21 |
| 初步有效紀錄涉及的材料種類 | 2 |
| 材料 ID 1：有效交易／獨立建案 | 11 / 8 |
| 材料 ID 2：有效交易／獨立建案 | 10 / 8 |
| 歷史交易標記刪除／標記 Outlier／無建案連結／非正數 | 2 / 0 / 0 / 0 |

所有未刪除的建案目前 `project_type` 均為「住宅」；因此目前未觀察到跨建案類型的有效樣本分散，但尚未分析實際建案相似度得分。

## 2. 現行資料庫設定（settings_id=1）

- TopK = 8
- MinimumSampleCount = 5
- MinimumSimilarityScore = 0
- Lower / Median / Upper quantile = 0.15 / 0.50 / 0.85
- TimeHalfLifeYears = 3

## 3. 依程式碼確認的實際限制

- `崴正/Services/ProjectSimilarityService.cs`：候選建案需未刪除且與目標 `ProjectType` 相同，之後套用相似度門檻。
- `崴正/Services/RangeEstimationService.cs`：歷史資料過濾 `IsDeleted`、`IsOutlier`，以 `(MaterialId, ProjectId)` 彙總，再檢查正面積、建案日期、正數量；依相似度排序後取 `TopK`；不足 `MinimumSampleCount` 時回報樣本不足。
- 目前 **TopK=8**，所以增加總建案數不會自動增加每次估算的參考數量。
- **8 個初步有效建案不等於每個目標的最終參考建案數**；仍需用現行服務逐案回測並檢查相似度、權重及資料排除情形。

## 4. 可重現的唯讀查詢

```sql
SELECT
  (SELECT COUNT(*) FROM public.project_master) AS project_count,
  (SELECT COUNT(*) FROM public.project_master WHERE NOT is_deleted) AS active_project_count,
  (SELECT COUNT(*) FROM public.historical_transaction) AS transaction_count,
  (SELECT COUNT(*) FROM public.historical_transaction
   WHERE NOT is_deleted AND NOT is_outlier AND actual_quantity > 0
     AND project_master_id IS NOT NULL) AS prelim_valid_transactions;

SELECT h.material_id, COUNT(*) AS valid_transactions,
       COUNT(DISTINCT h.project_master_id) AS valid_projects
FROM public.historical_transaction AS h
WHERE NOT h.is_deleted AND NOT h.is_outlier
  AND h.actual_quantity > 0 AND h.project_master_id IS NOT NULL
GROUP BY h.material_id ORDER BY valid_projects DESC;

SELECT top_k, minimum_sample_count, minimum_similarity_score,
       lower_quantile, median_quantile, upper_quantile, time_half_life_years
FROM public.estimation_settings WHERE settings_id = 1;
```

## 5. 尚未驗證與下一個驗證點

1. 逐案留一回測：將每個已知住宅建案作為測試目標，排除自己後計算實際可用參考建案數，避免資料洩漏。
2. 量測候選建案數 → 相似度過濾後 → 具材料交易 → 有效權重 → TopK 的逐段流失比例。
3. 計算加權有效樣本數 `(sum(w))^2 / sum(w^2)`，不可只用資料筆數代表可信度。
4. 將 P15–P85 涵蓋率、區間寬度、材料覆蓋率分開報告；僅 8 個具交易建案，留一回測之估計不確定性很高。
5. 不得把生成資料混入正式企業驗證；資料來源標記與環境隔離尚待實作。
6. 此次僅資料查詢和文件整理；**未執行應用程式編譯、E2E、回測，也未變更資料庫或演算法**。
