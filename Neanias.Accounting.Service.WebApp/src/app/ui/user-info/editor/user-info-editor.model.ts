import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BackendErrorValidator } from '@common/forms/validation/custom-validator';
import { ValidationErrorModel } from '@common/forms/validation/error-model/validation-error-model';
import { Validation, ValidationContext } from '@common/forms/validation/validation-context';
import { BaseEditorModel } from '@common/base/base-editor.model';
import { Guid } from '@common/types/guid';
import { AppPermission } from '@app/core/enum/permission.enum';
import { Service } from '@app/core/model/service/service.model';
import { AuthService } from '@app/core/services/ui/auth.service';
import { UserInfo, UserInfoPersist } from '@app/core/model/accounting/user-info.model';

export class UserInfoEditorModel extends BaseEditorModel implements UserInfoPersist {
	subject: string;
	issuer: string;
	name: string;
	email: string;
	parentId: Guid;
	parent: UserInfo;
	serviceId: Guid;
	service: Service;
	resolved: boolean;
	canEdit = false;
	canDelete = false;
	canEditService = true;
	canEditUser = true;

	public validationErrorModel: ValidationErrorModel = new ValidationErrorModel();
	protected formBuilder: FormBuilder = new FormBuilder();

	constructor(private authService: AuthService) {
		super();
		this.canEditService = true;
		this.canEditUser = true;
		this.canEdit = this.authService.hasPermission(AppPermission.EditUserInfo);
		this.canDelete = this.authService.hasPermission(AppPermission.DeleteUserInfo);
	}

	public fromModel(item: UserInfo): UserInfoEditorModel {
		if (item) {
			super.fromModel(item);
			this.name = item.name;
			this.subject = item.subject;
			this.issuer = item.issuer;
			this.email = item.email;
			this.resolved = item.resolved;
			if (item.service) { this.serviceId = item.service.id; }
			this.service = item.service;
			if (item.parent) { this.parentId = item.parent.id; }
			this.parent = item.parent;
			this.canEdit = this.authService.hasPermission(AppPermission.EditUserInfo) || item.authorizationFlags?.some(x => x === AppPermission.EditUserInfo);
			this.canDelete = this.authService.hasPermission(AppPermission.DeleteUserInfo) || item.authorizationFlags?.some(x => x === AppPermission.DeleteUserInfo);
			this.canEditUser = this.authService.hasPermission(AppPermission.EditUserInfoUser) || item.authorizationFlags?.some(x => x === AppPermission.EditUserInfoUser);
			this.canEditService = false;
		}
		return this;
	}

	buildForm(context: ValidationContext = null, disabled: boolean = false): FormGroup {
		if (context == null) { context = this.createValidationContext(); }

		return this.formBuilder.group({
			id: [{ value: this.id, disabled: disabled }, context.getValidation('id').validators],
			name: [{ value: this.name, disabled: disabled }, context.getValidation('name').validators],
			subject: [{ value: this.subject, disabled: disabled  || !this.canEditUser }, context.getValidation('subject').validators],
			email: [{ value: this.email, disabled: disabled }, context.getValidation('email').validators],
			issuer: [{ value: this.issuer, disabled: disabled  || !this.canEditUser }, context.getValidation('issuer').validators],
			resolved: [{ value: this.resolved, disabled: disabled }, context.getValidation('resolved').validators],
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
		baseValidationArray.push({ key: 'subject', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Subject')] });
		baseValidationArray.push({ key: 'email', validators: [Validators.email, BackendErrorValidator(this.validationErrorModel, 'Email')] });
		baseValidationArray.push({ key: 'issuer', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Issuer')] });
		baseValidationArray.push({ key: 'resolved', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'Resolved')] });
		baseValidationArray.push({ key: 'service', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'ServiceId')] });
		baseValidationArray.push({ key: 'parent', validators: [BackendErrorValidator(this.validationErrorModel, 'ParentId')] });
		baseValidationArray.push({ key: 'hash', validators: [] });

		baseContext.validation = baseValidationArray;
		return baseContext;
	}
}
