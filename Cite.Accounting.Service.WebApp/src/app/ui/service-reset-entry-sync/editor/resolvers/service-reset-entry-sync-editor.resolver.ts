import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import { ServiceResetEntrySync } from "@app/core/model/service-reset-entry-sync/service-reset-entry-sync.model";
import { Service } from "@app/core/model/service/service.model";
import { ServiceResetEntrySyncService } from "@app/core/services/http/service-reset-entry-sync.service";
import { BreadcrumbService } from "@app/ui/misc/breadcrumb/breadcrumb.service";
import { BaseEditorResolver } from "@common/base/base-editor.resolver";
import { Guid } from "@common/types/guid";
import { takeUntil, tap } from "rxjs/operators";
import { nameof } from "ts-simple-nameof";


@Injectable()
export class ServiceResetEntrySyncEditorEnityResolver extends BaseEditorResolver {

  constructor(
    private serviceResetEntrySyncService: ServiceResetEntrySyncService,
    private breadcrumbService: BreadcrumbService,
  ) {
    super();
  }

  public static serviceResetEntrySyncEditorEnityResolverLookupFields(): string[] {
		return [
			...BaseEditorResolver.lookupFields(),
      nameof<ServiceResetEntrySync>(x => x.lastSyncAt),
      nameof<ServiceResetEntrySync>(x => x.lastSyncEntryTimestamp),
      nameof<ServiceResetEntrySync>(x => x.lastSyncEntryId),
      nameof<ServiceResetEntrySync>(x => x.id),
      nameof<ServiceResetEntrySync>(x => x.status),
      nameof<ServiceResetEntrySync>(x => x.service) + '.' + nameof<Service>(x => x.id),
      nameof<ServiceResetEntrySync>(x => x.service) + '.' + nameof<Service>(x => x.name),
		];
	}

  resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {

    const id = route.paramMap.get('id');

    if (id != null) {
      return this.serviceResetEntrySyncService.getSingle(Guid.parse(id), ServiceResetEntrySyncEditorEnityResolver.serviceResetEntrySyncEditorEnityResolverLookupFields()).pipe(takeUntil(this._destroyed))
        .pipe(tap((serviceResetEntrySync: ServiceResetEntrySync) => this.breadcrumbService.addIdResolvedValue(id, serviceResetEntrySync?.service?.name)));
    }
  }
}