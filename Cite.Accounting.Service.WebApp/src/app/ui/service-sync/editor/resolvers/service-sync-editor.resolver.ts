import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import { ServiceSync } from "@app/core/model/service-sync/service-sync.model";
import { Service } from "@app/core/model/service/service.model";
import { ServiceSyncService } from "@app/core/services/http/service-sync.service";
import { BreadcrumbService } from "@app/ui/misc/breadcrumb/breadcrumb.service";
import { BaseEditorResolver } from "@common/base/base-editor.resolver";
import { Guid } from "@common/types/guid";
import { takeUntil, tap } from "rxjs/operators";
import { nameof } from "ts-simple-nameof";


@Injectable()
export class ServiceSyncEditorEnityResolver extends BaseEditorResolver {

  constructor(
    private serviceSyncService: ServiceSyncService,
    private breadcrumbService: BreadcrumbService,
  ) {
    super();
  }

  public static serviceSyncEditorEnityResolverLookupFields(): string[] {
		return [
			...BaseEditorResolver.lookupFields(),

      nameof<ServiceSync>(x => x.lastSyncAt),
      nameof<ServiceSync>(x => x.lastSyncEntryTimestamp),
      nameof<ServiceSync>(x => x.status),
      nameof<ServiceSync>(x => x.service) + '.' + nameof<Service>(x => x.id),
      nameof<ServiceSync>(x => x.service) + '.' + nameof<Service>(x => x.name),
		];
	}

  resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {

    const id = route.paramMap.get('id');

    if (id != null) {
      return this.serviceSyncService.getSingle(Guid.parse(id), ServiceSyncEditorEnityResolver.serviceSyncEditorEnityResolverLookupFields()).pipe(takeUntil(this._destroyed))
        .pipe(tap((serviceSync: ServiceSync) => this.breadcrumbService.addIdResolvedValue(id, serviceSync?.service?.name)));
    }
  }
}