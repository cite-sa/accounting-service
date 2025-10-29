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
import { UserRoleRoutingModule } from '@app/ui/user-role/user-role-routing.module';
import { UserRoleListingComponent } from '@app/ui/user-role/listing/user-role-listing.component';
import { UserRoleEditorComponent } from '@app/ui/user-role/editor/user-role-editor.component';
import { UserRoleListingFiltersComponent } from '@app/ui/user-role/listing/filters/user-role-listing-filters.component';

@NgModule({
	imports: [
		CommonUiModule,
		CommonFormsModule,
		ConfirmationDialogModule,
		ListingModule,
		TextFilterModule,
		FormattingModule,
		UserRoleRoutingModule,
		EditorActionsModule,
		AutoCompleteModule,
		UserSettingsModule
	],
	declarations: [
		UserRoleListingComponent,
		UserRoleEditorComponent,
		UserRoleListingFiltersComponent
	]
})
export class UserRoleModule { }
