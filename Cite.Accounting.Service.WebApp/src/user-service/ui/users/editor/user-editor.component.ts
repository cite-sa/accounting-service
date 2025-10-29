import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormArray, FormGroup } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { LanguageType } from '@app/core/enum/language-type.enum';
import { AppPermission } from '@app/core/enum/permission.enum';
import { AppEnumUtils } from '@app/core/formatting/enum-utils.service';
import { ServiceUser } from '@app/core/model/service-user/service-user.model';
import { Service } from '@app/core/model/service/service.model';
import { UserRole } from '@app/core/model/user-role/user-role.model';
import { ServiceService } from '@app/core/services/http/service.service';
import { UserRoleService } from '@app/core/services/http/user-role.service';
import { AuthService } from '@app/core/services/ui/auth.service';
import { BasePendingChangesComponent } from '@common/base/base-pending-changes.component';
import { FormService } from '@common/forms/form-service';
import { InstallationConfigurationService } from '@common/installation-configuration/installation-configuration.service';
import { LoggingService } from '@common/logging/logging-service';
import { ConfirmationDialogComponent } from '@common/modules/confirmation-dialog/confirmation-dialog.component';
import { HttpError, HttpErrorHandlingService } from '@common/modules/errors/error-handling/http-error-handling.service';
import { SnackBarNotificationLevel, UiNotificationService } from '@common/modules/notification/ui-notification-service';
import { Guid } from '@common/types/guid';
import { TranslateService } from '@ngx-translate/core';
import { IsActive } from '@user-service/core/enum/is-active.enum';
import { UserServiceUser, UserServiceUserProfile } from '@user-service/core/model/user.model';
import { CultureInfo, CultureService } from '@user-service/services/culture.service';
import { UserService } from '@user-service/services/http/user.service';
import { LanguageService } from '@user-service/services/language.service';
import { TimezoneService } from '@user-service/services/timezone.service';
import { ServiceUserEditorModel, UserEditorModel } from '@user-service/ui/users/editor/user-editor.model';
import { Observable } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { nameof } from 'ts-simple-nameof';

@Component({
	selector: 'app-user-editor',
	templateUrl: './user-editor.component.html',
	styleUrls: ['./user-editor.component.scss']
})

export class UserEditorComponent extends BasePendingChangesComponent implements OnInit {
	canEdit = false;
	canDelete = false;

	cultureValues = new Array<CultureInfo>();
	timezoneValues = new Array<string>();
	languageTypeValues: Array<LanguageType>;
	filteredCultures = new Array<CultureInfo>();
	filteredTimezones = new Array<string>();
	isNew = true;
	isDeleted = false;

	formGroup: FormGroup = null;
	user: UserEditorModel;
	singleServiceAutocompleteConfiguration = null;
	singleUserRoleAutocompleteConfiguration = null;

	constructor(
		public authService: AuthService,
		private dialog: MatDialog,
		private userService: UserService,
		private route: ActivatedRoute,
		private router: Router,
		private language: TranslateService,
		public appEnumUtils: AppEnumUtils,
		private formService: FormService,
		private cultureService: CultureService,
		private timezoneService: TimezoneService,
		private uiNotificationService: UiNotificationService,
		private logger: LoggingService,
		private httpErrorHandlingService: HttpErrorHandlingService,
		public languageService: LanguageService,
		public serviceService: ServiceService,
		public userRoleService: UserRoleService,
		private installationConfiguration: InstallationConfigurationService,
	) {
		super();
	}

