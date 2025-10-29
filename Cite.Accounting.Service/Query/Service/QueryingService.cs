using Cite.Accounting.Service.Model;
using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Query
{
	public class QueryingService : IQueryingService
	{
		public QueryingService(
			ILogger<QueryingService> logger)
		{
			this._logger = logger;
		}

		private readonly ILogger<QueryingService> _logger;

		public async Task<List<D>> CollectAsync<D>(Query<D> query)
			where D : class
		{
			List<D> datas = await query.CollectAsync();
			this._logger.Debug("collected {count} items", datas?.Count);
			this._logger.Trace(new DataLogEntry("query", query));
			return datas;
		}

		public async Task<List<M>> CollectAsync<D, M>(Query<D> query, Builder<M, D> builder, IFieldSet builderProjection)
			where D : class
		{
			List<D> datas = await query.CollectAsync();
			this._logger.Debug("collected {count} items", datas?.Count);
			this._logger.Trace(new DataLogEntry("query", query));
			List<M> models = await builder.Build(builderProjection, datas);
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		public async Task<List<R>> CollectAsAsync<D, R>(Query<D> query, Expression<Func<D, R>> projection)
			where D : class
		{
			List<R> dtos = await query.CollectAsync(projection);
			this._logger.Debug("collected {count} items", dtos?.Count);
			this._logger.Trace(new DataLogEntry("query", query));
			return dtos;
		}

		public async Task<List<M>> CollectAsAsync<D, R, M>(Query<D> query, Expression<Func<D, R>> projection, Builder<M, R> builder, IFieldSet builderProjection)
			where D : class
			where R : class
		{
			List<R> dtos = await query.CollectAsync(projection);
			this._logger.Debug("collected {count} items", dtos?.Count);
			this._logger.Trace(new DataLogEntry("query", query));
			List<M> models = await builder.Build(builderProjection, dtos);
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		public async Task<List<M>> CollectAsAsync<D, M>(Query<D> query, IFieldSet queryProjection, Builder<M, D> builder, IFieldSet builderProjection)
			where D : class
		{
			List<D> datas = await query.CollectAsync(queryProjection);
			this._logger.Debug("collected {count} items", datas?.Count);
			this._logger.Trace(new DataLogEntry("query", query));
			List<M> models = await builder.Build(builderProjection, datas);
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		public async Task<List<M>> CollectAsAsync<D, M>(Query<D> query, Builder<M, D> builder, IFieldSet projection)
			where D : class
		{
			List<D> datas = await query.CollectAsync(projection);
			this._logger.Debug("collected {count} items", datas?.Count);
			this._logger.Trace(new DataLogEntry("query", query));
			List<M> models = await builder.Build(projection, datas);
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		public async Task<int> CountAsync<D>(Query<D> query)
			where D : class
		{
			int count = await query.CountAsync();
			this._logger.Debug("counted {count} items", count);
			this._logger.Trace(new DataLogEntry("query", query));
			return count;
		}

		public async Task<D> FirstAsync<D>(Query<D> query)
			where D : class
		{
			D datas = await query.FirstAsync();
			this._logger.Debug("collected {count} items", (datas == null ? 0 : 1));
			this._logger.Trace(new DataLogEntry("query", query));
			return datas;
		}

		public async Task<M> FirstAsync<D, M>(Query<D> query, Builder<M, D> builder, IFieldSet builderProjection)
			where D : class
		{
			D datas = await query.FirstAsync();
			this._logger.Debug("collected {count} items", (datas == null ? 0 : 1));
			this._logger.Trace(new DataLogEntry("query", query));
			M models = await builder.Build(builderProjection, datas);
			this._logger.Debug("build {count} items", (models == null ? 0 : 1));
			return models;
		}

		public async Task<R> FirstAsAsync<D, R>(Query<D> query, Expression<Func<D, R>> projection)
			where D : class
		{
			R datas = await query.FirstAsync(projection);
			this._logger.Debug("collected {count} items", (datas == null ? 0 : 1));
			this._logger.Trace(new DataLogEntry("query", query));
			return datas;
		}

		public async Task<M> FirstAsAsync<D, R, M>(Query<D> query, Expression<Func<D, R>> projection, Builder<M, R> builder, IFieldSet builderProjection)
			where D : class
			where R : class
		{
			R datas = await query.FirstAsync(projection);
			this._logger.Debug("collected {count} items", (datas == null ? 0 : 1));
			this._logger.Trace(new DataLogEntry("query", query));
			M models = await builder.Build(builderProjection, datas);
			this._logger.Debug("build {count} items", (models == null ? 0 : 1));
			return models;
		}

		public async Task<M> FirstAsAsync<D, M>(Query<D> query, IFieldSet queryProjection, Builder<M, D> builder, IFieldSet builderProjection)
			where D : class
		{
			D datas = await query.FirstAsync(queryProjection);
			this._logger.Debug("collected {count} items", (datas == null ? 0 : 1));
			this._logger.Trace(new DataLogEntry("query", query));
			M models = await builder.Build(builderProjection, datas);
			this._logger.Debug("build {count} items", (models == null ? 0 : 1));
			return models;
		}

		public async Task<M> FirstAsAsync<D, M>(Query<D> query, Builder<M, D> builder, IFieldSet projection)
			where D : class
		{
			D datas = await query.FirstAsync(projection);
			this._logger.Debug("collected {count} items", (datas == null ? 0 : 1));
			this._logger.Trace(new DataLogEntry("query", query));
			M models = await builder.Build(projection, datas);
			this._logger.Debug("build {count} items", (models == null ? 0 : 1));
			return models;
		}
	}
}
