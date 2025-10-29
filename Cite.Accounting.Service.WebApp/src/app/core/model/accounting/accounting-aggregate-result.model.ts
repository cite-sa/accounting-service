import { AccountingValueType } from '@app/core/enum/accounting-value-type';
import { DateIntervalType } from '@app/core/enum/date-interval-type';
import { MeasureType } from '@app/core/enum/measure-type';
import { ServiceAction } from '@app/core/model/service-action/service-action.model';
import { ServiceResource } from '@app/core/model/service-resource/service-resource.model';
import { Service } from '@app/core/model/service/service.model';
import { UserInfo } from '@app/core/model/accounting/user-info.model';

export interface AccountingAggregateResultGroup {
	service: Service;
	userId: string;
	userDelagate: string;
	resource: ServiceResource;
	action: ServiceAction;
	user: UserInfo;
	timeStamp: string;
}


export interface AccountingAggregateResultItem {
	group: AccountingAggregateResultGroup;
	sum?: number;
	average?: number;
	min?: number;
	max?: number;
}
