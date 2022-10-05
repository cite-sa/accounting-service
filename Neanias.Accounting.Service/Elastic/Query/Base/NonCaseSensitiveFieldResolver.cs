using System;

namespace Neanias.Accounting.Service.Elastic.Query
{
	public class NonCaseSensitiveFieldResolver : Cite.Tools.Data.Query.FieldResolver
	{
		public NonCaseSensitiveFieldResolver(String field)
			: base(field)
		{
			this.Field = field.ToLower();
		}
	}
}
