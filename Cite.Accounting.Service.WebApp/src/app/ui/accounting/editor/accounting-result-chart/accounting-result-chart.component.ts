import { DatePipe } from '@angular/common';
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
import { isNullOrUndefined } from '@swimlane/ngx-datatable';
import { EChartsOption } from 'echarts';

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
		protected queryParamsService: QueryParamsService,
		private datePipe: DatePipe,
	) {
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

	private getTimestamp(entry: AccountingAggregateResultItem): string {
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
		return labels.join('_');
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

	private createChart(aggregateType: AggregateType): Chart {

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
		let datasetMap = new Map<string, any>();
		let series = [];
		for (let i = 0; i < this.data.length; i++) {
			const item = this.data[i];
			const code = this.getCode(item);
			let dataset = datasetMap.get(code);
			if (isNullOrUndefined(dataset)) {
				dataset = {
					name: this.getLabel(item),
					type: 'line',
					data: [],
					animationDelay: idx => idx * 10,
					// backgroundColor: [this.randomColor()],
					// borderColor: this.randomColor(),
					// fill: false,
				}
				datasetMap.set(code, dataset);
				series.push(dataset);
			}
			dataset.data.push(
				[
					this.datePipe.transform(this.getTimestamp(item), 'dd/MM hh:mm a'),
					this.getValue(item, aggregateType)
				]);
		}
		// if (datasetMap.size > 100) chartData.options.legend.display = false;

		let dataZoom = [];
		dataZoom.push({
			type: 'inside',
		});
		dataZoom.push({
			type: 'slider'
		});

		// const axisLabel = config?.xAxis?.axisLabel ? {
		// 	width: config.xAxis.axisLabel.width,
		// 	rotate: config.xAxis.axisLabel.rotate ?? 0,
		// 	overflow: 'truncate'
		// } : {};

		const chartData: Chart = {
			aggregateType: aggregateType,
			options: {
				dataZoom: dataZoom,
				legend: {
					orient: 'vertical',
					right: 10,
					top: 30,
					backgroundColor: '#fff',
					textStyle: {
						width: 120,
						overflow: 'break'
					},
					type: "scroll",
					bottom: 20,
					data: this.data.map(x => this.getLabel(x)),
					//align: 'left',
				},
				tooltip: {},
				xAxis: {
					// type: 'time',
					// axisLabel: {
					// 	formatter: (function (value) {
					// 		let label;
					// 		if (value.getMinutes() < 10) {
					// 			label = value.getHours() + ":0" + value.getMinutes();
					// 		}
					// 		else {
					// 			label = value.getHours() + ":" + value.getMinutes();
					// 		}
					// 		return label;
					// 	})
					// },
					// name: config?.xAxis?.name,
					nameGap: 40,
					// boundaryGap: true,
					data: [
						...new Set(
							this.data
								.filter(x => x?.group?.timeStamp != null)
								.map(x => this.datePipe.transform(x.group.timeStamp, 'dd/MM hh:mm a'))
						)
					],					
					silent: false,
					splitLine: {
						show: false,
					},
					// axisLabel,
					// data: this.data.map(x => this.getTimestamp(x).toString())
				},
				yAxis: {},
				series: series,
				animationEasing: 'elasticOut',
				animationDelayUpdate: idx => idx * 5,
			// type: 'line',
			// data: {
			// 	datasets: [],
			// 	labels: []
			// },
			// options: {
			// 	responsive: true,
			// 	maintainAspectRatio: true,
			// 	legend: {
			// 		display: true,
			// 		position: 'bottom',
			// 	},
			// 	scales: {
			// 		xAxes: [{
			// 			type: 'time',
			// 			display: true,
			// 			autoSkip: true,
			// 			maxTicksLimit: 20,
			// 			time: {
			// 				unit: unit
			// 			},
			// 			ticks: {
			// 				major: {
			// 					fontStyle: 'bold',
			// 					fontColor: '#FF0000'
			// 				}
			// 			}
			// 		}],
			// 		yAxes: [{
			// 			display: true
			// 		}]
			// 	}
			// }
			} as EChartsOption,
		};

		return chartData;

		// let datasetMap = new Map<string, any>();
		// for (let i = 0; i < this.data.length; i++) {
		// 	const item = this.data[i];
		// 	const code = this.getCode(item);
		// 	let dataset = datasetMap.get(code);
		// 	if (isNullOrUndefined(dataset)) {
		// 		dataset = {
		// 			name: this.getLabel(item),
		// 			type: 'line',
		// 			data: [],
		// 			animationDelay: idx => idx * 10,
		// 			// backgroundColor: [this.randomColor()],
		// 			// borderColor: this.randomColor(),
		// 			// fill: false,
		// 		}
		// 		datasetMap.set(code, dataset);
		// 		chartData.options.series.push(dataset);
		// 	}
		// 	dataset.data.push({
		// 		x: this.getTimestamp(item),
		// 		y: this.getValue(item, aggregateType)
		// 	})
		// }
		// if (datasetMap.size > 100) chartData.options.legend.display = false;
		// return chartData;
	}

	randomColor(): string {
		return '#' + (0x1000000 + (Math.random()) * 0xffffff).toString(16).substr(1, 6);
	}
}

export class Chart {
	options: EChartsOption;
	aggregateType: AggregateType;
}
