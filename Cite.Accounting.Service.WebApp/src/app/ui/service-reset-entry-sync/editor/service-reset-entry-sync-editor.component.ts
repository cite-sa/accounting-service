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
import { ServiceResetEntrySyncService } from '@app/core/services/http/service-reset-entry-sync.service';
import { ServiceResetEntrySync } from '@app/core/model/service-reset-entry-sync/service-reset-entry-sync.model';
import { ServiceResetEntrySyncEditorModel } from '@app/ui/service-reset-entry-sync/editor/service-reset-entry-sync-editor.model';
import { Service } from '@app/core/model/service/service.model';
import { ServiceSyncStatus } from '@app/core/enum/service-sync-status.enum copy';

@Component({
	selector: 'app-service-reset-entry-sync-editor',
	templateUrl: './service-reset-entry-sync-editor.component.html',
	styleUrls: ['./service-reset-entry-sync-editor.component.scss']
})
export class ServiceResetEntrySyncEditorComponent extends BaseEditor<ServiceResetEntrySyncEditorModel, ServiceResetEntrySync> implements OnInit {

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
		public serviceResetEntrySyncService: ServiceResetEntrySyncService,
		private logger: LoggingService
	) {
		super(dialog, language, formService, router, uiNotificationService, httpErrorHandlingService, filterService, datePipe, route, queryParamsService);
	}

	ngOnInit(): void {
		this.singleServiceAutocompleteConfiguration = this.serviceService.CreateSingleAutoCompleteConfiguration(null);
		super.ngOnInit();
		this.serviceSyncStatusValues = this.enumUtils.getEnumValues(ServiceSyncStatus);
	}

	getItem(itemId: Guid, successFunction: (item: ServiceResetEntrySync) => void): void {
		this.serviceResetEntrySyncService.getSingle(itemId,
			[
				...BaseEditor.commonFormFieldNames(),
				nameof<ServiceResetEntrySync>(x => x.lastSyncAt),
				nameof<ServiceResetEntrySync>(x => x.lastSyncEntryTimestamp),
				nameof<ServiceResetEntrySync>(x => x.lastSyncEntryId),
				nameof<ServiceResetEntrySync>(x => x.id),
				nameof<ServiceResetEntrySync>(x => x.status),
				nameof<ServiceResetEntrySync>(x => x.service) + '.' + nameof<Service>(x => x.id),
				nameof<ServiceResetEntrySync>(x => x.service) + '.' + nameof<Service>(x => x.name),
			])
			.pipe(map(data => data as ServiceResetEntrySync), takeUntil(this._destroyed))
			.subscribe(
				data => successFunction(data),
				error => this.onCallbackError(error)
			);
	}

	prepareForm(data: ServiceResetEntrySync): void {
		try {
			this.editorModel = data ? new ServiceResetEntrySyncEditorModel().fromModel(data) : new ServiceResetEntrySyncEditorModel();
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
		this.getItem(this.editorModel.id, (data: ServiceResetEntrySync) => this.prepareForm(data));
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

		this.serviceResetEntrySyncService.persist(formData)
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
					this.serviceResetEntrySyncService.delete(value.id).pipe(takeUntil(this._destroyed))
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
