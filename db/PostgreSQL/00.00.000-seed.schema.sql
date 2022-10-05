
CREATE TABLE public.acc_metric (
    id uuid NOT NULL,
    definition xml,
    name character varying(250) NOT NULL,
    code character varying(50) NOT NULL,
    service uuid NOT NULL,
    tenant uuid,
    is_active smallint NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at timestamp without time zone NOT NULL
);


CREATE TABLE public.acc_service (
    id uuid NOT NULL,
    code character varying(50) NOT NULL,
    name character varying(250) NOT NULL,
    description text,
    parent uuid,
    tenant uuid,
    is_active smallint NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at timestamp without time zone NOT NULL
);


CREATE TABLE public.acc_service_action (
    id uuid NOT NULL,
    code character varying NOT NULL,
    name character varying NOT NULL,
    parent uuid,
    service uuid NOT NULL,
    tenant uuid,
    is_active smallint NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at timestamp without time zone NOT NULL
);


CREATE TABLE public.acc_service_resource (
    id uuid NOT NULL,
    code character varying NOT NULL,
    name character varying NOT NULL,
    parent uuid,
    service uuid NOT NULL,
    tenant uuid,
    is_active smallint NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at timestamp without time zone NOT NULL
);


CREATE TABLE public.acc_service_sync (
    id uuid NOT NULL,
    tenant uuid,
    service uuid NOT NULL,
    is_active smallint NOT NULL,
    status smallint NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at bigint NOT NULL,
    last_sync_at timestamp without time zone NOT NULL
);


CREATE TABLE public.acc_service_user (
    service uuid NOT NULL,
    "user" uuid NOT NULL,
    role uuid NOT NULL,
    id uuid NOT NULL,
    tenant uuid,
    created_at timestamp without time zone NOT NULL,
    updated_at timestamp without time zone NOT NULL
);


CREATE TABLE public.acc_user_role (
    id uuid NOT NULL,
    name character varying(50) NOT NULL,
    rights xml,
    propagate integer NOT NULL,
    tenant uuid,
    is_active smallint NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at timestamp without time zone NOT NULL
);


CREATE TABLE public.forget_me (
    id uuid NOT NULL,
    tenant uuid,
    "user" uuid NOT NULL,
    is_active smallint NOT NULL,
    state smallint NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at bigint NOT NULL
);


CREATE TABLE public.queue_inbox (
    id uuid NOT NULL,
    tenant uuid,
    queue character varying(50) NOT NULL,
    exchange character varying(50) NOT NULL,
    route character varying(50),
    application_id character varying(100) NOT NULL,
    message_id uuid NOT NULL,
    message text NOT NULL,
    is_active smallint NOT NULL,
    status smallint NOT NULL,
    retry_count integer NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at bigint NOT NULL
);

CREATE TABLE public.queue_outbox (
    id uuid NOT NULL,
    tenant uuid,
    exchange character varying(50) NOT NULL,
    route character varying(50),
    message_id uuid NOT NULL,
    message text NOT NULL,
    is_active smallint NOT NULL,
    status smallint NOT NULL,
    retry_count integer NOT NULL,
    published_at timestamp without time zone,
    confirmed_at timestamp without time zone,
    created_at timestamp without time zone NOT NULL,
    updated_at bigint NOT NULL
);



CREATE TABLE public.storage_file (
    id uuid NOT NULL,
    tenant uuid,
    file_ref character varying(50) NOT NULL,
    name character varying(100) NOT NULL,
    extension character varying(10) NOT NULL,
    mime_type character varying(50) NOT NULL,
    created_at timestamp without time zone NOT NULL,
    purge_at timestamp without time zone,
    purged_at timestamp without time zone
);



CREATE TABLE public.tenant (
    id uuid NOT NULL,
    code character varying(50) NOT NULL,
    is_active smallint NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at timestamp without time zone NOT NULL
);


CREATE TABLE public.tenant_configuration (
    id uuid NOT NULL,
    tenant uuid,
    type smallint NOT NULL,
    value text NOT NULL,
    is_active smallint NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at timestamp without time zone NOT NULL
);



