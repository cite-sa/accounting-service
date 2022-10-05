import { Injectable } from '@angular/core';
import { BaseEnumUtilsService } from '@common/base/base-enum-utils.service';
import { TranslateService } from '@ngx-translate/core';
import { IsActive } from '@user-service/core/enum/is-active.enum';

@Injectable()
export class UserServiceEnumUtils extends BaseEnumUtilsService {
	constructor(private language: TranslateService) { super(); }

	public toIsActiveString(value: IsActive): string {
		switch (value) {
			case IsActive.Active: return this.language.instant('USER-SERVICE.TYPES.IS-ACTIVE.ACTIVE');
			case IsActive.Inactive: return this.language.instant('USER-SERVICE.TYPES.IS-ACTIVE.INACTIVE');
			default: return '';
		}
	}
}
