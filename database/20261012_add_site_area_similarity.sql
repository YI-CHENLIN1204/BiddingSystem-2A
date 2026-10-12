BEGIN;

ALTER TABLE project_master
    ADD COLUMN IF NOT EXISTS site_area numeric(18,2);

ALTER TABLE analysis_case
    ADD COLUMN IF NOT EXISTS site_area numeric(18,2);

UPDATE project_master
SET site_area = 1
WHERE site_area IS NULL;

UPDATE analysis_case
SET site_area = 1
WHERE site_area IS NULL;

ALTER TABLE project_master
    ALTER COLUMN site_area SET NOT NULL;

ALTER TABLE analysis_case
    ALTER COLUMN site_area SET NOT NULL;

ALTER TABLE project_master
    DROP CONSTRAINT IF EXISTS ck_project_master_site_area_positive;

ALTER TABLE project_master
    ADD CONSTRAINT ck_project_master_site_area_positive
    CHECK (site_area > 0);

ALTER TABLE analysis_case
    DROP CONSTRAINT IF EXISTS ck_analysis_case_site_area_positive;

ALTER TABLE analysis_case
    ADD CONSTRAINT ck_analysis_case_site_area_positive
    CHECK (site_area > 0);

ALTER TABLE estimation_settings
    ADD COLUMN IF NOT EXISTS site_area_weight numeric(10,4) NOT NULL DEFAULT 10,
    ADD COLUMN IF NOT EXISTS site_area_difference_cutoff_ratio numeric(10,4) NOT NULL DEFAULT 0.50;

UPDATE estimation_settings
SET
    site_area_weight = COALESCE(site_area_weight, 10),
    site_area_difference_cutoff_ratio = COALESCE(site_area_difference_cutoff_ratio, 0.50)
WHERE settings_id = 1;

ALTER TABLE estimation_settings
    DROP CONSTRAINT IF EXISTS ck_estimation_site_area_weight_nonnegative;

ALTER TABLE estimation_settings
    ADD CONSTRAINT ck_estimation_site_area_weight_nonnegative
    CHECK (site_area_weight >= 0);

ALTER TABLE estimation_settings
    DROP CONSTRAINT IF EXISTS ck_estimation_site_area_cutoff;

ALTER TABLE estimation_settings
    ADD CONSTRAINT ck_estimation_site_area_cutoff
    CHECK (
        site_area_difference_cutoff_ratio > 0
        AND site_area_difference_cutoff_ratio <= 1
    );

COMMIT;