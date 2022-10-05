using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Common.Enum;
using Neanias.Accounting.Service.Data;
using Neanias.Accounting.Service.Data.Context;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Query;
using Cite.Tools.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neanias.Accounting.Service.Query
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class QueueInboxQuery : Query<QueueInbox>
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
		[JsonProperty, LogRename("applicationIds")]
		private List<String> _applicationIds { get; set; }
		[JsonProperty, LogRename("isActive")]
		private List<IsActive> _isActive { get; set; }
		[JsonProperty, LogRename("status")]
		private List<QueueInboxStatus> _status { get; set; }
		[JsonProperty, LogRename("retryThreshold")]
		private int? _retryThreshold { get; set; }
		[JsonProperty, LogRename("createdAfter")]
		private DateTime? _createdAfter { get; set; }

		public QueueInboxQuery(AppDbContext dbContext)
		{
			this._dbContext = dbContext;
		}

		private readonly AppDbContext _dbContext;

		public QueueInboxQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public QueueInboxQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public QueueInboxQuery ExcludedIds(IEnumerable<Guid> excludedIds) { this._excludedIds = this.ToList(excludedIds); return this; }
		public QueueInboxQuery ExcludedIds(Guid excludedId) { this._excludedIds = this.ToList(excludedId.AsArray()); return this; }
		public QueueInboxQuery IsActive(IEnumerable<IsActive> isActive) { this._isActive = this.ToList(isActive); return this; }
		public QueueInboxQuery IsActive(IsActive isActive) { this._isActive = this.ToList(isActive.AsArray()); return this; }
		public QueueInboxQuery Status(IEnumerable<QueueInboxStatus> status) { this._status = this.ToList(status); return this; }
		public QueueInboxQuery Status(QueueInboxStatus status) { this._status = this.ToList(status.AsArray()); return this; }
		public QueueInboxQuery Queues(IEnumerable<String> queues) { this._queues = this.ToList(queues); return this; }
		public QueueInboxQuery Queues(String queue) { this._queues = this.ToList(queue.AsArray()); return this; }
		public QueueInboxQuery Exchanges(IEnumerable<String> exchanges) { this._exchanges = this.ToList(exchanges); return this; }
		public QueueInboxQuery Exchanges(String exchange) { this._exchanges = this.ToList(exchange.AsArray()); return this; }
		public QueueInboxQuery Routes(IEnumerable<String> routes) { this._routes = this.ToList(routes); return this; }
		public QueueInboxQuery Routes(String route) { this._routes = this.ToList(route.AsArray()); return this; }
		public QueueInboxQuery ApplicationIds(IEnumerable<String> applicationIds) { this._applicationIds = this.ToList(applicationIds); return this; }
		public QueueInboxQuery ApplicationIds(String applicationId) { this._applicationIds = this.ToList(applicationId.AsArray()); return this; }
		public QueueInboxQuery CreatedAfter(DateTime? createdAfter) { this._createdAfter = createdAfter; return this; }
		public QueueInboxQuery RetryThreshold(int? retryThreshold) { this._retryThreshold = retryThreshold; return this; }
		public QueueInboxQuery EnableTracking() { base.NoTracking = false; return this; }
		public QueueInboxQuery DisableTracking() { base.NoTracking = true; return this; }
		public QueueInboxQuery Ordering(Ordering ordering) { this.Order = ordering; return this; }
		public QueueInboxQuery Paging(Paging paging) { this.Page = paging; return this; }
		public QueueInboxQuery AsDistinct() { base.Distinct = true; return this; }
		public QueueInboxQuery AsNotDistinct() { base.Distinct = false; return this; }

		protected override IQueryable<QueueInbox> ApplyFilters(IQueryable<QueueInbox> query)
		{
			if (this._ids != null) query = query.Where(x => this._ids.Contains(x.Id));
			if (this._excludedIds != null) query = query.Where(x => !this._excludedIds.Contains(x.Id));
			if (this._isActive != null) query = query.Where(x => this._isActive.Contains(x.IsActive));
			if (this._exchanges != null) query = query.Where(x => this._exchanges.Contains(x.Exchange));
			if (this._queues != null) query = query.Where(x => this._queues.Contains(x.Queue));
			if (this._routes != null) query = query.Where(x => this._routes.Contains(x.Route));
			if (this._applicationIds != null) query = query.Where(x => this._applicationIds.Contains(x.ApplicationId));
			if (this._status != null) query = query.Where(x => this._status.Contains(x.Status));
			if (this._retryThreshold.HasValue) query = query.Where(x => !x.RetryCount.HasValue || (x.RetryCount.HasValue && x.RetryCount <= this._retryThreshold));
			if (this._createdAfter.HasValue) query = query.Where(x => x.CreatedAt > this._createdAfter.Value);
			return query;
		}

		protected override List<string> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Data.QueueInbox.Id))) projectionFields.Add(nameof(Data.QueueInbox.Id));
				else if (item.Match(nameof(Data.QueueInbox.IsActive))) projectionFields.Add(nameof(Data.QueueInbox.IsActive));
				else if (item.Match(nameof(Data.QueueInbox.Exchange))) projectionFields.Add(nameof(Data.QueueInbox.Exchange));
				else if (item.Match(nameof(Data.QueueInbox.Queue))) projectionFields.Add(nameof(Data.QueueInbox.Queue));
				else if (item.Match(nameof(Data.QueueInbox.Route))) projectionFields.Add(nameof(Data.QueueInbox.Route));
				else if (item.Match(nameof(Data.QueueInbox.ApplicationId))) projectionFields.Add(nameof(Data.QueueInbox.ApplicationId));
				else if (item.Match(nameof(Data.QueueInbox.MessageId))) projectionFields.Add(nameof(Data.QueueInbox.MessageId));
				else if (item.Match(nameof(Data.QueueInbox.RetryCount))) projectionFields.Add(nameof(Data.QueueInbox.RetryCount));
				else if (item.Match(nameof(Data.QueueInbox.Status))) projectionFields.Add(nameof(Data.QueueInbox.Status));
				else if (item.Match(nameof(Data.QueueInbox.CreatedAt))) projectionFields.Add(nameof(Data.QueueInbox.CreatedAt));
				else if (item.Match(nameof(Data.QueueInbox.UpdatedAt))) projectionFields.Add(nameof(Data.QueueInbox.UpdatedAt));
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._isActive) || this.IsEmpty(this._status) || this.IsEmpty(this._queues)
				|| this.IsEmpty(this._exchanges) || this.IsEmpty(this._routes) || this.IsEmpty(this._applicationIds);
		}

		protected override IOrderedQueryable<QueueInbox> OrderClause(IQueryable<QueueInbox> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<Data.QueueInbox> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<Data.QueueInbox>;

			if (item.Match(nameof(Data.QueueInbox.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Id);
			else if (item.Match(nameof(Data.QueueInbox.IsActive))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.IsActive);
			else if (item.Match(nameof(Data.QueueInbox.Exchange))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Exchange);
			else if (item.Match(nameof(Data.QueueInbox.Queue))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Queue);
			else if (item.Match(nameof(Data.QueueInbox.Route))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Route);
			else if (item.Match(nameof(Data.QueueInbox.ApplicationId))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.ApplicationId);
			else if (item.Match(nameof(Data.QueueInbox.Status))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Status);
			else if (item.Match(nameof(Data.QueueInbox.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(Data.QueueInbox.UpdatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UpdatedAt);
			else return null;

			return orderedQuery;
		}

		protected override IQueryable<QueueInbox> Queryable()
		{
			IQueryable<Data.QueueInbox> query = this._dbContext.QueueInboxes.AsQueryable();
			return query;
		}
	}
}
