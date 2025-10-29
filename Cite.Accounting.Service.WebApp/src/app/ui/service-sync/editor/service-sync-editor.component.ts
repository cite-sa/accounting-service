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
import { ServiceSyncService } from '@app/core/services/http/service-sync.service';
import { ServiceSync } from '@app/core/model/service-sync/service-sync.model';
import { ServiceSyncEditorModel } from '@app/ui/service-sync/editor/service-sync-editor.model';
import { Service } from '@app/core/model/service/service.model';
import { ServiceSyncStatus } from '@app/core/enum/service-sync-status.enum copy';

@Component({
	selector: 'app-service-sync-editor',
	templateUrl: './service-sync-editor.component.html',
	styleUrls: ['./service-sync-editor.component.scss']
})
export class ServiceSyncEditorComponent extends BaseEditor<ServiceSyncEditorModel, ServiceSync> implements OnInit {

	isNew = true;
	isDeleted = false;
	saveClicked = false;
	formGroup: FormGroup = null;
	serviceSyncStatusValues: Array<ServiceSyncStatus>;
	singleServiceAutocompleteConfiguration = null;

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
		public serviceSyncService: ServiceSyncService,
		private logger: LoggingService
	) {
		super(dialog, language, formService, router, uiNotificationService, httpErrorHandlingService, filterService, datePipe, route, queryParamsService);
	}

	ngOnInit(): void {
		this.singleServiceAutocompleteConfiguration = this.serviceService.CreateSingleAutoCompleteConfiguration(null);
		super.ngOnInit();
		this.serviceSyncStatusValues = this.enumUtils.getEnumValues(ServiceSyncStatus);
	}

	getItem(itemId: Guid, successFunction: (item: ServiceSync) => void): void {
		this.serviceSyncService.getSingle(itemId,
			[
				...BaseEditor.commonFormFieldNames(),
				nameof<ServiceSync>(x => x.lastSyncAt),
				nameof<ServiceSync>(x => x.lastSyncEntryTimestamp),
				nameof<ServiceSync>(x => x.status),
				nameof<ServiceSync>(x => x.service) + '.' + nameof<Service>(x => x.id),
				nameof<ServiceSync>(x => x.service) + '.' + nameof<Service>(x => x.name),
			])
			.pipe(map(data => data as ServiceSync), takeUntil(this._destroyed))
			.subscribe(
				data => successFunction(data),
				error => this.onCallbackError(error)
			);
	}

	prepareForm(data: ServiceSync): void {
		try {
			this.editorModel = data ? new ServiceSyncEditorModel().fromModel(data) : new ServiceSyncEditorModel();
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
		this.getItem(this.editorModel.id, (data: ServiceSync) => this.prepareForm(data));
	}

	refreshOnNavigateToData(id?: Guid): void {
		if (this.isNew) {
			this.formGroup.markAsPristine();
			this.router.navigate(['/service-syncs/' + (id ? id : this.editorModel.id)], { queryParams: { 'lookup': this.queryParamsService.serializeLookup(this.lookupParams), 'lv': ++this.lv }, replaceUrl: true });
		} else { this.internalRefreshData(); }
	}

	persistEntity(onSuccess?: (response) => void): void {
		const formData = this.formService.getValue(this.formGroup.value);

		if (formData.service && formData.service.id) {
			formData.serviceId = formData.service.id;
		} else {
			formData.serviceId = undefined;
		}

		this.serviceSyncService.persist(formData)
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
					this.serviceSyncService.delete(value.id).pipe(takeUntil(this._destroyed))
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
