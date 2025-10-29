import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BackendErrorValidator } from '@common/forms/validation/custom-validator';
import { ValidationErrorModel } from '@common/forms/validation/error-model/validation-error-model';
import { Validation, ValidationContext } from '@common/forms/validation/validation-context';
import { BaseEditorModel } from '@common/base/base-editor.model';
import { UserRole, UserRolePersist } from '@app/core/model/user-role/user-role.model';
import { PropagateType } from '@app/core/enum/propagate-type';

export class UserRoleEditorModel extends BaseEditorModel implements UserRolePersist {
	name: string;
	rights: string;
	propagate: PropagateType;

	public validationErrorModel: ValidationErrorModel = new ValidationErrorModel();
	protected formBuilder: FormBuilder = new FormBuilder();

	constructor() { super(); }

	public fromModel(item: UserRole): UserRoleEditorModel {
		if (item) {
			super.fromModel(item);
			this.name = item.name;
			this.propagate = item.propagate;
			this.rights = item.rights;
		}
		return this;
	}

	buildForm(context: ValidationContext = null, disabled: boolean = false): FormGroup {
		if (context == null) { context = this.createValidationContext(); }

		return this.formBuilder.group({
			id: [{ value: this.id, disabled: disabled }, context.getValidation('id').validators],
			name: [{ value: this.name, disabled: disabled }, context.getValidation('name').validators],
			propagate: [{ value: this.propagate, disabled: disabled }, context.getValidation('propagate').validators],
			rights: [{ value: this.rights, disabled: disabled }, context.getValidation('rights').validators],
			hash: [{ value: this.hash, disabled: disabled }, context.getValidation('hash').validators],
		});
	}

	createValidationContext(): ValidationContext {
		const baseContext: ValidationContext = new ValidationContext();
		const baseValidationArray: Validation[] = new Array<Validation>();
		baseValidationArray.push({ key: 'id', validators: [BackendErrorValidator(this.validationErrorModel, 'Id')] });
		baseValidationArray.push({ key: 'name', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Name')] });
		baseValidationArray.push({ key: 'propagate', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Propagate')] });
		baseValidationArray.push({ key: 'rights', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Rights')] });
		baseValidationArray.push({ key: 'hash', validators: [] });

		baseContext.validation = baseValidationArray;
		return baseContext;
	}
}
