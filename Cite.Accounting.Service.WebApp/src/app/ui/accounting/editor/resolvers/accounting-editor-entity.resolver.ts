import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";
import { UserInfo } from "@app/core/model/accounting/user-info.model";
import { Service } from "@app/core/model/service/service.model";
import { ServiceService } from "@app/core/services/http/service.service";
import { AuthService } from "@app/core/services/ui/auth.service";
import { BreadcrumbService } from "@app/ui/misc/breadcrumb/breadcrumb.service";
import { BaseEditorResolver } from "@common/base/base-editor.resolver";
import { Guid } from "@common/types/guid";
import { of } from "rxjs";
import { takeUntil, tap } from "rxjs/operators";
import { nameof } from "ts-simple-nameof";


@Injectable()
export class AccountingEditorEnityResolver extends BaseEditorResolver {

  constructor(
    private serviceService: ServiceService,
    private authService: AuthService,
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
		];
	}

  resolve(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {

    const serviceId = route.paramMap.get('serviceId');

    if (serviceId != null) {
      return this.serviceService.getSingle(Guid.parse(serviceId), AccountingEditorEnityResolver.serviceLookupFields()).pipe(takeUntil(this._destroyed))
        .pipe(tap((service: Service) => {
          this.breadcrumbService.addIdResolvedValue(serviceId, service.name);
        }));
    } else {
      return of({
        id: this.authService.userId(),
        service: null,
        subject: this.authService.subject().toString(),
        issuer: "",
        name: this.authService.getPrincipalName(),
        email: "",
        createdAt: null,
        updatedAt: null,
        hash: null,
        isActive: null,
        parent: null,
        authorizationFlags: null
      } as UserInfo);
    }
  }
}