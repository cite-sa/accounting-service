import { Serializable } from '@common/types/json/serializable';
import { IsActive } from '@app/core/enum/is-active.enum';
import { Lookup } from '@common/model/lookup';
import { UserSettingsInformation, UserSettingsLookupBuilder } from '@user-service/core/model/user-settings.model';
import { nameof } from 'ts-simple-nameof';
import { ServiceSync } from '@app/core/model/service-sync/service-sync.model';
import { ServiceSyncLookup } from '@app/core/query/service-sync.lookup';
import { Service } from '@app/core/model/service/service.model';

export class ServiceSyncListingUserSettings implements Serializable<ServiceSyncListingUserSettings>, UserSettingsLookupBuilder<ServiceSyncLookup> {

	private like: string;
	private isActive: IsActive[] = [IsActive.Active];
	private order: Lookup.Ordering = { items: [nameof<ServiceSync>(x => x.service) + '.' + nameof<Service>(x => x.name)] };
	private project: Lookup.FieldDirectives = {
		fields: [
			nameof<ServiceSync>(x => x.id),
			nameof<ServiceSync>(x => x.service) + '.' + nameof<Service>(x => x.name),
			nameof<ServiceSync>(x => x.createdAt)
		]
	};

	static getUserSettingsInformation(): UserSettingsInformation<ServiceSyncListingUserSettings> {
		return {
			key: 'ServiceSyncListingUserSettings',
			type: ServiceSyncListingUserSettings
		};
	}

	public fromJSONObject(item: any): ServiceSyncListingUserSettings {
		this.like = item.like;
		this.isActive = item.isActive;
		this.order = item.order;
		this.project = item.project;
		return this;
	}

	public update(lookup: ServiceSyncLookup) {
		this.like = lookup.like;
		this.isActive = lookup.isActive;
		this.order = lookup.order;
		this.project = lookup.project;
	}

	public apply(lookup: ServiceSyncLookup): ServiceSyncLookup {
		lookup.like = this.like;
		lookup.isActive = this.isActive;
		lookup.order = this.order;
		lookup.project = this.project;
		return lookup;
	}
}
