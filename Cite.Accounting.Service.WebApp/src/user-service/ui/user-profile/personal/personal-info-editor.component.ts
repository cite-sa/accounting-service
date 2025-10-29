
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { AuthService } from '@app/core/services/ui/auth.service';
import { BaseComponent } from '@common/base/base.component';
import { FormService } from '@common/forms/form-service';
import { LoggingService } from '@common/logging/logging-service';
import { HttpError, HttpErrorHandlingService } from '@common/modules/errors/error-handling/http-error-handling.service';
import { SnackBarNotificationLevel, UiNotificationService } from '@common/modules/notification/ui-notification-service';
import { TranslateService } from '@ngx-translate/core';
import { UserServiceNamePatch, UserServiceUser } from '@user-service/core/model/user.model';
import { UserService } from '@user-service/services/http/user.service';
import { UserProfilePersonalInfoEditorModel } from '@user-service/ui/user-profile/personal/personal-info-editor.model';
import { takeUntil } from 'rxjs/operators';
import { nameof } from 'ts-simple-nameof';

@Component({
	selector: 'app-personal-info-editor',
	templateUrl: './personal-info-editor.component.html',
	styleUrls: ['./personal-info-editor.component.scss']
})

export class UserProfilePersonalInfoEditorComponent extends BaseComponent implements OnInit {

	returnUrl: string;
	formGroup: FormGroup;
	personalModel: UserProfilePersonalInfoEditorModel;
	user: UserServiceUser;

	constructor(
		private userService: UserService,
		private route: ActivatedRoute,
		private router: Router,
		private language: TranslateService,
		private authService: AuthService,
		private formService: FormService,
		private uiNotificationService: UiNotificationService,
		private logger: LoggingService,
		private httpErrorHandlingService: HttpErrorHandlingService,
	) {
		super();
	}

	ngOnInit(): void {
		this.route.queryParamMap.pipe(takeUntil(this._destroyed)).subscribe((paramMap: ParamMap) => {
			this.returnUrl = paramMap.get('returnUrl') || '/';
		});

		const userId = this.authService.userId();
		if (userId) {
			this.userService.getSingle(userId, [
				nameof<UserServiceUser>(x => x.id),
				nameof<UserServiceUser>(x => x.name),
				nameof<UserServiceUser>(x => x.email),
				nameof<UserServiceUser>(x => x.hash),
			]).pipe(takeUntil(this._destroyed))
				.subscribe(
					data => {
						try {
							this.user = data;
							this.personalModel = new UserProfilePersonalInfoEditorModel().fromModel(this.user);
							this.formGroup = this.personalModel.buildForm(true);
						} catch {
							this.logger.error('Could not parse UserProfile: ' + data);
							this.uiNotificationService.snackBarNotification(this.language.instant('COMMONS.ERRORS.DEFAULT'), SnackBarNotificationLevel.Error);
						}
					},
					error => this.onCallbackError(error)
				);
		}
	}

	formSubmit(): void {
		this.formService.touchAllFormFields(this.formGroup);
		if (!this.isFormValid()) { return; }

		const item: UserServiceNamePatch = {
			id: this.user.id,
			name: this.formGroup.value.name
		};
		this.userService.updateUserName(item)
			.pipe(takeUntil(this._destroyed))
			.subscribe(
				complete => this.onCallbackSuccess(),
				error => this.onCallbackError(error)
			);
	}

	public isFormValid() {
		return this.formGroup.valid;
	}

	public save() {
		this.clearErrorModel();
	}

	public cancel(): void {
		this.router.navigate([this.returnUrl]);
	}

	onCallbackSuccess(): void {
		// we need to refresh the page to apply culture changes
		window.location.href = this.returnUrl;
	}

	onCallbackError(errorResponse: HttpErrorResponse) {
		const error: HttpError = this.httpErrorHandlingService.getError(errorResponse);
		if (error.statusCode === 400) {
			this.personalModel.validationErrorModel.fromJSONObject(errorResponse.error);
			this.formService.validateAllFormFields(this.formGroup);
		} else {
			this.uiNotificationService.snackBarNotification(error.getMessagesString(), SnackBarNotificationLevel.Warning);
		}
	}

	clearErrorModel() {
		this.personalModel.validationErrorModel.clear();
		this.formService.validateAllFormFields(this.formGroup);
	}
}
