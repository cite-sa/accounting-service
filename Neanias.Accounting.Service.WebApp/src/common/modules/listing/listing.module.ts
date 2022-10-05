import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FormattingModule } from '@app/core/formatting/formatting.module';
import { DataTableHeaderFormattingService } from '@common/modules/listing/data-table-header-formatting-service';
import { ListingSettingsDialogComponent } from '@common/modules/listing/listing-settings/listing-settings-dialog.component';
import { ListingComponent } from '@common/modules/listing/listing.component';
import { CommonUiModule } from '@common/ui/common-ui.module';
import { NgxDatatableModule } from '@swimlane/ngx-datatable';

@NgModule({
	imports: [
		CommonUiModule,
		FormsModule,
		FormattingModule,
		NgxDatatableModule
	],
	declarations: [
		ListingComponent,
		ListingSettingsDialogComponent
	],
	exports: [
		ListingComponent
	],
	entryComponents: [
		ListingSettingsDialogComponent
	],
	providers: [
		DataTableHeaderFormattingService
	]
})
export class ListingModule {
	constructor() { }
}
