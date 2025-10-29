import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '@app/core/auth-guard.service';
import { AppPermission } from '@app/core/enum/permission.enum';
import { ServiceEditorComponent } from '@app/ui/service/editor/service-editor.component';
import { ServiceListingComponent } from '@app/ui/service/listing/service-listing.component';
import { ServiceManagementEditorComponent } from '@app/ui/service/management-editor/service-management-editor.component';
import { PendingChangesGuard } from '@common/forms/pending-form-changes/pending-form-changes-guard.service';
import { ServiceEditorEnityResolver } from './editor/resolvers/service-editor-entity.resolver';
import { ServiceManagementEditorResolver } from './management-editor/resolvers/service-management-editor.resolver';

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
		resolve: {
			'entity': ServiceEditorEnityResolver
		},
	},
	{
		path: ':id/manage',
		canActivate: [AuthGuard],
		component: ServiceManagementEditorComponent,
		canDeactivate: [PendingChangesGuard],
		resolve: {
			'entity': ServiceManagementEditorResolver,
		}
	},
	{ path: '**', loadChildren: () => import('@common/page-not-found/page-not-found.module').then(m => m.PageNotFoundModule) },
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule],
	providers: [ServiceEditorEnityResolver, ServiceManagementEditorResolver],
})
export class ServiceRoutingModule { }
