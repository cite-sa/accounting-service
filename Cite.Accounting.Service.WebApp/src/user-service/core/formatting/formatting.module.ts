import { DatePipe } from '@angular/common';
import { NgModule } from '@angular/core';
import { CommonFormattingModule } from '@common/formatting/common-formatting.module';
import { UserServiceEnumUtils } from '@user-service/core/formatting/enum-utils.service';
import { CultureInfoDisplayPipe } from '@user-service/core/formatting/pipes/culture-info-display.pipe';
import { IsActiveTypePipe } from '@user-service/core/formatting/pipes/is-active-type.pipe';
import { TimezoneInfoDisplayPipe } from '@user-service/core/formatting/pipes/timezone-info-display.pipe';

//
//
// This is shared module that provides all user service's formatting utils. Its imported only once on the AppModule.
//
//
@NgModule({
	imports: [
		CommonFormattingModule
	],
	declarations: [
		IsActiveTypePipe,
		CultureInfoDisplayPipe,
		TimezoneInfoDisplayPipe
	],
	exports: [
		CommonFormattingModule,
		IsActiveTypePipe,
		CultureInfoDisplayPipe,
		TimezoneInfoDisplayPipe
	],
	providers: [
		UserServiceEnumUtils,
		DatePipe,
		IsActiveTypePipe,
		CultureInfoDisplayPipe,
		TimezoneInfoDisplayPipe
	]
})
export class UserServiceFormattingModule { }
