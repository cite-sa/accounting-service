using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Data;
using Cite.Accounting.Service.Data.Context;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Query;
using Cite.Tools.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Query
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class QueueOutboxQuery : AsyncQuery<QueueOutbox>
	{
		[JsonProperty, LogRename("ids")]
		private List<Guid> _ids { get; set; }
		[JsonProperty, LogRename("excludedIds")]
		private List<Guid> _excludedIds { get; set; }
		[JsonProperty, LogRename("exchanges")]
		private List<String> _exchanges { get; set; }
		[JsonProperty, LogRename("queues")]
		private List<String> _queues { get; set; }
		[JsonProperty, LogRename("routes")]
		private List<String> _routes { get; set; }
		[JsonProperty, LogRename("isActive")]
		private List<IsActive> _isActive { get; set; }
		[JsonProperty, LogRename("notifyStatus")]
		private List<QueueOutboxNotifyStatus> _notifyStatus { get; set; }
		[JsonProperty, LogRename("retryThreshold")]
		private int? _retryThreshold { get; set; }
		[JsonProperty, LogRename("confirmTimeout")]
		private int? _confirmTimeout { get; set; }
		[JsonProperty, LogRename("createdAfter")]
		private DateTime? _createdAfter { get; set; }

		public QueueOutboxQuery(AppDbContext dbContext)
		{
			this._dbContext = dbContext;
		}

		private readonly AppDbContext _dbContext;

		public QueueOutboxQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public QueueOutboxQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public QueueOutboxQuery ExcludedIds(IEnumerable<Guid> excludedIds) { this._excludedIds = this.ToList(excludedIds); return this; }
		public QueueOutboxQuery ExcludedIds(Guid excludedId) { this._excludedIds = this.ToList(excludedId.AsArray()); return this; }
		public QueueOutboxQuery IsActive(IEnumerable<IsActive> isActive) { this._isActive = this.ToList(isActive); return this; }
		public QueueOutboxQuery IsActive(IsActive isActive) { this._isActive = this.ToList(isActive.AsArray()); return this; }
		public QueueOutboxQuery Status(IEnumerable<QueueOutboxNotifyStatus> notifyStatus) { this._notifyStatus = this.ToList(notifyStatus); return this; }
		public QueueOutboxQuery Status(QueueOutboxNotifyStatus notifyStatus) { this._notifyStatus = this.ToList(notifyStatus.AsArray()); return this; }
		public QueueOutboxQuery Queues(IEnumerable<String> queues) { this._queues = this.ToList(queues); return this; }
		public QueueOutboxQuery Queues(String queue) { this._queues = this.ToList(queue.AsArray()); return this; }
		public QueueOutboxQuery Exchanges(IEnumerable<String> exchanges) { this._exchanges = this.ToList(exchanges); return this; }
		public QueueOutboxQuery Exchanges(String exchange) { this._exchanges = this.ToList(exchange.AsArray()); return this; }
		public QueueOutboxQuery Routes(IEnumerable<String> routes) { this._routes = this.ToList(routes); return this; }
		public QueueOutboxQuery Routes(String route) { this._routes = this.ToList(route.AsArray()); return this; }
		public QueueOutboxQuery RetryThreshold(int? retryThreshold) { this._retryThreshold = retryThreshold; return this; }
		public QueueOutboxQuery ConfirmTimeout(int confirmTimeout) { this._confirmTimeout = confirmTimeout; return this; }
		public QueueOutboxQuery CreatedAfter(DateTime? createdAfter) { this._createdAfter = createdAfter; return this; }
		public QueueOutboxQuery EnableTracking() { base.NoTracking = false; return this; }
		public QueueOutboxQuery DisableTracking() { base.NoTracking = true; return this; }
		public QueueOutboxQuery Ordering(Ordering ordering) { this.Order = ordering; return this; }
		public QueueOutboxQuery Paging(Paging paging) { this.Page = paging; return this; }
		public QueueOutboxQuery AsDistinct() { base.Distinct = true; return this; }
		public QueueOutboxQuery AsNotDistinct() { base.Distinct = false; return this; }

		protected override Task<IQueryable<QueueOutbox>> ApplyFiltersAsync(IQueryable<QueueOutbox> query)
		{
			if (this._ids != null) query = query.Where(x => this._ids.Contains(x.Id));
			if (this._excludedIds != null) query = query.Where(x => !this._excludedIds.Contains(x.Id));
			if (this._isActive != null) query = query.Where(x => this._isActive.Contains(x.IsActive));
			if (this._exchanges != null) query = query.Where(x => x.Exchange != null && this._exchanges.Contains(x.Exchange));
			if (this._routes != null) query = query.Where(x => x.Route != null && this._routes.Contains(x.Route));
			if (this._notifyStatus != null) query = query.Where(x => this._notifyStatus.Contains(x.NotifyStatus));
			if (this._confirmTimeout.HasValue) query = query.Where(x => !x.PublishedAt.HasValue || (x.PublishedAt.HasValue && !x.ConfirmedAt.HasValue && x.PublishedAt.Value.AddSeconds(this._confirmTimeout.Value) < DateTime.UtcNow));
			if (this._retryThreshold.HasValue) query = query.Where(x => !x.RetryCount.HasValue || (x.RetryCount.HasValue && x.RetryCount <= this._retryThreshold));
			if (this._createdAfter.HasValue) query = query.Where(x => x.CreatedAt > this._createdAfter.Value);
			return Task.FromResult(query);
		}

		protected override List<string> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Data.QueueOutbox.Id))) projectionFields.Add(nameof(Data.QueueOutbox.Id));
				else if (item.Match(nameof(Data.QueueOutbox.IsActive))) projectionFields.Add(nameof(Data.QueueOutbox.IsActive));
				else if (item.Match(nameof(Data.QueueOutbox.Exchange))) projectionFields.Add(nameof(Data.QueueOutbox.Exchange));
				else if (item.Match(nameof(Data.QueueOutbox.Route))) projectionFields.Add(nameof(Data.QueueOutbox.Route));
				else if (item.Match(nameof(Data.QueueOutbox.NotifyStatus))) projectionFields.Add(nameof(Data.QueueOutbox.NotifyStatus));
				else if (item.Match(nameof(Data.QueueOutbox.CreatedAt))) projectionFields.Add(nameof(Data.QueueOutbox.CreatedAt));
				else if (item.Match(nameof(Data.QueueOutbox.UpdatedAt))) projectionFields.Add(nameof(Data.QueueOutbox.UpdatedAt));
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._isActive) || this.IsEmpty(this._notifyStatus) || this.IsEmpty(this._queues)
				|| this.IsEmpty(this._exchanges) || this.IsEmpty(this._routes);
		}

		protected override IOrderedQueryable<QueueOutbox> OrderClause(IQueryable<QueueOutbox> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<Data.QueueOutbox> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<Data.QueueOutbox>;

			if (item.Match(nameof(Data.QueueOutbox.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Id);
			else if (item.Match(nameof(Data.QueueOutbox.IsActive))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.IsActive);
			else if (item.Match(nameof(Data.QueueOutbox.Exchange))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Exchange);
			else if (item.Match(nameof(Data.QueueOutbox.Route))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Route);
			else if (item.Match(nameof(Data.QueueOutbox.NotifyStatus))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.NotifyStatus);
			else if (item.Match(nameof(Data.QueueOutbox.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(Data.QueueOutbox.UpdatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UpdatedAt);
			else return null;

			return orderedQuery;
		}

		protected override IQueryable<QueueOutbox> Queryable()
		{
			IQueryable<Data.QueueOutbox> query = this._dbContext.QueueOutboxes.AsQueryable();
			return query;
		}
	}
}
