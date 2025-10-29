import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from "@angular/core";
import { FormControl } from "@angular/forms";
import { BaseComponent } from "@common/base/base.component";
import { Subject } from "rxjs";
import { debounceTime, shareReplay, startWith, takeUntil } from "rxjs/operators";
import { FilterService } from "../filter-service";

@Component({
  selector: 'app-expandable-search-field',
  templateUrl: 'expandable-search-field.component.html',
  styleUrls: ['expandable-search-field.component.scss']
})
export class ExpandableSearchFieldComponent extends BaseComponent implements OnInit, OnChanges {
  @Input() placeholder: string;
	@Input() value: string;
	@Input() disableTransform = false;
	@Output() valueChange = new EventEmitter<string>();
  
  valueInput: FormControl;

  private subject$ = new Subject<boolean>();

	protected a$ = this.subject$.asObservable().pipe(
		debounceTime(200),
		takeUntil(this._destroyed),
		shareReplay(),
		startWith(false)
	);

  constructor(private filterService: FilterService) {
    super();
  }

  ngOnInit(): void {
    this.valueInput = new FormControl(this.value ?? '');

    this._registerListener();
  }

  ngOnChanges(changes: SimpleChanges): void {
		if (changes['value']) {
      const value = this.filterService.reverseLikeTransformation(this.value)
      this.valueInput = new FormControl(value ?? '');

      this._registerListener();
    }
  }

  protected onOpen(){
		this.subject$.next(true);
	}
	
	protected onClose(){
		this.subject$.next(false);
	}

  private _registerListener(): void {
    this.valueInput.valueChanges.pipe(takeUntil(this._destroyed)).subscribe((value) => {
      if (value === '') { value = null }
      if (this.value !== value) {
        this.value = value;
        this.valueChange.emit(this.filterService.transformLike(value));
      }
    });
  }
}
