--
-- PostgreSQL database dump
--

-- Dumped from database version 9.4.26
-- Dumped by pg_dump version 13.1

-- Started on 2020-12-16 12:07:06

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- TOC entry 2152 (class 1262 OID 154621)
-- Name: accounting_dev; Type: DATABASE; Schema: -; Owner: -
--

CREATE DATABASE accounting_dev WITH TEMPLATE = template0 ENCODING = 'UTF8' LOCALE = 'en_US.UTF-8';


\connect accounting_dev

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

--
-- TOC entry 187 (class 1259 OID 154786)
-- Name: acc_metric; Type: TABLE; Schema: public; Owner: -
--

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


--
-- TOC entry 183 (class 1259 OID 154744)
-- Name: acc_service; Type: TABLE; Schema: public; Owner: -
--

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


--
-- TOC entry 188 (class 1259 OID 157873)
-- Name: acc_service_action; Type: TABLE; Schema: public; Owner: -
--

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


--
-- TOC entry 186 (class 1259 OID 154773)
-- Name: acc_service_resource; Type: TABLE; Schema: public; Owner: -
--

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


--
-- TOC entry 189 (class 1259 OID 157942)
-- Name: acc_service_sync; Type: TABLE; Schema: public; Owner: -
--

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


--
-- TOC entry 184 (class 1259 OID 154757)
-- Name: acc_service_user; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.acc_service_user (
    service uuid NOT NULL,
    "user" uuid NOT NULL,
    role uuid NOT NULL,
    id uuid NOT NULL,
    tenant uuid,
    created_at timestamp without time zone NOT NULL,
    updated_at timestamp without time zone NOT NULL
);


--
-- TOC entry 185 (class 1259 OID 154762)
-- Name: acc_user_role; Type: TABLE; Schema: public; Owner: -
--

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


--
-- TOC entry 173 (class 1259 OID 154625)
-- Name: forget_me; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.forget_me (
    id uuid NOT NULL,
    tenant uuid,
    "user" uuid NOT NULL,
    is_active smallint NOT NULL,
    state smallint NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at bigint NOT NULL
);


--
-- TOC entry 174 (class 1259 OID 154628)
-- Name: queue_inbox; Type: TABLE; Schema: public; Owner: -
--

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


--
-- TOC entry 175 (class 1259 OID 154634)
-- Name: queue_outbox; Type: TABLE; Schema: public; Owner: -
--

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


--
-- TOC entry 176 (class 1259 OID 154640)
-- Name: storage_file; Type: TABLE; Schema: public; Owner: -
--

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


--
-- TOC entry 177 (class 1259 OID 154643)
-- Name: tenant; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.tenant (
    id uuid NOT NULL,
    code character varying(50) NOT NULL,
    is_active smallint NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at timestamp without time zone NOT NULL
);


--
-- TOC entry 178 (class 1259 OID 154646)
-- Name: tenant_configuration; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.tenant_configuration (
    id uuid NOT NULL,
    tenant uuid,
    type smallint NOT NULL,
    value text NOT NULL,
    is_active smallint NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at timestamp without time zone NOT NULL
);


--
-- TOC entry 179 (class 1259 OID 154652)
-- Name: user; Type: TABLE; Schema: public; Owner: -
--

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


--
-- TOC entry 180 (class 1259 OID 154655)
-- Name: user_profile; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.user_profile (
    id uuid NOT NULL,
    tenant uuid,
    timezone character varying(50) NOT NULL,
    culture character varying(20) NOT NULL,
    language character varying(50) NOT NULL,
    created_at timestamp without time zone NOT NULL,
    updated_at timestamp without time zone NOT NULL
);


--
-- TOC entry 181 (class 1259 OID 154658)
-- Name: version_info; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.version_info (
    key character varying(50) NOT NULL,
    version character varying(50) NOT NULL,
    released_at timestamp without time zone,
    deployed_at timestamp without time zone,
    description text
);


--
-- TOC entry 182 (class 1259 OID 154664)
-- Name: what_you_know_about_me; Type: TABLE; Schema: public; Owner: -
--

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


