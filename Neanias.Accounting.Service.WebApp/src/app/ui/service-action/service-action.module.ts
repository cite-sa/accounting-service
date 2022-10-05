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
import { ServiceActionRoutingModule } from '@app/ui/service-action/service-action-routing.module';
import { ServiceActionListingComponent } from '@app/ui/service-action/listing/service-action-listing.component';
import { ServiceActionEditorComponent } from '@app/ui/service-action/editor/service-action-editor.component';
import { ServiceActionListingFiltersComponent } from '@app/ui/service-action/listing/filters/service-action-listing-filters.component';

@NgModule({
	imports: [
		CommonUiModule,
		CommonFormsModule,
		ConfirmationDialogModule,
		ListingModule,
		TextFilterModule,
		FormattingModule,
		ServiceActionRoutingModule,
		EditorActionsModule,
		AutoCompleteModule,
		UserSettingsModule
	],
	declarations: [
		ServiceActionListingComponent,
		ServiceActionEditorComponent,
		ServiceActionListingFiltersComponent
	]
})
export class ServiceActionModule { }
