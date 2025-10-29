import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import { AppPermission } from "@app/core/enum/permission.enum";
import { ServiceAction } from "@app/core/model/service-action/service-action.model";
import { Service } from "@app/core/model/service/service.model";
import { ServiceActionService } from "@app/core/services/http/service-action.service";
import { BreadcrumbService } from "@app/ui/misc/breadcrumb/breadcrumb.service";
import { BaseEditorResolver } from "@common/base/base-editor.resolver";
import { Guid } from "@common/types/guid";
import { takeUntil, tap } from "rxjs/operators";
import { nameof } from "ts-simple-nameof";


@Injectable()
export class ServiceActionEditorEnityResolver extends BaseEditorResolver {

  constructor(
    private serviceActionService: ServiceActionService,
    private breadcrumbService: BreadcrumbService,
  ) {
    super();
  }

  public static serviceActionLookupFields(): string[] {
		return [
			...BaseEditorResolver.lookupFields(),
				nameof<ServiceAction>(x => x.name),
				nameof<ServiceAction>(x => x.code),
				nameof<ServiceAction>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.EditServiceAction],
				nameof<ServiceAction>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.DeleteServiceAction],
				nameof<ServiceAction>(x => x.parent) + '.' + nameof<ServiceAction>(x => x.id),
				nameof<ServiceAction>(x => x.parent) + '.' + nameof<ServiceAction>(x => x.name),
				nameof<ServiceAction>(x => x.parent) + '.' + nameof<ServiceAction>(x => x.service) + '.' + nameof<Service>(x => x.name),
				nameof<ServiceAction>(x => x.service) + '.' + nameof<Service>(x => x.id),
				nameof<ServiceAction>(x => x.service) + '.' + nameof<Service>(x => x.name),
		];
	}

  resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {

    const id = route.paramMap.get('id');

    if (id != null) {
      return this.serviceActionService.getSingle(Guid.parse(id), ServiceActionEditorEnityResolver.serviceActionLookupFields()).pipe(takeUntil(this._destroyed))
        .pipe(tap((serviceAction: ServiceAction) => this.breadcrumbService.addIdResolvedValue(id, serviceAction.name)));
    }
  }
}