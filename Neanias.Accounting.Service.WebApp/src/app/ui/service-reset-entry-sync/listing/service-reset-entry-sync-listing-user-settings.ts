import { Serializable } from '@common/types/json/serializable';
import { IsActive } from '@app/core/enum/is-active.enum';
import { Lookup } from '@common/model/lookup';
import { UserSettingsInformation, UserSettingsLookupBuilder } from '@user-service/core/model/user-settings.model';
import { nameof } from 'ts-simple-nameof';
import { ServiceResetEntrySync } from '@app/core/model/service-reset-entry-sync/service-reset-entry-sync.model';
import { ServiceResetEntrySyncLookup } from '@app/core/query/service-reset-entry-sync.lookup';
import { Service } from '@app/core/model/service/service.model';

export class ServiceResetEntrySyncListingUserSettings implements Serializable<ServiceResetEntrySyncListingUserSettings>, UserSettingsLookupBuilder<ServiceResetEntrySyncLookup> {

	private like: string;
	private isActive: IsActive[] = [IsActive.Active];
	private order: Lookup.Ordering = { items: [nameof<ServiceResetEntrySync>(x => x.service) + '.' + nameof<Service>(x => x.name)] };
	private project: Lookup.FieldDirectives = {
		fields: [
			nameof<ServiceResetEntrySync>(x => x.id),
			nameof<ServiceResetEntrySync>(x => x.service) + '.' + nameof<Service>(x => x.name),
			nameof<ServiceResetEntrySync>(x => x.createdAt)
		]
	};

	static getUserSettingsInformation(): UserSettingsInformation<ServiceResetEntrySyncListingUserSettings> {
		return {
			key: 'ServiceResetEntrySyncListingUserSettings',
			type: ServiceResetEntrySyncListingUserSettings
		};
	}

	public fromJSONObject(item: any): ServiceResetEntrySyncListingUserSettings {
		this.like = item.like;
		this.isActive = item.isActive;
		this.order = item.order;
		this.project = item.project;
		return this;
	}

	public update(lookup: ServiceResetEntrySyncLookup) {
		this.like = lookup.like;
		this.isActive = lookup.isActive;
		this.order = lookup.order;
		this.project = lookup.project;
	}

	public apply(lookup: ServiceResetEntrySyncLookup): ServiceResetEntrySyncLookup {
		lookup.like = this.like;
		lookup.isActive = this.isActive;
		lookup.order = this.order;
		lookup.project = this.project;
		return lookup;
	}
}
