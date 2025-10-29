using Elastic.Clients.Elasticsearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Cite.Accounting.Service.Elastic.Base.Query
{
	//Help from https://github.com/elastic/elasticsearch-net/blob/main/src/Elastic.Clients.Elasticsearch/_Shared/Core/Infer/Field/FieldExpressionVisitor.cs
	public class FieldInfoResolver
	{
		private readonly FieldInfoExpressionResolver _fieldInfoExpressionResolver;
		private readonly Field _field;
		private List<Attribute> _targetFieldAttributes;

		public FieldInfoResolver(Field field)
		{
			this._field = field;
			this._targetFieldAttributes = null;
			this._fieldInfoExpressionResolver = new FieldInfoExpressionResolver();
		}

		public List<Attribute> GetTargetFieldAttributes()
		{
			if (this._targetFieldAttributes == null) this.ResolveTargetFieldAttributes();
			return this._targetFieldAttributes.ToList();
		}

		public T GetTargetFieldAttribute<T>() where T : Attribute
		{
			if (this._targetFieldAttributes == null) this.ResolveTargetFieldAttributes();
			return (T)this.GetTargetFieldAttributes()?.Find(x => x is T);
		}

		private void ResolveTargetFieldAttributes()
		{
			this._targetFieldAttributes = new List<Attribute>();

			if (this._field != null)
			{
				if (this._field.Property != null)
				{
					this._targetFieldAttributes = Attribute.GetCustomAttributes(this._field.Property)?.ToList() ?? new List<Attribute>();
				}
				else if (this._field.Expression != null)
				{
					Stack<MemberInfo> stack = this._fieldInfoExpressionResolver.Resolve(this._field.Expression);
					this._targetFieldAttributes = stack != null && stack.Any() ? Attribute.GetCustomAttributes(stack.Last())?.ToList() ?? new List<Attribute>() : new List<Attribute>();
				}
				else
				{
					this._targetFieldAttributes = new List<Attribute>();
				}
			}
		}


		internal class FieldInfoExpressionResolver : ExpressionVisitor
		{
			private readonly Stack<MemberInfo> _stack = new();

			internal Stack<MemberInfo> Resolve(Expression expression)
			{
				if (!this._stack.Any()) Visit(expression);

				return new Stack<MemberInfo>(this._stack);
			}


			protected override Expression VisitMember(MemberExpression node)
			{
				if (_stack == null)
					return base.VisitMember(node);

				_stack.Push(node.Member);
				return base.VisitMember(node);
			}

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				if (IsLinqOperator(node.Method))
				{
					for (var i = 1; i < node.Arguments.Count; i++)
						Visit(node.Arguments[i]);
					Visit(node.Arguments[0]);
					return node;
				}
				return base.VisitMethodCall(node);
			}

			private static bool IsLinqOperator(MethodInfo methodInfo)
			{
				if (methodInfo.DeclaringType != typeof(Queryable) && methodInfo.DeclaringType != typeof(Enumerable))
					return false;

				return methodInfo.GetCustomAttribute<ExtensionAttribute>() != null;
			}

		}
	}
}
