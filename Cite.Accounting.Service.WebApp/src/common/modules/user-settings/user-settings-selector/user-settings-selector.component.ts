import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { FilterNameDialogComponent } from '@app/ui/filter-name-dialog/filter-name-dialog.component';
import { BaseComponent } from '@common/base/base.component';
import { Lookup } from '@common/model/lookup';
import { UserSetting, UserSettingsService } from '@user-service/services/user-settings.service';
import { takeUntil, filter } from 'rxjs/operators';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationDialogComponent } from '@common/modules/confirmation-dialog/confirmation-dialog.component';
import { isNullOrUndefined } from '@swimlane/ngx-datatable';

@Component({
	selector: 'app-user-settings-selector',
	templateUrl: './user-settings-selector.component.html',
	styleUrls: ['./user-settings-selector.component.scss']
})
export class UserSettingsSelectorComponent extends BaseComponent implements OnInit, OnChanges {

	@Input() key: any;
	@Input() lookup: Lookup;
	@Input() autoSelectUserSettings: boolean;
	@Input() editingFilter: boolean = false;
	@Output() onSettingSelected = new EventEmitter<Lookup>();

	settings: any;
	availableUserSettings: UserSetting<any>[] = [];
	currentUserSetting: UserSetting<any>;

	public get anyAvailableSetting(): boolean {
		return this.availableUserSettings?.length > 0 ?? false;
	}

	public get settingOptions(): UserSetting<any>[] {
		if (this.availableUserSettings == null || this.availableUserSettings?.length == 0 || this.currentUserSetting == null) return [];
		
		return this.availableUserSettings.filter(s => s.id != this.currentUserSetting.id);
	}

	constructor(
		private userSettingsService: UserSettingsService,
		private dialog: MatDialog,
		private language: TranslateService,
	) { super(); }

	ngOnInit() {

		this.userSettingsService.getUserSettingUpdatedObservable().pipe(takeUntil(this._destroyed)).subscribe(key => {
			if (key === this.key.key) { this.getSettings(); }
		});

		this.getSettings();
	}

	ngOnChanges(changes: SimpleChanges) {
		if (changes['autoSelectUserSettings']) {
			if (changes['autoSelectUserSettings'].currentValue) {
				if (this.currentUserSetting == null && this.settings != null) {
					this.currentUserSetting = this.settings.defaultSetting;
					this.onSettingSelected.emit(this.currentUserSetting ? this.currentUserSetting.value : null);
				}
			}
		}
		if (changes['lookup'] && this.currentUserSetting != null) {
			this.currentUserSetting.value = this.lookup;
		}
	}

	private getSettings() {
		this.userSettingsService.get(this.key).pipe(takeUntil(this._destroyed)).subscribe(s => {
			if (s != null) {
				const settings = JSON.parse(JSON.stringify(s));
				this.settings = settings;
				this.availableUserSettings = settings.settings;
				if (this.autoSelectUserSettings) {
					this.currentUserSetting = settings.defaultSetting;
				} else {
					if (this.currentUserSetting) {
						const filterIndex = this.availableUserSettings.findIndex(x => x.name === this.currentUserSetting.name);
						this.currentUserSetting = this.availableUserSettings[filterIndex];
					} else {
						this.currentUserSetting = settings.defaultSetting;
					}
				}
			}
			if (this.autoSelectUserSettings) { this.onSettingSelected.emit(s != null ? (this.currentUserSetting ? this.currentUserSetting.value : null) : null); }
		});
	}

	getSettingName(setting: UserSetting<any>) {
		return !isNullOrUndefined(setting.name) ? setting.name : 'Default';
	}

	settingSelected(userSetting: UserSetting<any>) {
		const setting = this.availableUserSettings.find(x => x.id === userSetting.id);
		if (setting === null) { return; }

		this.currentUserSetting = userSetting;
		//Persist the active user setting
		this.onSettingSelected.emit(setting.value);
		this.userSettingsService.set(setting, true, this.key);
	}

	settingDeleted() {
		const value = this.currentUserSetting.id;
		if (value) {
			const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
				maxWidth: '400px',
				restoreFocus: false,
				data: {
					message: this.language.instant('COMMONS.CONFIRMATION-DIALOG.DELETE-USER-SETTING-PROFILE'),
					confirmButton: this.language.instant('COMMONS.CONFIRMATION-DIALOG.ACTIONS.CONFIRMATION'),
					cancelButton: this.language.instant('COMMONS.CONFIRMATION-DIALOG.ACTIONS.CANCELATION')
				}
			});
			dialogRef.afterClosed().pipe(takeUntil(this._destroyed)).subscribe(result => {
				if (result) {
					this.userSettingsService.remove(this.currentUserSetting.id, this.key).pipe(takeUntil(this._destroyed)).subscribe((value) => {
						// this.currentUserSetting = value?.length > 0 ? value[0].settings[0] : null;
						let foo =true;
					});
				}
			});
		}
		/* this.userSettingsService.remove(this.currentUserSetting.id, this.key); */
	}

	saveFilter() {
		const saveDialogRef = this.dialog.open(FilterNameDialogComponent, {
			maxWidth: '600px',
			maxHeight: '400px',
			restoreFocus: false,
			data: { name: this.currentUserSetting ? this.currentUserSetting.name : '' },
			disableClose: false
		});

		saveDialogRef.afterClosed().subscribe(result => {
			if (result) { this.createNewFilter(result); }
		});
	}

	updateFilter() {
		this.currentUserSetting.value = this.lookup;
		this.persistLookupChangesManually(this.currentUserSetting, true);
	}

	private persistLookupChangesManually(setting: UserSetting<any>, isDefault: boolean) {
		this.userSettingsService.set(setting, isDefault, this.key);
	}

	private createNewFilter(name: string) {
		let temp: any;
		temp = this.currentUserSetting ? JSON.parse(JSON.stringify(this.currentUserSetting)) : {};
		temp.value = this.lookup;
		temp.id = null;
		temp.hash = null;
		temp.name = name;
		temp.isDefault = true;
		temp.createdAt = null;
		temp.updatedAt = null;
		temp.userId = null;
		
		this.currentUserSetting = temp;
		
		this.persistLookupChangesManually(temp, temp.isDefault);
	}

	compareFn(c1: UserSetting<any>, c2: UserSetting<any>): boolean {
		return c1 && c2 ? c1.id === c2.id : c1 === c2;
	}
}
