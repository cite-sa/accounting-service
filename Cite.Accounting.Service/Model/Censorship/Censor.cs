using Cite.Tools.Data.Censor;
using Cite.Tools.FieldSet;
using System;

namespace Cite.Accounting.Service.Model
{
	public class Censor : ICensor
	{
		protected Boolean IsEmpty(IFieldSet fields)
		{
			return fields == null || fields.IsEmpty();
		}
	}
}
