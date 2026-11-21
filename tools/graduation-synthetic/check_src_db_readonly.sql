-- 2A SRC integration DB audit — READ ONLY, PostgreSQL.
-- Run only against the disposable 55433/bidding2a_graduation_src test DB.
-- Never run seed, migration, UPDATE, DELETE, TRUNCATE or volume removal here.
BEGIN TRANSACTION READ ONLY;

SELECT current_database() AS database_name, current_user AS database_user;

SELECT COALESCE(structure_type, '[NULL]') AS structure_type,
       COALESCE(history_status, '[NULL]') AS history_status,
       COUNT(*) AS project_count,
       COUNT(*) FILTER (WHERE is_deleted = FALSE) AS active_project_count,
       COUNT(*) FILTER (WHERE is_deleted = FALSE AND total_floor_area > 0 AND site_area > 0)
         AS active_area_usable_count
FROM project_master
GROUP BY structure_type, history_status
ORDER BY structure_type, history_status;

SELECT COALESCE(p.structure_type, '[NULL]') AS structure_type,
       COUNT(DISTINCT p.project_id) AS projects_with_transactions,
       COUNT(h.transaction_id) AS transaction_rows,
       COUNT(*) FILTER (WHERE h.final_bid_item_id IS NULL) AS rows_without_bid_link
FROM historical_transaction h
LEFT JOIN project_master p ON p.project_id = h.project_master_id
WHERE h.is_deleted = FALSE
GROUP BY p.structure_type
ORDER BY p.structure_type;

SELECT COUNT(*) AS active_src_approved_projects
FROM project_master
WHERE is_deleted = FALSE AND history_status = 'Approved' AND structure_type = 'SRC';

COMMIT;
