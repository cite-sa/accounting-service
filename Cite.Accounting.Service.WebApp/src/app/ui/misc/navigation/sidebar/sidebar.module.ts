import { NgModule } from '@angular/core';
import { MatBadgeModule } from '@angular/material/badge';
import { RouterModule } from '@angular/router';
import { SidebarComponent } from '@app/ui/misc/navigation/sidebar/sidebar.component';
import { CommonUiModule } from '@common/ui/common-ui.module';

@NgModule({
	imports: [
		CommonUiModule,
		RouterModule,
		MatBadgeModule,
	],
	declarations: [
		SidebarComponent
	],
	exports: [SidebarComponent]
})
export class SidebarModule { }
