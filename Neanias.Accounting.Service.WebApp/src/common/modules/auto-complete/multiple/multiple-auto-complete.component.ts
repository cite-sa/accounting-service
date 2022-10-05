import { FocusMonitor } from '@angular/cdk/a11y';
import { COMMA, ENTER } from '@angular/cdk/keycodes';
import { Component, ElementRef, EventEmitter, Input, OnDestroy, OnInit, Optional, Output, Self, TemplateRef, ViewChild } from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';
import { MatAutocompleteSelectedEvent, MatAutocompleteTrigger } from '@angular/material/autocomplete';
import { MatChipInputEvent } from '@angular/material/chips';
import { MatFormFieldControl } from '@angular/material/form-field';
import { AutoCompleteGroup } from '@common/modules/auto-complete/auto-complete-group';
import { MultipleAutoCompleteConfiguration } from '@common/modules/auto-complete/multiple/multiple-auto-complete-configuration';
import { Observable, of as observableOf, Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, map, mergeMap, startWith, tap } from 'rxjs/operators';

@Component({
	selector: 'app-multiple-auto-complete',
	templateUrl: './multiple-auto-complete.component.html',
	styleUrls: ['./multiple-auto-complete.component.scss'],
	providers: [{ provide: MatFormFieldControl, useExisting: MultipleAutoCompleteComponent }]
})
export class MultipleAutoCompleteComponent implements OnInit, MatFormFieldControl<string>, ControlValueAccessor, OnDestroy {

	static nextId = 0;

	separatorKeysCodes = [ENTER, COMMA];

	@Input() configuration: MultipleAutoCompleteConfiguration;
	// Selected Option Event
	@Output() optionSelected: EventEmitter<any> = new EventEmitter();
	// Removed Option Event
	@Output() optionRemoved: EventEmitter<any> = new EventEmitter();

	id = `multiple-autocomplete-${MultipleAutoCompleteComponent.nextId++}`;
	stateChanges = new Subject<void>();
	focused = false;
	errorState = false;
	controlType = 'multiple-autocomplete';
	describedBy = '';
	_inputValue: string;
	_inputSubject = new Subject<string>();
	loading = false;
	_items: Observable<any[]>;
	_groupedItems: Observable<AutoCompleteGroup[]>;
	private requestDelay = 200; //ms
	private minFilteringChars = 3;
	private loadDataOnStart = true;
	visible = true;
	selectable = true;
	removable = true;
	addOnBlur = false;

	get empty() {
		return !this._inputValue || this._inputValue.length === 0;
	}

	get shouldLabelFloat() { return this.focused || !this.empty; }

	@Input()
	get placeholder() { return this._placeholder; }
	set placeholder(placeholder) {
		this._placeholder = placeholder;
		this.stateChanges.next();
	}
	private _placeholder: string;

	@Input()
	get required() { return this._required; }
	set required(req) {
		this._required = !!(req);
		this.stateChanges.next();
	}
	private _required = false;

	@Input()
	get disabled() { return this._disabled; }
	set disabled(dis) {
		this._disabled = !!(dis);
		this.stateChanges.next();
	}
	private _disabled = false;

	@Input()
	get value(): any | null {
		return this._selectedValue;
	}
	set value(value: any | null) {
		this._selectedValue = value;
		this._inputValue = value;
		this.stateChanges.next();
	}
	private _selectedValue;





	@ViewChild('textInput') textInput: ElementRef;
	@ViewChild(MatAutocompleteTrigger, { static: true }) autocomplete: MatAutocompleteTrigger;




	constructor(
		private fm: FocusMonitor,
		private elRef: ElementRef,
		@Optional() @Self() public ngControl: NgControl) {

		fm.monitor(elRef.nativeElement, true).subscribe((origin) => {
			this.focused = !!origin;
			this.stateChanges.next();
		});

		if (this.ngControl != null) {
			// Setting the value accessor directly (instead of using
			// the providers) to avoid running into a circular import.
			this.ngControl.valueAccessor = this;
		}
	}

	ngOnInit() {

	}

	filter(query: string): Observable<any[]> {
		// If loadDataOnStart is enabled and query is empty we return the initial items.
		if (this.isNullOrEmpty(query) && this.loadDataOnStart) {
			return this.configuration.initialItems(this.value || [], this.configuration.extraData) || observableOf([]);
		} else if (query && query.length >= this.minFilteringChars) {
			if (this.configuration.filterFn) {
				return this.configuration.filterFn(query, this.value || [], this.configuration.extraData);
			} else {
				return this.configuration.initialItems(this.value || [], this.configuration.extraData) || observableOf([]);
			}
		} else {
			return observableOf([]);
		}
	}

	isNullOrEmpty(query: string): boolean {
		return typeof query !== 'string' || query === null || query.length === 0;
	}

