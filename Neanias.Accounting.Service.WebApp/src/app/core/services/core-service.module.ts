import { ModuleWithProviders, NgModule, Optional, SkipSelf } from '@angular/core';
import { AuthGuard } from '@app/core/auth-guard.service';
import { PrincipalService as AppPrincipalService } from '@app/core/services/http/principal.service';
import { TenantService } from '@app/core/services/http/tenant.service';
import { AuthService } from '@app/core/services/ui/auth.service';
import { ProgressIndicationService } from '@app/core/services/ui/progress-indication.service';
import { ThemingService } from '@app/core/services/ui/theming.service';
import { BaseHttpService } from '@common/base/base-http.service';
import { FormService } from '@common/forms/form-service';
import { LoggingService } from '@common/logging/logging-service';
import { HttpErrorHandlingService } from '@common/modules/errors/error-handling/http-error-handling.service';
import { UiNotificationService } from '@common/modules/notification/ui-notification-service';
import { FilterService } from '@common/modules/text-filter/filter-service';
import { CollectionUtils } from '@common/utilities/collection-utils.service';
import { TypeUtils } from '@common/utilities/type-utils.service';
import { QueryParamsService } from '@app/core/services/ui/query-params.service';
import { ServiceService } from '@app/core/services/http/service.service';
import { UserRoleService } from '@app/core/services/http/user-role.service';
import { ServiceResourceService } from '@app/core/services/http/service-resource.service';
import { ServiceActionService } from '@app/core/services/http/service-action.service';
import { ServiceSyncService } from '@app/core/services/http/service-sync.service';
import { AccountingService } from '@app/core/services/http/accounting.service';
import { UserInfoService } from '@app/core/services/http/user-info.service';
import { ServiceResetEntrySyncService } from '@app/core/services/http/service-reset-entry-sync.service';

//
//
// This is shared module that provides all the services. Its imported only once on the AppModule.
//
//

@NgModule({})
export class CoreAppServiceModule {
	constructor(@Optional() @SkipSelf() parentModule: CoreAppServiceModule) {
		if (parentModule) {
			throw new Error(
				'CoreAppServiceModule is already loaded. Import it in the AppModule only');
		}
	}
	static forRoot(): ModuleWithProviders<CoreAppServiceModule> {
		return {
			ngModule: CoreAppServiceModule,
			providers: [
				AuthService,
				BaseHttpService,
				AuthGuard,
				ThemingService,
				CollectionUtils,
				UiNotificationService,
				ProgressIndicationService,
				HttpErrorHandlingService,
				TenantService,
				FilterService,
				ServiceService,
				ServiceResourceService,
				ServiceSyncService,
				ServiceResetEntrySyncService,
				ServiceActionService,
				AccountingService,
				UserRoleService,
				FormService,
				LoggingService,
				TypeUtils,
				AppPrincipalService,
				QueryParamsService,
				UserInfoService
			],
		};
	}
}
