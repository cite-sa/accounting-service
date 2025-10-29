import { HttpErrorResponse } from '@angular/common/http';
import { Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseComponent } from '@common/base/base.component';
import { LoggingService } from '@common/logging/logging-service';
import { HttpError, HttpErrorHandlingService } from '@common/modules/errors/error-handling/http-error-handling.service';
import { SnackBarNotificationLevel, UiNotificationService } from '@common/modules/notification/ui-notification-service';
import { Guid } from '@common/types/guid';
import { TranslateService } from '@ngx-translate/core';
import {  takeUntil } from 'rxjs/operators';
import { InstallationConfigurationService } from '@common/installation-configuration/installation-configuration.service';
import { KeycloakService } from 'keycloak-angular';
import { from } from 'rxjs';
import { AuthService } from '@app/core/services/ui/auth.service';

export enum LoginComponentMode {
	Basic = 0,
	DirectLinkMail = 1,
}

@Component({
	templateUrl: './login.component.html',
	styleUrls: ['./login.component.scss'],
})
export class LoginComponent extends BaseComponent implements OnInit {
	returnUrl: string;
	tenantId: Guid;
	loginComponentMode = LoginComponentMode.Basic;
	loginComponentModeEnum = LoginComponentMode;

	//Totp
	totpKey: string;
	needsTotpVerification = false;

	constructor(
		private zone: NgZone,
		private route: ActivatedRoute,
		private router: Router,
		private language: TranslateService,
		private uiNotificationService: UiNotificationService,
		private loggingService: LoggingService,
		private httpErrorHandlingService: HttpErrorHandlingService,
		public installationConfiguration: InstallationConfigurationService,
		private keycloak: KeycloakService,
		private authService: AuthService
	) {
		super();
	}

	ngOnInit() {
		// get return url from route parameters or default to '/'
		this.returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/';
		// this.route.paramMap.pipe(takeUntil(this._destroyed)).subscribe((paramMap) => {
		// 	if (paramMap.has('tenantCode')) {
		// 		// this.getTenantId(paramMap.get('tenantCode'));
		// 	}
		// });

		//if (!this.installationConfiguration.isMultitenant) { this.getAuthManager(null); }
		if (!this.keycloak.isLoggedIn()) {
			this.authService.authenticate(this.returnUrl);
		} else {
			this.authService.prepareAuthRequestNew(from(this.keycloak.getToken())).pipe(takeUntil(this._destroyed)).subscribe(() => this.onAuthenticateSuccess(), (error) => this.onAuthenticateError(error));
		}
	}


	onAuthenticateSuccess(): void {
		this.loggingService.info('Successful Login');
		this.uiNotificationService.snackBarNotification(this.language.instant('COMMONS.SNACK-BAR.SUCCESSFUL-LOGIN'), SnackBarNotificationLevel.Success);
		this.zone.run(() => this.router.navigate([this.returnUrl]));
	}

	onAuthenticateError(errorResponse: HttpErrorResponse) {
		this.zone.run(() => {
			const error: HttpError = this.httpErrorHandlingService.getError(errorResponse);
			this.uiNotificationService.snackBarNotification(error.getMessagesString(), SnackBarNotificationLevel.Warning);
		});
	}
}
