import { Component, OnInit } from "@angular/core";
import { AuthService } from "@app/core/services/ui/auth.service";
import { SidebarService } from "./services/sidebar.service";
import { ProgressIndicationService } from "@app/core/services/ui/progress-indication.service";
import { BaseComponent } from "@common/base/base.component";
import { takeUntil } from "rxjs/operators";
import { LanguageService } from "@user-service/services/language.service";
import { LanguageType } from "@app/core/enum/language-type.enum";

@Component({
  selector: 'app-navbar',
  templateUrl: 'navbar.component.html',
  styleUrls: ['navbar.component.scss']
})
export class NavbarComponent extends BaseComponent implements OnInit {

	progressIndication = false;

	selectedLanguage: LanguageType;
	languages: LanguageType[] = [];

  public get getPrincipalName(): string {
		return this.authService.getPrincipalName() || '';
	}

  public get selectedLanguageValue(): string {
    return this.languageService.getCurrentLanguageValue();
  }
  
  constructor(
    private authService: AuthService,
    private sidebarService: SidebarService,
		private progressIndicationService: ProgressIndicationService,
    private languageService: LanguageService,
  ) {
    super();

    this.languages = this.languageService.getAvailableLanguageTypes();
    this.selectedLanguage = this.languageService.getCurrentLanguage();
  }

  ngOnInit(): void {
    this.progressIndicationService.getProgressIndicationObservable().pipe(takeUntil(this._destroyed)).subscribe(x => {
			setTimeout(() => { this.progressIndication = x; });
		});
  }

  public isAuthenticated(): boolean {
		return this.authService.currentAccountIsAuthenticated();
	}

  getLanguage(selectedLanguage: LanguageType) {
		this.selectedLanguage = selectedLanguage;
	}

  public toggleSidebar(): void {
    this.sidebarService.toggle();
  }
}