--
-- TOC entry 2000 (class 2606 OID 155991)
-- Name: acc_metric acc_metric_code_service_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_metric
    ADD CONSTRAINT acc_metric_code_service_key UNIQUE (code, service);


--
-- TOC entry 2002 (class 2606 OID 154793)
-- Name: acc_metric acc_metric_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_metric
    ADD CONSTRAINT acc_metric_pkey PRIMARY KEY (id);


--
-- TOC entry 2007 (class 2606 OID 157882)
-- Name: acc_service_action acc_service_action_code_service_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service_action
    ADD CONSTRAINT acc_service_action_code_service_key UNIQUE (code, service);


--
-- TOC entry 2009 (class 2606 OID 157880)
-- Name: acc_service_action acc_service_action_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service_action
    ADD CONSTRAINT acc_service_action_pkey PRIMARY KEY (id);


--
-- TOC entry 1980 (class 2606 OID 155985)
-- Name: acc_service acc_service_code_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service
    ADD CONSTRAINT acc_service_code_key UNIQUE (code);


--
-- TOC entry 1982 (class 2606 OID 154751)
-- Name: acc_service acc_service_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service
    ADD CONSTRAINT acc_service_pkey PRIMARY KEY (id);


--
-- TOC entry 1994 (class 2606 OID 155993)
-- Name: acc_service_resource acc_service_resource_code_service_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service_resource
    ADD CONSTRAINT acc_service_resource_code_service_key UNIQUE (code, service);


--
-- TOC entry 1996 (class 2606 OID 154780)
-- Name: acc_service_resource acc_service_resource_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service_resource
    ADD CONSTRAINT acc_service_resource_pkey PRIMARY KEY (id);


--
-- TOC entry 1986 (class 2606 OID 154902)
-- Name: acc_service_user acc_service_user_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service_user
    ADD CONSTRAINT acc_service_user_pkey PRIMARY KEY (id);


--
-- TOC entry 1988 (class 2606 OID 154761)
-- Name: acc_service_user acc_service_user_unique_set; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service_user
    ADD CONSTRAINT acc_service_user_unique_set UNIQUE (role, "user", service);


--
-- TOC entry 1990 (class 2606 OID 154769)
-- Name: acc_user_role acc_user_role_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_user_role
    ADD CONSTRAINT acc_user_role_pkey PRIMARY KEY (id);


--
-- TOC entry 1958 (class 2606 OID 154670)
-- Name: forget_me forget_me_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.forget_me
    ADD CONSTRAINT forget_me_pkey PRIMARY KEY (id);


--
-- TOC entry 1962 (class 2606 OID 154672)
-- Name: queue_outbox queue_inbox_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.queue_outbox
    ADD CONSTRAINT queue_inbox_pkey PRIMARY KEY (id);


--
-- TOC entry 1960 (class 2606 OID 154674)
-- Name: queue_inbox queue_outbox_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.queue_inbox
    ADD CONSTRAINT queue_outbox_pkey PRIMARY KEY (id);


--
-- TOC entry 1992 (class 2606 OID 154771)
-- Name: acc_user_role role_name_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_user_role
    ADD CONSTRAINT role_name_unique UNIQUE (name);


--
-- TOC entry 2013 (class 2606 OID 157946)
-- Name: acc_service_sync service_synce_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service_sync
    ADD CONSTRAINT service_synce_pkey PRIMARY KEY (id);


--
-- TOC entry 1964 (class 2606 OID 154676)
-- Name: storage_file storage_file_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.storage_file
    ADD CONSTRAINT storage_file_pkey PRIMARY KEY (id);


--
-- TOC entry 1968 (class 2606 OID 154678)
-- Name: tenant_configuration tenant_configuration_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tenant_configuration
    ADD CONSTRAINT tenant_configuration_pkey PRIMARY KEY (id);


--
-- TOC entry 1966 (class 2606 OID 154680)
-- Name: tenant tenant_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tenant
    ADD CONSTRAINT tenant_pkey PRIMARY KEY (id);


