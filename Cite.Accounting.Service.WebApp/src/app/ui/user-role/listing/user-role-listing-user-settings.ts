import { Serializable } from '@common/types/json/serializable';
import { IsActive } from '@app/core/enum/is-active.enum';
import { Lookup } from '@common/model/lookup';
import { UserSettingsInformation, UserSettingsLookupBuilder } from '@user-service/core/model/user-settings.model';
import { nameof } from 'ts-simple-nameof';
import { UserRole } from '@app/core/model/user-role/user-role.model';
import { UserRoleLookup } from '@app/core/query/user-role.lookup';
import { Service } from '@app/core/model/service/service.model';

export class UserRoleListingUserSettings implements Serializable<UserRoleListingUserSettings>, UserSettingsLookupBuilder<UserRoleLookup> {

	private like: string;
	private isActive: IsActive[] = [IsActive.Active];
	private order: Lookup.Ordering = { items: [nameof<UserRole>(x => x.name)] };
	private project: Lookup.FieldDirectives = {
		fields: [
			nameof<UserRole>(x => x.id),
			nameof<UserRole>(x => x.name),
			nameof<UserRole>(x => x.propagate),
			nameof<UserRole>(x => x.createdAt)
		]
	};

	static getUserSettingsInformation(): UserSettingsInformation<UserRoleListingUserSettings> {
		return {
			key: 'UserRoleListingUserSettings',
			type: UserRoleListingUserSettings
		};
	}

	public fromJSONObject(item: any): UserRoleListingUserSettings {
		this.like = item.like;
		this.isActive = item.isActive;
		this.order = item.order;
		this.project = item.project;
		return this;
	}

	public update(lookup: UserRoleLookup) {
		this.like = lookup.like;
		this.isActive = lookup.isActive;
		this.order = lookup.order;
		this.project = lookup.project;
	}

	public apply(lookup: UserRoleLookup): UserRoleLookup {
		lookup.like = this.like;
		lookup.isActive = this.isActive;
		lookup.order = this.order;
		lookup.project = this.project;
		return lookup;
	}
}
