import { Component, Input, OnInit, SimpleChanges } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { AggregateType } from '@app/core/enum/aggregate-type';
import { DateIntervalType } from '@app/core/enum/date-interval-type';
import { AppEnumUtils } from '@app/core/formatting/enum-utils.service';
import { AccountingAggregateResultItem } from '@app/core/model/accounting/accounting-aggregate-result.model';
import { AccountingService } from '@app/core/services/http/accounting.service';
import { ServiceActionService } from '@app/core/services/http/service-action.service';
import { ServiceResourceService } from '@app/core/services/http/service-resource.service';
import { ServiceService } from '@app/core/services/http/service.service';
import { AuthService } from '@app/core/services/ui/auth.service';
import { QueryParamsService } from '@app/core/services/ui/query-params.service';
import { AccountingEditorMode } from '@app/ui/accounting/editor/accounting-editor-mode';
import { AccountingEditorModel } from '@app/ui/accounting/editor/accounting-editor.model';
import { ChartData } from '@common/modules/chart-js/models/chart-data';
import { ChartOptions } from '@common/modules/chart-js/models/chart-options';
import { isNullOrUndefined } from '@swimlane/ngx-datatable';

@Component({
	selector: 'app-accounting-result-chart',
	templateUrl: './accounting-result-chart.component.html',
	styleUrls: ['./accounting-result-chart.component.scss']
})
export class AccountingResultChartComponent implements OnInit {
	@Input() data: AccountingAggregateResultItem[];
	@Input() editorModel: AccountingEditorModel;
	charts: Chart[];
	hasToManyDataForRender = false;
	constructor(
		protected dialog: MatDialog,
		public authService: AuthService,
		public enumUtils: AppEnumUtils,
		public serviceService: ServiceService,
		public serviceResourceService: ServiceResourceService,
		public serviceActionService: ServiceActionService,
		public accountingService: AccountingService,
		protected queryParamsService: QueryParamsService	) {
	}

	ngOnInit(): void {

	}

	ngOnChanges(changes: SimpleChanges): void {
		if (changes['editorModel']) {

		}
		if (changes['data']) {
			this.createCharts();
		}
	}

	private getValue(entry: AccountingAggregateResultItem, aggregateType: AggregateType): number {
		switch (aggregateType) {
			case AggregateType.Average: {
				return entry.average
			}
			case AggregateType.Min: {
				return entry.min
			}
			case AggregateType.Max: {
				return entry.max
			}
			case AggregateType.Sum: {
				return entry.sum
			}
		}

	}

	private getTimestamp(entry: AccountingAggregateResultItem): Date {
		return entry.group?.timeStamp;
	}

	private getLabel(entry: AccountingAggregateResultItem): string {
		const labels = [];
		if (this.editorModel.editorMode !== AccountingEditorMode.Service && entry.group?.service?.name) labels.push(entry.group?.service?.name);
		if (entry.group?.resource?.name) labels.push(entry.group?.resource?.name);
		if (entry.group?.action?.name) labels.push(entry.group?.action?.name);
		if (this.editorModel.editorMode !== AccountingEditorMode.User && entry.group?.user?.name) labels.push(entry.group?.user?.name);
		return labels.join(', ');
	}

	private getCode(entry: AccountingAggregateResultItem): string {
		const labels = [];
		if (entry.group?.service?.code) labels.push(entry.group?.service?.code);
		if (entry.group?.resource?.code) labels.push(entry.group?.resource?.code);
		if (entry.group?.action?.code) labels.push(entry.group?.action?.code);
		if (entry.group?.user?.subject) labels.push(entry.group?.user?.subject);
		return labels.join(', ');
	}

	private createCharts() {
		this.charts = [];
		this.hasToManyDataForRender = false;
		if (isNullOrUndefined(this.editorModel.dateInterval)) {
			return;
		}
		if (this.data.length > 1000) {
		 	this.hasToManyDataForRender = true;
		 	return;
		}
		for (let i = 0; i < this.editorModel.aggregateTypes.length; i++) {
			const aggregateType = this.editorModel.aggregateTypes[i];
			this.charts.push(this.createChart(aggregateType));
		}
	}

	private createChart(aggregateType: AggregateType) : Chart {

		let unit = 'day';
		switch (this.editorModel.dateInterval) {
			case DateIntervalType.Day: {
				unit = 'day';
				break;
			}
			case DateIntervalType.Hour: {
				unit = 'hour';
				break;
			}
			case DateIntervalType.Month: {
				unit = 'month';
				break;
			}
			case DateIntervalType.Day: {
				unit = 'day';
				break;
			}
		}
		const chartData: Chart = {
			aggregateType: aggregateType,
			type: 'line',
			data: {
				datasets: [],
				labels: []
			},
			options: {
				responsive: true,
				maintainAspectRatio: true,
				legend: {
						display: true,
						position: 'bottom',
					},
				scales: {
					xAxes: [{
						type: 'time',
						display: true,
						autoSkip: true,
        				maxTicksLimit: 20,
						time: {
							unit: unit
						},
						ticks: {
							major: {
								fontStyle: 'bold',
								fontColor: '#FF0000'
							}
						}
					}],
					yAxes: [{
						display: true
					}]
				}
			}
		};


		let datasetMap = new Map<string, any>();
		for (let i = 0; i < this.data.length; i++) {
			const item = this.data[i];
			const code = this.getCode(item);
			let dataset = datasetMap.get(code);
			if (isNullOrUndefined(dataset)) {
				dataset = {
					label: this.getLabel(item),
					backgroundColor: this.randomColor(),
					borderColor: this.randomColor(),
					fill: false,
					data: [],
				}
				datasetMap.set(code, dataset);
				chartData.data.datasets.push(dataset);
			}
			dataset.data.push({
				x: this.getTimestamp(item),
				y: this.getValue(item, aggregateType)
			})
		}
		if (datasetMap.size > 100) chartData.options.legend.display = false;
		return chartData;
	}

	randomColor(): string {
    	return '#'+(0x1000000+(Math.random())*0xffffff).toString(16).substr(1,6);
	}
}

export class Chart {
	type: string;
	data: ChartData;
	options: ChartOptions;
	aggregateType: AggregateType;
}
