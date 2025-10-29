import { Serializable } from '@common/types/json/serializable';
import { IsActive } from '@app/core/enum/is-active.enum';
import { Service } from '@app/core/model/service/service.model';
import { ServiceLookup } from '@app/core/query/service.lookup';
import { Lookup } from '@common/model/lookup';
import { UserSettingsInformation, UserSettingsLookupBuilder } from '@user-service/core/model/user-settings.model';
import { nameof } from 'ts-simple-nameof';
import { AppPermission } from '@app/core/enum/permission.enum';

export class ServiceListingUserSettings implements Serializable<ServiceListingUserSettings>, UserSettingsLookupBuilder<ServiceLookup> {

	private like: string;
	private isActive: IsActive[] = [IsActive.Active];
	private order: Lookup.Ordering = { items: [nameof<Service>(x => x.name)] };
	private project: Lookup.FieldDirectives = {
		fields: [
			nameof<Service>(x => x.id),
			nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.EnforceServiceSync],
			nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.ServiceCleanUp],
			nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.AddDummyAccountingEntry],
			nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.CalculateServiceAccountingInfo],
			nameof<Service>(x => x.name),
			nameof<Service>(x => x.code),
			nameof<Service>(x => x.parent) + '.' + nameof<Service>(x => x.name),
			nameof<Service>(x => x.createdAt)
		]
	};

	static getUserSettingsInformation(): UserSettingsInformation<ServiceListingUserSettings> {
		return {
			key: 'ServiceListingUserSettings',
			type: ServiceListingUserSettings
		};
	}

	public fromJSONObject(item: any): ServiceListingUserSettings {
		this.like = item.like;
		this.isActive = item.isActive;
		this.order = item.order;
		this.project = item.project;
		return this;
	}

	public update(lookup: ServiceLookup) {
		this.like = lookup.like;
		this.isActive = lookup.isActive;
		this.order = lookup.order;
		this.project = lookup.project;
	}

	public apply(lookup: ServiceLookup): ServiceLookup {
		lookup.like = this.like;
		lookup.isActive = this.isActive;
		lookup.order = this.order;
		lookup.project = this.project;
		return lookup;
	}
}
