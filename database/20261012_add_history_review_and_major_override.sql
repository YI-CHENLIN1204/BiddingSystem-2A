BEGIN;

ALTER TABLE project_master
    ADD COLUMN IF NOT EXISTS history_status varchar(20) NOT NULL DEFAULT 'Draft',
    ADD COLUMN IF NOT EXISTS history_approved_at timestamptz NULL,
    ADD COLUMN IF NOT EXISTS history_approved_by varchar(200) NULL;

UPDATE project_master
SET history_status = 'Draft'
WHERE history_status IS NULL
   OR btrim(history_status) = '';

ALTER TABLE project_master
    DROP CONSTRAINT IF EXISTS ck_project_master_history_status;

ALTER TABLE project_master
    ADD CONSTRAINT ck_project_master_history_status
    CHECK (history_status IN ('Draft', 'Approved'));

ALTER TABLE final_bid_item
    ADD COLUMN IF NOT EXISTS major_category_override varchar(100) NULL;

CREATE INDEX IF NOT EXISTS ix_project_master_history_status
    ON project_master(history_status)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ix_final_bid_item_major_category_override
    ON final_bid_item(project_master_id, major_category_override)
    WHERE major_category_override IS NOT NULL
      AND is_ignored = false;

COMMIT;