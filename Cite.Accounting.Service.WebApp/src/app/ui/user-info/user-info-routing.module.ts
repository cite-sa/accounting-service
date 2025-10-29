import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '@app/core/auth-guard.service';
import { AppPermission } from '@app/core/enum/permission.enum';
import { UserInfoEditorComponent } from '@app/ui/user-info/editor/user-info-editor.component';
import { UserInfoListingComponent } from '@app/ui/user-info/listing/user-info-listing.component';
import { PendingChangesGuard } from '@common/forms/pending-form-changes/pending-form-changes-guard.service';
import { UserInfoEditorEnityResolver } from './editor/resolvers/user-info-editor.resolver';

const routes: Routes = [
	{
		path: '',
		component: UserInfoListingComponent,
		canActivate: [AuthGuard]
	},
	{
		path: 'new',
		canActivate: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.NewUserInfo]
			}
		},
		component: UserInfoEditorComponent,
		canDeactivate: [PendingChangesGuard],
	},
	{
		path: ':id',
		canActivate: [AuthGuard],
		component: UserInfoEditorComponent,
		canDeactivate: [PendingChangesGuard],
		resolve: {
			'entity': UserInfoEditorEnityResolver,
		}
	},
	{ path: '**', loadChildren: () => import('@common/page-not-found/page-not-found.module').then(m => m.PageNotFoundModule) },
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule],
	providers: [UserInfoEditorEnityResolver]
})
export class UserInfoRoutingModule { }
