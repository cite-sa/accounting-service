import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '@app/core/auth-guard.service';
import { AppPermission } from '@app/core/enum/permission.enum';
import { ServiceResourceEditorComponent } from '@app/ui/service-resource/editor/service-resource-editor.component';
import { ServiceResourceListingComponent } from '@app/ui/service-resource/listing/service-resource-listing.component';
import { PendingChangesGuard } from '@common/forms/pending-form-changes/pending-form-changes-guard.service';
import { ServiceResourceEditorEnityResolver } from './editor/resolvers/service-resource-editor.resolver';

const routes: Routes = [
	{
		path: '',
		component: ServiceResourceListingComponent,
		canActivate: [AuthGuard]
	},
	{
		path: 'new',
		canActivate: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.NewServiceResource]
			}
		},
		component: ServiceResourceEditorComponent,
		canDeactivate: [PendingChangesGuard],
	},
	{
		path: ':id',
		canActivate: [AuthGuard],
		component: ServiceResourceEditorComponent,
		canDeactivate: [PendingChangesGuard],
		resolve: {
			'entity': ServiceResourceEditorEnityResolver,
		}
	},
	{ path: '**', loadChildren: () => import('@common/page-not-found/page-not-found.module').then(m => m.PageNotFoundModule) },
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule],
	providers: [ServiceResourceEditorEnityResolver],
})
export class ServiceResourceRoutingModule { }
