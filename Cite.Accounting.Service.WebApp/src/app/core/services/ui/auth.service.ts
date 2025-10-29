
import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, NgZone } from '@angular/core';
import { Router } from '@angular/router';
import { AppPermission } from '@app/core/enum/permission.enum';
import { RoleType } from '@app/core/enum/role-type.enum';
import { AppAccount } from '@app/core/model/auth/principal.model';
import { PrincipalService as AppPrincipalService } from '@app/core/services/http/principal.service';
import { BaseService } from '@common/base/base.service';
import { InstallationConfigurationService } from '@common/installation-configuration/installation-configuration.service';
import { SnackBarNotificationLevel, UiNotificationService } from '@common/modules/notification/ui-notification-service';
import { Guid } from '@common/types/guid';
import { TranslateService } from '@ngx-translate/core';
import { KeycloakEventType, KeycloakService } from 'keycloak-angular';
import { forkJoin, from, Observable, of, Subject } from 'rxjs';
import { exhaustMap, map, takeUntil } from 'rxjs/operators';

export interface ResolutionContext {
	roles: RoleType[];
	permissions: AppPermission[];
}

export interface AuthenticationToken {
	access_token: string;
	token_type: string;
	expires_in: number;
	refresh_token: string;
	scope: string;
	state?: string;
}

export interface AuthenticationState {
	loginStatus: LoginStatus;
}

export enum LoginStatus {
	LoggedIn = 0,
	LoggingOut = 1,
	LoggedOut = 2
}

@Injectable()
export class AuthService extends BaseService {

	public authenticationStateSubject: Subject<AuthenticationState>;
	public permissionEnum = AppPermission;
	private accessToken: String;
	private appAccount: AppAccount;

	private _authState: boolean; // Boolean to indicate if a user if authorized. It's also used to sync the auth state across different tabs, using local storage.

	constructor(
		private installationConfiguration: InstallationConfigurationService,
		private appPrincipalService: AppPrincipalService,
		private router: Router,
		protected readonly keycloakService: KeycloakService,
		private language: TranslateService,
		private zone: NgZone,
		private uiNotificationService: UiNotificationService,
	) {
		super();
		// this.account = this.currentAccount();
		this.authenticationStateSubject = new Subject<AuthenticationState>();

		window.addEventListener('storage', (event: StorageEvent) => {
			// Logout if we receive event that logout action occurred in a different tab.
			if (event.key && event.key === 'authState' && event.newValue === 'false' && this._authState) {
				this.clear();
				this.router.navigate(['/unauthorized'], { queryParams: { returnUrl: this.router.url } });
			}
		});
	}

	public getAuthenticationStateObservable(): Observable<AuthenticationState> {
		return this.authenticationStateSubject.asObservable();
	}

	public beginLogOutProcess(): void {
		this.authenticationStateSubject.next({ loginStatus: LoginStatus.LoggingOut });
	}

	public clear(): void {
		this.authState(false);
		this.accessToken = undefined;
		this.appAccount = undefined;
	}

	private authState(authState?: boolean): boolean {
		if (authState !== undefined) {
			this._authState = authState;
			localStorage.setItem('authState', authState ? 'true' : 'false');
			if (authState) {
				this.authenticationStateSubject.next({ loginStatus: LoginStatus.LoggedIn });
			} else {
				this.authenticationStateSubject.next({ loginStatus: LoginStatus.LoggedOut });
			}
		}
		if (this._authState === undefined) {
			this._authState = localStorage.getItem('authState') === 'true' ? true : false;
		}
		return this._authState;
	}

	public currentAccountIsAuthenticated(): boolean {
		return this.appAccount && this.appAccount.isAuthenticated;
	}

	//Should this be name @isAuthenticated@ instead?
	public hasAccessToken(): boolean { return Boolean(this.currentAuthenticationToken()); }

	public currentAuthenticationToken(accessToken?: string): String {
		if (accessToken) {
			this.accessToken = accessToken;
		}
		return this.accessToken;
	}

	//
	//
	// Account data
	//
	//

	public CanManageAnySevice(): boolean {
		if (this.appAccount && this.appAccount.principal) { return this.appAccount.principal.canManageAnySevice; }
		return false;
	}

	public userId(): Guid {
		if (this.appAccount && this.appAccount.principal && this.appAccount.principal.userId) { return this.appAccount.principal.userId; }
		return null;
	}

	public subject(): Guid {
		if (this.appAccount && this.appAccount.principal && this.appAccount.principal.subject) { return this.appAccount.principal.subject; }
		return null;
	}

	public tenantId(): Guid {
		if (this.appAccount && this.appAccount.profile && this.appAccount.profile.tenant) { return this.appAccount.profile.tenant; }
		return null;
	}

	public getPrincipalName(): string {
		if (this.appAccount && this.appAccount.principal) { return this.appAccount.principal.name; }
		return null;
	}

	public getUserProfileLanguage(): string {
		if (this.appAccount && this.appAccount.profile) { return this.appAccount.profile.language; }
		return null;
	}

	public getUserProfileCulture(): string {
		if (this.appAccount && this.appAccount.profile) { return this.appAccount.profile.culture; }
		return null;
	}

	public getUserProfileTimezone(): string {
		if (this.appAccount && this.appAccount.profile) { return this.appAccount.profile.timezone; }
		return null;
	}

	public isAdmin(): boolean {
		return this.hasRole(RoleType.Admin);
	}

