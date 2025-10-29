import { Component, EventEmitter, Input, OnInit, Output, OnChanges, SimpleChanges } from '@angular/core';
import { IsActive } from '@app/core/enum/is-active.enum';
import { AppEnumUtils } from '@app/core/formatting/enum-utils.service';
import { BaseComponent } from '@common/base/base.component';
import { MatCheckboxChange } from '@angular/material/checkbox';
import { ServiceResourceFilter } from '@app/core/query/service-resource.lookup';
import { ServiceResourceService } from '@app/core/services/http/service-resource.service';
import { Lookup } from '@common/model/lookup';

@Component({
	selector: 'app-service-resource-listing-filters',
	templateUrl: './service-resource-listing-filters.component.html',
	styleUrls: ['./service-resource-listing-filters.component.scss']
})
export class ServiceResourceListingFiltersComponent extends BaseComponent implements OnInit, OnChanges {

	@Input() key = { key: 'ServiceResourceListingUserSettings' };
	@Input() lookup: Lookup;
	@Input() filter: ServiceResourceFilter;
	@Input() autoSelectUserSettings: boolean = false;

	@Output() filterChange = new EventEmitter<ServiceResourceFilter>();
	@Output() changeSetting = new EventEmitter<Lookup>();
	
	filterSelections: { [key: string]: boolean } = {};
	editingFilter: boolean = false;

	constructor(
		public enumUtils: AppEnumUtils,
		public serviceResourceService: ServiceResourceService
	) { super(); }

	ngOnInit() {
	}

	ngOnChanges(changes: SimpleChanges): void {
		if (changes['filter']) { 
			this.editingFilter = false;

			this.visibleFields(this.filter); 
		}
	}

	onFilterChange() {
		this.editingFilter = true;

		this.filterChange.emit(this.filter);
	}

	onSettingChange(event: Lookup): void {
		this.changeSetting.emit(event);
	}

	//
	// Filter getters / setters
	// Implement here any custom logic regarding how these fields are applied to the lookup.
	//
	visibleFields(filter: ServiceResourceFilter) {
		this.filterSelections['isActiveOption'] = (filter.isActive && filter.isActive.length !== 0) ? true : false;
		this.filterSelections['likeOption'] = (filter.like && filter.like.length !== 0) ? true : false;
	}

	get like(): string {
		return this.filter.like;
	}
	set like(value: string) {
		this.filter.like = value;
		this.onFilterChange();
	}

	get isActive(): boolean {
		return this.filter.isActive ? this.filter.isActive.includes(IsActive.Inactive) : true;
	}
	set isActive(value: boolean) {
		this.filter.isActive = value ? [IsActive.Active, IsActive.Inactive] : [IsActive.Active];
		this.onFilterChange();
	}

	selectAll() {
		Object.keys(this.filterSelections).forEach(key => {
			this.filterSelections[key] = this._isFilterInMenu(key);
		});

		this.onFilterChange();
	}

	deselectAll() {
		this.filter.isActive = undefined;
		this.filter.like = undefined;

		this.visibleFields(this.filter);
		this.onFilterChange();
	}

	areAllSelected(): boolean {
		if (Object.values(this.filterSelections).indexOf(false) === -1) {
			return true;
		} else { return false; }
	}

	areNoneSelected(): boolean {
		if (Object.values(this.filterSelections).indexOf(true) === -1) {
			return true;
		} else { return false; }
	}

	countOpenFilters(): number {
		const filtered = Object.entries(this.filterSelections).filter(([k, v]) => this._isFilterInMenu(k) && v === true);
		return filtered.length === 0 ? undefined : filtered.length;
	}

	checkBoxChanged(event: MatCheckboxChange, filter: string) {
		if (!event.checked && this.filter[filter] != null) {
			this.filter[filter] = filter !== 'isActive' ? undefined : [IsActive.Active];
			this.filterChange.emit(this.filter);
		}
	}

	private _isFilterInMenu(filterName: string): boolean {

		switch(filterName) {
			case "likeOption":
				return false; 
			default: 
				return true;
		}
	}
}
