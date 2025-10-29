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
import { UserRoleService } from '@app/core/services/http/user-role.service';
import { UserRole } from '@app/core/model/user-role/user-role.model';
import { UserRoleEditorModel } from '@app/ui/user-role/editor/user-role-editor.model';
import { PropagateType } from '@app/core/enum/propagate-type';

@Component({
	selector: 'app-user-role-editor',
	templateUrl: './user-role-editor.component.html',
	styleUrls: ['./user-role-editor.component.scss']
})
export class UserRoleEditorComponent extends BaseEditor<UserRoleEditorModel, UserRole> implements OnInit {

	isNew = true;
	isDeleted = false;
	saveClicked = false;
	formGroup: FormGroup = null;
	propagateTypeValues: Array<PropagateType>;

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
		public userRoleService: UserRoleService,
		private logger: LoggingService
	) {
		super(dialog, language, formService, router, uiNotificationService, httpErrorHandlingService, filterService, datePipe, route, queryParamsService);
	}

	ngOnInit(): void {
		super.ngOnInit();
		this.propagateTypeValues = this.enumUtils.getEnumValues(PropagateType);
	}

	getItem(itemId: Guid, successFunction: (item: UserRole) => void): void {
		this.userRoleService.getSingle(itemId,
			[
				...BaseEditor.commonFormFieldNames(),
				nameof<UserRole>(x => x.name),
				nameof<UserRole>(x => x.rights),
				nameof<UserRole>(x => x.propagate),
			])
			.pipe(map(data => data as UserRole), takeUntil(this._destroyed))
			.subscribe(
				data => successFunction(data),
				error => this.onCallbackError(error)
			);
	}

	prepareForm(data: UserRole): void {
		try {
			this.editorModel = data ? new UserRoleEditorModel().fromModel(data) : new UserRoleEditorModel();
			this.isDeleted = data ? data.isActive === IsActive.Inactive : false;
			this.buildForm();
		} catch {
			this.logger.error('Could not parse Service: ' + data);
			this.uiNotificationService.snackBarNotification(this.language.instant('COMMONS.ERRORS.DEFAULT'), SnackBarNotificationLevel.Error);
		}
	}

	buildForm() {
		this.formGroup = this.editorModel.buildForm(null, this.isDeleted || !this.authService.hasPermission(AppPermission.EditService));
	}

	refreshData(): void {
		this.getItem(this.editorModel.id, (data: UserRole) => this.prepareForm(data));
	}

	refreshOnNavigateToData(id?: Guid): void {
		if (this.isNew) {
			this.formGroup.markAsPristine();
			this.router.navigate(['/user-roles/' + (id ? id : this.editorModel.id)], { queryParams: { 'lookup': this.queryParamsService.serializeLookup(this.lookupParams), 'lv': ++this.lv }, replaceUrl: true });
		} else { this.internalRefreshData(); }
	}

	persistEntity(onSuccess?: (response) => void): void {
		const formData = this.formService.getValue(this.formGroup.value);

		this.userRoleService.persist(formData)
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
					this.userRoleService.delete(value.id).pipe(takeUntil(this._destroyed))
						.subscribe(
							complete => this.onCallbackSuccess(),
							error => this.onCallbackError(error)
						);
				}
			});
		}
	}

	clearErrorModel() {
		this.editorModel.validationErrorModel.clear();
		this.formService.validateAllFormFields(this.formGroup);
	}
}
