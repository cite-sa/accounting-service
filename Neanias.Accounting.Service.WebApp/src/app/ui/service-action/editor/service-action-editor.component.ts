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
import { ServiceActionService } from '@app/core/services/http/service-action.service';
import { ServiceAction } from '@app/core/model/service-action/service-action.model';
import { ServiceActionEditorModel } from '@app/ui/service-action/editor/service-action-editor.model';
import { Service } from '@app/core/model/service/service.model';

@Component({
	selector: 'app-service-action-editor',
	templateUrl: './service-action-editor.component.html',
	styleUrls: ['./service-action-editor.component.scss']
})
export class ServiceActionEditorComponent extends BaseEditor<ServiceActionEditorModel, ServiceAction> implements OnInit {

	isNew = true;
	isDeleted = false;
	saveClicked = false;
	formGroup: FormGroup = null;
	singleServiceAutocompleteConfiguration = null;
	singleServiceActionAutocompleteConfiguration = null;

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
		public serviceActionService: ServiceActionService,
		private logger: LoggingService
	) {
		super(dialog, language, formService, router, uiNotificationService, httpErrorHandlingService, filterService, datePipe, route, queryParamsService);
	}

	ngOnInit(): void {
		this.singleServiceAutocompleteConfiguration = this.serviceService.CreateSingleAutoCompleteConfiguration({ onlyCanEdit: true  });
		this.singleServiceActionAutocompleteConfiguration = this.serviceActionService.CreateSingleAutoCompleteConfiguration({ onlyCanEdit: true  });
		super.ngOnInit();
	}

	getItem(itemId: Guid, successFunction: (item: ServiceAction) => void): void {
		this.serviceActionService.getSingle(itemId,
			[
				...this.commonFormFieldNames(),
				nameof<ServiceAction>(x => x.name),
				nameof<ServiceAction>(x => x.code),
				nameof<ServiceAction>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.EditServiceAction],
				nameof<ServiceAction>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.DeleteServiceAction],
				nameof<ServiceAction>(x => x.parent) + '.' + nameof<ServiceAction>(x => x.id),
				nameof<ServiceAction>(x => x.parent) + '.' + nameof<ServiceAction>(x => x.name),
				nameof<ServiceAction>(x => x.parent) + '.' + nameof<ServiceAction>(x => x.service) + '.' + nameof<Service>(x => x.name),
				nameof<ServiceAction>(x => x.service) + '.' + nameof<Service>(x => x.id),
				nameof<ServiceAction>(x => x.service) + '.' + nameof<Service>(x => x.name),
			])
			.pipe(map(data => data as ServiceAction), takeUntil(this._destroyed))
			.subscribe(
				data => successFunction(data),
				error => this.onCallbackError(error)
			);
	}

	prepareForm(data: ServiceAction): void {
		try {
			if (data) this.singleServiceActionAutocompleteConfiguration = this.serviceActionService.CreateSingleAutoCompleteConfiguration({ onlyCanEdit: true, excludedIds: [data.id], serviceIds: [data.service.id]  });
			this.editorModel = data ? new ServiceActionEditorModel(this.authService).fromModel(data) : new ServiceActionEditorModel(this.authService);
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
		this.getItem(this.editorModel.id, (data: ServiceAction) => this.prepareForm(data));
	}

	refreshOnNavigateToData(id?: Guid): void {
		if (this.isNew) {
			this.formGroup.markAsPristine();
			this.router.navigate(['/service-actions/' + (id ? id : this.editorModel.id)], { queryParams: { 'lookup': this.queryParamsService.serializeLookup(this.lookupParams), 'lv': ++this.lv }, replaceUrl: true });
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
		if (!this.editorModel.canEditCode) {
			formData.code = this.editorModel.code;
		}
		this.serviceActionService.persist(formData)
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
					this.serviceActionService.delete(value.id).pipe(takeUntil(this._destroyed))
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
