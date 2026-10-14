DELETE FROM item_dictionary
WHERE keyword = '1-6';

INSERT INTO audit_log (
    module,
    action_type,
    entity_type,
    entity_id,
    project_id,
    description,
    old_value,
    new_value,
    action_user,
    created_at
)
SELECT
    '材料對應',
    'CleanupDictionaryKeyword',
    'ItemDictionary',
    null,
    null,
    '移除不應作為材料關鍵字的工項代碼「1-6」',
    '{"keyword":"1-6","reason":"item code"}',
    '{"removed":true}',
    'System',
    now()
WHERE NOT EXISTS (
    SELECT 1
    FROM audit_log
    WHERE action_type = 'CleanupDictionaryKeyword'
      AND description = '移除不應作為材料關鍵字的工項代碼「1-6」'
);