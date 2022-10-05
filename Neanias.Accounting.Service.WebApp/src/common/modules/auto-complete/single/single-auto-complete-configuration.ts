import { TemplateRef } from '@angular/core';
import { AutoCompleteGroup } from '@common/modules/auto-complete/auto-complete-group';
import { Observable } from 'rxjs';

export interface SingleAutoCompleteConfiguration {
	// Delay for performing the request. Default: 200ms.
	requestDelay?: number;
	// Min characters for the filtering to be applied. Default: 1.
	minFilteringChars?: number;
	// Load and present items from start, without user query. Default: true.
	loadDataOnStart?: boolean;
	// Static or initial items.
	initialItems?: (data?: any) => Observable<any[]>;
	// Data retrieval function
	filterFn?: (searchQuery: string, data?: any) => Observable<any[]>;
	// Property formating for input
	displayFn?: (item: any) => string;
	// Property formating for dropdown
	groupingFn?: (items: any[]) => AutoCompleteGroup[];
	// Property formating for dropdown
	titleFn?: (item: any) => string;
	// Property formating for dropdown
	subtitleFn?: (item: any) => string;
	//Extra data passed to query function
	extraData?: any;
	// Callback to intercept value assignment based on item selection
	valueAssign?: (selectedItem: any) => any;
	// Property formating template
	optionTemplate?: TemplateRef<any>;
	// Selected value formating template
	selectedValueTemplate?: TemplateRef<any>;
}
