BEGIN;

-- 開發階段硬重置：移除所有測試/舊流程資料，重新建立乾淨主檔。
TRUNCATE TABLE
    analysis_run,
    analysis_bid_item,
    analysis_bid_version,
    analysis_case,
    historical_transaction,
    final_bid_item,
    project_master,
    quantity_rule_base,
    detection_category_material_map,
    detection_category,
    item_dictionary,
    material_master,
    material_category,
    unit_master,
    unit_category,
    audit_log,
    estimation_settings
RESTART IDENTITY CASCADE;

-- project_master：移除未被目前模型使用的舊欄位，
-- floor_range 也移除，因為可由 floor_count / basement_floor_count 推導。
ALTER TABLE project_master
    DROP COLUMN IF EXISTS floor_range,
    DROP COLUMN IF EXISTS tag,
    DROP COLUMN IF EXISTS address,
    DROP COLUMN IF EXISTS client_name,
    DROP COLUMN IF EXISTS designer_name,
    DROP COLUMN IF EXISTS contractor_name,
    DROP COLUMN IF EXISTS project_status,
    DROP COLUMN IF EXISTS contract_amount,
    DROP COLUMN IF EXISTS expected_finish_date,
    DROP COLUMN IF EXISTS note;

-- historical_transaction：來源追溯統一透過 final_bid_item_id，
-- 移除舊版重複保存的 source_* 欄位。
ALTER TABLE historical_transaction
    DROP COLUMN IF EXISTS source_project_key,
    DROP COLUMN IF EXISTS source_project_name,
    DROP COLUMN IF EXISTS source_sheet_name,
    DROP COLUMN IF EXISTS source_row_number,
    DROP COLUMN IF EXISTS source_item_code,
    DROP COLUMN IF EXISTS source_item_name,
    DROP COLUMN IF EXISTS source_spec,
    DROP COLUMN IF EXISTS source_unit,
    DROP COLUMN IF EXISTS source_amount,
    DROP COLUMN IF EXISTS source_fingerprint,
    DROP COLUMN IF EXISTS imported_at,
    DROP COLUMN IF EXISTS source_file_name;

-- quantity_rule_base：舊版時間權重/損耗率欄位已不再由目前程式使用。
ALTER TABLE quantity_rule_base
    DROP COLUMN IF EXISTS loss_rate,
    DROP COLUMN IF EXISTS short_term_days,
    DROP COLUMN IF EXISTS long_term_days,
    DROP COLUMN IF EXISTS short_term_weight,
    DROP COLUMN IF EXISTS long_term_weight,
    DROP COLUMN IF EXISTS effective_date,
    DROP COLUMN IF EXISTS is_active,
    DROP COLUMN IF EXISTS created_by;

-- 乾淨單位主檔：目前 2A 核心只需要重量、體積，
-- 另外保留「其他」給鋼筋續接器等不可換算工項。
INSERT INTO unit_category (
    category_name,
    allow_conversion,
    is_active,
    sort_order
)
VALUES
    ('重量', true, true, 1),
    ('體積', true, true, 2),
    ('其他', false, true, 3);

INSERT INTO unit_master (
    unit_name,
    unit_category_id,
    to_base_factor,
    is_base_unit,
    is_active,
    sort_order
)
SELECT 'kg', unit_category_id, 1, true, true, 1
FROM unit_category WHERE category_name = '重量';

INSERT INTO unit_master (
    unit_name,
    unit_category_id,
    to_base_factor,
    is_base_unit,
    is_active,
    sort_order
)
SELECT '公斤', unit_category_id, 1, false, true, 2
FROM unit_category WHERE category_name = '重量';

INSERT INTO unit_master (
    unit_name,
    unit_category_id,
    to_base_factor,
    is_base_unit,
    is_active,
    sort_order
)
SELECT 't', unit_category_id, 1000, false, true, 3
FROM unit_category WHERE category_name = '重量';

INSERT INTO unit_master (
    unit_name,
    unit_category_id,
    to_base_factor,
    is_base_unit,
    is_active,
    sort_order
)
SELECT '噸', unit_category_id, 1000, false, true, 4
FROM unit_category WHERE category_name = '重量';

INSERT INTO unit_master (
    unit_name,
    unit_category_id,
    to_base_factor,
    is_base_unit,
    is_active,
    sort_order
)
SELECT '立方公尺', unit_category_id, 1, true, true, 1
FROM unit_category WHERE category_name = '體積';

INSERT INTO unit_master (
    unit_name,
    unit_category_id,
    to_base_factor,
    is_base_unit,
    is_active,
    sort_order
)
SELECT 'm3', unit_category_id, 1, false, true, 2
FROM unit_category WHERE category_name = '體積';

INSERT INTO unit_master (
    unit_name,
    unit_category_id,
    to_base_factor,
    is_base_unit,
    is_active,
    sort_order
)
SELECT '組', unit_category_id, NULL, false, true, 1
FROM unit_category WHERE category_name = '其他';

-- 目前偵錯範圍聚焦結構體工程。
INSERT INTO material_category (
    category_name,
    is_active,
    sort_order
)
VALUES ('結構體工程', true, 1);