CREATE TABLE public."user" (
    id uuid NOT NULL,
    tenant uuid,
    profile uuid NOT NULL,
    is_active smallint NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at timestamp without time zone NOT NULL,
    email character varying(250),
    subject character varying(250) NOT NULL,
    issuer character varying(250) NOT NULL,
    name character varying(250) NOT NULL
);



CREATE TABLE public.user_profile (
    id uuid NOT NULL,
    tenant uuid,
    timezone character varying(50) NOT NULL,
    culture character varying(20) NOT NULL,
    language character varying(50) NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at timestamp without time zone NOT NULL
);



CREATE TABLE public.version_info (
    key character varying(50) NOT NULL,
    version character varying(50) NOT NULL,
    released_at timestamp without time zone,
    deployed_at timestamp without time zone,
    description text
);



CREATE TABLE public.what_you_know_about_me (
    id uuid NOT NULL,
    tenant uuid,
    "user" uuid NOT NULL,
    is_active smallint NOT NULL,
    state smallint NOT NULL,
    storage_file uuid,
    created_at timestamp without time zone NOT NULL,
    updated_at bigint NOT NULL
);



ALTER TABLE ONLY public.acc_metric
    ADD CONSTRAINT acc_metric_code_service_key UNIQUE (code, service);



ALTER TABLE ONLY public.acc_metric
    ADD CONSTRAINT acc_metric_pkey PRIMARY KEY (id);



ALTER TABLE ONLY public.acc_service_action
    ADD CONSTRAINT acc_service_action_code_service_key UNIQUE (code, service);


ALTER TABLE ONLY public.acc_service_action
    ADD CONSTRAINT acc_service_action_pkey PRIMARY KEY (id);



ALTER TABLE ONLY public.acc_service
    ADD CONSTRAINT acc_service_code_key UNIQUE (code);



ALTER TABLE ONLY public.acc_service
    ADD CONSTRAINT acc_service_pkey PRIMARY KEY (id);



ALTER TABLE ONLY public.acc_service_resource
    ADD CONSTRAINT acc_service_resource_code_service_key UNIQUE (code, service);



ALTER TABLE ONLY public.acc_service_resource
    ADD CONSTRAINT acc_service_resource_pkey PRIMARY KEY (id);



ALTER TABLE ONLY public.acc_service_user
    ADD CONSTRAINT acc_service_user_pkey PRIMARY KEY (id);



ALTER TABLE ONLY public.acc_service_user
    ADD CONSTRAINT acc_service_user_unique_set UNIQUE (role, "user", service);



ALTER TABLE ONLY public.acc_user_role
    ADD CONSTRAINT acc_user_role_pkey PRIMARY KEY (id);



ALTER TABLE ONLY public.forget_me
    ADD CONSTRAINT forget_me_pkey PRIMARY KEY (id);



ALTER TABLE ONLY public.queue_outbox
    ADD CONSTRAINT queue_inbox_pkey PRIMARY KEY (id);



ALTER TABLE ONLY public.queue_inbox
    ADD CONSTRAINT queue_outbox_pkey PRIMARY KEY (id);



ALTER TABLE ONLY public.acc_user_role
    ADD CONSTRAINT role_name_unique UNIQUE (name);



ALTER TABLE ONLY public.acc_service_sync
    ADD CONSTRAINT service_synce_pkey PRIMARY KEY (id);



ALTER TABLE ONLY public.storage_file
    ADD CONSTRAINT storage_file_pkey PRIMARY KEY (id);



ALTER TABLE ONLY public.tenant_configuration
    ADD CONSTRAINT tenant_configuration_pkey PRIMARY KEY (id);



ALTER TABLE ONLY public.tenant
    ADD CONSTRAINT tenant_pkey PRIMARY KEY (id);



ALTER TABLE ONLY public."user"
    ADD CONSTRAINT user_pkey PRIMARY KEY (id);



ALTER TABLE ONLY public.user_profile
    ADD CONSTRAINT user_profile_pkey PRIMARY KEY (id);



ALTER TABLE ONLY public."user"
    ADD CONSTRAINT user_subject_issuer_key UNIQUE (subject, issuer);



ALTER TABLE ONLY public.version_info
    ADD CONSTRAINT version_info_pkey PRIMARY KEY (key);



ALTER TABLE ONLY public.what_you_know_about_me
    ADD CONSTRAINT what_you_know_about_me_pkey PRIMARY KEY (id);



