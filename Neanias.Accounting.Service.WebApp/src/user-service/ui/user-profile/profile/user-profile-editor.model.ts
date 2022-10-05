import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BackendErrorValidator } from '@common/forms/validation/custom-validator';
import { ValidationErrorModel } from '@common/forms/validation/error-model/validation-error-model';
import { Validation, ValidationContext } from '@common/forms/validation/validation-context';
import { Guid } from '@common/types/guid';
import { UserServiceUserProfile, UserServiceUserProfilePersist } from '@user-service/core/model/user.model';

export class UserProfileEditorModel implements UserServiceUserProfilePersist {
	id?: Guid;
	timezone: string;
	culture: string;
	language: string;
	hash: string;

	public validationErrorModel: ValidationErrorModel = new ValidationErrorModel();
	protected formBuilder: FormBuilder = new FormBuilder();

	constructor() { }

	public fromModel(item: UserServiceUserProfile): UserProfileEditorModel {
		this.id = item.id;
		this.timezone = item.timezone;
		this.culture = item.culture;
		this.language = item.language;
		this.hash = item.hash;
		return this;
	}

	buildForm(context: ValidationContext = null, disabled: boolean = false): FormGroup {
		if (context == null) { context = this.createValidationContext(); }

		return this.formBuilder.group({
			id: [{ value: this.id, disabled: disabled }, context.getValidation('id').validators],
			hash: [{ value: this.hash, disabled: disabled }, context.getValidation('hash').validators],
			timezone: [{ value: this.timezone, disabled: disabled }, context.getValidation('timezone').validators],
			culture: [{ value: this.culture, disabled: disabled }, context.getValidation('culture').validators],
			language: [{ value: this.language, disabled: disabled }, context.getValidation('language').validators],
		});
	}

	createValidationContext(): ValidationContext {
		const baseContext: ValidationContext = new ValidationContext();
		const baseValidationArray: Validation[] = new Array<Validation>();

		baseValidationArray.push({ key: 'id', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Id')] });
		baseValidationArray.push({ key: 'hash', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Hash')] });
		baseValidationArray.push({ key: 'timezone', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Timezone')] });
		baseValidationArray.push({ key: 'culture', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Culture')] });
		baseValidationArray.push({ key: 'language', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Language')] });

		baseContext.validation = baseValidationArray;
		return baseContext;
	}
}
// ADD THIS MODULE IN YOUR PROJECT, AND LOAD IT IN THE MAIN CLASS
import { MatInput } from '@angular/material/input';
import { coerceBooleanProperty } from '@angular/cdk/coercion';

/**
 * Fix for the MatInput required asterisk.
 */
Object.defineProperty(MatInput.prototype, 'required', {
	get: function (): boolean {
		if (this._required) {
			return this._required;
		}

		// The required attribute is set
		// when the control return an error from validation with an empty value
		if (this.ngControl && this.ngControl.control && this.ngControl.control.validator) {
			const emptyValueControl = Object.assign({}, this.ngControl.control);
			(emptyValueControl as any).value = null;
			return 'required' in (this.ngControl.control.validator(emptyValueControl) || {});
		}
		return false;
	},
	set: function (value: boolean) {
		this._required = coerceBooleanProperty(value);
	}
});
