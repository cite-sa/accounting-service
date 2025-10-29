import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import { AppPermission } from "@app/core/enum/permission.enum";
import { ServiceSync } from "@app/core/model/service-sync/service-sync.model";
import { Service } from "@app/core/model/service/service.model";
import { ServiceService } from "@app/core/services/http/service.service";
import { BreadcrumbService } from "@app/ui/misc/breadcrumb/breadcrumb.service";
import { BaseEditorResolver } from "@common/base/base-editor.resolver";
import { Guid } from "@common/types/guid";
import { takeUntil, tap } from "rxjs/operators";
import { nameof } from "ts-simple-nameof";


@Injectable()
export class ServiceEditorEnityResolver extends BaseEditorResolver {

  constructor(
    private serviceService: ServiceService,
    private breadcrumbService: BreadcrumbService,
  ) {
    super();
  }

  public static serviceLookupFields(): string[] {
		return [
			...BaseEditorResolver.lookupFields(),
				nameof<Service>(x => x.name),
				nameof<Service>(x => x.code),
				nameof<Service>(x => x.description),
				nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.EditService],
				nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.DeleteService],
				nameof<Service>(x => x.parent) + '.' + nameof<Service>(x => x.id),
				nameof<Service>(x => x.parent) + '.' + nameof<Service>(x => x.name),
				nameof<Service>(x => x.serviceSyncs) + '.' + nameof<ServiceSync>(x => x.status),
				nameof<Service>(x => x.serviceSyncs) + '.' + nameof<ServiceSync>(x => x.lastSyncAt),
				nameof<Service>(x => x.serviceSyncs) + '.' + nameof<ServiceSync>(x => x.lastSyncEntryTimestamp),
				nameof<Service>(x => x.serviceSyncs) + '.' + nameof<ServiceSync>(x => x.isActive),
		];
	}

  resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {

    const id = route.paramMap.get('id');

    if (id != null) {
      return this.serviceService.getSingle(Guid.parse(id), ServiceEditorEnityResolver.serviceLookupFields()).pipe(takeUntil(this._destroyed))
        .pipe(tap((service: Service) => this.breadcrumbService.addIdResolvedValue(id, service.name)));
    }
  }
}