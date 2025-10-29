import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '@app/core/auth-guard.service';
import { AppPermission } from '@app/core/enum/permission.enum';
import { AccountingEditorComponent } from '@app/ui/accounting/editor/accounting-editor.component';
import { PendingChangesGuard } from '@common/forms/pending-form-changes/pending-form-changes-guard.service';
import { AccountingEditorEnityResolver } from './editor/resolvers/accounting-editor-entity.resolver';
import { BreadcrumbService } from '../misc/breadcrumb/breadcrumb.service';

const routes: Routes = [
	{
		path: '',
		canActivate: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.CalculateAccountingInfo]
			},
		},
		resolve: {
			'entity': AccountingEditorEnityResolver
		},
		component: AccountingEditorComponent,
		canDeactivate: [PendingChangesGuard],
	}, {
		path: 'service/:serviceId',
		canActivate: [AuthGuard],
		data: {
			authContext: {
				permissions: [AppPermission.CalculateAccountingInfo]
			},
			...BreadcrumbService.generateRouteDataConfiguration({
				skipNavigation: true,
			})
		},
		resolve: {
			'entity': AccountingEditorEnityResolver
		},
		component: AccountingEditorComponent,
		canDeactivate: [PendingChangesGuard],
	},
	{ path: '**', loadChildren: () => import('@common/page-not-found/page-not-found.module').then(m => m.PageNotFoundModule) },
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule],
	providers: [AccountingEditorEnityResolver],
})
export class AccountingRoutingModule { }
