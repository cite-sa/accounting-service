using System;

namespace Neanias.Accounting.Service.Elastic.Query
{
	public class NonCaseSensitiveOrderingFieldResolver : NonCaseSensitiveFieldResolver
	{
		public NonCaseSensitiveOrderingFieldResolver(String field) : base(field)
		{
			this.IsAscending = true;

			if (!String.IsNullOrEmpty(this.Field))
			{
				this.IsAscending = !this.Field.StartsWith("-");
				if (this.Field.StartsWith("-") || this.Field.StartsWith("+")) this.Field = this.Field.Substring(1);
			}
		}

		public Boolean IsAscending { get; set; }
	}
}
