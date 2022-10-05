import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ValidationErrorModel } from '@common/forms/validation/error-model/validation-error-model';
import { UserServiceNamePatch, UserServiceUser } from '@user-service/core/model/user.model';

export class UserProfilePersonalInfoEditorModel implements UserServiceNamePatch {
	name: string;
	email: string;

	public validationErrorModel: ValidationErrorModel = new ValidationErrorModel();
	protected formBuilder: FormBuilder = new FormBuilder();

	constructor() { }

	public fromModel(item: UserServiceUser): UserProfilePersonalInfoEditorModel {
		this.name = item.name;
		this.email = item.email;
		return this;
	}

	buildForm(disabled: boolean = false): FormGroup {
		return this.formBuilder.group({
			name: [{ value: this.name, disabled: disabled }, [Validators.required]],
			email: [{ value: this.email, disabled: disabled }, [Validators.required]],
		});
	}
}
