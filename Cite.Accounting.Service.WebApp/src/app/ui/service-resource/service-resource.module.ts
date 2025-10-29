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
import { ServiceResourceRoutingModule } from '@app/ui/service-resource/service-resource-routing.module';
import { ServiceResourceListingComponent } from '@app/ui/service-resource/listing/service-resource-listing.component';
import { ServiceResourceEditorComponent } from '@app/ui/service-resource/editor/service-resource-editor.component';
import { ServiceResourceListingFiltersComponent } from '@app/ui/service-resource/listing/filters/service-resource-listing-filters.component';

@NgModule({
	imports: [
		CommonUiModule,
		CommonFormsModule,
		ConfirmationDialogModule,
		ListingModule,
		TextFilterModule,
		FormattingModule,
		ServiceResourceRoutingModule,
		EditorActionsModule,
		AutoCompleteModule,
		UserSettingsModule
	],
	declarations: [
		ServiceResourceListingComponent,
		ServiceResourceEditorComponent,
		ServiceResourceListingFiltersComponent
	]
})
export class ServiceResourceModule { }
