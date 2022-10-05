import { NgModule } from '@angular/core';
import { MatBadgeModule } from '@angular/material/badge';
import { RouterModule } from '@angular/router';
import { NavigationComponent } from '@app/ui/misc/navigation/navigation.component';
import { CommonUiModule } from '@common/ui/common-ui.module';

@NgModule({
	imports: [
		CommonUiModule,
		RouterModule,
		MatBadgeModule,
	],
	declarations: [
		NavigationComponent
	],
	exports: [NavigationComponent]
})
export class NavigationModule { }
