using Cite.Tools.FieldSet;
using Cite.Tools.Data.Query;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Model;

namespace Neanias.Accounting.Service.Query
{
	public interface IQueryingService
	{
		Task<List<D>> CollectAsync<D>(Query<D> query) where D : class;
		Task<List<M>> CollectAsync<D, M>(Query<D> query, Builder<M, D> builder, IFieldSet builderProjection) where D : class;
		Task<List<M>> CollectAsAsync<D, M>(Query<D> query, IFieldSet queryProjection, Builder<M, D> builder, IFieldSet builderProjection) where D : class;
		Task<List<M>> CollectAsAsync<D, M>(Query<D> query, Builder<M, D> builder, IFieldSet projection) where D : class;
		Task<List<R>> CollectAsAsync<D, R>(Query<D> query, Expression<Func<D, R>> projection) where D : class;
		Task<List<M>> CollectAsAsync<D, R, M>(Query<D> query, Expression<Func<D, R>> projection, Builder<M, R> builder, IFieldSet builderProjection) where R : class where D : class;

		Task<D> FirstAsync<D>(Query<D> query) where D : class;
		Task<M> FirstAsync<D, M>(Query<D> query, Builder<M, D> builder, IFieldSet builderProjection) where D : class;
		Task<M> FirstAsAsync<D, M>(Query<D> query, IFieldSet queryProjection, Builder<M, D> builder, IFieldSet builderProjection) where D : class;
		Task<M> FirstAsAsync<D, M>(Query<D> query, Builder<M, D> builder, IFieldSet projection) where D : class;
		Task<R> FirstAsAsync<D, R>(Query<D> query, Expression<Func<D, R>> projection) where D : class;
		Task<M> FirstAsAsync<D, R, M>(Query<D> query, Expression<Func<D, R>> projection, Builder<M, R> builder, IFieldSet builderProjection) where R : class where D : class;

		Task<int> CountAsync<D>(Query<D> query) where D : class;
	}
}
