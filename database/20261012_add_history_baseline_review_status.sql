ALTER TABLE project_master
    ADD COLUMN IF NOT EXISTS history_status text NOT NULL DEFAULT 'Draft',
    ADD COLUMN IF NOT EXISTS history_approved_at timestamptz,
    ADD COLUMN IF NOT EXISTS history_approved_by text;

UPDATE project_master
SET history_status = 'Draft'
WHERE history_status IS NULL
   OR history_status NOT IN ('Draft', 'Approved');

ALTER TABLE project_master
    DROP CONSTRAINT IF EXISTS ck_project_master_history_status;

ALTER TABLE project_master
    ADD CONSTRAINT ck_project_master_history_status
    CHECK (history_status IN ('Draft', 'Approved'));

CREATE INDEX IF NOT EXISTS ix_project_master_history_status
ON project_master (history_status)
WHERE is_deleted = false;