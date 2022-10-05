import { Component, OnInit } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { IsActive } from '@app/core/enum/is-active.enum';
import { AppPermission } from '@app/core/enum/permission.enum';
import { AppEnumUtils } from '@app/core/formatting/enum-utils.service';
import { ServiceService } from '@app/core/services/http/service.service';
import { AuthService } from '@app/core/services/ui/auth.service';
import { FormService } from '@common/forms/form-service';
import { LoggingService } from '@common/logging/logging-service';
import { ConfirmationDialogComponent } from '@common/modules/confirmation-dialog/confirmation-dialog.component';
import { HttpErrorHandlingService } from '@common/modules/errors/error-handling/http-error-handling.service';
import { SnackBarNotificationLevel, UiNotificationService } from '@common/modules/notification/ui-notification-service';
import { Guid } from '@common/types/guid';
import { TranslateService } from '@ngx-translate/core';
import { map, takeUntil } from 'rxjs/operators';
import { nameof } from 'ts-simple-nameof';
import { BaseEditor } from '@common/base/base-editor';
import { FilterService } from '@common/modules/text-filter/filter-service';
import { DatePipe } from '@angular/common';
import { QueryParamsService } from '@app/core/services/ui/query-params.service';
import { UserInfoService } from '@app/core/services/http/user-info.service';
import { UserInfoEditorModel } from '@app/ui/user-info/editor/user-info-editor.model';
import { Service } from '@app/core/model/service/service.model';
import { UserInfo } from '@app/core/model/accounting/user-info.model';

@Component({
	selector: 'app-user-info-editor',
	templateUrl: './user-info-editor.component.html',
	styleUrls: ['./user-info-editor.component.scss']
})
export class UserInfoEditorComponent extends BaseEditor<UserInfoEditorModel, UserInfo> implements OnInit {

	isNew = true;
	isDeleted = false;
	saveClicked = false;
	formGroup: FormGroup = null;
	singleServiceAutocompleteConfiguration = null;
	singleUserInfoAutocompleteConfiguration = null;

	constructor(
		// BaseFormEditor injected dependencies
		protected dialog: MatDialog,
		protected language: TranslateService,
		protected formService: FormService,
		protected router: Router,
		protected uiNotificationService: UiNotificationService,
		protected httpErrorHandlingService: HttpErrorHandlingService,
		protected filterService: FilterService,
		protected datePipe: DatePipe,
		protected route: ActivatedRoute,
		protected queryParamsService: QueryParamsService,
		// Rest dependencies. Inject any other needed deps here:
		public authService: AuthService,
		public enumUtils: AppEnumUtils,
		public serviceService: ServiceService,
		public userInfoService: UserInfoService,
		private logger: LoggingService
	) {
		super(dialog, language, formService, router, uiNotificationService, httpErrorHandlingService, filterService, datePipe, route, queryParamsService);
	}

	ngOnInit(): void {
		this.singleServiceAutocompleteConfiguration = this.serviceService.CreateSingleAutoCompleteConfiguration({ onlyCanEdit: true  });
		this.singleUserInfoAutocompleteConfiguration = this.userInfoService.CreateSingleAutoCompleteConfiguration({ onlyCanEdit: true  });
		super.ngOnInit();
	}

