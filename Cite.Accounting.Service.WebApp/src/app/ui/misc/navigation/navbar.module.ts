import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { BaseComponent } from '@common/base/base.component';
import { CommonUiModule } from '@common/ui/common-ui.module';
import { NavbarComponent } from './navbar.component';
import { LanguageModule } from '@app/ui/language/language.module';

@NgModule({
	imports: [
		CommonUiModule,
		RouterModule,
		LanguageModule,
	],
	declarations: [
		NavbarComponent
	],
	exports: [
		NavbarComponent
	],
})
export class NavbarModule extends BaseComponent {

}