	_optionTemplate(item: any): TemplateRef<any> {
		if (this.configuration.optionTemplate && item) { return this.configuration.optionTemplate; }
		return null;
	}

	_selectedValueTemplate(item: any): TemplateRef<any> {
		if (this.configuration.selectedValueTemplate && item) { return this.configuration.selectedValueTemplate; }
		return null;
	}

	_displayFn(item: any): string {
		if (this.configuration.displayFn && item) { return this.configuration.displayFn(item); }
		return item;
	}

	_titleFn(item: any): string {
		if (this.configuration.titleFn && item) { return this.configuration.titleFn(item); }
		return item;
	}

	_subtitleFn(item: any): string {
		if (this.configuration.subtitleFn && item) { return this.configuration.subtitleFn(item); }
		return null;
	}

	_requestDelay(): number {
		return this.configuration.requestDelay || this.requestDelay;
	}

	_minFilteringChars(): number {
		return this.configuration.minFilteringChars || this.minFilteringChars;
	}

	_loadDataOnStart(): boolean {
		return this.configuration.loadDataOnStart || this.loadDataOnStart;
	}

	_chipItems(): any[] {
		return this.value || [];
	}

	_optionSelected(event: MatAutocompleteSelectedEvent) {
		const newValue = this.value || [];
		newValue.push(event.option.value);
		// this.reactiveFormControl.patchValue(newValue);
		// this.inputFormControl.setValue(null);
		this._setValue(newValue);
		this.stateChanges.next();
		this.optionSelected.emit(newValue);
		this.textInput.nativeElement.value = '';
	}

	_onArrowClickedFocus(evt) {
		setTimeout(() => {
			if (!this.autocomplete.panelOpen) {
				this.autocomplete.openPanel();
				this._inputValueChange(null);
			}
		}, 0);
	}

	private _setValue(value: any) {
		this.value = value;
		this.pushChanges(this.value);
	}

	_onInputFocus() {
		// We set the items observable on focus to avoid the request being executed on component load.
		if (!this._items) {
			this._items = this._inputSubject.pipe(
				startWith(null),
				debounceTime(this.requestDelay),
				distinctUntilChanged(),
				tap(() => { this.loading = true; }),
				mergeMap(query => {
					// If its a valid object, a selection just made and the object is set as the value of the form control. That means we should fire an extra request to the server.
					if (this._isValidObject(query)) { return observableOf([]); }

					// Since the object is changed we need to clear any existing selections, except for the first time.
					if (query !== null) { this.pushChanges(null); }
					return this.filter(query);
				}),
				tap(() => { this.loading = false; }));

			if (this.configuration.groupingFn) { this._groupedItems = this._items.pipe(map(items => this.configuration.groupingFn(items))); }
		}
	}

	_inputValueChange(value: string) {
		this._inputValue = value;
		this._inputSubject.next(value);
		this.stateChanges.next();
	}

	_isValidObject(value: any): boolean {
		try {
			if (!value) { return false; }
			if (typeof value !== 'object') { JSON.parse(value); }
		} catch (e) {
			return false;
		}
		return true;
	}

	_removeSelectedItem(item: any): void {
		const index = this.value.indexOf(item);
		if (index >= 0) {
			this.value.splice(index, 1);
			this.optionRemoved.emit(item);
		}
		this.textInput.nativeElement.focus();
		this.pushChanges(this.value);
	}

	_onInputClick(item: any) {
		if (!this.autocomplete.panelOpen) {
			this.autocomplete.openPanel();
		}
	}

	_addItem(event: MatChipInputEvent): void {
		// const input = event.input;
		// const value = event.value;
		// // Add our fruit
		// if ((value || '').trim()) {
		// 	this.selectedItems.push(value.trim());
		// }
		// // Reset the input value
		// if (input) {
		// 	input.value = '';
		// }
		// this.inputFormControl.setValue(null);
	}

	onChange = (_: any) => { };
	onTouched = () => { };
	writeValue(value: any): void { this.value = value || ''; }
	pushChanges(value: any) { this.onChange(value); }
	registerOnChange(fn: (_: any) => {}): void { this.onChange = fn; }
	registerOnTouched(fn: () => {}): void { this.onTouched = fn; }
	setDisabledState(isDisabled: boolean): void { }

	setDescribedByIds(ids: string[]) {
		this.describedBy = ids.join(' ');
	}

	onContainerClick(event: MouseEvent) {
		if ((event.target as Element).tagName.toLowerCase() !== 'input') {
			this.elRef.nativeElement.querySelector('input').focus();
		}
	}

	ngOnDestroy() {
		this.stateChanges.complete();
		this.fm.stopMonitoring(this.elRef.nativeElement);
	}
}
