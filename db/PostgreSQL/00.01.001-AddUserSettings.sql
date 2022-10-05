CREATE TABLE public.user_settings
(
    id uuid NOT NULL,
    tenant uuid,
    "user" uuid,
    name character varying(200),
    key character varying(250),
    type smallint NOT NULL,
    value text,
    created_at timestamp without time zone NOT NULL,
    updated_at timestamp without time zone NOT NULL,
    PRIMARY KEY (id),
    CONSTRAINT user_settings_user_fkey FOREIGN KEY ("user")
        REFERENCES public."user" (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID,
    CONSTRAINT user_settings_tenant_fkey FOREIGN KEY (tenant)
        REFERENCES public.tenant (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID
)
WITH (
    OIDS = FALSE
);

	
INSERT INTO public.version_info(
	key, version, released_at, deployed_at, description)
	VALUES ('DB.Core'
           ,'00.01.001'
           ,'2021-01-05 00:00:00.000'
           , (select current_timestamp at time zone 'utc')
           ,'Add user settings');