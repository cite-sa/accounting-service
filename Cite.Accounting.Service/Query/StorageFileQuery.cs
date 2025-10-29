using Cite.Accounting.Service.Data;
using Cite.Accounting.Service.Data.Context;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Query;
using Cite.Tools.Logging;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Query
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class StorageFileQuery : AsyncQuery<StorageFile>
	{
		[JsonProperty, LogRename("ids")]
		private List<Guid> _ids { get; set; }
		[JsonProperty, LogRename("excludedIds")]
		private List<Guid> _excludedIds { get; set; }
		[JsonProperty, LogRename("canPurge")]
		private Boolean? _canPurge { get; set; }
		[JsonProperty, LogRename("isPurged")]
		private Boolean? _isPurged { get; set; }
		[JsonProperty, LogRename("whatYouKnowAboutMeQuery")]
		private WhatYouKnowAboutMeQuery _whatYouKnowAboutMeQuery { get; set; }
		[JsonProperty, LogRename("createdAfter")]
		private DateTime? _createdAfter { get; set; }

		public StorageFileQuery(TenantDbContext dbContext)
		{
			this._dbContext = dbContext;
		}

		private readonly TenantDbContext _dbContext;

		public StorageFileQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public StorageFileQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public StorageFileQuery ExcludedIds(IEnumerable<Guid> excludedIds) { this._excludedIds = this.ToList(excludedIds); return this; }
		public StorageFileQuery ExcludedIds(Guid excludedId) { this._excludedIds = this.ToList(excludedId.AsArray()); return this; }
		public StorageFileQuery CanPurge(Boolean? canPurge) { this._canPurge = canPurge; return this; }
		public StorageFileQuery IsPurged(Boolean? isPurged) { this._isPurged = isPurged; return this; }
		public StorageFileQuery WhatYouKnowAboutMeSubQuery(WhatYouKnowAboutMeQuery subquery) { this._whatYouKnowAboutMeQuery = subquery; return this; }
		public StorageFileQuery CreatedAfter(DateTime? createdAfter) { this._createdAfter = createdAfter; return this; }
		public StorageFileQuery EnableTracking() { base.NoTracking = false; return this; }
		public StorageFileQuery DisableTracking() { base.NoTracking = true; return this; }
		public StorageFileQuery Ordering(Ordering ordering) { this.Order = ordering; return this; }
		public StorageFileQuery AsDistinct() { base.Distinct = true; return this; }
		public StorageFileQuery AsNotDistinct() { base.Distinct = false; return this; }

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._ids) || this.IsEmpty(this._excludedIds) || this.IsFalseQuery(this._whatYouKnowAboutMeQuery);
		}

		public async Task<Data.StorageFile> Find(Guid id, Boolean tracked = true)
		{
			if (tracked) return await this._dbContext.StorageFiles.FindAsync(id);
			else return await this._dbContext.StorageFiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		}

		protected override IQueryable<StorageFile> Queryable()
		{
			IQueryable<StorageFile> query = this._dbContext.StorageFiles.AsQueryable();
			return query;
		}

		protected override async Task<IQueryable<StorageFile>> ApplyFiltersAsync(IQueryable<StorageFile> query)
		{
			if (this._ids != null) query = query.Where(x => this._ids.Contains(x.Id));
			if (this._excludedIds != null) query = query.Where(x => !this._excludedIds.Contains(x.Id));
			if (this._canPurge.HasValue) query = query.Where(x => x.PurgeAt.HasValue && x.PurgeAt <= DateTime.UtcNow);
			if (this._isPurged.HasValue && this._isPurged.Value) query = query.Where(x => x.PurgedAt.HasValue);
			if (this._isPurged.HasValue && !this._isPurged.Value) query = query.Where(x => !x.PurgedAt.HasValue);
			if (this._whatYouKnowAboutMeQuery != null)
			{
				//TODO: Try out how this behaves because of the nulls
				IQueryable<Guid?> subQuery = (await this.BindSubQueryAsync(this._whatYouKnowAboutMeQuery, this._dbContext.WhatYouKnowAboutMes, y => y.StorageFileId)).Distinct();
				query = query.Where(x => subQuery.Contains(x.Id));
			}
			if (this._createdAfter.HasValue) query = query.Where(x => x.CreatedAt > this._createdAfter.Value);
			return query;
		}

		protected override IOrderedQueryable<StorageFile> OrderClause(IQueryable<StorageFile> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<StorageFile> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<StorageFile>;

			if (item.Match(nameof(Model.StorageFile.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Id);
			else if (item.Match(nameof(Model.StorageFile.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(Model.StorageFile.PurgeAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.PurgeAt);
			else if (item.Match(nameof(Model.StorageFile.PurgedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.PurgedAt);
			else if (item.Match(nameof(Model.StorageFile.FileRef))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.FileRef);
			else if (item.Match(nameof(Model.StorageFile.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Name);
			else if (item.Match(nameof(Model.StorageFile.Extension))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Extension);
			else if (item.Match(nameof(Model.StorageFile.MimeType))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.MimeType);
			else return null;

			return orderedQuery;
		}

		protected override List<String> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Model.StorageFile.Id))) projectionFields.Add(nameof(StorageFile.Id));
				else if (item.Match(nameof(Model.StorageFile.CreatedAt))) projectionFields.Add(nameof(StorageFile.CreatedAt));
				else if (item.Match(nameof(Model.StorageFile.PurgeAt))) projectionFields.Add(nameof(StorageFile.PurgeAt));
				else if (item.Match(nameof(Model.StorageFile.PurgedAt))) projectionFields.Add(nameof(StorageFile.PurgedAt));
				else if (item.Match(nameof(Model.StorageFile.FileRef))) projectionFields.Add(nameof(StorageFile.FileRef));
				else if (item.Match(nameof(Model.StorageFile.Name))) projectionFields.Add(nameof(StorageFile.Name));
				else if (item.Match(nameof(Model.StorageFile.Extension))) projectionFields.Add(nameof(StorageFile.Extension));
				else if (item.Match(nameof(Model.StorageFile.MimeType))) projectionFields.Add(nameof(StorageFile.MimeType));
				else if (item.Match(nameof(Model.StorageFile.Hash))) projectionFields.Add(nameof(Data.StorageFile.CreatedAt));
				else if (item.Match(nameof(Model.StorageFile.FullName)))
				{
					projectionFields.Add(nameof(Data.StorageFile.Name));
					projectionFields.Add(nameof(Data.StorageFile.Extension));
				}
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}
	}
}
