import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '@app/core/auth-guard.service';
import { UserProfileComponent } from '@user-service/ui/user-profile/user-profile.component';

const routes: Routes = [
	{
		path: ':tab',
		canActivate: [AuthGuard],
		component: UserProfileComponent
	},
	{
		path: '',
		canActivate: [AuthGuard],
		component: UserProfileComponent
	},
	{ path: '**', loadChildren: () => import('@common/page-not-found/page-not-found.module').then(m => m.PageNotFoundModule) },
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule]
})
export class UserProfileRoutingModule { }
