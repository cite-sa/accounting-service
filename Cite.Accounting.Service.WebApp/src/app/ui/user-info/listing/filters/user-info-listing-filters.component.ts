import { Component, EventEmitter, Input, OnInit, Output, OnChanges, SimpleChanges } from '@angular/core';
import { AppEnumUtils } from '@app/core/formatting/enum-utils.service';
import { BaseComponent } from '@common/base/base.component';
import { UserInfoFilter } from '@app/core/query/user-info.lookup';
import { UserInfoService } from '@app/core/services/http/user-info.service';
import { Lookup } from '@common/model/lookup';

@Component({
	selector: 'app-user-info-listing-filters',
	templateUrl: './user-info-listing-filters.component.html',
	styleUrls: ['./user-info-listing-filters.component.scss']
})
export class UserInfoListingFiltersComponent extends BaseComponent implements OnInit, OnChanges {

	@Input() key = { key: 'UserInfoListingUserSettings' };
	@Input() lookup: Lookup;
	@Input() filter: UserInfoFilter;
	@Input() autoSelectUserSettings: boolean = false;
	
	@Output() filterChange = new EventEmitter<UserInfoFilter>();
	@Output() changeSetting = new EventEmitter<Lookup>();
	
	filterSelections: { [key: string]: boolean } = {};
	editingFilter: boolean = false;

	constructor(
		public enumUtils: AppEnumUtils,
		public userInfoService: UserInfoService
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
	visibleFields(filter: UserInfoFilter) {
		this.filterSelections['likeOption'] = (filter.like && filter.like.length !== 0) ? true : false;
	}

	get like(): string {
		return this.filter.like;
	}
	set like(value: string) {
		this.filter.like = value;
		this.filterChange.emit(this.filter);
	}
}
