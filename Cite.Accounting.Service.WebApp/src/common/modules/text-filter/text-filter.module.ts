import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TextFilterComponent } from '@common/modules/text-filter/text-filter.component';
import { CommonUiModule } from '@common/ui/common-ui.module';
import { ExpandableSearchFieldComponent } from './expandable-search-field/expandable-search-field.component';

@NgModule({
	imports: [
		CommonUiModule,
		FormsModule,
		ReactiveFormsModule,
	],
	declarations: [
		TextFilterComponent,
		ExpandableSearchFieldComponent,
	],
	exports: [
		TextFilterComponent,
		ExpandableSearchFieldComponent,
	]
})
export class TextFilterModule {
	constructor() { }
}
