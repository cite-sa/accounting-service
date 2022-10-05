import { Serializable } from '@common/types/json/serializable';
import { IsActive } from '@app/core/enum/is-active.enum';
import { Lookup } from '@common/model/lookup';
import { UserSettingsInformation, UserSettingsLookupBuilder } from '@user-service/core/model/user-settings.model';
import { nameof } from 'ts-simple-nameof';
import { ServiceAction } from '@app/core/model/service-action/service-action.model';
import { ServiceActionLookup } from '@app/core/query/service-action.lookup';
import { Service } from '@app/core/model/service/service.model';

export class ServiceActionListingUserSettings implements Serializable<ServiceActionListingUserSettings>, UserSettingsLookupBuilder<ServiceActionLookup> {

	private like: string;
	private isActive: IsActive[] = [IsActive.Active];
	private order: Lookup.Ordering = { items: [nameof<ServiceAction>(x => x.name)] };
	private project: Lookup.FieldDirectives = {
		fields: [
			nameof<ServiceAction>(x => x.id),
			nameof<ServiceAction>(x => x.name),
			nameof<ServiceAction>(x => x.code),
			nameof<ServiceAction>(x => x.service) + '.' + nameof<Service>(x => x.name),
			nameof<ServiceAction>(x => x.parent) + '.' + nameof<ServiceAction>(x => x.name),
			nameof<ServiceAction>(x => x.createdAt)
		]
	};

	static getUserSettingsInformation(): UserSettingsInformation<ServiceActionListingUserSettings> {
		return {
			key: 'ServiceActionListingUserSettings',
			type: ServiceActionListingUserSettings
		};
	}

	public fromJSONObject(item: any): ServiceActionListingUserSettings {
		this.like = item.like;
		this.isActive = item.isActive;
		this.order = item.order;
		this.project = item.project;
		return this;
	}

	public update(lookup: ServiceActionLookup) {
		this.like = lookup.like;
		this.isActive = lookup.isActive;
		this.order = lookup.order;
		this.project = lookup.project;
	}

	public apply(lookup: ServiceActionLookup): ServiceActionLookup {
		lookup.like = this.like;
		lookup.isActive = this.isActive;
		lookup.order = this.order;
		lookup.project = this.project;
		return lookup;
	}
}
