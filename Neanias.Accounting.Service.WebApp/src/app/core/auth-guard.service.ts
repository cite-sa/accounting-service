import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, CanLoad, Route, Router, RouterStateSnapshot } from '@angular/router';
import { AuthService, ResolutionContext } from '@app/core/services/ui/auth.service';
import { KeycloakAuthGuard, KeycloakService } from 'keycloak-angular';
import { from, Observable, of as observableOf } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';

@Injectable()
export class AuthGuard extends KeycloakAuthGuard implements CanLoad {
	constructor(
		private authService: AuthService,
		protected router: Router,
		protected readonly keycloak: KeycloakService
	)
	{
		super(router, keycloak);
	}

	// canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> {
	// 	const url: string = state.url;
	// 	const authContext = route.data ? route.data['authContext'] as ResolutionContext : null;
	// 	return this.applyGuard(url, authContext);
	// }

	canLoad(route: Route): Observable<boolean> {
		const url = `/${route.path}`;
		const authContext = route.data ? route.data['authContext'] as ResolutionContext : null;
		return this.applyGuard(url, authContext);
	}

	public async isAccessAllowed(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {
		const url: string = state.url;
		const authContext = route.data ? route.data['authContext'] as ResolutionContext : null;
		return this.applyGuard(url, authContext).toPromise();
	}

	private applyGuard(url: string, authContext: ResolutionContext) {
		return this.checkGuard( authContext).pipe(tap(authorized => {
			if (!authorized) {
				this.router.navigate(['/unauthorized'], { queryParams: { returnUrl: url } });
			} else {
				if (!url.startsWith('/user-profile') && this.authService.isProfileTentative()) {
					this.router.navigate(['/user-profile'], { queryParams: { returnUrl: url } });
				}
			}
		}));
	}

	private checkGuard(authContext: ResolutionContext): Observable<boolean> {
		if (!this.authService.isLoggedIn()) { return observableOf(false); }
		return this.authService.hasAccessToken()
			? observableOf(this.authService.authorize(authContext))
			: this.authService.prepareAuthRequestNew(from(this.keycloak.getToken())).pipe(
				map(x => this.authService.hasAccessToken() && this.authService.authorize(authContext)
			));
	}
}
