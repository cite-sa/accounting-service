import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Service, ServicePersist } from '@app/core/model/service/service.model';
import { BackendErrorValidator } from '@common/forms/validation/custom-validator';
import { ValidationErrorModel } from '@common/forms/validation/error-model/validation-error-model';
import { Validation, ValidationContext } from '@common/forms/validation/validation-context';
import { BaseEditorModel } from '@common/base/base-editor.model';
import { Guid } from '@common/types/guid';
import { AuthService } from '@app/core/services/ui/auth.service';
import { AppPermission } from '@app/core/enum/permission.enum';
import { ServiceSync } from '@app/core/model/service-sync/service-sync.model';
import { IsActive } from '@idp-service/core/enum/is-active.enum';

export class ServiceEditorModel extends BaseEditorModel implements ServicePersist {
	name: string;
	code: string;
	description: string;
	parentId: Guid;
	parent: Service;
	serviceSync: ServiceSync;
	canEdit = false;
	canDelete = false;

	public validationErrorModel: ValidationErrorModel = new ValidationErrorModel();
	protected formBuilder: FormBuilder = new FormBuilder();

	constructor(private authService: AuthService) {
		super();
		this.canEdit = this.authService.hasPermission(AppPermission.NewService);
		this.canDelete = this.authService.hasPermission(AppPermission.DeleteService);
	}

	public fromModel(item: Service): ServiceEditorModel {
		if (item) {
			super.fromModel(item);
			this.name = item.name;
			this.code = item.code;
			this.description = item.description;
			if (item.parent) { this.parentId = item.parent.id; }
			this.parent = item.parent;

			if (item.serviceSyncs && item.serviceSyncs.filter(x=> x.isActive === IsActive.Active).length > 0) {
				this.serviceSync = item.serviceSyncs.filter(x=> x.isActive === IsActive.Active)[0]
			}

			this.canEdit = this.authService.hasPermission(AppPermission.EditService) || item.authorizationFlags?.some(x => x === AppPermission.EditService);
			this.canDelete = this.authService.hasPermission(AppPermission.DeleteService) || item.authorizationFlags?.some(x => x === AppPermission.DeleteService);
		}
		return this;
	}

	buildForm(context: ValidationContext = null, disabled: boolean = false): FormGroup {
		if (context == null) { context = this.createValidationContext(); }


		return this.formBuilder.group({
			id: [{ value: this.id, disabled: disabled }, context.getValidation('id').validators],
			name: [{ value: this.name, disabled: disabled }, context.getValidation('name').validators],
			code: [{ value: this.code, disabled: disabled || !this.authService.hasPermission(AppPermission.EditServiceCode) }, context.getValidation('code').validators],
			description: [{ value: this.description, disabled: disabled }, context.getValidation('description').validators],
			parent: [{ value: this.parent, disabled: disabled }, context.getValidation('parent').validators],
			serviceSync: [{ value: this.serviceSync, disabled: true } ],
			hash: [{ value: this.hash, disabled: disabled }, context.getValidation('hash').validators],
		});
	}

	createValidationContext(): ValidationContext {
		const baseContext: ValidationContext = new ValidationContext();
		const baseValidationArray: Validation[] = new Array<Validation>();
		baseValidationArray.push({ key: 'id', validators: [BackendErrorValidator(this.validationErrorModel, 'Id')] });
		baseValidationArray.push({ key: 'name', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Name')] });
		baseValidationArray.push({ key: 'code', validators: [BackendErrorValidator(this.validationErrorModel, 'Code')] });
		baseValidationArray.push({ key: 'description', validators: [BackendErrorValidator(this.validationErrorModel, 'Description')] });
		baseValidationArray.push({ key: 'parent', validators: [BackendErrorValidator(this.validationErrorModel, 'ParentId')] });
		baseValidationArray.push({ key: 'hash', validators: [] });

		baseContext.validation = baseValidationArray;
		return baseContext;
	}
}
