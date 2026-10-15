-- 僅清除 2026-10-15 模擬展示批次；不會刪除 AnalysisCase／Version／Run。
-- 在 Supabase SQL Editor 執行。先檢查下方 preview 計數，再單獨執行清除交易。
-- 本腳本會永久刪除符合條件的資料，執行前務必確認目前為開發／展示環境。
BEGIN;
DO $$
DECLARE n integer;
BEGIN
  SELECT count(*) INTO n
  FROM public.project_master
  WHERE project_name LIKE '【模擬資料 DEMO_2A_20261015】%'
    AND source_file_name = 'DEMO_2A_20261015_SIMULATED.xlsx'
    AND history_approved_by = 'DEMO_ONLY_NOT_REAL_REVIEW';
  IF n <> 8 THEN
    RAISE EXCEPTION '預期清除 8 筆模擬建案，目前符合 % 筆。已中止。', n;
  END IF;
END $$;

CREATE TEMP TABLE demo_2a_cleanup_ids ON COMMIT DROP AS
SELECT project_id FROM public.project_master
WHERE project_name LIKE '【模擬資料 DEMO_2A_20261015】%'
 AND source_file_name = 'DEMO_2A_20261015_SIMULATED.xlsx'
 AND history_approved_by = 'DEMO_ONLY_NOT_REAL_REVIEW';

DELETE FROM public.historical_transaction
WHERE project_master_id IN (SELECT project_id FROM demo_2a_cleanup_ids);
DELETE FROM public.final_bid_item
WHERE project_master_id IN (SELECT project_id FROM demo_2a_cleanup_ids);
DELETE FROM public.project_master
WHERE project_id IN (SELECT project_id FROM demo_2a_cleanup_ids);
COMMIT;

-- 執行後確認：
-- SELECT count(*) FROM public.project_master WHERE project_name LIKE '%DEMO_2A_20261015%';
