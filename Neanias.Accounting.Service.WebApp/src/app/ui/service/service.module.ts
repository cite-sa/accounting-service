import { NgModule } from '@angular/core';
import { FormattingModule } from '@app/core/formatting/formatting.module';
import { ServiceRoutingModule } from '@app/ui/service/service-routing.module';
import { ServiceEditorComponent } from '@app/ui/service/editor/service-editor.component';
import { ServiceListingComponent } from '@app/ui/service/listing/service-listing.component';
import { ServiceListingFiltersComponent } from '@app/ui/service/listing/filters/service-listing-filters.component';
import { CommonFormsModule } from '@common/forms/common-forms.module';
import { ConfirmationDialogModule } from '@common/modules/confirmation-dialog/confirmation-dialog.module';
import { ListingModule } from '@common/modules/listing/listing.module';
import { TextFilterModule } from '@common/modules/text-filter/text-filter.module';
import { CommonUiModule } from '@common/ui/common-ui.module';
import { EditorActionsModule } from '@app/ui/editor-actions/editor-actions.module';
import { UserSettingsModule } from '@common/modules/user-settings/user-settings.module';
import { AutoCompleteModule } from '@common/modules/auto-complete/auto-complete.module';
import { ServiceManagementEditorComponent } from '@app/ui/service/management-editor/service-management-editor.component';

@NgModule({
	imports: [
		CommonUiModule,
		CommonFormsModule,
		ConfirmationDialogModule,
		ListingModule,
		TextFilterModule,
		FormattingModule,
		ServiceRoutingModule,
		EditorActionsModule,
		AutoCompleteModule,
		UserSettingsModule
	],
	declarations: [
		ServiceListingComponent,
		ServiceEditorComponent,
		ServiceListingFiltersComponent,
		ServiceManagementEditorComponent
	]
})
export class ServiceModule { }
