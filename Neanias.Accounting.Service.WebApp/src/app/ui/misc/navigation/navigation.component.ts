import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { LanguageType } from '@app/core/enum/language-type.enum';
import { ThemeType } from '@app/core/enum/theme-type.enum';
import { AuthService } from '@app/core/services/ui/auth.service';
import { ProgressIndicationService } from '@app/core/services/ui/progress-indication.service';
import { ThemingService } from '@app/core/services/ui/theming.service';
import { BaseComponent } from '@common/base/base.component';
import { InstallationConfigurationService } from '@common/installation-configuration/installation-configuration.service';
import { LanguageService } from '@user-service/services/language.service';
import { takeUntil } from 'rxjs/operators';

@Component({
	selector: 'app-navigation',
	templateUrl: './navigation.component.html',
	styleUrls: ['./navigation.component.scss'],
	encapsulation: ViewEncapsulation.None
})
export class NavigationComponent extends BaseComponent implements OnInit {
	progressIndication = false;
	themeTypes = ThemeType;
	languageTypes = LanguageType;
	inAppNotificationCount = 0;

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

	public getPrincipalName(): string {
		return this.authService.getPrincipalName() || '';
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
}
