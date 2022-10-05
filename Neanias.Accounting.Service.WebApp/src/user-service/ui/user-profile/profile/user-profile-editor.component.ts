
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { LanguageType } from '@app/core/enum/language-type.enum';
import { AppEnumUtils } from '@app/core/formatting/enum-utils.service';
import { AuthService } from '@app/core/services/ui/auth.service';
import { BaseComponent } from '@common/base/base.component';
import { FormService } from '@common/forms/form-service';
import { LoggingService } from '@common/logging/logging-service';
import { HttpError, HttpErrorHandlingService } from '@common/modules/errors/error-handling/http-error-handling.service';
import { SnackBarNotificationLevel, UiNotificationService } from '@common/modules/notification/ui-notification-service';
import { Guid } from '@common/types/guid';
import { TranslateService } from '@ngx-translate/core';
import { UserServiceUserProfile } from '@user-service/core/model/user.model';
import { CultureInfo, CultureService } from '@user-service/services/culture.service';
import { UserService } from '@user-service/services/http/user.service';
import { LanguageService } from '@user-service/services/language.service';
import { TimezoneService } from '@user-service/services/timezone.service';
import { UserProfileEditorModel } from '@user-service/ui/user-profile/profile/user-profile-editor.model';
import { takeUntil } from 'rxjs/operators';
import { nameof } from 'ts-simple-nameof';

@Component({
	selector: 'app-user-profile-editor',
	templateUrl: './user-profile-editor.component.html',
	styleUrls: ['./user-profile-editor.component.scss']
})
export class UserProfileEditorComponent extends BaseComponent implements OnInit {

	cultureValues = new Array<CultureInfo>();
	timezoneValues = new Array<string>();
	languageTypeValues: Array<LanguageType>;
	filteredCultures = new Array<CultureInfo>();
	filteredTimezones = new Array<string>();
	profilePhoto: any;

	returnUrl: string;
	formGroup: FormGroup;
	userProfile: UserProfileEditorModel;

	constructor(
		private userService: UserService,
		private route: ActivatedRoute,
		private router: Router,
		private language: TranslateService,
		public appEnumUtils: AppEnumUtils,
		private authService: AuthService,
		private formService: FormService,
		public languageService: LanguageService,
		private cultureService: CultureService,
		private timezoneService: TimezoneService,
		private uiNotificationService: UiNotificationService,
		private logger: LoggingService,
		private httpErrorHandlingService: HttpErrorHandlingService,
		private dialog: MatDialog,
	) {
		super();
	}

	ngOnInit(): void {
		this.route.queryParamMap.pipe(takeUntil(this._destroyed)).subscribe((paramMap: ParamMap) => {
			this.returnUrl = paramMap.get('returnUrl') || '/';
		});

		this.cultureValues = this.cultureService.getCultureValues();
		this.timezoneValues = this.timezoneService.getTimezoneValues();
		this.languageTypeValues = this.appEnumUtils.getEnumValues<LanguageType>(LanguageType).filter(x=> x === LanguageType.English);

		const userId = this.authService.userId();
		if (userId) {
			this.userService.getUserProfile(userId, [
				nameof<UserServiceUserProfile>(x => x.id),
				nameof<UserServiceUserProfile>(x => x.timezone),
				nameof<UserServiceUserProfile>(x => x.culture),
				nameof<UserServiceUserProfile>(x => x.language),
				nameof<UserServiceUserProfile>(x => x.hash),
				nameof<UserServiceUserProfile>(x => x.updatedAt)
			]).pipe(takeUntil(this._destroyed))
				.subscribe(
					data => {
						try {
							this.userProfile = new UserProfileEditorModel().fromModel(data);
							this.formGroup = this.userProfile.buildForm();
							this.registerChangeListeners();
						} catch {
							this.logger.error('Could not parse UserProfile: ' + data);
							this.uiNotificationService.snackBarNotification(this.language.instant('COMMONS.ERRORS.DEFAULT'), SnackBarNotificationLevel.Error);
						}
					},
					error => this.onCallbackError(error)
				);
		}
	}

	registerChangeListeners() {
		this.filteredCultures = this.cultureValues.filter((culture) => culture.name === this.userProfile.culture);
		this.filteredTimezones = this.timezoneValues.filter((zone) => zone === this.userProfile.timezone);

		// set change listeners
		this.formGroup.get('timezone').valueChanges
			.pipe(takeUntil(this._destroyed))
			.subscribe((text) => {
				const searchText = text.toLowerCase();
				const result = this.timezoneValues.filter((zone) => zone.toLowerCase().indexOf(searchText) >= 0);
				this.filteredTimezones = result;
			});

		this.formGroup.get('culture').valueChanges
			.pipe(takeUntil(this._destroyed))
			.subscribe((text) => {
				const searchText = text.toLowerCase();
				const result = this.cultureValues.filter((culture) =>
					culture.name.toLowerCase().indexOf(searchText) >= 0 ||
					culture.nativeName.toLowerCase().indexOf(searchText) >= 0 ||
					culture.displayName.toLowerCase().indexOf(searchText) >= 0
				);
				this.filteredCultures = result;
			});
	}

	formSubmit(): void {
		this.formService.touchAllFormFields(this.formGroup);
		if (!this.isFormValid()) { return; }

		this.userService.updateUserProfile(this.formGroup.value)
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
			this.userProfile.validationErrorModel.fromJSONObject(errorResponse.error);
			this.formService.validateAllFormFields(this.formGroup);
		} else {
			this.uiNotificationService.snackBarNotification(error.getMessagesString(), SnackBarNotificationLevel.Warning);
		}
	}

	clearErrorModel() {
		this.userProfile.validationErrorModel.clear();
		this.formService.validateAllFormFields(this.formGroup);
	}
}
