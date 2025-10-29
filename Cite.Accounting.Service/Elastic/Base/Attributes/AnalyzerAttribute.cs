using System;

namespace Cite.Accounting.Service.Elastic.Base.Attributes
{
	[AttributeUsage(AttributeTargets.Property, Inherited = false)]
	public class AnalyzerAttribute : Attribute
	{
		public string Analyzer { get; set; }

		public AnalyzerAttribute(string analyzer)
		{
			this.Analyzer = analyzer;
		}
	}
}