--
-- TOC entry 1970 (class 2606 OID 154682)
-- Name: user user_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."user"
    ADD CONSTRAINT user_pkey PRIMARY KEY (id);


--
-- TOC entry 1974 (class 2606 OID 154684)
-- Name: user_profile user_profile_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_profile
    ADD CONSTRAINT user_profile_pkey PRIMARY KEY (id);


--
-- TOC entry 1972 (class 2606 OID 155995)
-- Name: user user_subject_issuer_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."user"
    ADD CONSTRAINT user_subject_issuer_key UNIQUE (subject, issuer);


--
-- TOC entry 1976 (class 2606 OID 154686)
-- Name: version_info version_info_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.version_info
    ADD CONSTRAINT version_info_pkey PRIMARY KEY (key);


--
-- TOC entry 1978 (class 2606 OID 154688)
-- Name: what_you_know_about_me what_you_know_about_me_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.what_you_know_about_me
    ADD CONSTRAINT what_you_know_about_me_pkey PRIMARY KEY (id);


--
-- TOC entry 2003 (class 1259 OID 154801)
-- Name: fki_metric_service_link; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX fki_metric_service_link ON public.acc_metric USING btree (service);


--
-- TOC entry 2004 (class 1259 OID 156004)
-- Name: idx_metric_code; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_metric_code ON public.acc_metric USING btree (code);


--
-- TOC entry 2005 (class 1259 OID 156005)
-- Name: idx_metric_service; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_metric_service ON public.acc_metric USING btree (service);


--
-- TOC entry 2010 (class 1259 OID 157893)
-- Name: idx_service_action_code; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_service_action_code ON public.acc_service_action USING btree (code);


--
-- TOC entry 2011 (class 1259 OID 157894)
-- Name: idx_service_action_service; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_service_action_service ON public.acc_service_action USING btree (service);


--
-- TOC entry 1983 (class 1259 OID 156003)
-- Name: idx_service_code; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_service_code ON public.acc_service USING btree (code);


--
-- TOC entry 1984 (class 1259 OID 156006)
-- Name: idx_service_parent; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_service_parent ON public.acc_service USING btree (parent);


--
-- TOC entry 1997 (class 1259 OID 156007)
-- Name: idx_service_resource_code; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_service_resource_code ON public.acc_service_resource USING btree (code);


--
-- TOC entry 1998 (class 1259 OID 156008)
-- Name: idx_service_resource_service; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_service_resource_service ON public.acc_service_resource USING btree (service);


--
-- TOC entry 2033 (class 2606 OID 154796)
-- Name: acc_metric acc_metric_service_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_metric
    ADD CONSTRAINT acc_metric_service_fkey FOREIGN KEY (service) REFERENCES public.acc_service(id) NOT VALID;


--
-- TOC entry 2032 (class 2606 OID 154881)
-- Name: acc_metric acc_metric_tenant_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_metric
    ADD CONSTRAINT acc_metric_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;


--
-- TOC entry 2034 (class 2606 OID 157883)
-- Name: acc_service_action acc_service_action_service_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service_action
    ADD CONSTRAINT acc_service_action_service_fkey FOREIGN KEY (service) REFERENCES public.acc_service(id);


--
-- TOC entry 2035 (class 2606 OID 157888)
-- Name: acc_service_action acc_service_action_tenant_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service_action
    ADD CONSTRAINT acc_service_action_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id);


--
-- TOC entry 2023 (class 2606 OID 154752)
-- Name: acc_service acc_service_parent_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service
    ADD CONSTRAINT acc_service_parent_fkey FOREIGN KEY (parent) REFERENCES public.acc_service(id) NOT VALID;


--
-- TOC entry 2030 (class 2606 OID 154781)
-- Name: acc_service_resource acc_service_resource_service_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service_resource
    ADD CONSTRAINT acc_service_resource_service_fkey FOREIGN KEY (service) REFERENCES public.acc_service(id);


--
-- TOC entry 2031 (class 2606 OID 154886)
-- Name: acc_service_resource acc_service_resource_tenant_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service_resource
    ADD CONSTRAINT acc_service_resource_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;


