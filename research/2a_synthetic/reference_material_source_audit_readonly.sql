-- 2A 唯讀聚合：material_id 對應、來源疑似 DEMO 與重複交易（2026-11-12）
-- 先確認連接到正確開發 DB。僅查 metadata 與聚合，不輸出建案／客戶／檔名。
BEGIN READ ONLY;

-- Q5: 材料 ID 的正確名稱與基準單位（主檔資料，非個案）
SELECT m.material_id, m.material_name, u.unit_name AS standard_unit
FROM public.material_master m
LEFT JOIN public.unit_master u ON u.unit_id=m.unit_id
WHERE m.material_id IN (1, 2)
ORDER BY m.material_id;

-- Q6: 只按疑似測試來源名稱聚合件數（不是正式來源鑑真）
-- 來源檔名未填另列「沒有檔案名稱」；不要把非 DEMO 當成已核真實。
SELECT
  CASE
    WHEN p.project_name ~* '(DEMO|TEST|模擬|測試)' OR
         COALESCE(p.source_file_name,'') ~* '(DEMO|TEST|模擬|測試)'
      THEN '疑似展示或測試'
    WHEN p.source_file_name IS NULL OR BTRIM(p.source_file_name)=''
      THEN '缺少來源檔名'
    ELSE '尚未核實來源'
  END AS source_screen_group,
  p.history_status,
  COUNT(DISTINCT p.project_id) AS distinct_projects
FROM public.project_master p
WHERE NOT p.is_deleted
GROUP BY 1,2
ORDER BY 1,2;

-- Q7: 同一 Project + Material 有多筆有效交易的分布；不輸出 ProjectId
WITH material_counts AS (
 SELECT p.project_id, h.material_id, COUNT(*) AS transactions
 FROM public.historical_transaction h
 JOIN public.project_master p ON p.project_id=h.project_master_id
 WHERE p.history_status='Approved'
   AND NOT p.is_deleted AND NOT h.is_deleted AND NOT h.is_outlier
   AND h.actual_quantity>0
 GROUP BY p.project_id,h.material_id
)
SELECT material_id,transactions AS transactions_per_project_material,
       COUNT(*) AS distinct_project_material_groups
FROM material_counts
GROUP BY material_id,transactions
ORDER BY material_id,transactions;

COMMIT;
