-- 2A 資料品質「聚合」唯讀查核（2026-11-12）
-- 僅使用已由使用者 information_schema 匯出確認存在的欄位。
-- 不回傳建案名稱、交易明細、檔名、Hash 或個資；不修改資料庫。
-- 請先在 SQL Editor 確認目標為正確的開發資料庫。
BEGIN READ ONLY;

-- Q1. 不同歷史審核狀態及工程用途／結構的獨立建案數
SELECT
  COALESCE(p.history_status, '(NULL)') AS history_status,
  COALESCE(NULLIF(BTRIM(p.project_type), ''), '(missing)') AS project_type,
  COALESCE(NULLIF(BTRIM(p.structure_type), ''), '(missing)') AS structure_type,
  COUNT(DISTINCT p.project_id) AS distinct_projects,
  COUNT(*) FILTER (WHERE p.is_deleted) AS marked_deleted_projects
FROM public.project_master p
GROUP BY 1,2,3
ORDER BY 1,2,3;

-- Q2. Approved、未刪除歷史案的必要資訊與檔案追溯完整性
-- 只列缺漏總數；有來源檔名／Hash 不代表是真實定案資料。
SELECT
  COUNT(*) AS approved_active_projects,
  COUNT(*) FILTER (WHERE p.project_type IS NULL OR BTRIM(p.project_type) = '') AS missing_project_type,
  COUNT(*) FILTER (WHERE p.structure_type IS NULL OR BTRIM(p.structure_type) = '') AS missing_structure_type,
  COUNT(*) FILTER (WHERE p.total_floor_area IS NULL OR p.total_floor_area <= 0) AS invalid_floor_area,
  COUNT(*) FILTER (WHERE p.site_area <= 0) AS invalid_site_area,
  COUNT(*) FILTER (WHERE p.start_date IS NULL) AS missing_start_date,
  COUNT(*) FILTER (WHERE p.source_file_name IS NULL OR BTRIM(p.source_file_name) = '') AS missing_source_filename,
  COUNT(*) FILTER (WHERE p.source_file_hash IS NULL OR BTRIM(p.source_file_hash) = '') AS missing_source_hash,
  COUNT(*) FILTER (WHERE p.history_approved_at IS NULL) AS missing_approved_at,
  COUNT(*) FILTER (WHERE p.history_approved_by IS NULL OR BTRIM(p.history_approved_by) = '') AS missing_approved_by
FROM public.project_master p
WHERE p.history_status = 'Approved' AND NOT p.is_deleted;

-- Q3. 原始歷史材料交易對 Approved、未刪除 ProjectMaster 的連結品質
SELECT
  COUNT(*) AS historical_rows_total,
  COUNT(*) FILTER (WHERE h.is_deleted) AS marked_deleted_rows,
  COUNT(*) FILTER (WHERE h.is_outlier) AS marked_outlier_rows,
  COUNT(*) FILTER (WHERE h.project_master_id IS NULL) AS missing_project_reference,
  COUNT(*) FILTER (WHERE h.final_bid_item_id IS NULL) AS missing_bid_item_reference,
  COUNT(*) FILTER (WHERE h.actual_quantity <= 0) AS nonpositive_quantity_rows,
  COUNT(*) FILTER (WHERE h.project_master_id IS NOT NULL AND p.project_id IS NULL) AS unresolved_project_reference,
  COUNT(*) FILTER (WHERE h.project_master_id IS NOT NULL AND
                         p.project_id IS NOT NULL AND
                         (p.history_status <> 'Approved' OR p.is_deleted)) AS linked_to_nonapproved_or_deleted_project,
  COUNT(*) FILTER (WHERE h.final_bid_item_id IS NOT NULL AND f.final_bid_item_id IS NULL)
    AS unresolved_bid_item_reference,
  COUNT(*) FILTER (WHERE h.final_bid_item_id IS NOT NULL AND f.final_bid_item_id IS NOT NULL
                         AND f.project_master_id IS DISTINCT FROM h.project_master_id)
    AS bid_item_project_mismatch
FROM public.historical_transaction h
LEFT JOIN public.project_master p ON p.project_id = h.project_master_id
LEFT JOIN public.final_bid_item f ON f.final_bid_item_id = h.final_bid_item_id;

-- Q4. Approved、未刪除且有效的歷史用量：各用途／結構／材料之獨立建案數
-- 警告：此查詢只檢查「來源表狀態及量 > 0」，仍未核實來源真實／工程範圍。
SELECT
  COALESCE(NULLIF(BTRIM(p.project_type), ''), '(missing)') AS project_type,
  COALESCE(NULLIF(BTRIM(p.structure_type), ''), '(missing)') AS structure_type,
  h.material_id,
  COUNT(DISTINCT p.project_id) AS distinct_approved_projects,
  COUNT(*) AS historical_rows,
  COUNT(DISTINCT h.final_bid_item_id) AS linked_bid_items
FROM public.historical_transaction h
JOIN public.project_master p ON p.project_id = h.project_master_id
WHERE p.history_status = 'Approved'
  AND NOT p.is_deleted
  AND NOT h.is_deleted
  AND NOT h.is_outlier
  AND h.actual_quantity > 0
GROUP BY 1,2,3
ORDER BY 1,2,3;

COMMIT;
