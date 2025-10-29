import { NgModule } from '@angular/core';
import { UnauthorizedRoutingModule } from '@common/unauthorized/unauthorized-routing.module';
import { UnauthorizedComponent } from '@common/unauthorized/unauthorized.component';
import { CommonUiModule } from '@common/ui/common-ui.module';

@NgModule({
	imports: [
		CommonUiModule,
		UnauthorizedRoutingModule,
	],
	declarations: [
		UnauthorizedComponent
	]
})
export class UnauthorizedModule { }
