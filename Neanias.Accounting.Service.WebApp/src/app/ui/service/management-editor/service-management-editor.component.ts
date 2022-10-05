import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { MeasureType } from '@app/core/enum/measure-type';
import { AppPermission } from '@app/core/enum/permission.enum';
import { AppEnumUtils } from '@app/core/formatting/enum-utils.service';
import { ServiceSync } from '@app/core/model/service-sync/service-sync.model';
import { Service } from '@app/core/model/service/service.model';
import { ServiceService } from '@app/core/services/http/service.service';
import { AuthService } from '@app/core/services/ui/auth.service';
import { QueryParamsService } from '@app/core/services/ui/query-params.service';
import { DummyAccountingEntriesEditorModel } from '@app/ui/service/management-editor/service-management-editor.model';
import { BasePendingChangesComponent } from '@common/base/base-pending-changes.component';
import { FormService } from '@common/forms/form-service';
import { LoggingService } from '@common/logging/logging-service';
import { ConfirmationDialogComponent } from '@common/modules/confirmation-dialog/confirmation-dialog.component';
import { HttpError, HttpErrorHandlingService } from '@common/modules/errors/error-handling/http-error-handling.service';
import { SnackBarNotificationLevel, UiNotificationService } from '@common/modules/notification/ui-notification-service';
import { Guid } from '@common/types/guid';
import { TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { nameof } from 'ts-simple-nameof';

@Component({
	selector: 'app-service-management-editor',
	templateUrl: './service-management-editor.component.html',
	styleUrls: ['./service-management-editor.component.scss']
})

export class ServiceManagementEditorComponent extends BasePendingChangesComponent implements OnInit {
	canEdit = false;
	lookupParams: any;

	isNew = true;
	isDeleted = false;

	formGroup: FormGroup = null;
	editorModel: DummyAccountingEntriesEditorModel;
	measureTypeValues: Array<MeasureType>;
	protected lv = 0;

	constructor(
		protected dialog: MatDialog,
		public authService: AuthService,
		private route: ActivatedRoute,
		private router: Router,
		private language: TranslateService,
		public enumUtils: AppEnumUtils,
		private formService: FormService,
		private uiNotificationService: UiNotificationService,
		private logger: LoggingService,
		private httpErrorHandlingService: HttpErrorHandlingService,
		public serviceService: ServiceService,
		protected queryParamsService: QueryParamsService
	) {
		super();
	}

	ngOnInit(): void {
		this.measureTypeValues = this.enumUtils.getEnumValues(MeasureType);
		this.route.queryParamMap.pipe(takeUntil(this._destroyed)).subscribe((params: ParamMap) => {
			// If lookup is on the query params load it
			if (params.keys.length > 0 && params.has('lookup')) {
				this.lookupParams = this.queryParamsService.deSerializeLookup(params.get('lookup'));
			}
		});

		this.route.paramMap.pipe(takeUntil(this._destroyed)).subscribe((paramMap: ParamMap) => {
			const itemId = paramMap.get('id');

			if (itemId != null) {
				this.isNew = false;
				this.serviceService.getSingle(Guid.parse(itemId), [
					nameof<Service>(x => x.id),
					nameof<Service>(x => x.name),
					nameof<Service>(x => x.code),
					nameof<Service>(x => x.description),
					nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.EnforceServiceSync],
					nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.ServiceCleanUp],
					nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.AddDummyAccountingEntry],
					nameof<Service>(x => x.parent) + '.' + nameof<Service>(x => x.id),
					nameof<Service>(x => x.parent) + '.' + nameof<Service>(x => x.name),
					nameof<Service>(x => x.serviceSyncs) + '.' + nameof<ServiceSync>(x => x.status),
					nameof<Service>(x => x.serviceSyncs) + '.' + nameof<ServiceSync>(x => x.lastSyncAt),
					nameof<Service>(x => x.serviceSyncs) + '.' + nameof<ServiceSync>(x => x.lastSyncEntryTimestamp),
					nameof<Service>(x => x.serviceSyncs) + '.' + nameof<ServiceSync>(x => x.isActive),
				]).pipe(takeUntil(this._destroyed))
					.subscribe(
						data => {
							try {
								this.editorModel = new DummyAccountingEntriesEditorModel(this.authService).fromModel(data);
								this.canEdit = this.editorModel.canAddDummyAccountingEntry;
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
				this.editorModel = new DummyAccountingEntriesEditorModel(this.authService);
				this.buildForm();
			}
		});
	}

	buildForm(disabled: boolean = false) {
		this.formGroup = this.editorModel.buildForm(null, disabled);
	}

	formSubmit(): void {
		this.formService.touchAllFormFields(this.formGroup);
		if (!this.isFormValid()) { return; }
		const formData = this.formService.getValue(this.formGroup.value);

		this.serviceService.createDummyData(formData)
			.pipe(takeUntil(this._destroyed))
			.subscribe(
				() => this.onCallbackSuccess(),
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

	public cancel(): void {
		this.router.navigate(['..', '..'], { relativeTo: this.route, queryParams: { 'lookup': this.queryParamsService.serializeLookup(this.lookupParams), 'lv': ++this.lv }, replaceUrl: false });
	}

	onCallbackSuccess(): void {
		// this.formGroup.reset();
		this.uiNotificationService.snackBarNotification(this.language.instant('COMMONS.SNACK-BAR.SUCCESSFUL-CREATION'), SnackBarNotificationLevel.Success);
	}

	onCallbackError(errorResponse: HttpErrorResponse) {
		const error: HttpError = this.httpErrorHandlingService.getError(errorResponse);
		if (error.statusCode === 400) {
			this.editorModel.validationErrorModel.fromJSONObject(errorResponse.error);
			this.formService.validateAllFormFields(this.formGroup);
		} else {
			this.uiNotificationService.snackBarNotification(error.getMessagesString(), SnackBarNotificationLevel.Warning);
		}
	}

	clearErrorModel() {
		this.editorModel.validationErrorModel.clear();
		this.formService.validateAllFormFields(this.formGroup);
	}

	canDeactivate(): boolean | Observable<boolean> {
		return this.formGroup ? !this.formGroup.dirty : true;
	}

	syncNow(): void {
		const value = this.formGroup.value;
		if (value.serviceId) {
			const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
				maxWidth: '300px',
				data: {
					message: this.language.instant('APP.SERVICE-MANAGEMENT-EDITOR.CONFIRMATION-DIALOG.SYNC-ITEM'),
					confirmButton: this.language.instant('APP.SERVICE-MANAGEMENT-EDITOR.CONFIRMATION-DIALOG.ACTIONS.CONFIRM'),
					cancelButton: this.language.instant('APP.SERVICE-MANAGEMENT-EDITOR.CONFIRMATION-DIALOG.ACTIONS.CANCEL')
				}
			});
			dialogRef.afterClosed().pipe(takeUntil(this._destroyed)).subscribe(result => {
				if (result) {
					this.serviceService.syncElasticData(value.serviceId).pipe(takeUntil(this._destroyed))
						.subscribe(
							complete => this.onCallbackSuccess(),
							error => this.onCallbackError(error)
						);
				}
			});
		}
	}

	clenUp(): void {
		const value = this.formGroup.value;
		if (value.serviceId) {
			const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
				maxWidth: '300px',
				data: {
					message: this.language.instant('APP.SERVICE-MANAGEMENT-EDITOR.CONFIRMATION-DIALOG.CLEANUP-ITEM'),
					confirmButton: this.language.instant('APP.SERVICE-MANAGEMENT-EDITOR.CONFIRMATION-DIALOG.ACTIONS.CONFIRM'),
					cancelButton: this.language.instant('APP.SERVICE-MANAGEMENT-EDITOR.CONFIRMATION-DIALOG.ACTIONS.CANCEL')
				}
			});
			dialogRef.afterClosed().pipe(takeUntil(this._destroyed)).subscribe(result => {
				if (result) {
					this.serviceService.cleanUp(value.serviceId).pipe(takeUntil(this._destroyed))
						.subscribe(
							complete => this.onCallbackSuccess(),
							error => this.onCallbackError(error)
						);
				}
			});
		}
	}
}
