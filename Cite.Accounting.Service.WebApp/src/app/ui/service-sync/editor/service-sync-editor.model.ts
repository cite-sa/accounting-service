import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BackendErrorValidator } from '@common/forms/validation/custom-validator';
import { ValidationErrorModel } from '@common/forms/validation/error-model/validation-error-model';
import { Validation, ValidationContext } from '@common/forms/validation/validation-context';
import { BaseEditorModel } from '@common/base/base-editor.model';
import { Guid } from '@common/types/guid';
import { ServiceSync, ServiceSyncPersist } from '@app/core/model/service-sync/service-sync.model';
import { Service } from '@app/core/model/service/service.model';
import { ServiceSyncStatus } from '@app/core/enum/service-sync-status.enum copy';

export class ServiceSyncEditorModel extends BaseEditorModel implements ServiceSyncPersist {
	lastSyncAt: Date;
	lastSyncEntryTimestamp: Date;
	status: ServiceSyncStatus;
	serviceId: Guid;
	service: Service;

	public validationErrorModel: ValidationErrorModel = new ValidationErrorModel();
	protected formBuilder: FormBuilder = new FormBuilder();

	constructor() { super(); }

	public fromModel(item: ServiceSync): ServiceSyncEditorModel {
		if (item) {
			super.fromModel(item);
			this.lastSyncAt = item.lastSyncAt;
			this.lastSyncEntryTimestamp = item.lastSyncEntryTimestamp;
			this.status = item.status;
			if (item.service) { this.serviceId = item.service.id; }
			this.service = item.service;
		}
		return this;
	}

	buildForm(context: ValidationContext = null, disabled: boolean = false): FormGroup {
		if (context == null) { context = this.createValidationContext(); }

		return this.formBuilder.group({
			id: [{ value: this.id, disabled: disabled }, context.getValidation('id').validators],
			lastSyncAt: [{ value: this.lastSyncAt, disabled: disabled }, context.getValidation('lastSyncAt').validators],
			lastSyncEntryTimestamp: [{ value: this.lastSyncEntryTimestamp, disabled: disabled }, context.getValidation('lastSyncEntryTimestamp').validators],
			status: [{ value: this.status, disabled: disabled }, context.getValidation('status').validators],
			service: [{ value: this.service, disabled: disabled }, context.getValidation('service').validators],
			hash: [{ value: this.hash, disabled: disabled }, context.getValidation('hash').validators],
		});
	}

	createValidationContext(): ValidationContext {
		const baseContext: ValidationContext = new ValidationContext();
		const baseValidationArray: Validation[] = new Array<Validation>();
		baseValidationArray.push({ key: 'id', validators: [BackendErrorValidator(this.validationErrorModel, 'Id')] });
		baseValidationArray.push({ key: 'lastSyncAt', validators: [BackendErrorValidator(this.validationErrorModel, 'LastSyncAt')] });
		baseValidationArray.push({ key: 'lastSyncEntryTimestamp', validators: [BackendErrorValidator(this.validationErrorModel, 'LastSyncEntryTimestamp')] });
		baseValidationArray.push({ key: 'status', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Status')] });
		baseValidationArray.push({ key: 'service', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'ServiceId')] });
		baseValidationArray.push({ key: 'hash', validators: [] });

		baseContext.validation = baseValidationArray;
		return baseContext;
	}
}