-- 標準材料：鋼筋、混凝土為偵錯材料；
-- 鋼筋續接器保留獨立材料，避免被「鋼筋」關鍵字誤併入重量。
INSERT INTO material_master (
    material_name,
    category_id,
    is_active,
    unit_id
)
SELECT
    '鋼筋',
    mc.category_id,
    true,
    um.unit_id
FROM material_category mc
JOIN unit_master um ON um.unit_name = 'kg'
WHERE mc.category_name = '結構體工程';

INSERT INTO material_master (
    material_name,
    category_id,
    is_active,
    unit_id
)
SELECT
    '混凝土',
    mc.category_id,
    true,
    um.unit_id
FROM material_category mc
JOIN unit_master um ON um.unit_name = '立方公尺'
WHERE mc.category_name = '結構體工程';

INSERT INTO material_master (
    material_name,
    category_id,
    is_active,
    unit_id
)
SELECT
    '鋼筋續接器',
    mc.category_id,
    true,
    um.unit_id
FROM material_category mc
JOIN unit_master um ON um.unit_name = '組'
WHERE mc.category_name = '結構體工程';

-- 目前正式偵錯細項只保留鋼筋、混凝土。
INSERT INTO detection_category (
    major_category_name,
    detection_category_name,
    unit_id,
    is_active,
    sort_order
)
SELECT
    '結構體工程',
    '鋼筋',
    unit_id,
    true,
    10
FROM unit_master
WHERE unit_name = 'kg';

INSERT INTO detection_category (
    major_category_name,
    detection_category_name,
    unit_id,
    is_active,
    sort_order
)
SELECT
    '結構體工程',
    '混凝土',
    unit_id,
    true,
    20
FROM unit_master
WHERE unit_name = '立方公尺';

INSERT INTO detection_category_material_map (
    material_id,
    detection_category_id,
    is_active,
    created_at,
    updated_at
)
SELECT
    mm.material_id,
    dc.detection_category_id,
    true,
    now(),
    now()
FROM material_master mm
JOIN detection_category dc
  ON dc.major_category_name = '結構體工程'
 AND dc.detection_category_name = '鋼筋'
WHERE mm.material_name = '鋼筋';

INSERT INTO detection_category_material_map (
    material_id,
    detection_category_id,
    is_active,
    created_at,
    updated_at
)
SELECT
    mm.material_id,
    dc.detection_category_id,
    true,
    now(),
    now()
FROM material_master mm
JOIN detection_category dc
  ON dc.major_category_name = '結構體工程'
 AND dc.detection_category_name = '混凝土'
WHERE mm.material_name = '混凝土';

-- 乾淨材料字典。
INSERT INTO item_dictionary (keyword, material_id)
SELECT '鋼筋續接器', material_id FROM material_master WHERE material_name = '鋼筋續接器';
INSERT INTO item_dictionary (keyword, material_id)
SELECT '續接器', material_id FROM material_master WHERE material_name = '鋼筋續接器';

INSERT INTO item_dictionary (keyword, material_id)
SELECT '高拉力鋼筋', material_id FROM material_master WHERE material_name = '鋼筋';
INSERT INTO item_dictionary (keyword, material_id)
SELECT '竹節鋼筋', material_id FROM material_master WHERE material_name = '鋼筋';
INSERT INTO item_dictionary (keyword, material_id)
SELECT 'SD420W', material_id FROM material_master WHERE material_name = '鋼筋';
INSERT INTO item_dictionary (keyword, material_id)
SELECT 'SD420', material_id FROM material_master WHERE material_name = '鋼筋';
INSERT INTO item_dictionary (keyword, material_id)
SELECT 'SD280', material_id FROM material_master WHERE material_name = '鋼筋';
INSERT INTO item_dictionary (keyword, material_id)
SELECT '鋼筋', material_id FROM material_master WHERE material_name = '鋼筋';

INSERT INTO item_dictionary (keyword, material_id)
SELECT '預拌混凝土', material_id FROM material_master WHERE material_name = '混凝土';
INSERT INTO item_dictionary (keyword, material_id)
SELECT '混凝土', material_id FROM material_master WHERE material_name = '混凝土';

-- 舊估算頁仍使用核心材料表，保留最小必要設定。
INSERT INTO quantity_rule_base (
    material_id,
    project_type,
    structure_type,
    is_core_estimation
)
SELECT material_id, NULL, NULL, true
FROM material_master
WHERE material_name IN ('鋼筋', '混凝土');

-- 模型參數保留目前使用中的設定。
INSERT INTO estimation_settings (
    settings_id,
    top_k,
    minimum_sample_count,
    lower_quantile,
    median_quantile,
    upper_quantile,
    similarity_exponent,
    time_half_life_years,
    minimum_similarity_score,
    structure_weight,
    area_weight,
    floor_weight,
    year_weight,
    city_weight,
    area_difference_cutoff_ratio,
    ground_floor_penalty_ratio,
    basement_floor_penalty_ratio,
    year_difference_cutoff_years,
    updated_at,
    updated_by
)
VALUES (
    1,
    8,
    5,
    0.15,
    0.50,
    0.85,
    2,
    3,
    0,
    25,
    20,
    10,
    0,
    5,
    0.50,
    0.03,
    0.10,
    6,
    now(),
    'System'
);

COMMIT;