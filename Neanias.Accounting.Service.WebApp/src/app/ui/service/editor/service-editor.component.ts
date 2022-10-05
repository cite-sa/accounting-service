import { Component, OnInit } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { IsActive } from '@app/core/enum/is-active.enum';
import { AppPermission } from '@app/core/enum/permission.enum';
import { AppEnumUtils } from '@app/core/formatting/enum-utils.service';
import { Service } from '@app/core/model/service/service.model';
import { ServiceService } from '@app/core/services/http/service.service';
import { AuthService } from '@app/core/services/ui/auth.service';
import { ServiceEditorModel } from '@app/ui/service/editor/service-editor.model';
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
import { ServiceSync } from '@app/core/model/service-sync/service-sync.model';

@Component({
	selector: 'app-service-editor',
	templateUrl: './service-editor.component.html',
	styleUrls: ['./service-editor.component.scss']
})
export class ServiceEditorComponent extends BaseEditor<ServiceEditorModel, Service> implements OnInit {

	isNew = true;
	isDeleted = false;
	saveClicked = false;
	formGroup: FormGroup = null;
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
		private logger: LoggingService
	) {
		super(dialog, language, formService, router, uiNotificationService, httpErrorHandlingService, filterService, datePipe, route, queryParamsService);
	}

	ngOnInit(): void {
		this.singleServiceAutocompleteConfiguration = this.serviceService.CreateSingleAutoCompleteConfiguration(null);
		super.ngOnInit();
	}

	getItem(itemId: Guid, successFunction: (item: Service) => void): void {
		this.serviceService.getSingle(itemId,
			[
				...this.commonFormFieldNames(),
				nameof<Service>(x => x.name),
				nameof<Service>(x => x.code),
				nameof<Service>(x => x.description),
				nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.EditService],
				nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.DeleteService],
				nameof<Service>(x => x.parent) + '.' + nameof<Service>(x => x.id),
				nameof<Service>(x => x.parent) + '.' + nameof<Service>(x => x.name),
				nameof<Service>(x => x.serviceSyncs) + '.' + nameof<ServiceSync>(x => x.status),
				nameof<Service>(x => x.serviceSyncs) + '.' + nameof<ServiceSync>(x => x.lastSyncAt),
				nameof<Service>(x => x.serviceSyncs) + '.' + nameof<ServiceSync>(x => x.lastSyncEntryTimestamp),
				nameof<Service>(x => x.serviceSyncs) + '.' + nameof<ServiceSync>(x => x.isActive),
			])
			.pipe(map(data => data as Service), takeUntil(this._destroyed))
			.subscribe(
				data => successFunction(data),
				error => this.onCallbackError(error)
			);
	}

	prepareForm(data: Service): void {
		try {
			if (data) this.singleServiceAutocompleteConfiguration = this.serviceService.CreateSingleAutoCompleteConfiguration({ excludedIds: [data.id]});
			this.editorModel = data ? new ServiceEditorModel(this.authService).fromModel(data) : new ServiceEditorModel(this.authService);
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
		this.getItem(this.editorModel.id, (data: Service) => this.prepareForm(data));
	}

	refreshOnNavigateToData(id?: Guid): void {
		if (this.isNew) {
			this.formGroup.markAsPristine();
			this.router.navigate(['/services/' + (id ? id : this.editorModel.id)], { queryParams: { 'lookup': this.queryParamsService.serializeLookup(this.lookupParams), 'lv': ++this.lv }, replaceUrl: true });
		} else { this.internalRefreshData(); }
	}

	persistEntity(onSuccess?: (response) => void): void {
		const formData = this.formService.getValue(this.formGroup.value);

		if (!this.authService.hasPermission(AppPermission.EditServiceCode)) formData.code = this.formGroup.getRawValue().code
		if (formData.parent && formData.parent.id) {
			formData.parentId = formData.parent.id;
		} else {
			formData.parentId = undefined;
		}
		this.serviceService.persist(formData)
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
					this.serviceService.delete(value.id).pipe(takeUntil(this._destroyed))
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
