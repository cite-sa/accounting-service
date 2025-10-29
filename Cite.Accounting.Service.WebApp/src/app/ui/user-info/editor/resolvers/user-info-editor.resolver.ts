import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import { AppPermission } from "@app/core/enum/permission.enum";
import { UserInfo } from "@app/core/model/accounting/user-info.model";
import { Service } from "@app/core/model/service/service.model";
import { UserInfoService } from "@app/core/services/http/user-info.service";
import { BreadcrumbService } from "@app/ui/misc/breadcrumb/breadcrumb.service";
import { BaseEditorResolver } from "@common/base/base-editor.resolver";
import { Guid } from "@common/types/guid";
import { takeUntil, tap } from "rxjs/operators";
import { nameof } from "ts-simple-nameof";


@Injectable()
export class UserInfoEditorEnityResolver extends BaseEditorResolver {

  constructor(
    private userInfoService: UserInfoService,
    private breadcrumbService: BreadcrumbService,
  ) {
    super();
  }

  public static userInfoLookupFields(): string[] {
		return [
			...BaseEditorResolver.lookupFields(),
      nameof<UserInfo>(x => x.name),
      nameof<UserInfo>(x => x.email),
      nameof<UserInfo>(x => x.issuer),
      nameof<UserInfo>(x => x.subject),
      nameof<UserInfo>(x => x.resolved),
      nameof<UserInfo>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.EditUserInfo],
      nameof<UserInfo>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.DeleteUserInfo],
      nameof<UserInfo>(x => x.parent) + '.' + nameof<UserInfo>(x => x.service) + '.' + nameof<Service>(x => x.name),
      nameof<UserInfo>(x => x.parent) + '.' + nameof<UserInfo>(x => x.id),
      nameof<UserInfo>(x => x.parent) + '.' + nameof<UserInfo>(x => x.name),
      nameof<UserInfo>(x => x.service) + '.' + nameof<Service>(x => x.id),
      nameof<UserInfo>(x => x.service) + '.' + nameof<Service>(x => x.name),
      nameof<UserInfo>(x => x.service) + '.' + nameof<Service>(x => x.code),
		];
	}

  resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {

    const id = route.paramMap.get('id');

    if (id != null) {
      return this.userInfoService.getSingle(Guid.parse(id), UserInfoEditorEnityResolver.userInfoLookupFields()).pipe(takeUntil(this._destroyed))
        .pipe(tap((userInfo: UserInfo) => this.breadcrumbService.addIdResolvedValue(id, userInfo.name)));
    }
  }
}