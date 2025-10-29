import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '@app/core/auth-guard.service';
import { AppPermission } from '@app/core/enum/permission.enum';
import { PendingChangesGuard } from '@common/forms/pending-form-changes/pending-form-changes-guard.service';
import { UserEditorComponent } from '@user-service/ui/users/editor/user-editor.component';
import { UserListingComponent } from '@user-service/ui/users/listing/user-listing.component';
import { UserEditorEnityResolver } from './editor/resolvers/user-editor-entity.resolver';

const routes: Routes = [
	{
		path: '',
		component: UserListingComponent,
		canActivate: [AuthGuard]
	},
	{
		path: 'new',
		canActivate: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.EditUser]
			}
		},
		component: UserEditorComponent,
		canDeactivate: [PendingChangesGuard],
	},
	{
		path: ':id',
		canActivate: [AuthGuard],
		component: UserEditorComponent,
		canDeactivate: [PendingChangesGuard],
		resolve: {
			'entity': UserEditorEnityResolver,
		}
	},
	{ path: '**', loadChildren: () => import('@common/page-not-found/page-not-found.module').then(m => m.PageNotFoundModule) },
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule],
	providers: [UserEditorEnityResolver],
})
export class UserRoutingModule { }
