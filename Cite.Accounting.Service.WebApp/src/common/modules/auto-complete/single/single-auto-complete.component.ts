import { FocusMonitor } from '@angular/cdk/a11y';
import { COMMA, ENTER } from '@angular/cdk/keycodes';
import { Component, DoCheck, ElementRef, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Optional, Output, Self, SimpleChanges, TemplateRef, ViewChild } from '@angular/core';
import { ControlValueAccessor, FormGroupDirective, NgControl, NgForm } from '@angular/forms';
import { MatAutocomplete, MatAutocompleteSelectedEvent, MatAutocompleteTrigger } from '@angular/material/autocomplete';
import { ErrorStateMatcher, mixinErrorState } from '@angular/material/core';
import { MatFormFieldControl } from '@angular/material/form-field';
import { BaseComponent } from '@common/base/base.component';
import { Observable, Subject, Subscription, of as observableOf, of } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, map, mergeMap, startWith, switchMap, takeUntil, tap } from 'rxjs/operators';
import { SingleAutoCompleteConfiguration } from './single-auto-complete-configuration';
import { AutoCompleteGroup } from '../auto-complete-group';

export class CustomComponentBase extends BaseComponent {
	constructor(
		public _defaultErrorStateMatcher: ErrorStateMatcher,
		public _parentForm: NgForm,
		public _parentFormGroup: FormGroupDirective,
		public ngControl: NgControl,
		public stateChanges: Subject<void>
	) { super(); }
}
export const _CustomComponentMixinBase = mixinErrorState(CustomComponentBase);

@Component({
	selector: 'app-single-auto-complete',
	templateUrl: './single-auto-complete.component.html',
	styleUrls: ['./single-auto-complete.component.scss'],
	providers: [{ provide: MatFormFieldControl, useExisting: SingleAutoCompleteComponent }],
})
export class SingleAutoCompleteComponent extends _CustomComponentMixinBase implements OnInit, MatFormFieldControl<string>, ControlValueAccessor, OnDestroy, DoCheck {

	static nextId = 0;
	errorState: boolean;
	errorStateMatcher: ErrorStateMatcher;
	@ViewChild('autocomplete', { static: true }) autocomplete: MatAutocomplete;
	@ViewChild('autocompleteTrigger', { static: true }) autocompleteTrigger: MatAutocompleteTrigger;
	@ViewChild('autocompleteInput', { static: true }) autocompleteInput: ElementRef;

	@Input() configuration: SingleAutoCompleteConfiguration;
	// Selected Option Event
	@Output() optionSelected: EventEmitter<any> = new EventEmitter();

