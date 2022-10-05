import { Pipe, PipeTransform } from '@angular/core';
import { AppEnumUtils } from '@app/core/formatting/enum-utils.service';

@Pipe({ name: 'ServiceSyncStatusFormat' })
export class ServiceSyncStatusPipe implements PipeTransform {
	constructor(private enumUtils: AppEnumUtils) { }

	public transform(value): any {
		return this.enumUtils.toServiceSyncStatusString(value);
	}
}
