import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '@app/core/auth-guard.service';
import { AppPermission } from '@app/core/enum/permission.enum';
import { ServiceSyncEditorComponent } from '@app/ui/service-sync/editor/service-sync-editor.component';
import { ServiceSyncListingComponent } from '@app/ui/service-sync/listing/service-sync-listing.component';
import { PendingChangesGuard } from '@common/forms/pending-form-changes/pending-form-changes-guard.service';
import { ServiceSyncEditorEnityResolver } from './editor/resolvers/service-sync-editor.resolver';

const routes: Routes = [
	{
		path: '',
		component: ServiceSyncListingComponent,
		canActivate: [AuthGuard]
	},
	{
		path: 'new',
		canActivate: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.EditServiceSync]
			}
		},
		component: ServiceSyncEditorComponent,
		canDeactivate: [PendingChangesGuard],
	},
	{
		path: ':id',
		canActivate: [AuthGuard],
		component: ServiceSyncEditorComponent,
		canDeactivate: [PendingChangesGuard],
		resolve: {
			'entity': ServiceSyncEditorEnityResolver
		}
	},
	{ path: '**', loadChildren: () => import('@common/page-not-found/page-not-found.module').then(m => m.PageNotFoundModule) },
];
@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule],
	providers: [ServiceSyncEditorEnityResolver],
})
export class ServiceSyncRoutingModule { }
