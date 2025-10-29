import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import { AppPermission } from "@app/core/enum/permission.enum";
import { ServiceResource } from "@app/core/model/service-resource/service-resource.model";
import { Service } from "@app/core/model/service/service.model";
import { ServiceResourceService } from "@app/core/services/http/service-resource.service";
import { BreadcrumbService } from "@app/ui/misc/breadcrumb/breadcrumb.service";
import { BaseEditorResolver } from "@common/base/base-editor.resolver";
import { Guid } from "@common/types/guid";
import { takeUntil, tap } from "rxjs/operators";
import { nameof } from "ts-simple-nameof";


@Injectable()
export class ServiceResourceEditorEnityResolver extends BaseEditorResolver {

  constructor(
    private serviceResourceService: ServiceResourceService,
    private breadcrumbService: BreadcrumbService,
  ) {
    super();
  }

  public static serviceResourceLookupFields(): string[] {
		return [
			...BaseEditorResolver.lookupFields(),
				nameof<ServiceResource>(x => x.name),
				nameof<ServiceResource>(x => x.code),
				nameof<ServiceResource>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.EditServiceResource],
				nameof<ServiceResource>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.DeleteServiceResource],
				nameof<ServiceResource>(x => x.parent) + '.' + nameof<ServiceResource>(x => x.id),
				nameof<ServiceResource>(x => x.parent) + '.' + nameof<ServiceResource>(x => x.name),
				nameof<ServiceResource>(x => x.parent) + '.' + nameof<ServiceResource>(x => x.service) + '.' + nameof<Service>(x => x.name),
				nameof<ServiceResource>(x => x.service) + '.' + nameof<Service>(x => x.id),
				nameof<ServiceResource>(x => x.service) + '.' + nameof<Service>(x => x.name),
		];
	}

  resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {

    const id = route.paramMap.get('id');

    if (id != null) {
      return this.serviceResourceService.getSingle(Guid.parse(id), ServiceResourceEditorEnityResolver.serviceResourceLookupFields()).pipe(takeUntil(this._destroyed))
        .pipe(tap((serviceResource: ServiceResource) => this.breadcrumbService.addIdResolvedValue(id, serviceResource.name)));
    }
  }
}