	ngOnInit(): void {
		this.singleServiceAutocompleteConfiguration = this.serviceService.CreateSingleAutoCompleteConfiguration(null);
		this.singleUserRoleAutocompleteConfiguration = this.userRoleService.CreateSingleAutoCompleteConfiguration(null);
		this.cultureValues = this.cultureService.getCultureValues();
		this.timezoneValues = this.timezoneService.getTimezoneValues();
		this.languageTypeValues = this.appEnumUtils.getEnumValues<LanguageType>(LanguageType).filter(x=> x === LanguageType.English);

		this.route.paramMap.pipe(takeUntil(this._destroyed)).subscribe((paramMap: ParamMap) => {
			const itemId = paramMap.get('id');

			if (itemId != null) {
				this.isNew = false;
				this.userService.getSingle(Guid.parse(itemId), [
					nameof<UserServiceUser>(x => x.id), nameof<UserServiceUser>(x => x.name),
					nameof<UserServiceUser>(x => x.subject), nameof<UserServiceUser>(x => x.email),
					nameof<UserServiceUser>(x => x.issuer),
					nameof<UserServiceUser>(x => x.isActive),
					nameof<UserServiceUser>(x => x.isActive),
					nameof<UserServiceUser>(x => x.hash), nameof<UserServiceUser>(x => x.updatedAt),
					nameof<UserServiceUser>(x => x.profile) + '.' + nameof<UserServiceUserProfile>(x => x.id),
					nameof<UserServiceUser>(x => x.profile) + '.' + nameof<UserServiceUserProfile>(x => x.timezone),
					nameof<UserServiceUser>(x => x.profile) + '.' + nameof<UserServiceUserProfile>(x => x.culture),
					nameof<UserServiceUser>(x => x.profile) + '.' + nameof<UserServiceUserProfile>(x => x.language),
					nameof<UserServiceUser>(x => x.serviceUsers) + '.' + nameof<ServiceUser>(x => x.id),
					nameof<UserServiceUser>(x => x.serviceUsers) + '.' + nameof<ServiceUser>(x => x.service) + '.' + nameof<Service>(x => x.id),
					nameof<UserServiceUser>(x => x.serviceUsers) + '.' + nameof<ServiceUser>(x => x.service) + '.' + nameof<Service>(x => x.name),
					nameof<UserServiceUser>(x => x.serviceUsers) + '.' + nameof<ServiceUser>(x => x.role)+ '.' + nameof<UserRole>(x => x.id),
					nameof<UserServiceUser>(x => x.serviceUsers) + '.' + nameof<ServiceUser>(x => x.role)+ '.' + nameof<UserRole>(x => x.name),
				]).pipe(takeUntil(this._destroyed))
					.subscribe(
						data => {
							try {
								const userObject = data;
								this.user = new UserEditorModel().fromModel(userObject);
								this.isDeleted = data.isActive === IsActive.Inactive;
								this.canEdit = this.authService.hasPermission(AppPermission.EditUser);
								this.canDelete = this.authService.hasPermission(AppPermission.DeleteUser);
								this.buildForm(this.isDeleted || !this.canEdit);
								return;
							} catch (e) {
								this.logger.error('Could not parse User: ' + data);
								this.uiNotificationService.snackBarNotification(this.language.instant('COMMONS.ERRORS.DEFAULT'), SnackBarNotificationLevel.Error);
							}
						},
						error => this.onCallbackError(error)
					);
			} else {
				this.user = new UserEditorModel();
				this.buildForm();
			}
		});
	}

	buildForm(disabled: boolean = false) {
		this.formGroup = this.user.buildForm(this.installationConfiguration, this.languageService, null, disabled);

		this.filteredCultures = this.isNew ?
			this.cultureValues :
			this.cultureValues.filter((culture) => culture.name === this.user.profile.culture);
		this.filteredTimezones = this.isNew ?
			this.timezoneValues :
			this.timezoneValues.filter((zone) => zone === this.user.profile.timezone);

		// set change listeners
		this.formGroup.get('profile.timezone').valueChanges
			.pipe(takeUntil(this._destroyed))
			.subscribe((text) => {
				const searchText = text.toLowerCase();
				const result = this.timezoneValues.filter((zone) => zone.toLowerCase().indexOf(searchText) >= 0);
				this.filteredTimezones = result;
			});

		this.formGroup.get('profile.culture').valueChanges
			.pipe(takeUntil(this._destroyed))
			.subscribe((text) => {
				const searchText = text.toLowerCase();
				const result = this.cultureValues.filter((culture) =>
					culture.name.toLowerCase().indexOf(searchText) >= 0 ||
					culture.displayName.toLowerCase().indexOf(searchText) >= 0
				);
				this.filteredCultures = result;
			});
	}

