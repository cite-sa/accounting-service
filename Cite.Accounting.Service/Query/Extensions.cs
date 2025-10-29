using Cite.Accounting.Service.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Cite.Accounting.Service.Query.Extensions
{
	public static class Extensions
	{
		public static bool Like(this DbFunctions _, string matchExpression, string pattern, DbProviderConfig.DbProvider provider)
		{
			switch (provider)
			{
				case DbProviderConfig.DbProvider.PostgreSQL: return EF.Functions.ILike(matchExpression, pattern);
				case DbProviderConfig.DbProvider.SQLServer:
				default: return EF.Functions.Like(matchExpression, pattern);
			}
		}

		public static bool ILike(this DbFunctions _, string matchExpression, string pattern, string escapeCharacter, DbProviderConfig.DbProvider provider)
		{
			switch (provider)
			{
				case DbProviderConfig.DbProvider.PostgreSQL: return EF.Functions.ILike(matchExpression, pattern, escapeCharacter);
				case DbProviderConfig.DbProvider.SQLServer:
				default: return EF.Functions.Like(matchExpression, pattern, escapeCharacter);
			}
		}

		public static IQueryable<T> Like<T>(this IQueryable<T> query, DbProviderConfig.DbProvider dbProvider, string like, string escapeCharacter, params Expression<Func<T, String>>[] valueFuncs)
		{
			if (valueFuncs == null || valueFuncs.Length < 1) return query;

			Expression<Func<T, bool>> finalLikeExpression = null;
			foreach (Expression<Func<T, String>> value in valueFuncs)
			{
				ParameterExpression entityParam = Expression.Parameter(typeof(T), "x");
				var propValue = value.Body.ReplaceParameter(value.Parameters[0], entityParam);

				Expression<Func<String, bool>> likeExpr = null;
				switch (dbProvider)
				{
					case DbProviderConfig.DbProvider.PostgreSQL: { likeExpr = (x) => EF.Functions.ILike(x, like, escapeCharacter); break; }
					case DbProviderConfig.DbProvider.SQLServer:
					default: { likeExpr = (x) => EF.Functions.Like(x, like, escapeCharacter); break; }
				}

				ParameterExpression propValueParam = likeExpr.Parameters[0];

				Expression expr = likeExpr.Body.ReplaceParameter(propValueParam, propValue);
				Expression<Func<T, bool>> likeExpression = Expression.Lambda<Func<T, bool>>(expr, entityParam);

				if (finalLikeExpression == null) finalLikeExpression = likeExpression;
				else finalLikeExpression = finalLikeExpression.Or(likeExpression);
			}
			query = query.Where(finalLikeExpression);
			return query;
		}

		public static IQueryable<T> Like<T>(this IQueryable<T> query, DbProviderConfig.DbProvider dbProvider, string like, params Expression<Func<T, String>>[] valueFuncs)
		{
			if (valueFuncs == null || valueFuncs.Length < 1) return query;

			Expression<Func<T, bool>> finalLikeExpression = null;
			foreach (Expression<Func<T, String>> value in valueFuncs)
			{
				ParameterExpression entityParam = Expression.Parameter(typeof(T), "x");
				var propValue = value.Body.ReplaceParameter(value.Parameters[0], entityParam);

				Expression<Func<String, bool>> likeExpr = null;
				switch (dbProvider)
				{
					case DbProviderConfig.DbProvider.PostgreSQL: { likeExpr = (x) => EF.Functions.ILike(x, like); break; }
					case DbProviderConfig.DbProvider.SQLServer:
					default: { likeExpr = (x) => EF.Functions.Like(x, like); break; }
				}

				ParameterExpression propValueParam = likeExpr.Parameters[0];

				Expression expr = likeExpr.Body.ReplaceParameter(propValueParam, propValue);
				Expression<Func<T, bool>> likeExpression = Expression.Lambda<Func<T, bool>>(expr, entityParam);

				if (finalLikeExpression == null) finalLikeExpression = likeExpression;
				else finalLikeExpression = finalLikeExpression.Or(likeExpression);
			}
			query = query.Where(finalLikeExpression);
			return query;
		}
	}



	public static class ExpressionUtils
	{
		public static Expression ReplaceParameter(this Expression expression, ParameterExpression source, Expression target)
		{
			return new ParameterReplacer { Source = source, Target = target }.Visit(expression);
		}

		public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1,
													  Expression<Func<T, bool>> expr2)
		{
			var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
			return Expression.Lambda<Func<T, bool>>
				  (Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
		}

		class ParameterReplacer : ExpressionVisitor
		{
			public ParameterExpression Source;
			public Expression Target;
			protected override Expression VisitParameter(ParameterExpression node)
				=> node == Source ? Target : base.VisitParameter(node);
		}
	}
}