--
-- TOC entry 2024 (class 2606 OID 154876)
-- Name: acc_service acc_service_tenant_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service
    ADD CONSTRAINT acc_service_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;


--
-- TOC entry 2027 (class 2606 OID 154908)
-- Name: acc_service_user acc_service_user_role_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service_user
    ADD CONSTRAINT acc_service_user_role_fkey FOREIGN KEY (role) REFERENCES public.acc_user_role(id) NOT VALID;


--
-- TOC entry 2026 (class 2606 OID 154903)
-- Name: acc_service_user acc_service_user_service_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service_user
    ADD CONSTRAINT acc_service_user_service_fkey FOREIGN KEY (service) REFERENCES public.acc_service(id) NOT VALID;


--
-- TOC entry 2025 (class 2606 OID 154896)
-- Name: acc_service_user acc_service_user_tenant_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service_user
    ADD CONSTRAINT acc_service_user_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;


--
-- TOC entry 2028 (class 2606 OID 154913)
-- Name: acc_service_user acc_service_user_user_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service_user
    ADD CONSTRAINT acc_service_user_user_fkey FOREIGN KEY ("user") REFERENCES public."user"(id) NOT VALID;


--
-- TOC entry 2029 (class 2606 OID 154891)
-- Name: acc_user_role acc_user_role_tenant_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_user_role
    ADD CONSTRAINT acc_user_role_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;


--
-- TOC entry 2014 (class 2606 OID 154699)
-- Name: forget_me forget_me_tenant_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.forget_me
    ADD CONSTRAINT forget_me_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;


--
-- TOC entry 2015 (class 2606 OID 154704)
-- Name: forget_me forget_me_user_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.forget_me
    ADD CONSTRAINT forget_me_user_fkey FOREIGN KEY ("user") REFERENCES public."user"(id) NOT VALID;


--
-- TOC entry 2037 (class 2606 OID 157952)
-- Name: acc_service_sync service_sync_service_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service_sync
    ADD CONSTRAINT service_sync_service_fkey FOREIGN KEY (service) REFERENCES public.acc_service(id);


--
-- TOC entry 2036 (class 2606 OID 157947)
-- Name: acc_service_sync service_sync_tenant_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.acc_service_sync
    ADD CONSTRAINT service_sync_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id);


--
-- TOC entry 2016 (class 2606 OID 154709)
-- Name: storage_file storage_file_tenant_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.storage_file
    ADD CONSTRAINT storage_file_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;


--
-- TOC entry 2017 (class 2606 OID 154714)
-- Name: tenant_configuration tenant_configuration_tenant_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tenant_configuration
    ADD CONSTRAINT tenant_configuration_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;


--
-- TOC entry 2018 (class 2606 OID 154719)
-- Name: user user_profile_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."user"
    ADD CONSTRAINT user_profile_fkey FOREIGN KEY (profile) REFERENCES public.user_profile(id) NOT VALID;


--
-- TOC entry 2020 (class 2606 OID 154724)
-- Name: user_profile user_profile_tenant_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_profile
    ADD CONSTRAINT user_profile_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;


--
-- TOC entry 2019 (class 2606 OID 154729)
-- Name: user user_tenant_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."user"
    ADD CONSTRAINT user_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;


--
-- TOC entry 2021 (class 2606 OID 154734)
-- Name: what_you_know_about_me what_you_know_about_me_tenant_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.what_you_know_about_me
    ADD CONSTRAINT what_you_know_about_me_tenant_fkey FOREIGN KEY (tenant) REFERENCES public.tenant(id) NOT VALID;


--
-- TOC entry 2022 (class 2606 OID 154739)
-- Name: what_you_know_about_me what_you_know_about_me_user_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.what_you_know_about_me
    ADD CONSTRAINT what_you_know_about_me_user_fkey FOREIGN KEY ("user") REFERENCES public."user"(id) NOT VALID;


-- Completed on 2020-12-16 12:07:11

--
-- PostgreSQL database dump complete
--

