using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Common.Extentions;
using Cite.Accounting.Service.Elastic.Base.Query.Models;
using Cite.Accounting.Service.Service.Accounting;
using Cite.Accounting.Service.Service.ResetEntry;
using Cite.Tools.Data.Query;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using Prometheus;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ElasticSearch = Elastic.Clients.Elasticsearch;

namespace Cite.Accounting.Service.Service.Prometheus
{
	public class PrometheusService : IPrometheusService
	{
		private readonly QueryFactory _queryFactory;
		private readonly ILogger<PrometheusService> _logger;
		private readonly AccountingServiceConfig _accountingServiceConfig;
		private readonly PrometheusServiceConfig _prometheusServiceConfig;


		public PrometheusService(
			ILogger<PrometheusService> logger,
			QueryFactory queryFactory,
			AccountingServiceConfig accountingServiceConfig,
			PrometheusServiceConfig prometheusServiceConfig
			)
		{
			this._logger = logger;
			this._queryFactory = queryFactory;
			this._accountingServiceConfig = accountingServiceConfig;
			this._prometheusServiceConfig = prometheusServiceConfig;
		}

		public async void InitializeGauges()
		{
			if (!this._prometheusServiceConfig.Enable) return;

            AggregateResult result = await this.Calculate();

			if (result != null && result.Items != null)
			{

				foreach (AggregateResultItem resultItem in result.Items)
				{
					if (resultItem.Group != null && resultItem.Group.Items != null && resultItem.Group.Items.ContainsKey(nameof(Model.AccountingEntry.Action)))
					{
						try
						{
							string code = resultItem.Group.Items[nameof(Model.AccountingEntry.Action)];
							double value = 0;
							string tenantValue = null;

							if (resultItem.Values != null && resultItem.Values.ContainsKey(AggregateType.Sum))
								value = (double)resultItem.Values.GetValueOrDefault(AggregateType.Sum, null);

							if (resultItem.Group.Items.ContainsKey(nameof(Model.AccountingEntry.Resource)))
								tenantValue = resultItem.Group.Items[nameof(Model.AccountingEntry.Resource)];

							this.SetGaugeSafe(code, value, tenantValue != null ? "tenant" : null, tenantValue);

						}
						catch (SystemException ex)
						{
							this._logger.Error(ex);
						}

					}
				}
			}

		}

		private async Task<AggregateResult> Calculate()
		{
			this._logger.Debug(new MapLogEntry("calculate values for prometheus"));

			Elastic.Query.AccountingEntryQuery query = this._queryFactory.Query<Elastic.Query.AccountingEntryQuery>();

			query.ServiceIds(this._prometheusServiceConfig.AccountingServiceCode);
			query.Measures(Common.MeasureType.Unit);
			query.HasAction(true);
			query.HasResource(true);
			query.HasUser(true);

			AggregationMetric aggregationMetric = new AggregationMetric();
			aggregationMetric.AggregateTypes = new List<AggregateType> { AggregateType.Sum };
			aggregationMetric.AggregateField = nameof(Model.AccountingEntry.Value);

			GroupingField serviceGroupField = new GroupingField();
			serviceGroupField.Field = nameof(Model.AccountingEntry.Service);
			serviceGroupField.Order = ElasticSearch.SortOrder.Asc;

			GroupingField actionGroupField = new GroupingField();
			actionGroupField.Field = nameof(Model.AccountingEntry.Action);
			actionGroupField.Order = ElasticSearch.SortOrder.Asc;

			GroupingField resourceGroupField = new GroupingField();
			resourceGroupField.Field = nameof(Model.AccountingEntry.Resource);
			resourceGroupField.Order = ElasticSearch.SortOrder.Asc;

			aggregationMetric.GroupingFields = new List<GroupingField> { serviceGroupField, actionGroupField, resourceGroupField };

			AggregateResult allResults = new AggregateResult();
			do
			{
				AggregateResult result = await query.GroupByAsync(aggregationMetric, this._accountingServiceConfig.CalculateBatchSize, allResults.AfterKey?.ToDictionary(x => x.Key, x => x.Value.ToString()));

				allResults.Items.AddRange(result.Items);
				allResults.AfterKey = result.AfterKey;

			} while (allResults.AfterKey != null);

			return allResults;
		}

		private void SetGaugeSafe(string code, double value, string label = null, string tenantValue = null)
		{
			try
			{
				if (!this._prometheusServiceConfig.Enable) return;

				//reformat code for valid prometheus name
				code = Regex.Replace(code, @"[^a-zA-Z0-9_]", "_");

                var config = label != null
                        ? new GaugeConfiguration { LabelNames = new[] { label } }
                        : null;

                var gauge = Metrics.CreateGauge(code.ToLower(), null, config);
                if (tenantValue != null)
                    gauge.WithLabels(tenantValue).Set(value);
                else
                    gauge.Set(value);


            }
			catch (System.Exception e)
			{
				this._logger.Error($"Failed to set gauge value: {value}", e);
			}
		}

	}
}