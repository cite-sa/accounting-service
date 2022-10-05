import { NgModule } from '@angular/core';
import { HomeRoutingModule } from '@app/ui/home/home-routing.module';
import { HomeComponent } from '@app/ui/home/home.component';
import { CommonUiModule } from '@common/ui/common-ui.module';

@NgModule({
	imports: [
		CommonUiModule,
		HomeRoutingModule
	],
	declarations: [
		HomeComponent,
	]
})
export class HomeModule { }
