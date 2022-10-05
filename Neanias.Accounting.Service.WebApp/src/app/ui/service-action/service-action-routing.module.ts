import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '@app/core/auth-guard.service';
import { AppPermission } from '@app/core/enum/permission.enum';
import { ServiceActionEditorComponent } from '@app/ui/service-action/editor/service-action-editor.component';
import { ServiceActionListingComponent } from '@app/ui/service-action/listing/service-action-listing.component';
import { PendingChangesGuard } from '@common/forms/pending-form-changes/pending-form-changes-guard.service';

const routes: Routes = [
	{
		path: '',
		component: ServiceActionListingComponent,
		canActivate: [AuthGuard]
	},
	{
		path: 'new',
		canActivate: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.NewServiceAction]
			}
		},
		component: ServiceActionEditorComponent,
		canDeactivate: [PendingChangesGuard],
	},
	{
		path: ':id',
		canActivate: [AuthGuard],
		component: ServiceActionEditorComponent,
		canDeactivate: [PendingChangesGuard],
	},
	{ path: '**', loadChildren: () => import('@common/page-not-found/page-not-found.module').then(m => m.PageNotFoundModule) },
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule]
})
export class ServiceActionRoutingModule { }
