import { Component, EventEmitter, Input, OnInit, Output } from "@angular/core";
import { MatButtonToggleChange } from "@angular/material/button-toggle";
import { Router } from "@angular/router";
import { LanguageType } from "@app/core/enum/language-type.enum";
import { AuthService } from "@app/core/services/ui/auth.service";
import { BaseComponent } from "@common/base/base.component";
import { InstallationConfigurationService } from "@common/installation-configuration/installation-configuration.service";
import { TranslateService } from "@ngx-translate/core";
import { LanguageService } from "@user-service/services/language.service";


@Component({
  selector: 'app-language',
  templateUrl: './language.component.html',
  styleUrls: ['./language.component.scss'],
})
export class LanguageComponent extends BaseComponent implements OnInit {

  @Input() languages: LanguageType[] = [];
  @Input() selectedLanguage: LanguageType = this.config.defaultLanguage;

	@Output() languageChange: EventEmitter<any> = new EventEmitter();

  constructor(
    private router: Router,
    private languageService: LanguageService,
    private translation: TranslateService,
    private config: InstallationConfigurationService,
  ) {
    super();
  }

  ngOnInit(): void {
    this.languageChange.emit(this.languageService.getCurrentLanguage());
  }

  displayFn(language: LanguageType): string {
    switch (language){
      case LanguageType.English: return this.translation.instant('COMMONS.LANGUAGES.EN');
      case LanguageType.Greek: return this.translation.instant('COMMONS.LANGUAGES.GR');
      default : return this.translation.instant('COMMONS.LANGUAGES.' + this.languageService.getLanguageValue(this.config.defaultLanguage).toUpperCase())
    }
  }

  onLanguageSelected(selectedLanguage: MatButtonToggleChange): void {
    this.languageService.changeLanguage(selectedLanguage.value);
    this.languageChange.emit(selectedLanguage.value);
    this.router.navigateByUrl(this.router.url);
  }
}