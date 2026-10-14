BEGIN;

-- 2A 需要辨識 1D 常見來源單位：
-- KG -> 已有 kg
-- T  -> 新增 t
-- M2 -> 新增 m2
-- M3 -> 新增體積類別與 m3

INSERT INTO unit_master (
    unit_name,
    unit_category_id,
    to_base_factor,
    is_base_unit,
    is_active,
    sort_order
)
SELECT
    't',
    uc.unit_category_id,
    1,
    false,
    true,
    COALESCE((
        SELECT max(um.sort_order)
        FROM unit_master um
        WHERE um.unit_category_id = uc.unit_category_id
    ), 0) + 1
FROM unit_category uc
WHERE uc.category_name = '重量'
  AND NOT EXISTS (
      SELECT 1
      FROM unit_master um
      WHERE lower(um.unit_name) = 't'
  );

INSERT INTO unit_master (
    unit_name,
    unit_category_id,
    to_base_factor,
    is_base_unit,
    is_active,
    sort_order
)
SELECT
    'm2',
    uc.unit_category_id,
    1,
    false,
    true,
    COALESCE((
        SELECT max(um.sort_order)
        FROM unit_master um
        WHERE um.unit_category_id = uc.unit_category_id
    ), 0) + 1
FROM unit_category uc
WHERE uc.category_name = '面積'
  AND NOT EXISTS (
      SELECT 1
      FROM unit_master um
      WHERE lower(um.unit_name) = 'm2'
  );

INSERT INTO unit_category (
    category_name,
    allow_conversion,
    is_active,
    sort_order
)
SELECT
    '體積',
    true,
    true,
    COALESCE((SELECT max(sort_order) FROM unit_category), 0) + 1
WHERE NOT EXISTS (
    SELECT 1
    FROM unit_category
    WHERE category_name = '體積'
);

INSERT INTO unit_master (
    unit_name,
    unit_category_id,
    to_base_factor,
    is_base_unit,
    is_active,
    sort_order
)
SELECT
    '立方公尺',
    uc.unit_category_id,
    1,
    true,
    true,
    1
FROM unit_category uc
WHERE uc.category_name = '體積'
  AND NOT EXISTS (
      SELECT 1
      FROM unit_master um
      WHERE um.unit_category_id = uc.unit_category_id
        AND um.is_base_unit = true
  );

INSERT INTO unit_master (
    unit_name,
    unit_category_id,
    to_base_factor,
    is_base_unit,
    is_active,
    sort_order
)
SELECT
    'm3',
    uc.unit_category_id,
    1,
    false,
    true,
    COALESCE((
        SELECT max(um.sort_order)
        FROM unit_master um
        WHERE um.unit_category_id = uc.unit_category_id
    ), 0) + 1
FROM unit_category uc
WHERE uc.category_name = '體積'
  AND NOT EXISTS (
      SELECT 1
      FROM unit_master um
      WHERE lower(um.unit_name) = 'm3'
  );

-- 建立目前已確認適合歷史數量估算的 2A 偵錯細項。
-- 鋼筋使用 kg，鋼骨使用噸，混凝土使用立方公尺，模板使用平方公尺。

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
    um.unit_id,
    true,
    10
FROM unit_master um
WHERE lower(um.unit_name) = 'kg'
ORDER BY um.unit_id
LIMIT 1
ON CONFLICT (
    major_category_name,
    detection_category_name
)
DO UPDATE SET
    unit_id = EXCLUDED.unit_id,
    is_active = true,
    sort_order = EXCLUDED.sort_order;

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
    um.unit_id,
    true,
    20
FROM unit_master um
WHERE um.unit_name = '立方公尺'
ORDER BY um.unit_id
LIMIT 1
ON CONFLICT (
    major_category_name,
    detection_category_name
)
DO UPDATE SET
    unit_id = EXCLUDED.unit_id,
    is_active = true,
    sort_order = EXCLUDED.sort_order;

INSERT INTO detection_category (
    major_category_name,
    detection_category_name,
    unit_id,
    is_active,
    sort_order
)
SELECT
    '結構體工程',
    '模板',
    um.unit_id,
    true,
    30
FROM unit_master um
WHERE um.unit_name = '平方公尺'
ORDER BY um.unit_id
LIMIT 1
ON CONFLICT (
    major_category_name,
    detection_category_name
)
DO UPDATE SET
    unit_id = EXCLUDED.unit_id,
    is_active = true,
    sort_order = EXCLUDED.sort_order;

INSERT INTO detection_category (
    major_category_name,
    detection_category_name,
    unit_id,
    is_active,
    sort_order
)
SELECT
    '鋼骨鋼構工程',
    '鋼骨',
    um.unit_id,
    true,
    40
FROM unit_master um
WHERE um.unit_name = '噸'
ORDER BY um.unit_id
LIMIT 1
ON CONFLICT (
    major_category_name,
    detection_category_name
)
DO UPDATE SET
    unit_id = EXCLUDED.unit_id,
    is_active = true,
    sort_order = EXCLUDED.sort_order;

COMMIT;
