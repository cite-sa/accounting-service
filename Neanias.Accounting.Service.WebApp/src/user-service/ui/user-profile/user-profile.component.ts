import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { AuthService } from '@app/core/services/ui/auth.service';
import { BaseComponent } from '@common/base/base.component';
import { takeUntil } from 'rxjs/operators';

@Component({
	selector: 'app-user-profile',
	templateUrl: './user-profile.component.html',
	styleUrls: ['./user-profile.component.scss']
})
export class UserProfileComponent extends BaseComponent implements OnInit {

	totpEnabled = false;
	selectedTabIndex = 0;
	requestedTab: string;

	constructor(
		public authService: AuthService,
		private route: ActivatedRoute,
	) {
		super();
	}

	ngOnInit(): void {
		this.route.paramMap.pipe(takeUntil(this._destroyed)).subscribe((paramMap: ParamMap) => {
			const tab = paramMap.get('tab');
			if (tab != null) { this.requestedTab = tab; }
		});
	}

	setRequestedTab() {
		if (this.requestedTab === 'profile') { this.selectedTabIndex = 0; }
		else if (this.requestedTab === 'personal') { this.selectedTabIndex = 1; }
	}
}
