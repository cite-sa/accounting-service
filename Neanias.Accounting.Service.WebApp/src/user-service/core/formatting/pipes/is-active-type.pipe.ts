import { Pipe, PipeTransform } from '@angular/core';
import { UserServiceEnumUtils } from '@user-service/core/formatting/enum-utils.service';

@Pipe({ name: 'IsActiveTypeFormat' })
export class IsActiveTypePipe implements PipeTransform {
	constructor(private enumUtils: UserServiceEnumUtils) { }

	public transform(value): any {
		return this.enumUtils.toIsActiveString(value);
	}
}
