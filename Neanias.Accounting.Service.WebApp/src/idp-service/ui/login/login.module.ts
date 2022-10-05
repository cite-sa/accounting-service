import { NgModule } from '@angular/core';
import { CommonFormsModule } from '@common/forms/common-forms.module';
import { CommonUiModule } from '@common/ui/common-ui.module';
import { LoginRoutingModule } from '@idp-service/ui/login/login-routing.module';
import { LoginComponent } from '@idp-service/ui/login/login.component';
import { NgxCaptchaModule } from 'ngx-captcha';

@NgModule({
	imports: [
		CommonUiModule,
		CommonFormsModule,
		NgxCaptchaModule,
		LoginRoutingModule
	],
	declarations: [LoginComponent],
	entryComponents: [
	]
})
export class LoginModule { }
