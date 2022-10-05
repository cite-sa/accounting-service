CREATE TABLE public.acc_service_reset_entry_sync
(
    id uuid NOT NULL,
    tenant uuid,
    service uuid NOT NULL,
    is_active smallint NOT NULL,
    status smallint NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at bigint NOT NULL,
    last_sync_at timestamp without time zone,
    last_sync_entry_timestamp timestamp without time zone,
    last_sync_entry_id character varying(250) COLLATE pg_catalog."default",
    CONSTRAINT service_reset_entry_sync_pkey PRIMARY KEY (id),
    CONSTRAINT service_reset_entry_sync_service_fkey FOREIGN KEY (service)
        REFERENCES public.acc_service (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT service_reset_entry_sync_tenant_fkey FOREIGN KEY (tenant)
        REFERENCES public.tenant (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

Update public.version_info
		SET version = '00.03.001',
		  released_at = '2021-02-08 00:00:00.000',
		  deployed_at = (select current_timestamp at time zone 'utc'),
		  description = 'Add service_reset_entry_sync'
		  where key = 'DB.Core';