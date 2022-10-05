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
import { ServiceSyncRoutingModule } from '@app/ui/service-sync/service-sync-routing.module';
import { ServiceSyncListingComponent } from '@app/ui/service-sync/listing/service-sync-listing.component';
import { ServiceSyncEditorComponent } from '@app/ui/service-sync/editor/service-sync-editor.component';
import { ServiceSyncListingFiltersComponent } from '@app/ui/service-sync/listing/filters/service-sync-listing-filters.component';

@NgModule({
	imports: [
		CommonUiModule,
		CommonFormsModule,
		ConfirmationDialogModule,
		ListingModule,
		TextFilterModule,
		FormattingModule,
		ServiceSyncRoutingModule,
		EditorActionsModule,
		AutoCompleteModule,
		UserSettingsModule
	],
	declarations: [
		ServiceSyncListingComponent,
		ServiceSyncEditorComponent,
		ServiceSyncListingFiltersComponent
	]
})
export class ServiceSyncModule { }
