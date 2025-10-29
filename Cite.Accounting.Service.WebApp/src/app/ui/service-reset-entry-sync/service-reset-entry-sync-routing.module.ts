import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '@app/core/auth-guard.service';
import { AppPermission } from '@app/core/enum/permission.enum';
import { ServiceResetEntrySyncEditorComponent } from '@app/ui/service-reset-entry-sync/editor/service-reset-entry-sync-editor.component';
import { ServiceResetEntrySyncListingComponent } from '@app/ui/service-reset-entry-sync/listing/service-reset-entry-sync-listing.component';
import { PendingChangesGuard } from '@common/forms/pending-form-changes/pending-form-changes-guard.service';
import { ServiceResetEntrySyncEditorEnityResolver } from './editor/resolvers/service-reset-entry-sync-editor.resolver';

const routes: Routes = [
	{
		path: '',
		component: ServiceResetEntrySyncListingComponent,
		canActivate: [AuthGuard]
	},
	{
		path: 'new',
		canActivate: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.EditServiceResetEntrySync]
			}
		},
		component: ServiceResetEntrySyncEditorComponent,
		canDeactivate: [PendingChangesGuard],
	},
	{
		path: ':id',
		canActivate: [AuthGuard],
		component: ServiceResetEntrySyncEditorComponent,
		canDeactivate: [PendingChangesGuard],
		resolve: {
			'entity': ServiceResetEntrySyncEditorEnityResolver,
		}
	},
	{ path: '**', loadChildren: () => import('@common/page-not-found/page-not-found.module').then(m => m.PageNotFoundModule) },
];
@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule],
	providers: [ServiceResetEntrySyncEditorEnityResolver]
})
export class ServiceResetEntrySyncRoutingModule { }
