import { NgModule } from '@angular/core';
import { DataTableDateFormatPipe, DateFormatPipe } from '@common/formatting/pipes/date-format.pipe';
import { DataTableDateTimeFormatPipe, DateTimeFormatPipe } from '@common/formatting/pipes/date-time-format.pipe';

//
//
// This is shared module that provides all formatting utils. Its imported only once on the AppModule.
//
//
@NgModule({
	declarations: [
		DateFormatPipe,
		DateTimeFormatPipe,
		DataTableDateFormatPipe,
		DataTableDateTimeFormatPipe,
	],
	exports: [
		DateFormatPipe,
		DateTimeFormatPipe,
		DataTableDateFormatPipe,
		DataTableDateTimeFormatPipe,
	],
	providers: [
		DateFormatPipe,
		DateTimeFormatPipe,
		DataTableDateFormatPipe,
		DataTableDateTimeFormatPipe,
	]
})
export class CommonFormattingModule { }