	id = `single-autocomplete-${SingleAutoCompleteComponent.nextId++}`;
	stateChanges = new Subject<void>();
	focused = false;
	controlType = 'single-autocomplete';
	describedBy = '';
	_inputValue: string = '';
	_inputSubject = new Subject<string>();
	loading = false;
	_items: Observable<any[]>;
	_arrow_items: Observable<any[]>;
	_groupedItems: Observable<AutoCompleteGroup[]>;
	private requestDelay = 200; //ms
	private minFilteringChars = 0;
	private loadDataOnStart = true;
	separatorKeysCodes: number[] = [ENTER, COMMA];

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
		this.stateChanges.next();
	}
	private _selectedValue;

	constructor(
		private fm: FocusMonitor,
		private elRef: ElementRef,
		@Optional() @Self() public ngControl: NgControl,
		@Optional() _parentForm: NgForm,
		@Optional() _parentFormGroup: FormGroupDirective,
		_defaultErrorStateMatcher: ErrorStateMatcher
	) {
		super(_defaultErrorStateMatcher, _parentForm, _parentFormGroup, ngControl, new Subject<void>());

		fm.monitor(elRef.nativeElement, true).pipe(takeUntil(this._destroyed)).subscribe((origin) => {
			//this.focused = !!origin;
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

	ngDoCheck(): void {
		if (this.ngControl) {
			this.updateErrorState();
		}
	}

	filter(query: string): Observable<any[]> {
		// If loadDataOnStart is enabled and query is empty we return the initial items.
		if (this.isNullOrEmpty(query) && this.loadDataOnStart) {
			return this.configuration.initialItems(this.configuration.extraData) || observableOf([]);
		} else if (query && query.length >= this.minFilteringChars) {
			if (this.configuration.filterFn) {
				return this.configuration.filterFn(query, this.configuration.extraData);
			} else {
				return this.configuration.initialItems(this.configuration.extraData) || observableOf([]);
			}
		} else {
			return observableOf([]);
		}
	}

	isNullOrEmpty(query: string): boolean {
		return typeof query !== 'string' || query === null || query.length === 0;
	}

	_displayFn(item: any): string {
		if (this.configuration.displayFn && item) { return this.configuration.displayFn(item); }
		return item;
	}

	_titleFn(item: any): string {
		if (this.configuration.titleFn && item) { return this.configuration.titleFn(item); }
		return item;
	}

	_optionTemplate(item: any): TemplateRef<any> {
		if (this.configuration.optionTemplate && item) { return this.configuration.optionTemplate; }
		return null;
	}

	_selectedValueTemplate(item: any): TemplateRef<any> {
		if (this.configuration.selectedValueTemplate && item) { return this.configuration.selectedValueTemplate; }
		return null;
	}

	_subtitleFn(item: any): string {
		if (this.configuration.subtitleFn && item) { return this.configuration.subtitleFn(item); }
		return null;
	}

	_valueToAssign(item: any): any {
		if (this.configuration.valueAssign && item) { return this.configuration.valueAssign(item); }
		return item;
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

	_optionSelected(event: MatAutocompleteSelectedEvent) {
		this._inputValue = this._displayFn(event.option.value);
		this._optionSelectedInternal(event.option.value);
	}

	private _optionSelectedInternal(item: any): void {
		const newValue = this._valueToAssign(item);
		this._setValue(newValue);

		this.stateChanges.next();
		this.optionSelected.emit(item);
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
				mergeMap(query => this.filter(query)),
				catchError(error => {
					this._items = null;
					console.error(error);
					return of(null);
				})
				);

			if (this.configuration.groupingFn) { this._groupedItems = this._items.pipe(map(items => this.configuration.groupingFn(items))); }
		}
	}

	_onArrowClickedFocus() {
		if (this.disabled) { return; }
		this.chipRemove();
		this._onInputFocus();
		setTimeout(() => {
			if (!this.autocompleteTrigger.panelOpen) {
				this.autocompleteTrigger.openPanel();
				this._inputValueChange(null);
			}
		}, 0);
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

	public onKeyUp(event: KeyboardEvent) {
		this._inputValue = (event.currentTarget as HTMLInputElement)?.value;

		if (event.key !== 'Enter') {
			if ((this._inputValue?.length === 0 || this._inputValue == null) && this.value != null) {
				this._clearValue();
				this._onInputFocus();
				return;
			}
			this._inputSubject.next(this._inputValue);
		}
	}

	public onBlur($event: MouseEvent) {
		if (this.value != null) {
			const inputLabel = this.autocompleteInput.nativeElement.value;
			const selectedLabel = this._displayFn(this.value);
			if (inputLabel != null && selectedLabel !== inputLabel) {
				this._inputValue = selectedLabel;
			}
		} else if (this._inputValue && this._inputValue.length > 1 && this.autocomplete.options && this.autocomplete.options.length > 0) {
			this._inputValue = this._displayFn(this.autocomplete.options.first.value);
			this._optionSelectedInternal(this.autocomplete.options.first.value);
		}
	}

	onChange = (_: any) => { };
	private _onTouched = () => { };
	writeValue(value: any): void { 
		this.value = value;
		
		if (value != null) {
			this._inputValue = this._displayFn(value);
		} else if (this.autocompleteInput && this.autocompleteInput.nativeElement && this.autocompleteInput.nativeElement.value) {
				this.autocompleteInput.nativeElement.value = '';
		}
	}
	pushChanges(value: any) { this.onChange(value); }
	registerOnChange(fn: (_: any) => {}): void { this.onChange = fn; }
	registerOnTouched(fn: () => {}): void { this._onTouched = fn; }
	setDisabledState(isDisabled: boolean): void { this.disabled = isDisabled; }

	setDescribedByIds(ids: string[]) {
		this.describedBy = ids.join(' ');
	}

	onContainerClick(event: MouseEvent) {
		event.stopPropagation();
		if (this.disabled) { return; }
		this._onInputFocus();
		if (!this.autocomplete.isOpen) {
			this.autocompleteTrigger.openPanel();
		}
	}

	chipRemove(): void {
		this._setValue(null);
		this._inputValueChange(null);
	}

	autoCompleteDisplayFn() {
		return (val) => '';
	}

	private _clearValue(): void{
		this._setValue(null);
		this.stateChanges.next();
		this.optionSelected.emit(null);
		this._inputValue = null;
	}

	ngOnDestroy() {
		this.stateChanges.complete();
		this.fm.stopMonitoring(this.elRef.nativeElement);
	}
}
