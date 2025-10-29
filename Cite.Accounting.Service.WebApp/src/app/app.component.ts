import { OverlayContainer } from '@angular/cdk/overlay';
import { AfterViewInit, Component, HostBinding, OnInit, ViewChild } from '@angular/core';
import { ThemeType } from '@app/core/enum/theme-type.enum';
import { AuthService, LoginStatus } from '@app/core/services/ui/auth.service';
import { ThemingService } from '@app/core/services/ui/theming.service';
import { BaseComponent } from '@common/base/base.component';
import { InstallationConfigurationService } from '@common/installation-configuration/installation-configuration.service';
import { LoggingService } from '@common/logging/logging-service';
import { CultureService } from '@user-service/services/culture.service';
import { LanguageService } from '@user-service/services/language.service';
import { TimezoneService } from '@user-service/services/timezone.service';
import { CookieService } from 'ngx-cookie-service';
import { takeUntil } from 'rxjs/operators';
import { MatSidenav } from '@angular/material/sidenav';
import { SidebarService } from './ui/misc/navigation/services/sidebar.service';

@Component({
	selector: 'app-root',
	templateUrl: './app.component.html',
	styleUrls: ['./app.component.scss']
})
export class AppComponent extends BaseComponent implements OnInit, AfterViewInit {
	@HostBinding('class') componentCssClass;

	@ViewChild('sidenav') sidenav: MatSidenav;

	constructor(
		private installationConfiguration: InstallationConfigurationService,
		private authService: AuthService,
		private overlayContainer: OverlayContainer,
		private cookieService: CookieService,
		private themingService: ThemingService,
		private languageService: LanguageService,
		private cultureService: CultureService,
		private timezoneService: TimezoneService,
		private logger: LoggingService,
		private sidebarService: SidebarService,
	) {
		super();

		this.initializeServices();

		this.authService.getAuthenticationStateObservable().pipe(takeUntil(this._destroyed)).subscribe(authenticationState => {
			if (authenticationState.loginStatus === LoginStatus.LoggedIn) {
				this.updateServices();
			}
		});
	}

	ngOnInit() {
	}
	
	ngAfterViewInit(): void {
		setTimeout(() => {
			this.sidebarService.status().pipe(takeUntil(this._destroyed)).subscribe(isopen => {
				const hamburger = document.getElementById('hamburger');
				if (isopen) {
					//update value of hamburfer
					if (!hamburger) {//try later
						setTimeout(() => {
							const hamburger = document.getElementById('hamburger');
							if (hamburger) {
								hamburger.classList.add('change');
							}
						}, 300);
					} else {
						hamburger.classList.add('change');
					}
					this.sidenav.open()
				} else {//closed
					if (!hamburger) {//try later
						setTimeout(() => {
							const hamburger = document.getElementById('hamburger');
							if (hamburger) {
								hamburger.classList.remove('change');
							}
						}, 300);
					} else {
						hamburger.classList.remove('change');
					}
					this.sidenav.close();
				}
			});
		});
	}

	isMac(): boolean {
		let bool = false;
		if (navigator.platform.toUpperCase().indexOf('MAC') >= 0 || navigator.platform.toUpperCase().indexOf('IPAD') >= 0) {
			bool = true;
		}
		return bool;
	}

	initializeServices() {
		this.languageService.changeLanguage(this.installationConfiguration.defaultLanguage);
		this.languageService.languageSelected(this.installationConfiguration.defaultLanguage, false);

		this.cultureService.cultureSelected(this.installationConfiguration.defaultCulture);
		this.timezoneService.timezoneSelected(this.installationConfiguration.defaultTimezone);

		const selectedTheme: ThemeType = Number.parseInt(this.cookieService.get('theme')) || this.installationConfiguration.defaultTheme;
		this.overlayContainer.getContainerElement().classList.add(this.themingService.getThemeClass(selectedTheme));
		this.componentCssClass = this.themingService.getThemeClass(selectedTheme);
		this.themingService.themeSelected(selectedTheme);
		this.themingService.applyCssColors(this.installationConfiguration.cssConfiguration);

		this.setupChangeListeners();
	}

	updateServices() {
		const language = this.authService.getUserProfileLanguage();
		if (language) {
			const accLanguage = this.languageService.getLanguageKey(language);
			if (accLanguage !== undefined) {
				this.languageService.changeLanguage(accLanguage);
				this.languageService.languageSelected(accLanguage, false);
			} else { // TODO: throw error if unsupported language?
				this.logger.error(`unsupported language: ${language}`);
			}
		}

		const culture = this.authService.getUserProfileCulture();
		if (culture) {
			const accCulture = this.cultureService.getCultureValue(culture);
			if (accCulture !== undefined) {
				this.cultureService.cultureSelected(accCulture);
			} else { // TODO: throw error if unsupported culture?
				this.logger.error(`unsupported culture: ${culture}`);
			}
		}

		const timezone = this.authService.getUserProfileTimezone();
		if (timezone) {
			if (this.timezoneService.hasTimezoneValue(timezone)) {
				this.timezoneService.timezoneSelected(timezone);
			} else { // TODO: throw error if unsupported timezone?
				this.logger.error(`unsupported timezone: ${timezone}`);
			}
		}
	}

	setupChangeListeners() {
		this.languageService.getLanguageChangeObservable().pipe(takeUntil(this._destroyed)).subscribe(newLanguage => {
			this.languageService.changeLanguage(newLanguage);
		});

		this.themingService.getThemeChangeObservable().pipe(takeUntil(this._destroyed)).subscribe(newTheme => {
			const overlayContainer = this.overlayContainer.getContainerElement();
			const selectedThemeClass = this.themingService.getThemeClass(newTheme);
			this.themingService.getThemeClasses().forEach(theme => {
				if (theme !== selectedThemeClass) { overlayContainer.classList.remove(theme); }
			});

			overlayContainer.classList.add(selectedThemeClass);
			this.componentCssClass = selectedThemeClass;
			this.cookieService.set('theme', newTheme.toString(), null, '/');
		});
	}

	public isAuthenticated(): boolean {
		return this.authService.currentAccountIsAuthenticated();
	}
}
