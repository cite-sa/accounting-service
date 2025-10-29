ALTER TABLE IF EXISTS public.acc_metric RENAME TO metric;
ALTER TABLE IF EXISTS public.acc_service RENAME TO service;
ALTER TABLE IF EXISTS public.acc_service_action RENAME TO service_action;
ALTER TABLE IF EXISTS public.acc_service_reset_entry_sync RENAME TO service_reset_entry_sync;
ALTER TABLE IF EXISTS public.acc_service_resource RENAME TO service_resource;
ALTER TABLE IF EXISTS public.acc_service_sync RENAME TO service_sync;
ALTER TABLE IF EXISTS public.acc_service_user RENAME TO service_user;
ALTER TABLE IF EXISTS public.acc_user_role RENAME TO user_role;

Update public.version_info
		SET version = '00.04.000',
		  released_at = '2021-09-09 00:00:00.000',
		  deployed_at = (select current_timestamp at time zone 'utc'),
		  description = 'Remove Acc Prefix'
		  where key = 'DB.Core';