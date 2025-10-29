import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import { UserRole } from "@app/core/model/user-role/user-role.model";
import { UserRoleService } from "@app/core/services/http/user-role.service";
import { BreadcrumbService } from "@app/ui/misc/breadcrumb/breadcrumb.service";
import { BaseEditorResolver } from "@common/base/base-editor.resolver";
import { Guid } from "@common/types/guid";
import { takeUntil, tap } from "rxjs/operators";
import { nameof } from "ts-simple-nameof";


@Injectable()
export class UserRoleEditorEnityResolver extends BaseEditorResolver {

  constructor(
    private userRoleService: UserRoleService,
    private breadcrumbService: BreadcrumbService,
  ) {
    super();
  }

  public static userRoleLookupFields(): string[] {
		return [
			...BaseEditorResolver.lookupFields(),
      nameof<UserRole>(x => x.name),
      nameof<UserRole>(x => x.rights),
      nameof<UserRole>(x => x.propagate),
		];
	}

  resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {

    const id = route.paramMap.get('id');

    if (id != null) {
      return this.userRoleService.getSingle(Guid.parse(id), UserRoleEditorEnityResolver.userRoleLookupFields()).pipe(takeUntil(this._destroyed))
        .pipe(tap((userRole: UserRole) => this.breadcrumbService.addIdResolvedValue(id, userRole.name)));
    }
  }
}