CREATE INDEX fki_metric_service_link ON public.acc_metric USING btree (service);



CREATE INDEX idx_metric_code ON public.acc_metric USING btree (code);



CREATE INDEX idx_metric_service ON public.acc_metric USING btree (service);



CREATE INDEX idx_service_action_code ON public.acc_service_action USING btree (code);



CREATE INDEX idx_service_action_service ON public.acc_service_action USING btree (service);



CREATE INDEX idx_service_code ON public.acc_service USING btree (code);



CREATE INDEX idx_service_parent ON public.acc_service USING btree (parent);



CREATE INDEX idx_service_resource_code ON public.acc_service_resource USING btree (code);



CREATE INDEX idx_service_resource_service ON public.acc_service_resource USING btree (service);



ALTER TABLE ONLY public.acc_metric
    ADD CONSTRAINT acc_metric_service_fkey FOREIGN KEY (service) REFERENCES public.acc_service(id) NOT VALID;



ALTER TABLE ONLY public.acc_metric
    ADD CONSTRAINT acc_metric_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;



ALTER TABLE ONLY public.acc_service_action
    ADD CONSTRAINT acc_service_action_service_fkey FOREIGN KEY (service) REFERENCES public.acc_service(id);



ALTER TABLE ONLY public.acc_service_action
    ADD CONSTRAINT acc_service_action_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id);



ALTER TABLE ONLY public.acc_service
    ADD CONSTRAINT acc_service_parent_fkey FOREIGN KEY (parent) REFERENCES public.acc_service(id) NOT VALID;



ALTER TABLE ONLY public.acc_service_resource
    ADD CONSTRAINT acc_service_resource_service_fkey FOREIGN KEY (service) REFERENCES public.acc_service(id);



ALTER TABLE ONLY public.acc_service_resource
    ADD CONSTRAINT acc_service_resource_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;



ALTER TABLE ONLY public.acc_service
    ADD CONSTRAINT acc_service_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;



ALTER TABLE ONLY public.acc_service_user
    ADD CONSTRAINT acc_service_user_role_fkey FOREIGN KEY (role) REFERENCES public.acc_user_role(id) NOT VALID;



ALTER TABLE ONLY public.acc_service_user
    ADD CONSTRAINT acc_service_user_service_fkey FOREIGN KEY (service) REFERENCES public.acc_service(id) NOT VALID;



ALTER TABLE ONLY public.acc_service_user
    ADD CONSTRAINT acc_service_user_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;



ALTER TABLE ONLY public.acc_service_user
    ADD CONSTRAINT acc_service_user_user_fkey FOREIGN KEY ("user") REFERENCES public."user"(id) NOT VALID;



ALTER TABLE ONLY public.acc_user_role
    ADD CONSTRAINT acc_user_role_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;



ALTER TABLE ONLY public.forget_me
    ADD CONSTRAINT forget_me_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;



ALTER TABLE ONLY public.forget_me
    ADD CONSTRAINT forget_me_user_fkey FOREIGN KEY ("user") REFERENCES public."user"(id) NOT VALID;



ALTER TABLE ONLY public.acc_service_sync
    ADD CONSTRAINT service_sync_service_fkey FOREIGN KEY (service) REFERENCES public.acc_service(id);



ALTER TABLE ONLY public.acc_service_sync
    ADD CONSTRAINT service_sync_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id);



ALTER TABLE ONLY public.storage_file
    ADD CONSTRAINT storage_file_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;



ALTER TABLE ONLY public.tenant_configuration
    ADD CONSTRAINT tenant_configuration_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;



ALTER TABLE ONLY public."user"
    ADD CONSTRAINT user_profile_fkey FOREIGN KEY (profile) REFERENCES public.user_profile(id) NOT VALID;



ALTER TABLE ONLY public.user_profile
    ADD CONSTRAINT user_profile_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;



ALTER TABLE ONLY public."user"
    ADD CONSTRAINT user_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;



ALTER TABLE ONLY public.what_you_know_about_me
    ADD CONSTRAINT what_you_know_about_me_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;



ALTER TABLE ONLY public.what_you_know_about_me
    ADD CONSTRAINT what_you_know_about_me_user_fkey FOREIGN KEY ("user") REFERENCES public."user"(id) NOT VALID;

