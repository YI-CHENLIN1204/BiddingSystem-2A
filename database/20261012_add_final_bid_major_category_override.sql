ALTER TABLE final_bid_item
ADD COLUMN IF NOT EXISTS major_category_override text;

CREATE INDEX IF NOT EXISTS ix_final_bid_item_major_category_override
ON final_bid_item (major_category_override)
WHERE major_category_override IS NOT NULL;