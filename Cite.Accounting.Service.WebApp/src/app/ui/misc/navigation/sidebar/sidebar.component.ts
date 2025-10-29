import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { LanguageType } from '@app/core/enum/language-type.enum';
import { AppPermission } from '@app/core/enum/permission.enum';
import { ThemeType } from '@app/core/enum/theme-type.enum';
import { AuthService } from '@app/core/services/ui/auth.service';
import { ProgressIndicationService } from '@app/core/services/ui/progress-indication.service';
import { ThemingService } from '@app/core/services/ui/theming.service';
import { BaseComponent } from '@common/base/base.component';
import { InstallationConfigurationService } from '@common/installation-configuration/installation-configuration.service';
import { LanguageService } from '@user-service/services/language.service';
import { takeUntil } from 'rxjs/operators';

enum RouteType {
	System = 0,
	Configurable = 1,
}
declare interface RouteInfo {
	path: string;
	title: string;
	icon: string;
	externalUrl?: string;
	routeType: RouteType;
}
class GroupMenuItem {
	title: string;
	routes: RouteInfo[];
}

@Component({
	selector: 'app-sidebar',
	templateUrl: './sidebar.component.html',
	styleUrls: ['./sidebar.component.scss'],
	encapsulation: ViewEncapsulation.None
})
export class SidebarComponent extends BaseComponent implements OnInit {
	progressIndication = false;
	themeTypes = ThemeType;
	languageTypes = LanguageType;
	routeType = RouteType;
	inAppNotificationCount = 0;

	//Menu Items
	generalItems: GroupMenuItem;
	accountingItems: GroupMenuItem;
	servicesItems: GroupMenuItem;
	adminItems: GroupMenuItem;
	infoItems: GroupMenuItem;

	groupMenuItems: GroupMenuItem[] = [];

	constructor(
		public authService: AuthService,
		public router: Router,
		private route: ActivatedRoute,
		public themingService: ThemingService,
		public languageService: LanguageService,
		public installationConfigurationService: InstallationConfigurationService,
		private progressIndicationService: ProgressIndicationService,
	) {
		super();
	}
	
	ngOnInit() {
		this.progressIndicationService.getProgressIndicationObservable().pipe(takeUntil(this._destroyed)).subscribe(x => {
			setTimeout(() => { this.progressIndication = x; });
		});
		
		this.authService.getAuthenticationStateObservable().pipe(takeUntil(this._destroyed)).subscribe(authState => {
			this._calculateMenu();
		});
		
		this._calculateMenu();
	}

	public logout(): void {
		this.router.navigate(['/logout']);
	}

	public isAuthenticated(): boolean {
		return this.authService.currentAccountIsAuthenticated();
	}

	public isAdmin(): boolean {
		return this.authService.isAdmin();
	}

	onThemeSelected(selectedTheme: ThemeType) {
		this.themingService.themeSelected(selectedTheme);
	}

	onLanguageSelected(selectedLanguage: LanguageType) {
		this.languageService.languageSelected(selectedLanguage);
	}

	getUserProfileQueryParams(): any {
		const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || this.router.url;
		return { returnUrl: returnUrl };
	}

	goToExternalLink(url): void {
		window.open(url, '_blank');
	}

	private _calculateMenu(): void {
		this.groupMenuItems = []; 
		this.generalItems = { title: 'SIDE-BAR.GENERAL.TITLE', routes: [] };
		this.generalItems.routes.push({ path: '/home', 'title': 'SIDE-BAR.GENERAL.HOME', icon: 'home', routeType: RouteType.System });
		this.groupMenuItems.push(this.generalItems);
		
		this.accountingItems = { title: 'SIDE-BAR.MY-ACCOUNTING.TITLE', routes: [] };
		if (this.authService.hasPermission(AppPermission.ViewMyAccountingInfoPage)) this.accountingItems.routes.push({ path: '/my-accounting', 'title': 'SIDE-BAR.MY-ACCOUNTING.MY-ACCOUNTING', icon: 'query_stats', routeType: RouteType.System });
		this.groupMenuItems.push(this.accountingItems);
		
		this.servicesItems = { title: 'SIDE-BAR.SERVICES.TITLE', routes: [] };
		if (this.authService.hasPermission(AppPermission.ViewServicePage)) this.servicesItems.routes.push({ path: '/services', 'title': 'SIDE-BAR.SERVICES.SERVICES', icon: 'linked_services', routeType: RouteType.System });
		if (this.authService.hasPermission(AppPermission.ViewServiceResourcePage)) this.servicesItems.routes.push({ path: '/service-resources', 'title': 'SIDE-BAR.SERVICES.SERVICE-RESOURCES', icon: 'manage_search', routeType: RouteType.System });
		if (this.authService.hasPermission(AppPermission.ViewUserInfoPage)) this.servicesItems.routes.push({ path: '/service-users', 'title': 'SIDE-BAR.SERVICES.SERVICE-USERS', icon: 'contact_page', routeType: RouteType.System });
		if (this.authService.hasPermission(AppPermission.ViewServiceActionPage)) this.servicesItems.routes.push({ path: '/service-actions', 'title': 'SIDE-BAR.SERVICES.SERVICE-ACTIONS', icon: 'apps', routeType: RouteType.System });
		this.groupMenuItems.push(this.servicesItems);
		
		this.adminItems = { title: 'SIDE-BAR.ADMIN.TITLE', routes: [] };
		if (this.authService.hasPermission(AppPermission.ViewUsersPage)) this.adminItems.routes.push({ path: '/users', 'title': 'SIDE-BAR.ADMIN.USERS', icon: 'manage_accounts', routeType: RouteType.System });
		if (this.authService.hasPermission(AppPermission.ViewUserRolePage)) this.adminItems.routes.push({ path: '/user-roles', 'title': 'SIDE-BAR.ADMIN.USER-ROLES', icon: 'user_attributes', routeType: RouteType.System });
		if (this.authService.hasPermission(AppPermission.ViewServiceSyncPage)) this.adminItems.routes.push({ path: '/service-syncs', 'title': 'SIDE-BAR.ADMIN.SERVICE-SYNCS', icon: 'sync', routeType: RouteType.System });
		if (this.authService.hasPermission(AppPermission.ViewServiceResetEntrySyncPage)) this.adminItems.routes.push({ path: '/service-reset-entry-syncs', 'title': 'SIDE-BAR.ADMIN.SERVICE-RESET-ENTRY-SYNCS', icon: 'rule_settings', routeType: RouteType.System });
		this.groupMenuItems.push(this.adminItems);
		
		this.infoItems = { title: 'SIDE-BAR.INFO.TITLE', routes: [] };
		this.infoItems.routes.push({ path: null, externalUrl: '/assets/static-pages/privacy-statement.html', 'title': 'SIDE-BAR.INFO.PRIVACY', icon: 'privacy_tip', routeType: RouteType.Configurable });
		this.infoItems.routes.push({ path: null, externalUrl: '/assets/static-pages/terms-of-use.html', 'title': 'SIDE-BAR.INFO.TERMS', icon: 'policy', routeType: RouteType.Configurable });
		this.groupMenuItems.push(this.infoItems);
	}
}
