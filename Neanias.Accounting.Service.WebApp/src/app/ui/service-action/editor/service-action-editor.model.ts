import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BackendErrorValidator } from '@common/forms/validation/custom-validator';
import { ValidationErrorModel } from '@common/forms/validation/error-model/validation-error-model';
import { Validation, ValidationContext } from '@common/forms/validation/validation-context';
import { BaseEditorModel } from '@common/base/base-editor.model';
import { Guid } from '@common/types/guid';
import { AppPermission } from '@app/core/enum/permission.enum';
import { ServiceAction, ServiceActionPersist } from '@app/core/model/service-action/service-action.model';
import { Service } from '@app/core/model/service/service.model';
import { AuthService } from '@app/core/services/ui/auth.service';

export class ServiceActionEditorModel extends BaseEditorModel implements ServiceActionPersist {
	name: string;
	code: string;
	description: string;
	parentId: Guid;
	parent: ServiceAction;
	serviceId: Guid;
	service: Service;
	canEdit = false;
	canEditService = true;
	canEditCode = true;
	canDelete = false;

	public validationErrorModel: ValidationErrorModel = new ValidationErrorModel();
	protected formBuilder: FormBuilder = new FormBuilder();

	constructor(private authService: AuthService) {
		super();
		this.canEditService = true;
		this.canEditCode = true;
		this.canEdit = this.authService.hasPermission(AppPermission.NewServiceAction);
		this.canDelete = this.authService.hasPermission(AppPermission.DeleteServiceAction);
	}

	public fromModel(item: ServiceAction): ServiceActionEditorModel {
		if (item) {
			super.fromModel(item);
			this.name = item.name;
			this.code = item.code;
			if (item.service) { this.serviceId = item.service.id; }
			this.service = item.service;
			if (item.parent) { this.parentId = item.parent.id; }
			this.parent = item.parent;
			this.canEdit = this.authService.hasPermission(AppPermission.EditServiceAction) || item.authorizationFlags?.some(x => x === AppPermission.EditServiceAction);
			this.canDelete = this.authService.hasPermission(AppPermission.DeleteServiceAction) || item.authorizationFlags?.some(x => x === AppPermission.DeleteServiceAction);
			this.canEditCode = this.authService.hasPermission(AppPermission.EditServiceActionCode) || item.authorizationFlags?.some(x => x === AppPermission.EditServiceActionCode);
			this.canEditService = false;
		}
		return this;
	}

	buildForm(context: ValidationContext = null, disabled: boolean = false): FormGroup {
		if (context == null) { context = this.createValidationContext(); }

		return this.formBuilder.group({
			id: [{ value: this.id, disabled: disabled }, context.getValidation('id').validators],
			name: [{ value: this.name, disabled: disabled }, context.getValidation('name').validators],
			code: [{ value: this.code, disabled: disabled || !this.canEditCode }, context.getValidation('code').validators],
			service: [{ value: this.service, disabled: disabled || !this.canEditService }, context.getValidation('service').validators],
			parent: [{ value: this.parent, disabled: disabled }, context.getValidation('parent').validators],
			hash: [{ value: this.hash, disabled: disabled }, context.getValidation('hash').validators],
		});
	}

	createValidationContext(): ValidationContext {
		const baseContext: ValidationContext = new ValidationContext();
		const baseValidationArray: Validation[] = new Array<Validation>();
		baseValidationArray.push({ key: 'id', validators: [BackendErrorValidator(this.validationErrorModel, 'Id')] });
		baseValidationArray.push({ key: 'name', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Name')] });
		baseValidationArray.push({ key: 'code', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Code')] });
		baseValidationArray.push({ key: 'service', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'ServiceId')] });
		baseValidationArray.push({ key: 'parent', validators: [BackendErrorValidator(this.validationErrorModel, 'ParentId')] });
		baseValidationArray.push({ key: 'hash', validators: [] });

		baseContext.validation = baseValidationArray;
		return baseContext;
	}
}
