import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '@app/core/auth-guard.service';
import { AppPermission } from '@app/core/enum/permission.enum';
import { BreadcrumbService } from './ui/misc/breadcrumb/breadcrumb.service';

const appRoutes: Routes = [

	{ path: '', redirectTo: 'home', pathMatch: 'full' },
	{
		path: 'home',
		loadChildren: () => import('@app/ui/home/home.module').then(m => m.HomeModule),
		data: {
			...BreadcrumbService.generateRouteDataConfiguration({
				hideNavigationItem: true,
			})
		},
	},
	{
		path: 'login',
		loadChildren: () => import('@idp-service/ui/login/login.module').then(m => m.LoginModule)
	},
	{
		path: 'users',
		canLoad: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.ViewUsersPage]
			},
			...BreadcrumbService.generateRouteDataConfiguration({
				title: 'APP.NAVIGATION.USERS',
			})
		},
		loadChildren: () => import('@user-service/ui/users/user.module').then(m => m.UserModule)
	},
	{
		path: 'accounting',
		canLoad: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.ViewServiceAccountingInfoPage],
			},
		},
		loadChildren: () => import('@app/ui/accounting/accounting.module').then(m => m.AccountingModule)
	},
	{
		path: 'my-accounting',
		canLoad: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.ViewMyAccountingInfoPage]
			},
			...BreadcrumbService.generateRouteDataConfiguration({
				title: 'APP.NAVIGATION.MY-ACCOUNTING',
			})
		},
		loadChildren: () => import('@app/ui/accounting/accounting.module').then(m => m.AccountingModule)
	},
	{
		path: 'user-profile',
		canLoad: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.ViewUserProfilePage]
			}
		},
		loadChildren: () => import('@user-service/ui/user-profile/user-profile.module').then(m => m.UserProfileModule)
	},
	{
		path: 'services',
		canLoad: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.ViewServicePage]
			},
			...BreadcrumbService.generateRouteDataConfiguration({
				title: 'APP.NAVIGATION.SERVICES',
			})
		},
		loadChildren: () => import('@app/ui/service/service.module').then(m => m.ServiceModule)
	},
	{
		path: 'service-resources',
		canLoad: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.ViewServiceResourcePage]
			},
			...BreadcrumbService.generateRouteDataConfiguration({
				title: 'APP.NAVIGATION.SERVICE-RESOURCES',
			})
		},
		loadChildren: () => import('@app/ui/service-resource/service-resource.module').then(m => m.ServiceResourceModule)
	},
	{
		path: 'service-users',
		canLoad: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.ViewUserInfoPage]
			},
			...BreadcrumbService.generateRouteDataConfiguration({
				title: 'APP.NAVIGATION.USER-INFOS',
			})
		},
		loadChildren: () => import('@app/ui/user-info/user-info.module').then(m => m.UserInfoModule)
	},
	{
		path: 'service-syncs',
		canLoad: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.ViewServiceSyncPage]
			},
			...BreadcrumbService.generateRouteDataConfiguration({
				title: 'APP.NAVIGATION.SERVICE-SYNCS',
			})
		},
		loadChildren: () => import('@app/ui/service-sync/service-sync.module').then(m => m.ServiceSyncModule)
	},
	{
		path: 'service-reset-entry-syncs',
		canLoad: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.ViewServiceResetEntrySyncPage]
			},
			...BreadcrumbService.generateRouteDataConfiguration({
				title: 'APP.NAVIGATION.SERVICE-RESET-ENTRY-SYNCS',
			})
		},
		loadChildren: () => import('@app/ui/service-reset-entry-sync/service-reset-entry-sync.module').then(m => m.ServiceResetEntrySyncModule)
	},
	{
		path: 'user-roles',
		canLoad: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.ViewUserRolePage]
			},
			...BreadcrumbService.generateRouteDataConfiguration({
				title: 'APP.NAVIGATION.USER-ROLES',
			})
		},
		loadChildren: () => import('@app/ui/user-role/user-role.module').then(m => m.UserRoleModule)
	},
	{
		path: 'service-actions',
		canLoad: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.ViewServiceActionPage]
			},
			...BreadcrumbService.generateRouteDataConfiguration({
				title: 'APP.NAVIGATION.SERVICE-ACTIONS',
			})
		},
		loadChildren: () => import('@app/ui/service-action/service-action.module').then(m => m.ServiceActionModule)
	},
	{ path: 'logout', loadChildren: () => import('@idp-service/ui/logout/logout.module').then(m => m.LogoutModule) },
	{ path: 'unauthorized', loadChildren: () => import('@common/unauthorized/unauthorized.module').then(m => m.UnauthorizedModule) },
	{ path: '**', loadChildren: () => import('@common/page-not-found/page-not-found.module').then(m => m.PageNotFoundModule) },
];

@NgModule({
	imports: [RouterModule.forRoot(appRoutes)],
	exports: [RouterModule],
})
export class AppRoutingModule { }