	getItem(itemId: Guid, successFunction: (item: UserInfo) => void): void {
		this.userInfoService.getSingle(itemId,
			[
				...this.commonFormFieldNames(),
				nameof<UserInfo>(x => x.name),
				nameof<UserInfo>(x => x.email),
				nameof<UserInfo>(x => x.issuer),
				nameof<UserInfo>(x => x.subject),
				nameof<UserInfo>(x => x.resolved),
				nameof<UserInfo>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.EditUserInfo],
				nameof<UserInfo>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.DeleteUserInfo],
				nameof<UserInfo>(x => x.parent) + '.' + nameof<UserInfo>(x => x.service) + '.' + nameof<Service>(x => x.name),
				nameof<UserInfo>(x => x.parent) + '.' + nameof<UserInfo>(x => x.id),
				nameof<UserInfo>(x => x.parent) + '.' + nameof<UserInfo>(x => x.name),
				nameof<UserInfo>(x => x.service) + '.' + nameof<Service>(x => x.id),
				nameof<UserInfo>(x => x.service) + '.' + nameof<Service>(x => x.name),
				nameof<UserInfo>(x => x.service) + '.' + nameof<Service>(x => x.code),
			])
			.pipe(map(data => data as UserInfo), takeUntil(this._destroyed))
			.subscribe(
				data => successFunction(data),
				error => this.onCallbackError(error)
			);
	}

	prepareForm(data: UserInfo): void {
		try {
			if (data) this.singleUserInfoAutocompleteConfiguration = this.userInfoService.CreateSingleAutoCompleteConfiguration({ onlyCanEdit: true, excludedIds: [data.id], serviceCodes: [data.service.code] });
			this.editorModel = data ? new UserInfoEditorModel(this.authService).fromModel(data) : new UserInfoEditorModel(this.authService);
			this.isDeleted = data ? data.isActive === IsActive.Inactive : false;
			this.buildForm();
		} catch {
			this.logger.error('Could not parse Service: ' + data);
			this.uiNotificationService.snackBarNotification(this.language.instant('COMMONS.ERRORS.DEFAULT'), SnackBarNotificationLevel.Error);
		}
	}

	buildForm() {
		this.formGroup = this.editorModel.buildForm(null, this.isDeleted || !this.editorModel.canEdit);
	}

	refreshData(): void {
		this.getItem(this.editorModel.id, (data: UserInfo) => this.prepareForm(data));
	}

	refreshOnNavigateToData(id?: Guid): void {
		if (this.isNew) {
			this.formGroup.markAsPristine();
			this.router.navigate(['/user-infos/' + (id ? id : this.editorModel.id)], { queryParams: { 'lookup': this.queryParamsService.serializeLookup(this.lookupParams), 'lv': ++this.lv }, replaceUrl: true });
		} else { this.internalRefreshData(); }
	}

	persistEntity(onSuccess?: (response) => void): void {
		const formData = this.formService.getValue(this.formGroup.value);

		if (formData.parent && formData.parent.id) {
			formData.parentId = formData.parent.id;
		} else {
			formData.parentId = undefined;
		}

		if (formData.service && formData.service.id) {
			formData.serviceId = formData.service.id;
		} else if (!this.editorModel.canEditService) {
			formData.serviceId = this.editorModel.serviceId;
		} else {
			formData.serviceId = undefined;
		}
		if (!this.editorModel.canEditUser) {
			formData.subject = this.editorModel.subject;
			formData.issuer = this.editorModel.issuer;
		}
		this.userInfoService.persist(formData)
			.pipe(takeUntil(this._destroyed)).subscribe(
				complete => onSuccess ? onSuccess(complete) : this.onCallbackSuccess(complete),
				error => this.onCallbackError(error)
			);
	}

	formSubmit(): void {
		this.formService.touchAllFormFields(this.formGroup);
		if (!this.isFormValid()) {
			return;
		}

		this.persistEntity();
	}

	public delete() {
		const value = this.formGroup.value;
		if (value.id) {
			const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
				maxWidth: '300px',
				data: {
					message: this.language.instant('COMMONS.CONFIRMATION-DIALOG.DELETE-ITEM'),
					confirmButton: this.language.instant('COMMONS.CONFIRMATION-DIALOG.ACTIONS.CONFIRM'),
					cancelButton: this.language.instant('COMMONS.CONFIRMATION-DIALOG.ACTIONS.CANCEL')
				}
			});
			dialogRef.afterClosed().pipe(takeUntil(this._destroyed)).subscribe(result => {
				if (result) {
					this.userInfoService.delete(value.id).pipe(takeUntil(this._destroyed))
						.subscribe(
							complete => this.onCallbackSuccess(),
							error => this.onCallbackError(error)
						);
				}
			});
		}
	}

	onServiceChanged(event) {
		this.formGroup.patchValue({ parent: null });
	}

	clearErrorModel() {
		this.editorModel.validationErrorModel.clear();
		this.formService.validateAllFormFields(this.formGroup);
	}
}
