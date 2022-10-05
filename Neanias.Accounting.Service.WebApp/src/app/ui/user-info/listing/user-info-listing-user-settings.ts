import { Serializable } from '@common/types/json/serializable';
import { IsActive } from '@app/core/enum/is-active.enum';
import { Lookup } from '@common/model/lookup';
import { UserSettingsInformation, UserSettingsLookupBuilder } from '@user-service/core/model/user-settings.model';
import { nameof } from 'ts-simple-nameof';
import { UserInfoLookup } from '@app/core/query/user-info.lookup';
import { Service } from '@app/core/model/service/service.model';
import { UserInfo } from '@app/core/model/accounting/user-info.model';

export class UserInfoListingUserSettings implements Serializable<UserInfoListingUserSettings>, UserSettingsLookupBuilder<UserInfoLookup> {

	private like: string;
	private isActive: IsActive[] = [IsActive.Active];
	private order: Lookup.Ordering = { items: [nameof<UserInfo>(x => x.name)] };
	private project: Lookup.FieldDirectives = {
		fields: [
			nameof<UserInfo>(x => x.id),
			nameof<UserInfo>(x => x.name),
			nameof<UserInfo>(x => x.subject),
			nameof<UserInfo>(x => x.email),
			nameof<UserInfo>(x => x.service) + '.' + nameof<Service>(x => x.name),
			nameof<UserInfo>(x => x.parent) + '.' + nameof<UserInfo>(x => x.name),
			nameof<UserInfo>(x => x.createdAt)
		]
	};

	static getUserSettingsInformation(): UserSettingsInformation<UserInfoListingUserSettings> {
		return {
			key: 'UserInfoListingUserSettings',
			type: UserInfoListingUserSettings
		};
	}

	public fromJSONObject(item: any): UserInfoListingUserSettings {
		this.like = item.like;
		this.isActive = item.isActive;
		this.order = item.order;
		this.project = item.project;
		return this;
	}

	public update(lookup: UserInfoLookup) {
		this.like = lookup.like;
		this.order = lookup.order;
		this.project = lookup.project;
	}

	public apply(lookup: UserInfoLookup): UserInfoLookup {
		lookup.like = this.like;
		lookup.order = this.order;
		lookup.project = this.project;
		return lookup;
	}
}