	formSubmit(): void {
		this.formService.touchAllFormFields(this.formGroup);
		if (!this.isFormValid()) { return; }
		const formData = this.formService.getValue(this.formGroup.value);

		// Transformations
		if (Array.isArray(formData.serviceUsers)) {
			formData.serviceUsers.forEach(element => {
				if (element.service && Guid.isGuid(element.service.id)) {
					element.serviceId = element.service.id;
				} else {
					element.serviceId = undefined;
				}
				if (element.role && Guid.isGuid(element.role.id)) {
					element.roleId = element.role.id;
				} else {
					element.roleId = undefined;
				}
			});
		} else {
			formData.serviceUsers = [];
		}


		this.userService.persist(formData)
			.pipe(takeUntil(this._destroyed))
			.subscribe(
				complete => this.onCallbackSuccess(),
				error => this.onCallbackError(error)
			);
	}

	public isFormValid() {
		if (!this.formGroup.valid) { return false; }
		return true;
	}

	public save() {
		this.clearErrorModel();
	}

	public delete() {
		const value = this.formGroup.value;
		if (value.id) {
			const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
				maxWidth: '300px',
				restoreFocus: false,
				data: {
					message: this.language.instant('COMMONS.CONFIRMATION-DIALOG.DELETE-ITEM'),
					confirmButton: this.language.instant('COMMONS.CONFIRMATION-DIALOG.ACTIONS.CONFIRM'),
					cancelButton: this.language.instant('COMMONS.CONFIRMATION-DIALOG.ACTIONS.CANCEL')
				}
			});
			dialogRef.afterClosed()
				.pipe(takeUntil(this._destroyed))
				.subscribe(result => {
					if (result) {
						this.userService.delete(value.id).pipe(takeUntil(this._destroyed))
							.subscribe(
								complete => this.onCallbackSuccess(),
								error => this.onCallbackError(error)
							);
					}
				});
		}
	}

	public cancel(): void {
		this.router.navigate(['/users']);
	}

	onCallbackSuccess(): void {
		this.formGroup.reset();
		this.uiNotificationService.snackBarNotification(this.isNew ? this.language.instant('COMMONS.SNACK-BAR.SUCCESSFUL-CREATION') : this.language.instant('COMMONS.SNACK-BAR.SUCCESSFUL-UPDATE'), SnackBarNotificationLevel.Success);
		this.router.navigate(['/users']);
	}

	onCallbackError(errorResponse: HttpErrorResponse) {
		const error: HttpError = this.httpErrorHandlingService.getError(errorResponse);
		if (error.statusCode === 400) {
			this.user.validationErrorModel.fromJSONObject(errorResponse.error);
			this.formService.validateAllFormFields(this.formGroup);
		} else {
			this.uiNotificationService.snackBarNotification(error.getMessagesString(), SnackBarNotificationLevel.Warning);
		}
	}

	clearErrorModel() {
		this.user.validationErrorModel.clear();
		this.formService.validateAllFormFields(this.formGroup);
	}

	canDeactivate(): boolean | Observable<boolean> {
		return this.formGroup ? !this.formGroup.dirty : true;
	}

	addServiceUserItem() {
		const serviceUsersArray: FormArray = this.formGroup.get('serviceUsers') as FormArray;
		serviceUsersArray.push(this.user.buildServiceUserForm(new ServiceUserEditorModel(), serviceUsersArray.length, false));
	}

	removeServiceUserAt(index: number) {
		const serviceUsersArray: FormArray = this.formGroup.get('serviceUsers') as FormArray;
		const itemToBeDeleted = serviceUsersArray.controls[index];
		itemToBeDeleted.disable();
		serviceUsersArray.controls.splice(index, 1);
		this.clearErrorModel();
		this.user.helperReapplyValidators(serviceUsersArray);
		this.formGroup.updateValueAndValidity();
	}
}
