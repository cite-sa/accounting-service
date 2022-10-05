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
import { UserInfoRoutingModule } from '@app/ui/user-info/user-info-routing.module';
import { UserInfoListingComponent } from '@app/ui/user-info/listing/user-info-listing.component';
import { UserInfoEditorComponent } from '@app/ui/user-info/editor/user-info-editor.component';
import { UserInfoListingFiltersComponent } from '@app/ui/user-info/listing/filters/user-info-listing-filters.component';

@NgModule({
	imports: [
		CommonUiModule,
		CommonFormsModule,
		ConfirmationDialogModule,
		ListingModule,
		TextFilterModule,
		FormattingModule,
		UserInfoRoutingModule,
		EditorActionsModule,
		AutoCompleteModule,
		UserSettingsModule
	],
	declarations: [
		UserInfoListingComponent,
		UserInfoEditorComponent,
		UserInfoListingFiltersComponent
	]
})
export class UserInfoModule { }
