using System;

namespace Cite.Accounting.Service.Elastic.Base.Attributes
{
	[AttributeUsage(AttributeTargets.Property, Inherited = false)]
	public class PhoneticSubFieldAttribute : Attribute
	{
		public string Analyzer { get; set; }
		public PhoneticSubFieldAttribute(string analyzer)
		{
			this.Analyzer = analyzer;
		}
	}
}
