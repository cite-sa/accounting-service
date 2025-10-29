import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import { ServiceAction } from "@app/core/model/service-action/service-action.model";
import { ServiceUser } from "@app/core/model/service-user/service-user.model";
import { Service } from "@app/core/model/service/service.model";
import { UserRole } from "@app/core/model/user-role/user-role.model";
import { ServiceActionService } from "@app/core/services/http/service-action.service";
import { BreadcrumbService } from "@app/ui/misc/breadcrumb/breadcrumb.service";
import { BaseEditorResolver } from "@common/base/base-editor.resolver";
import { Guid } from "@common/types/guid";
import { UserServiceUser, UserServiceUserProfile } from "@user-service/core/model/user.model";
import { UserService } from "@user-service/services/http/user.service";
import { takeUntil, tap } from "rxjs/operators";
import { nameof } from "ts-simple-nameof";


@Injectable()
export class UserEditorEnityResolver extends BaseEditorResolver {

  constructor(
    private userService: UserService,
    private breadcrumbService: BreadcrumbService,
  ) {
    super();
  }

  public static userLookupFields(): string[] {
		return [
			...BaseEditorResolver.lookupFields(),
      nameof<UserServiceUser>(x => x.name),
      nameof<UserServiceUser>(x => x.subject), 
      nameof<UserServiceUser>(x => x.email),
      nameof<UserServiceUser>(x => x.issuer),
      nameof<UserServiceUser>(x => x.profile) + '.' + nameof<UserServiceUserProfile>(x => x.id),
      nameof<UserServiceUser>(x => x.profile) + '.' + nameof<UserServiceUserProfile>(x => x.timezone),
      nameof<UserServiceUser>(x => x.profile) + '.' + nameof<UserServiceUserProfile>(x => x.culture),
      nameof<UserServiceUser>(x => x.profile) + '.' + nameof<UserServiceUserProfile>(x => x.language),
      nameof<UserServiceUser>(x => x.serviceUsers) + '.' + nameof<ServiceUser>(x => x.id),
      nameof<UserServiceUser>(x => x.serviceUsers) + '.' + nameof<ServiceUser>(x => x.service) + '.' + nameof<Service>(x => x.id),
      nameof<UserServiceUser>(x => x.serviceUsers) + '.' + nameof<ServiceUser>(x => x.service) + '.' + nameof<Service>(x => x.name),
      nameof<UserServiceUser>(x => x.serviceUsers) + '.' + nameof<ServiceUser>(x => x.role)+ '.' + nameof<UserRole>(x => x.id),
      nameof<UserServiceUser>(x => x.serviceUsers) + '.' + nameof<ServiceUser>(x => x.role)+ '.' + nameof<UserRole>(x => x.name),
		];
	}

  resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {

    const id = route.paramMap.get('id');

    if (id != null) {
      return this.userService.getSingle(Guid.parse(id), UserEditorEnityResolver.userLookupFields()).pipe(takeUntil(this._destroyed))
        .pipe(tap((user: UserServiceUser) => this.breadcrumbService.addIdResolvedValue(id, user.name)));
    }
  }
}