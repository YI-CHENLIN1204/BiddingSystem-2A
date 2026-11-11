-- 2A 參考資料資格：唯讀結構查核（2026-11-12）
-- 僅讀取 PostgreSQL metadata；不更新資料、不刪資料、不查詢個案內容。
-- 請先確認 SQL editor 連線目標是正確的開發資料庫。
-- 可從 Supabase SQL Editor 執行並匯出欄位清單，避免貼出連線密碼。
BEGIN READ ONLY;

SELECT
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name IN (
    'project_master',
    'final_bid_item',
    'historical_transaction',
    'material_master',
    'unit_master',
    'estimation_settings'
  )
ORDER BY table_name, ordinal_position;

COMMIT;
