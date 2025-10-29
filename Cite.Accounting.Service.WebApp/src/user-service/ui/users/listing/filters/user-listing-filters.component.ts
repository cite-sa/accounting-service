import { Component, EventEmitter, Input, OnInit, Output, SimpleChanges, OnChanges } from '@angular/core';
import { BaseComponent } from '@common/base/base.component';
import { Lookup } from '@common/model/lookup';
import { IsActive } from '@user-service/core/enum/is-active.enum';
import { UserServiceEnumUtils } from '@user-service/core/formatting/enum-utils.service';
import { UserFilter } from '@user-service/core/query/user.lookup';

@Component({
	selector: 'app-user-listing-filters',
	templateUrl: './user-listing-filters.component.html',
	styleUrls: ['./user-listing-filters.component.scss']
})
export class UserListingFiltersComponent extends BaseComponent implements OnInit, OnChanges {

	@Input() key = { key: 'UserListingUserSettings' };
	@Input() lookup: Lookup;
	@Input() filter: UserFilter;
	@Input() autoSelectUserSettings: boolean = false;

	@Output() filterChange = new EventEmitter<UserFilter>();
	@Output() changeSetting = new EventEmitter<Lookup>();
	
	panelExpanded = false;
	editingFilter: boolean = false;

	constructor(
		public enumUtils: UserServiceEnumUtils
	) { super(); }

	ngOnInit() {
		this.panelExpanded = !this.areHiddenFieldsEmpty();
	}

	ngOnChanges(changes: SimpleChanges): void {
		if (changes['filter']) { 
			this.editingFilter = false;
			this.panelExpanded = !this.areHiddenFieldsEmpty(); 
		}
	}

	onFilterChange() {
		this.editingFilter = true;
		this.filterChange.emit(this.filter);
	}

	onSettingChange(event: Lookup): void {
		this.changeSetting.emit(event);
	}

	private areHiddenFieldsEmpty(): boolean {
		return (!this.filter.isActive || this.filter.isActive.length === 0 || (this.filter.isActive.length === 1 && this.filter.isActive[0] === IsActive.Active));
	}

	//
	// Filter getters / setters
	// Implement here any custom logic regarding how these fields are applied to the lookup.
	//
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
}
