import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { DummyAccountingEntriesPersist, Service, ServicePersist } from '@app/core/model/service/service.model';
import { BackendErrorValidator } from '@common/forms/validation/custom-validator';
import { ValidationErrorModel } from '@common/forms/validation/error-model/validation-error-model';
import { Validation, ValidationContext } from '@common/forms/validation/validation-context';
import { BaseEditorModel } from '@common/base/base-editor.model';
import { Guid } from '@common/types/guid';
import { AuthService } from '@app/core/services/ui/auth.service';
import { AppPermission } from '@app/core/enum/permission.enum';
import { ServiceSync } from '@app/core/model/service-sync/service-sync.model';
import { IsActive } from '@idp-service/core/enum/is-active.enum';
import { BaseFormEditorModel } from '@common/base/base-form-editor-model';

export class DummyAccountingEntriesEditorModel extends BaseFormEditorModel implements DummyAccountingEntriesPersist {
	count: number;
	myCount: number;
	from: Date;
	to: Date;
	resourceCodePrefix: string;
	resourceMaxValue: number;
	actionCodePrefix: string;
	actionMaxValue: number;
	userIdPrefix: string;
	userMaxValue: number;
	minValue: number;
	maxValue: number;
	measure: string;
	serviceId: Guid;
	service: Service;
	serviceSync: ServiceSync;
	canCleanUp = false;
	canSync = false;
	canAddDummyAccountingEntry = false;

	public validationErrorModel: ValidationErrorModel = new ValidationErrorModel();
	protected formBuilder: FormBuilder = new FormBuilder();

	constructor(private authService: AuthService) {
		super();
	}

	public fromModel(item: Service): DummyAccountingEntriesEditorModel {
		if (item) {
			this.serviceId = item.id;
			if (item.serviceSyncs && item.serviceSyncs.filter(x=> x.isActive === IsActive.Active).length > 0) {
				this.serviceSync = item.serviceSyncs.filter(x=> x.isActive === IsActive.Active)[0]
			}
			this.canCleanUp = this.authService.hasPermission(AppPermission.ServiceCleanUp) || item.authorizationFlags?.some(x => x === AppPermission.ServiceCleanUp);
			this.canSync = this.authService.hasPermission(AppPermission.EnforceServiceSync) || item.authorizationFlags?.some(x => x === AppPermission.EnforceServiceSync);
			this.canAddDummyAccountingEntry = this.authService.hasPermission(AppPermission.AddDummyAccountingEntry) || item.authorizationFlags?.some(x => x === AppPermission.AddDummyAccountingEntry);
		}
		this.service = item;
		return this;
	}

	buildForm(context: ValidationContext = null, disabled: boolean = false): FormGroup {
		if (context == null) { context = this.createValidationContext(); }


		return this.formBuilder.group({
			count: [{ value: this.count, disabled: disabled }, context.getValidation('count').validators],
			myCount: [{ value: this.myCount, disabled: disabled }, context.getValidation('myCount').validators],
			from: [{ value: this.from, disabled: disabled }, context.getValidation('from').validators],
			to: [{ value: this.to, disabled: disabled }, context.getValidation('to').validators],
			resourceCodePrefix: [{ value: this.resourceCodePrefix, disabled: disabled }, context.getValidation('resourceCodePrefix').validators],
			resourceMaxValue: [{ value: this.resourceMaxValue, disabled: disabled }, context.getValidation('resourceMaxValue').validators],
			actionCodePrefix: [{ value: this.actionCodePrefix, disabled: disabled }, context.getValidation('actionCodePrefix').validators],
			actionMaxValue: [{ value: this.actionMaxValue, disabled: disabled }, context.getValidation('actionMaxValue').validators],
			userIdPrefix: [{ value: this.userIdPrefix, disabled: disabled }, context.getValidation('userIdPrefix').validators],
			userMaxValue: [{ value: this.userMaxValue, disabled: disabled }, context.getValidation('userMaxValue').validators],
			minValue: [{ value: this.minValue, disabled: disabled }, context.getValidation('minValue').validators],
			maxValue: [{ value: this.maxValue, disabled: disabled }, context.getValidation('maxValue').validators],
			measure: [{ value: this.measure, disabled: disabled }, context.getValidation('measure').validators],
			serviceId: [{ value: this.serviceId, disabled: disabled }, context.getValidation('serviceId').validators],
			service: [{ value: this.service, disabled: disabled }],
			serviceSync: [{ value: this.serviceSync, disabled: disabled }],
		});
	}

	createValidationContext(): ValidationContext {
		const baseContext: ValidationContext = new ValidationContext();
		const baseValidationArray: Validation[] = new Array<Validation>();
		baseValidationArray.push({ key: 'count', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Count')] });
		baseValidationArray.push({ key: 'myCount', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'MyCount')] });
		baseValidationArray.push({ key: 'from', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'From')] });
		baseValidationArray.push({ key: 'to', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'To')] });
		baseValidationArray.push({ key: 'resourceCodePrefix', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'ResourceCodePrefix')] });
		baseValidationArray.push({ key: 'resourceMaxValue', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'ResourceMaxValue')] });
		baseValidationArray.push({ key: 'actionCodePrefix', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'ActionCodePrefix')] });
		baseValidationArray.push({ key: 'actionMaxValue', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'ActionMaxValue')] });
		baseValidationArray.push({ key: 'userIdPrefix', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'UserIdPrefix')] });
		baseValidationArray.push({ key: 'userMaxValue', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'UserMaxValue')] });
		baseValidationArray.push({ key: 'minValue', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'MinValue')] });
		baseValidationArray.push({ key: 'maxValue', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'MaxValue')] });
		baseValidationArray.push({ key: 'measure', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Measure')] });
		baseValidationArray.push({ key: 'serviceId', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'ServiceId')] });

		baseContext.validation = baseValidationArray;
		return baseContext;
	}
}
