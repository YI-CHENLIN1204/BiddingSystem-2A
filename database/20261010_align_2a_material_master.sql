BEGIN;

-- 建立鋼骨鋼構工程材料分類（若尚未存在）
INSERT INTO material_category (
    category_name,
    is_active,
    sort_order
)
SELECT
    '鋼骨鋼構工程',
    true,
    COALESCE((SELECT max(sort_order) FROM material_category), 0) + 1
WHERE NOT EXISTS (
    SELECT 1
    FROM material_category
    WHERE category_name = '鋼骨鋼構工程'
);

-- 修正現有標準材料的標準單位與工程分類。
UPDATE material_master
SET
    unit_id = (
        SELECT unit_id
        FROM unit_master
        WHERE lower(unit_name) = 'kg'
        ORDER BY unit_id
        LIMIT 1
    ),
    category_id = (
        SELECT category_id
        FROM material_category
        WHERE category_name = '結構體工程'
        ORDER BY category_id
        LIMIT 1
    )
WHERE material_name = '鋼筋';

UPDATE material_master
SET
    unit_id = (
        SELECT unit_id
        FROM unit_master
        WHERE unit_name = '噸'
        ORDER BY unit_id
        LIMIT 1
    ),
    category_id = (
        SELECT category_id
        FROM material_category
        WHERE category_name = '鋼骨鋼構工程'
        ORDER BY category_id
        LIMIT 1
    )
WHERE material_name = '鋼骨';

-- 新增目前 2A 需要的結構體標準材料。
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
JOIN unit_master um
    ON um.unit_name = '立方公尺'
WHERE mc.category_name = '結構體工程'
  AND NOT EXISTS (
      SELECT 1
      FROM material_master
      WHERE material_name = '混凝土'
  )
LIMIT 1;

INSERT INTO material_master (
    material_name,
    category_id,
    is_active,
    unit_id
)
SELECT
    '模板',
    mc.category_id,
    true,
    um.unit_id
FROM material_category mc
JOIN unit_master um
    ON um.unit_name = '平方公尺'
WHERE mc.category_name = '結構體工程'
  AND NOT EXISTS (
      SELECT 1
      FROM material_master
      WHERE material_name = '模板'
  )
LIMIT 1;

-- 建立偵錯細項與標準材料的 1:1 對應。
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
WHERE mm.material_name = '鋼筋'
ON CONFLICT (material_id)
DO UPDATE SET
    detection_category_id = EXCLUDED.detection_category_id,
    is_active = true,
    updated_at = now();

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
WHERE mm.material_name = '混凝土'
ON CONFLICT (material_id)
DO UPDATE SET
    detection_category_id = EXCLUDED.detection_category_id,
    is_active = true,
    updated_at = now();

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
   AND dc.detection_category_name = '模板'
WHERE mm.material_name = '模板'
ON CONFLICT (material_id)
DO UPDATE SET
    detection_category_id = EXCLUDED.detection_category_id,
    is_active = true,
    updated_at = now();

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
    ON dc.major_category_name = '鋼骨鋼構工程'
   AND dc.detection_category_name = '鋼骨'
WHERE mm.material_name = '鋼骨'
ON CONFLICT (material_id)
DO UPDATE SET
    detection_category_id = EXCLUDED.detection_category_id,
    is_active = true,
    updated_at = now();

-- 補上保守且可解釋的材料字典。
-- 較具體的關鍵字會由現有服務優先匹配。
INSERT INTO item_dictionary (keyword, material_id)
SELECT '鋼筋續接器', material_id
FROM material_master
WHERE material_name = '鋼筋續接器'
  AND NOT EXISTS (
      SELECT 1 FROM item_dictionary WHERE keyword = '鋼筋續接器'
  );

INSERT INTO item_dictionary (keyword, material_id)
SELECT '續接器', material_id
FROM material_master
WHERE material_name = '鋼筋續接器'
  AND NOT EXISTS (
      SELECT 1 FROM item_dictionary WHERE keyword = '續接器'
  );

INSERT INTO item_dictionary (keyword, material_id)
SELECT '鋼筋', material_id
FROM material_master
WHERE material_name = '鋼筋'
  AND NOT EXISTS (
      SELECT 1 FROM item_dictionary WHERE keyword = '鋼筋'
  );

INSERT INTO item_dictionary (keyword, material_id)
SELECT '混凝土', material_id
FROM material_master
WHERE material_name = '混凝土'
  AND NOT EXISTS (
      SELECT 1 FROM item_dictionary WHERE keyword = '混凝土'
  );

INSERT INTO item_dictionary (keyword, material_id)
SELECT '預拌混凝土', material_id
FROM material_master
WHERE material_name = '混凝土'
  AND NOT EXISTS (
      SELECT 1 FROM item_dictionary WHERE keyword = '預拌混凝土'
  );

INSERT INTO item_dictionary (keyword, material_id)
SELECT '模板', material_id
FROM material_master
WHERE material_name = '模板'
  AND NOT EXISTS (
      SELECT 1 FROM item_dictionary WHERE keyword = '模板'
  );

INSERT INTO item_dictionary (keyword, material_id)
SELECT '模版', material_id
FROM material_master
WHERE material_name = '模板'
  AND NOT EXISTS (
      SELECT 1 FROM item_dictionary WHERE keyword = '模版'
  );

INSERT INTO item_dictionary (keyword, material_id)
SELECT '鋼骨', material_id
FROM material_master
WHERE material_name = '鋼骨'
  AND NOT EXISTS (
      SELECT 1 FROM item_dictionary WHERE keyword = '鋼骨'
  );

INSERT INTO item_dictionary (keyword, material_id)
SELECT 'H型鋼', material_id
FROM material_master
WHERE material_name = '鋼骨'
  AND NOT EXISTS (
      SELECT 1 FROM item_dictionary WHERE keyword = 'H型鋼'
  );

INSERT INTO item_dictionary (keyword, material_id)
SELECT '鋼構', material_id
FROM material_master
WHERE material_name = '鋼骨'
  AND NOT EXISTS (
      SELECT 1 FROM item_dictionary WHERE keyword = '鋼構'
  );

COMMIT;