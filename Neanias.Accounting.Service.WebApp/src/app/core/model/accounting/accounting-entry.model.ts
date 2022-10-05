import { AccountingValueType } from '@app/core/enum/accounting-value-type';
import { MeasureType } from '@app/core/enum/measure-type';
import { ServiceAction } from '@app/core/model/service-action/service-action.model';
import { UserInfo } from '@app/core/model/accounting/user-info.model';
import { ServiceResource } from '@app/core/model/service-resource/service-resource.model';
import { Service } from '@app/core/model/service/service.model';
import { Guid } from '@common/types/guid';

export interface AccountingEntry {
	timeStamp?: Date;
	service: Service;
	level: string;
	user: UserInfo;
	userDelagate: string;
	resource: ServiceResource;
	action: ServiceAction;
	comment: string;
	value?: number;
	measure?: MeasureType;
	type?: AccountingValueType;
	startTime?: Date;
	endTime?: Date;
}


