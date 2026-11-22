-- T00/T01/T04 live-state integrity READ ONLY. No production data mutation.
-- Project currently contains SYNTHETIC data. A perfect self-check is NOT an independent accuracy test.
-- Run only with a read-only role if available; all statements in this file are SELECT.
WITH fac AS (
 SELECT f.* FROM public.final_bid_item f
 JOIN public.project_master p ON p.project_id=f.project_master_id
 WHERE p.project_name LIKE 'SYNTHETIC_FACTORY:%'
)
SELECT
 (SELECT COUNT(*) FROM public.item_dictionary) AS live_dictionary_count,
 (SELECT COUNT(*) FROM public.item_dictionary WHERE keyword LIKE '%廠房作業區%') AS factory_dictionary_count,
 (SELECT COUNT(*) FROM public.project_master WHERE project_name LIKE 'SYNTHETIC_FACTORY:%') AS factory_project_count,
 (SELECT COUNT(*) FROM public.project_master WHERE project_name LIKE 'SYNTHETIC_FACTORY:%' AND history_status='Approved') AS approved_factory_count,
 (SELECT COUNT(*) FROM fac) AS factory_item_count,
 (SELECT COUNT(*) FROM fac WHERE mapping_status='Auto') AS auto_count,
 (SELECT COUNT(*) FROM fac WHERE material_id IS NULL) AS null_material_count,
 (SELECT COUNT(*) FROM fac f LEFT JOIN public.item_dictionary d ON d.keyword=f.matched_keyword WHERE d.dictionary_id IS NULL) AS missing_dict_reference_count,
 (SELECT COUNT(*) FROM fac f JOIN public.item_dictionary d ON d.keyword=f.matched_keyword WHERE d.material_id IS DISTINCT FROM f.material_id) AS mismatch_material_count,
 (SELECT COUNT(*) FROM fac WHERE mapping_issue_code IS NOT NULL OR data_issue_code IS NOT NULL) AS flagged_issue_count,
 (SELECT COUNT(*) FROM fac WHERE qty<=0 OR unit_price<=0) AS invalid_qty_price_count,
 (SELECT COUNT(*) FROM fac WHERE amount IS NOT NULL AND ABS(amount-qty*unit_price)>0.02) AS invalid_amount_count,
 (SELECT COUNT(*) FROM public.unit_category) AS unit_category_count,
 (SELECT COUNT(*) FROM public.unit_category WHERE allow_conversion) AS allowed_conversion_categories,
 (SELECT COUNT(*) FROM public.unit_master) AS unit_master_count;

-- Provenance state; no names, documents, or sensitive payloads returned.
SELECT CASE WHEN project_name LIKE 'SYNTHETIC_FACTORY:%' THEN 'synthetic_factory_train' ELSE 'other_history' END AS cohort,
 COALESCE(history_status,'NULL') AS history_status,
 COUNT(*) AS project_count,
 COUNT(*) FILTER (WHERE history_approved_at IS NOT NULL) AS approved_at_present,
 MIN(history_approved_at) AS min_approved_at,
 MAX(history_approved_at) AS max_approved_at,
 COUNT(*) FILTER (WHERE source_file_hash IS NULL) AS missing_source_hash_count
FROM public.project_master
GROUP BY 1,2 ORDER BY 1,2;
