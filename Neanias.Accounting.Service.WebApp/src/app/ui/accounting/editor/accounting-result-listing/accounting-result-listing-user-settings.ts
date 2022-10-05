import { Serializable } from '@common/types/json/serializable';
import { IsActive } from '@app/core/enum/is-active.enum';
import { Lookup } from '@common/model/lookup';
import { UserSettingsInformation, UserSettingsLookupBuilder } from '@user-service/core/model/user-settings.model';
import { nameof } from 'ts-simple-nameof';
import { Service } from '@app/core/model/service/service.model';
import { AccountingAggregateResultGroup, AccountingAggregateResultItem } from '@app/core/model/accounting/accounting-aggregate-result.model';
import { ServiceResource } from '@app/core/model/service-resource/service-resource.model';
import { AccountingResultLookup } from '@app/core/query/accounting-result.lookup';

export class AccountingResultListingUserSettings implements Serializable<AccountingResultListingUserSettings>, UserSettingsLookupBuilder<AccountingResultLookup> {

	private like: string;
	private order: Lookup.Ordering = { items: [nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.service) + '.' + nameof<Service>(x => x.name)] };
	private project: Lookup.FieldDirectives = {
		fields: [
			nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.service) + '.' + nameof<Service>(x => x.name),
			nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.resource) + '.' + nameof<ServiceResource>(x => x.name),
		]
	};

	static getUserSettingsInformation(): UserSettingsInformation<AccountingResultListingUserSettings> {
		return {
			key: 'AccountingResultListingUserSettings',
			type: AccountingResultListingUserSettings
		};
	}

	public fromJSONObject(item: any): AccountingResultListingUserSettings {
		this.like = item.like;
		this.order = item.order;
		this.project = item.project;
		return this;
	}

	public update(lookup: AccountingResultLookup) {
		this.like = lookup.like;
		this.order = lookup.order;
		this.project = lookup.project;
	}

	public apply(lookup: AccountingResultLookup): AccountingResultLookup {
		lookup.like = this.like;
		lookup.order = this.order;
		lookup.project = this.project;
		return lookup;
	}
}
