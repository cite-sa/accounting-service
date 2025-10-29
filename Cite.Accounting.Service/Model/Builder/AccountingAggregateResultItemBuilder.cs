using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Convention;
using Cite.Accounting.Service.Elastic.Base.Query.Models;
using Cite.Tools.Data.Builder;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Model
{
	public class AccountingAggregateResultItemBuilder : Builder<AccountingAggregateResultItem, AggregateResultItem>
	{
		private readonly BuilderFactory _builderFactory;
		private Authorization.AuthorizationFlags _authorize = Authorization.AuthorizationFlags.None;

		public AccountingAggregateResultItemBuilder(
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ILogger<AccountingAggregateResultItemBuilder> logger,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._builderFactory = builderFactory;
		}
		public AccountingAggregateResultItemBuilder Authorize(Authorization.AuthorizationFlags authorize) { this._authorize = authorize; return this; }

		public override async Task<List<AccountingAggregateResultItem>> Build(IFieldSet fields, IEnumerable<AggregateResultItem> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Enumerable.Empty<AccountingAggregateResultItem>().ToList();

			IFieldSet groupFields = fields.ExtractPrefixed(this.AsPrefix(nameof(AccountingAggregateResultItem.Group)));
			Dictionary<AggregateResultGroup, AccountingAggregateResultGroup> groupMap = await this.CollectGroups(groupFields, datas);

			List<AccountingAggregateResultItem> models = new List<AccountingAggregateResultItem>();
			foreach (AggregateResultItem d in datas ?? new List<AggregateResultItem>())
			{
				AccountingAggregateResultItem m = new AccountingAggregateResultItem();
				if (fields.HasField(this.AsIndexer(nameof(AccountingAggregateResultItem.Sum))) && d.Values != null && d.Values.ContainsKey(AggregateType.Sum)) m.Sum = d.Values[AggregateType.Sum];
				if (fields.HasField(this.AsIndexer(nameof(AccountingAggregateResultItem.Min))) && d.Values != null && d.Values.ContainsKey(AggregateType.Min)) m.Min = d.Values[AggregateType.Min];
				if (fields.HasField(this.AsIndexer(nameof(AccountingAggregateResultItem.Max))) && d.Values != null && d.Values.ContainsKey(AggregateType.Max)) m.Max = d.Values[AggregateType.Max];
				if (fields.HasField(this.AsIndexer(nameof(AccountingAggregateResultItem.Average))) && d.Values != null && d.Values.ContainsKey(AggregateType.Average)) m.Average = d.Values[AggregateType.Average];
				if (!groupFields.IsEmpty() && groupMap != null && groupMap.ContainsKey(d.Group)) m.Group = groupMap[d.Group];

				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		private async Task<Dictionary<AggregateResultGroup, AccountingAggregateResultGroup>> CollectGroups(IFieldSet fields, IEnumerable<AggregateResultItem> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(AccountingAggregateResultGroup));

			Dictionary<AggregateResultGroup, AccountingAggregateResultGroup> itemMap = new Dictionary<AggregateResultGroup, AccountingAggregateResultGroup>();
			IFieldSet clone = new FieldSet(fields.Fields);
			IEnumerable<AccountingAggregateResultGroup> items = await this._builderFactory.Builder<AccountingAggregateResultGroupBuilder>().Authorize(this._authorize).Build(clone, datas.Where(x => x.Group != null).Select(x => x.Group));

			foreach (AccountingAggregateResultGroup item in items)
			{
				itemMap[item.Source] = item;
				item.Source = null;
			}

			return itemMap;
		}
	}
}
