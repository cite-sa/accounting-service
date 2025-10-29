import { FormArray, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { ServiceUser, ServiceUserForUserPersist, ServiceUserPersist } from '@app/core/model/service-user/service-user.model';
import { Service } from '@app/core/model/service/service.model';
import { UserRole } from '@app/core/model/user-role/user-role.model';
import { BaseFormEditorModel } from '@common/base/base-form-editor-model';
import { BackendErrorValidator, E164PhoneValidator } from '@common/forms/validation/custom-validator';
import { ValidationErrorModel } from '@common/forms/validation/error-model/validation-error-model';
import { Validation, ValidationContext } from '@common/forms/validation/validation-context';
import { InstallationConfigurationService } from '@common/installation-configuration/installation-configuration.service';
import { Guid } from '@common/types/guid';
import { UserServiceUser, UserServiceUserPersist, UserServiceUserProfile, UserServiceUserProfilePersist } from '@user-service/core/model/user.model';
import { LanguageService } from '@user-service/services/language.service';

export class UserProfileEditorModel extends BaseFormEditorModel implements UserServiceUserProfilePersist {
	id?: Guid;
	timezone: string;
	culture: string;
	language: string;
	hash: string;

	public fromModel(item: UserServiceUserProfile): UserProfileEditorModel {
		this.id = item.id;
		this.timezone = item.timezone;
		this.culture = item.culture;
		this.language = item.language;
		return this;
	}

	buildForm(installationConfiguration: InstallationConfigurationService, languageService: LanguageService, context: ValidationContext, disabled: boolean = false): FormGroup {
		return this.formBuilder.group({
			timezone: [{ value: this.timezone || installationConfiguration.defaultTimezone, disabled: disabled }, context.getValidation('timezone').validators],
			culture: [{ value: this.culture || installationConfiguration.defaultCulture, disabled: disabled }, context.getValidation('culture').validators],
			language: [{ value: this.language || languageService.getLanguageValue(installationConfiguration.defaultLanguage), disabled: disabled }, context.getValidation('language').validators],
		});
	}
}
export class UserEditorModel extends BaseFormEditorModel implements UserServiceUserPersist {
	id?: Guid;
	name: string;
	hash: string;
	subject: string;
	email: string;
	issuer: string;
	profile: UserProfileEditorModel;
	serviceUsers: ServiceUserEditorModel[] = [];

	public fromModel(item: UserServiceUser): UserEditorModel {
		this.id = item.id;
		this.name = item.name;
		this.hash = item.hash;
		this.email = item.email;
		this.issuer = item.issuer;
		this.subject = item.subject;
		if (item.profile) { this.profile = new UserProfileEditorModel().fromModel(item.profile); }
		if (item.serviceUsers) { this.serviceUsers = item.serviceUsers.map(x => new ServiceUserEditorModel().fromModel(x)); }

		return this;
	}

	buildForm(installationConfiguration: InstallationConfigurationService, languageService: LanguageService, context: ValidationContext = null, disabled: boolean = false): FormGroup {
		if (context == null) { context = this.createValidationContext(); }

		const serviceUsersFormArray = new Array<FormGroup>();
		if (this.serviceUsers) {
			this.serviceUsers.forEach((element, index) => {
				serviceUsersFormArray.push(this.buildServiceUserForm(element, index, disabled));
			});
		}
		return this.formBuilder.group({
			id: [{ value: this.id, disabled: disabled }, context.getValidation('id').validators],
			name: [{ value: this.name, disabled: disabled }, context.getValidation('name').validators],
			email: [{ value: this.email, disabled: disabled }, context.getValidation('email').validators],
			subject: [{ value: this.subject, disabled: disabled }, context.getValidation('subject').validators],
			issuer: [{ value: this.issuer, disabled: disabled }, context.getValidation('issuer').validators],
			hash: [{ value: this.hash, disabled: disabled }, context.getValidation('hash').validators],
			profile: this.profile ? this.profile.buildForm(installationConfiguration, languageService, context.getValidation('profile').descendantValidations, disabled) : new UserProfileEditorModel().buildForm(installationConfiguration, languageService, context.getValidation('profile').descendantValidations, disabled),
			serviceUsers: this.formBuilder.array(serviceUsersFormArray),
		});
	}

	createValidationContext(): ValidationContext {
		const baseContext: ValidationContext = new ValidationContext();
		const baseValidationArray: Validation[] = new Array<Validation>();
		baseValidationArray.push({ key: 'id', validators: [BackendErrorValidator(this.validationErrorModel, 'Id')] });
		baseValidationArray.push({ key: 'name', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Name')] });
		baseValidationArray.push({ key: 'email', validators: [Validators.email, BackendErrorValidator(this.validationErrorModel, 'Email')] });
		baseValidationArray.push({ key: 'issuer', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Issuer')] });
		baseValidationArray.push({ key: 'subject', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Subject')] });
		baseValidationArray.push({ key: 'hash', validators: [BackendErrorValidator(this.validationErrorModel, 'Hash')] });

		baseValidationArray.push({ key: 'roles', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Roles')] });

		const profileContext: ValidationContext = new ValidationContext();
		const profileArray: Validation[] = new Array<Validation>();
		profileArray.push({ key: 'timezone', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Profile.Timezone')] });
		profileArray.push({ key: 'culture', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Profile.Culture')] });
		profileArray.push({ key: 'language', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Profile.Language')] });
		profileContext.validation = profileArray;

		baseValidationArray.push({ key: 'profile', descendantValidations: profileContext });

		baseContext.validation = baseValidationArray;
		return baseContext;
	}

	buildServiceUserForm(serviceUser: ServiceUserEditorModel, index: number, disabled: boolean = false): FormGroup {
		return serviceUser.buildForm(null, `ServiceUsers.[${index}]`, disabled, this.validationErrorModel);
	}

	helperReapplyValidators(serviceUsers: FormArray) {
		this.validationErrorModel.clear();
		if (!Array.isArray(serviceUsers.controls)) { return; }
		serviceUsers.controls.forEach((element, index) => {
			const serviceUserItemEditorModel = new ServiceUserEditorModel();
			serviceUserItemEditorModel.validationErrorModel = this.validationErrorModel;
			const context = serviceUserItemEditorModel.createValidationContext(`ServiceUsers.[${index}]`);
			const formGroup = element as FormGroup;
			Object.keys(formGroup.controls).forEach(key => {
				formGroup.get(key).setValidators(context.getValidation(key).validators);
				formGroup.get(key).updateValueAndValidity();
			});
		});
	}
}


export class ServiceUserEditorModel extends BaseFormEditorModel implements ServiceUserForUserPersist {
	service: Service;
	serviceId: Guid;
	role: UserRole;
	roleId: Guid;

	public validationErrorModel;
	protected formBuilder: FormBuilder = new FormBuilder();

	constructor() { super(); }

	public fromModel(item: ServiceUser): ServiceUserEditorModel {
		if (item) {
			this.service = item.service;
			if (item.service) { this.serviceId = item.service.id; }
			this.role = item.role;
			if (item.role) { this.roleId = item.role.id; }
		}
		return this;
	}

	buildForm(context: ValidationContext = null, baseProperty: string = null, disabled: boolean = false, validationErrorModel: ValidationErrorModel): FormGroup {
		this.validationErrorModel = validationErrorModel;
		if (context == null) { context = this.createValidationContext(baseProperty); }
		return this.formBuilder.group({
			service: [{ value: this.service, disabled: disabled }, context.getValidation('service').validators],
			role: [{ value: this.role, disabled: disabled }, context.getValidation('role').validators],
		});
	}

	createValidationContext(baseProperty?: string): ValidationContext {
		const baseContext: ValidationContext = new ValidationContext();
		const baseValidationArray: Validation[] = new Array<Validation>();
		baseValidationArray.push({ key: 'service', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, this.helperGetValidation(baseProperty, 'ServiceId'))] });
		baseValidationArray.push({ key: 'role', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, this.helperGetValidation(baseProperty, 'RoleId'))] });
		baseContext.validation = baseValidationArray;
		return baseContext;
	}


	helperGetValidation(baseProperty: string, property: string) {
		if (baseProperty) {
			return `${baseProperty}.${property}`;
		} else {
			return property;
		}
	}
}
