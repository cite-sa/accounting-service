import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '@app/core/auth-guard.service';
import { AppPermission } from '@app/core/enum/permission.enum';
import { ServiceEditorComponent } from '@app/ui/service/editor/service-editor.component';
import { ServiceListingComponent } from '@app/ui/service/listing/service-listing.component';
import { ServiceManagementEditorComponent } from '@app/ui/service/management-editor/service-management-editor.component';
import { PendingChangesGuard } from '@common/forms/pending-form-changes/pending-form-changes-guard.service';

const routes: Routes = [
	{
		path: '',
		component: ServiceListingComponent,
		canActivate: [AuthGuard]
	},
	{
		path: 'new',
		canActivate: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.NewService]
			}
		},
		component: ServiceEditorComponent,
		canDeactivate: [PendingChangesGuard],
	},
	{
		path: ':id',
		canActivate: [AuthGuard],
		component: ServiceEditorComponent,
		canDeactivate: [PendingChangesGuard],
	},
	{
		path: ':id/manage',
		canActivate: [AuthGuard],
		component: ServiceManagementEditorComponent,
		canDeactivate: [PendingChangesGuard],
	},
	{ path: '**', loadChildren: () => import('@common/page-not-found/page-not-found.module').then(m => m.PageNotFoundModule) },
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule]
})
export class ServiceRoutingModule { }
