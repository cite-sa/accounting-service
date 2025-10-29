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
import { ServiceResetEntrySyncRoutingModule } from '@app/ui/service-reset-entry-sync/service-reset-entry-sync-routing.module';
import { ServiceResetEntrySyncListingComponent } from '@app/ui/service-reset-entry-sync/listing/service-reset-entry-sync-listing.component';
import { ServiceResetEntrySyncEditorComponent } from '@app/ui/service-reset-entry-sync/editor/service-reset-entry-sync-editor.component';
import { ServiceResetEntrySyncListingFiltersComponent } from '@app/ui/service-reset-entry-sync/listing/filters/service-reset-entry-sync-listing-filters.component';

@NgModule({
	imports: [
		CommonUiModule,
		CommonFormsModule,
		ConfirmationDialogModule,
		ListingModule,
		TextFilterModule,
		FormattingModule,
		ServiceResetEntrySyncRoutingModule,
		EditorActionsModule,
		AutoCompleteModule,
		UserSettingsModule
	],
	declarations: [
		ServiceResetEntrySyncListingComponent,
		ServiceResetEntrySyncEditorComponent,
		ServiceResetEntrySyncListingFiltersComponent
	]
})
export class ServiceResetEntrySyncModule { }
