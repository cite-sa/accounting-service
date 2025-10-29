import { NgModule } from '@angular/core';
import { FormattingModule } from '@app/core/formatting/formatting.module';
import { CommonFormsModule } from '@common/forms/common-forms.module';
import { ConfirmationDialogModule } from '@common/modules/confirmation-dialog/confirmation-dialog.module';
import { CommonUiModule } from '@common/ui/common-ui.module';
import { IdpServiceFormattingModule } from '@idp-service/core/formatting/formatting.module';
import { UserServiceFormattingModule } from '@user-service/core/formatting/formatting.module';
import { UserProfilePersonalInfoEditorComponent } from '@user-service/ui/user-profile/personal/personal-info-editor.component';
import { UserProfileEditorComponent } from '@user-service/ui/user-profile/profile/user-profile-editor.component';
import { UserProfileRoutingModule } from '@user-service/ui/user-profile/user-profile-routing.module';
import { UserProfileComponent } from '@user-service/ui/user-profile/user-profile.component';
import { AngularCropperjsModule } from 'angular-cropperjs';

@NgModule({
	imports: [
		UserProfileRoutingModule,
		CommonUiModule,
		CommonFormsModule,
		UserServiceFormattingModule,
		IdpServiceFormattingModule,
		ConfirmationDialogModule,
		FormattingModule,
		AngularCropperjsModule,
	],
	declarations: [
		UserProfileEditorComponent,
		UserProfileComponent,
		UserProfilePersonalInfoEditorComponent,
	]
})
export class UserProfileModule { }
