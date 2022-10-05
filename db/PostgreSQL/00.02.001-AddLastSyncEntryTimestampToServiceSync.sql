BEGIN;

	ALTER TABLE public.acc_service_sync ALTER COLUMN last_sync_at DROP NOT NULL;
	ALTER TABLE public.acc_service_sync ADD COLUMN last_sync_entry_timestamp timestamp without time zone;

	Update public.version_info
		SET version = '00.02.001',
		  released_at = '2021-02-08 00:00:00.000',
		  deployed_at = (select current_timestamp at time zone 'utc'),
		  description = 'Add last_sync_entry_timestamp to service_sync'
		  where key = 'DB.Core';
COMMIT;
