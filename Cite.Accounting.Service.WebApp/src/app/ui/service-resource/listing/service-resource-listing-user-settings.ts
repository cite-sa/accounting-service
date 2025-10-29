import { Serializable } from '@common/types/json/serializable';
import { IsActive } from '@app/core/enum/is-active.enum';
import { Lookup } from '@common/model/lookup';
import { UserSettingsInformation, UserSettingsLookupBuilder } from '@user-service/core/model/user-settings.model';
import { nameof } from 'ts-simple-nameof';
import { ServiceResource } from '@app/core/model/service-resource/service-resource.model';
import { ServiceResourceLookup } from '@app/core/query/service-resource.lookup';
import { Service } from '@app/core/model/service/service.model';

export class ServiceResourceListingUserSettings implements Serializable<ServiceResourceListingUserSettings>, UserSettingsLookupBuilder<ServiceResourceLookup> {

	private like: string;
	private isActive: IsActive[] = [IsActive.Active];
	private order: Lookup.Ordering = { items: [nameof<ServiceResource>(x => x.name)] };
	private project: Lookup.FieldDirectives = {
		fields: [
			nameof<ServiceResource>(x => x.id),
			nameof<ServiceResource>(x => x.name),
			nameof<ServiceResource>(x => x.code),
			nameof<ServiceResource>(x => x.service) + '.' + nameof<Service>(x => x.name),
			nameof<ServiceResource>(x => x.parent) + '.' + nameof<ServiceResource>(x => x.name),
			nameof<ServiceResource>(x => x.createdAt)
		]
	};

	static getUserSettingsInformation(): UserSettingsInformation<ServiceResourceListingUserSettings> {
		return {
			key: 'ServiceResourceListingUserSettings',
			type: ServiceResourceListingUserSettings
		};
	}

	public fromJSONObject(item: any): ServiceResourceListingUserSettings {
		this.like = item.like;
		this.isActive = item.isActive;
		this.order = item.order;
		this.project = item.project;
		return this;
	}

	public update(lookup: ServiceResourceLookup) {
		this.like = lookup.like;
		this.isActive = lookup.isActive;
		this.order = lookup.order;
		this.project = lookup.project;
	}

	public apply(lookup: ServiceResourceLookup): ServiceResourceLookup {
		lookup.like = this.like;
		lookup.isActive = this.isActive;
		lookup.order = this.order;
		lookup.project = this.project;
		return lookup;
	}
}
