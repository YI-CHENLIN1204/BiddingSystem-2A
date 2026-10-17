-- Run only after reviewing against the targeted development database.
CREATE TABLE IF NOT EXISTS public.history_operation_receipt (
    operation_id uuid PRIMARY KEY,
    project_id integer NOT NULL REFERENCES public.project_master(project_id),
    action_type varchar(40) NOT NULL CHECK (action_type IN ('PublishHistory','ReopenHistoryReview')),
    result_json jsonb NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_history_operation_receipt_project
    ON public.history_operation_receipt(project_id, created_at DESC);
COMMENT ON TABLE public.history_operation_receipt IS 'Durable successful Publish/Reopen receipts. Written atomically in the project transaction; retains operation IDs to reconcile lost responses.';
