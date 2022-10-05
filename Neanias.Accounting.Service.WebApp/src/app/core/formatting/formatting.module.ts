import { DatePipe } from '@angular/common';
import { NgModule } from '@angular/core';
import { AppEnumUtils } from '@app/core/formatting/enum-utils.service';
import { IsActiveTypePipe } from '@app/core/formatting/pipes/is-active-type.pipe';
import { ServiceSyncStatusPipe } from '@app/core/formatting/pipes/service-sync-status.pipe';
import { CommonFormattingModule } from '@common/formatting/common-formatting.module';
import { PipeService } from '@common/formatting/pipe.service';

//
//
// This is shared module that provides all formatting utils. Its imported only once on the AppModule.
//
//
@NgModule({
	imports: [
		CommonFormattingModule
	],
	declarations: [
		IsActiveTypePipe,
		ServiceSyncStatusPipe
	],
	exports: [
		CommonFormattingModule,
		IsActiveTypePipe,
		ServiceSyncStatusPipe
	],
	providers: [
		AppEnumUtils,
		PipeService,
		DatePipe,
		IsActiveTypePipe,
		ServiceSyncStatusPipe
	]
})
export class FormattingModule { }
