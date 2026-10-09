CREATE INDEX IF NOT EXISTS ix_detection_category_unit
    ON detection_category(unit_id);

CREATE INDEX IF NOT EXISTS ix_historical_transaction_material_id
    ON historical_transaction(material_id);

CREATE INDEX IF NOT EXISTS ix_item_dictionary_material_id
    ON item_dictionary(material_id);

CREATE INDEX IF NOT EXISTS ix_material_master_category_id
    ON material_master(category_id);

CREATE INDEX IF NOT EXISTS ix_quantity_rule_base_material_id
    ON quantity_rule_base(material_id);