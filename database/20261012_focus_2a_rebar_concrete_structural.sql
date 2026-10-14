UPDATE detection_category
SET is_active = CASE
    WHEN major_category_name = '結構體工程'
     AND detection_category_name IN ('鋼筋', '混凝土')
        THEN true
    ELSE false
END
WHERE detection_category_name IN ('鋼筋', '混凝土', '模板', '鋼骨')
   OR major_category_name IN ('結構體工程', '鋼骨鋼構工程');