	public isProfileTentative(): boolean {
		//if (this.userServiceAccount && this.userServiceAccount.profile) { return this.userServiceAccount.profile.isTentative === TentativeUserProfile.Tentative; }
		return false;
	}

	public hasTotp(): boolean {
		//if (this.appAccount && this.appAccount.credentials && this.appAccount.credentials.providers) { return this.appAccount.credentials.providers.includes(CredentialProvider.Totp); }
		return false;
	}

	//
	//
	// Me called on all services to get account data.
	//
	//
	public prepareAuthRequest(observable: Observable<string>, httpParams?: Object): Observable<boolean> {
		return observable.pipe(
			map((x) => this.currentAuthenticationToken(x)),
			exhaustMap(() => forkJoin([
				this.installationConfiguration.appServiceEnabled ? this.appPrincipalService.me(httpParams) : of(null)
			])),
			map((item) => {
				this.currentAccount(item[0]);
				return true;
			})
		);
	}

	public authenticate(returnUrl: string) {
		if (!this.keycloakService.isLoggedIn()) {
			this.keycloakService.login({
				scope: this.installationConfiguration.authScope,
			})
				.then(() => {
					this.keycloakService.keycloakEvents$.subscribe({
						next: (e) => {
							if (
								e.type === KeycloakEventType.OnTokenExpired
							) {
								this.refreshToken({}).then((isRefreshed) => {
									if (!isRefreshed) {
										this.clear();
									}

									return isRefreshed;
								}).catch(x => {
									this.clear();
									throw x;
								});
							}
						},
					});
					this.onAuthenticateSuccess(returnUrl);
				})
				.catch((error) => this.onAuthenticateError(error));
		} else {
			this.zone.run(() => this.router.navigateByUrl(returnUrl));
		}
	}

	onAuthenticateError(errorResponse: HttpErrorResponse) {
		this.zone.run(() => {
			this.uiNotificationService.snackBarNotification(
				errorResponse.message,
				SnackBarNotificationLevel.Warning
			);
		});
	}

	onAuthenticateSuccess(returnUrl: string): void {
		this.authState(true);
		this.uiNotificationService.snackBarNotification(
			this.language.instant('GENERAL.SNACK-BAR.SUCCESSFUL-LOGIN'),
			SnackBarNotificationLevel.Success
		);
		this.zone.run(() => this.router.navigateByUrl(returnUrl));
	}

	public refreshToken(httpParams?: Object): Promise<boolean> {
		return this.keycloakService.updateToken().then((isRefreshed) => {
			if (!isRefreshed) {
				return false;
			}

			return this.prepareAuthRequest(
				from(this.keycloakService.getToken()),
				httpParams
			)
				.pipe(takeUntil(this._destroyed))
				.pipe(
					map(
						() => {
							return true;
						},
						(error) => {
							this.onAuthenticateError(error);
							return false;
						}
					)
				)
				.toPromise();
		});
	}

	public prepareAuthRequestNew(observable: Observable<string>, httpParams?: Object): Observable<boolean> {
		return observable.pipe(
			map((x) => this.currentAuthenticationToken(x)),
			exhaustMap(() => forkJoin([
				this.installationConfiguration.appServiceEnabled ? this.appPrincipalService.me(httpParams) : of(null)
			])),
			map((item) => {
				this.currentAccount(item[0]);
				return true;
			})
		);
	}

	private currentAccount(appAccount: AppAccount): void {
		this.appAccount = appAccount;
		this.authState(true);
	}

	//
	//
	// Permissions
	//
	//

	public hasPermission(permission: AppPermission): boolean {
		if (this.appAccount?.permissions == null) return false;

		if (!this.installationConfiguration.appServiceEnabled) { return true; } //TODO: maybe reconsider
		return this.evaluatePermission(this.appAccount.permissions, permission);
	}

	private evaluatePermission(availablePermissions: string[], permissionToCheck: string): boolean {
		if (!permissionToCheck) { return false; }
		if (this.hasRole(RoleType.Admin)) { return true; }
		return availablePermissions.map(x => x.toLowerCase()).includes(permissionToCheck.toLowerCase());
	}

	public hasAnyRole(roles: RoleType[]): boolean {
		if (!roles) { return false; }
		return roles.filter((r) => this.hasRole(r)).length > 0;
	}

	public hasRole(role: RoleType): boolean {
		if (role === undefined) { return false; }
		if (!this.appAccount || !this.appAccount.claims || !this.appAccount.claims.roles || this.appAccount.claims.roles.length === 0) { return false; }
		return this.appAccount.claims.roles.map(x => x.toLowerCase()).includes(role.toLowerCase());
	}

	public hasAnyPermission(permissions: AppPermission[]): boolean {
		if (!permissions) { return false; }
		return permissions.filter((p) => this.hasPermission(p)).length > 0;
	}

	public authorize(context: ResolutionContext): boolean {

		if (!context || this.hasRole(RoleType.Admin)) { return true; }

		let roleAuthorized = false;
		if (context.roles && context.roles.length > 0) {
			roleAuthorized = this.hasAnyRole(context.roles);
		}

		let permissionAuthorized = false;
		if (context.permissions && context.permissions.length > 0) {
			permissionAuthorized = this.hasAnyPermission(context.permissions);
		}

		if (roleAuthorized || permissionAuthorized) { return true; }

		return false;
	}

	public isLoggedIn(): boolean {
		return this.authState();
	}
}
