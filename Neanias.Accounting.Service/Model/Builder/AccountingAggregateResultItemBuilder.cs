using Neanias.Accounting.Service.Convention;
using Neanias.Accounting.Service.Data.Context;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Query;
using Neanias.Accounting.Service.Common.Extentions;
using Neanias.Accounting.Service.Authorization;

namespace Neanias.Accounting.Service.Model
{
	public class AccountingAggregateResultItemBuilder : Builder<AccountingAggregateResultItem, Elastic.Query.AggregateResultItem>
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private readonly TenantDbContext _dbContext;
		private Authorization.AuthorizationFlags _authorize = Authorization.AuthorizationFlags.None;

		public AccountingAggregateResultItemBuilder(
			TenantDbContext dbContext,
			QueryFactory queryFactory,
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ILogger<AccountingAggregateResultItemBuilder> logger,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
		}
		public AccountingAggregateResultItemBuilder Authorize(Authorization.AuthorizationFlags authorize) { this._authorize = authorize; return this; }

		public override async Task<List<AccountingAggregateResultItem>> Build(IFieldSet fields, IEnumerable<Elastic.Query.AggregateResultItem> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Enumerable.Empty<AccountingAggregateResultItem>().ToList();

			IFieldSet groupFields = fields.ExtractPrefixed(this.AsPrefix(nameof(AccountingAggregateResultItem.Group)));
			Dictionary<Elastic.Query.AggregateResultGroup, AccountingAggregateResultGroup> groupMap = await this.CollectGroups(groupFields, datas);
			
			List<AccountingAggregateResultItem> models = new List<AccountingAggregateResultItem>();
			foreach (Elastic.Query.AggregateResultItem d in datas)
			{
				AccountingAggregateResultItem m = new AccountingAggregateResultItem();
				if (fields.HasField(this.AsIndexer(nameof(AccountingAggregateResultItem.Sum))) && d.Values != null && d.Values.ContainsKey(Elastic.Query.AggregateType.Sum)) m.Sum = d.Values[Elastic.Query.AggregateType.Sum];
				if (fields.HasField(this.AsIndexer(nameof(AccountingAggregateResultItem.Min))) && d.Values != null && d.Values.ContainsKey(Elastic.Query.AggregateType.Min)) m.Min = d.Values[Elastic.Query.AggregateType.Min];
				if (fields.HasField(this.AsIndexer(nameof(AccountingAggregateResultItem.Max))) && d.Values != null && d.Values.ContainsKey(Elastic.Query.AggregateType.Max)) m.Max = d.Values[Elastic.Query.AggregateType.Max];
				if (fields.HasField(this.AsIndexer(nameof(AccountingAggregateResultItem.Average))) && d.Values != null && d.Values.ContainsKey(Elastic.Query.AggregateType.Average)) m.Average = d.Values[Elastic.Query.AggregateType.Average];
				if (!groupFields.IsEmpty() && groupMap != null && groupMap.ContainsKey(d.Group)) m.Group = groupMap[d.Group];

				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		private async Task<Dictionary<Elastic.Query.AggregateResultGroup, AccountingAggregateResultGroup>> CollectGroups(IFieldSet fields, IEnumerable<Elastic.Query.AggregateResultItem> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(AccountingAggregateResultGroup));

			Dictionary<Elastic.Query.AggregateResultGroup, AccountingAggregateResultGroup> itemMap = new Dictionary<Elastic.Query.AggregateResultGroup, AccountingAggregateResultGroup>();
			IFieldSet clone = new FieldSet(fields.Fields);
			IEnumerable<AccountingAggregateResultGroup> items = await this._builderFactory.Builder<AccountingAggregateResultGroupBuilder>().Authorize(this._authorize).Build(clone, datas.Where(x=> x.Group != null).Select(x=> x.Group));

			foreach(AccountingAggregateResultGroup item in items)
			{
				itemMap[item.Source] = item;
				item.Source = null;
			}

			return itemMap;
		}
	}
}
