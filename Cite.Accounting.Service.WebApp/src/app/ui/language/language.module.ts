import { NgModule } from "@angular/core";
import { LanguageComponent } from "./language-content/language.component";
import { CommonUiModule } from "@common/ui/common-ui.module";

@NgModule({
  declarations: [
    LanguageComponent
  ],
  imports: [
    CommonUiModule
  ],
  exports: [
    LanguageComponent
  ],
})
export class LanguageModule { }