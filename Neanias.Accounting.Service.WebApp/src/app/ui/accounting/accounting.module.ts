import { NgModule } from '@angular/core';
import { FormattingModule } from '@app/core/formatting/formatting.module';
import { CommonFormsModule } from '@common/forms/common-forms.module';
import { ConfirmationDialogModule } from '@common/modules/confirmation-dialog/confirmation-dialog.module';
import { ListingModule } from '@common/modules/listing/listing.module';
import { TextFilterModule } from '@common/modules/text-filter/text-filter.module';
import { CommonUiModule } from '@common/ui/common-ui.module';
import { EditorActionsModule } from '@app/ui/editor-actions/editor-actions.module';
import { UserSettingsModule } from '@common/modules/user-settings/user-settings.module';
import { AutoCompleteModule } from '@common/modules/auto-complete/auto-complete.module';
import { AccountingEditorComponent } from '@app/ui/accounting/editor/accounting-editor.component';
import { AccountingRoutingModule } from '@app/ui/accounting/accounting-routing.module';
import { AccountingResultListingComponent } from '@app/ui/accounting/editor/accounting-result-listing/accounting-result-listing.component';
import { ChartJsModule } from '@common/modules/chart-js/chart-js.module';
import { AccountingResultChartComponent } from '@app/ui/accounting/editor/accounting-result-chart/accounting-result-chart.component';

@NgModule({
	imports: [
		CommonUiModule,
		CommonFormsModule,
		ConfirmationDialogModule,
		ListingModule,
		ChartJsModule,
		TextFilterModule,
		FormattingModule,
		AccountingRoutingModule,
		EditorActionsModule,
		AutoCompleteModule,
		UserSettingsModule
	],
	declarations: [
		AccountingEditorComponent,
		AccountingResultListingComponent,
		AccountingResultChartComponent
	]
})
export class AccountingModule { }
