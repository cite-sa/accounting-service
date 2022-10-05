using Neanias.Accounting.Service.Elastic.Attributes;
using Nest;
using System;
using System.Reflection;

namespace Neanias.Accounting.Service.Elastic.Client
{
	public static class ElasticFieldExtensions
	{
		public static PropertiesDescriptor<T> AddKeyword<T>(this PropertiesDescriptor<T> propertiesDescriptor) where T : class
		{
			return propertiesDescriptor.Keyword(tt => tt.Name(Elastic.Client.Constants.KeywordPropertyName));
		}

		public static PropertiesDescriptor<T> AddPhonetic<T>(this PropertiesDescriptor<T> propertiesDescriptor) where T : class
		{
			return propertiesDescriptor.Text(tt => tt
						 .Name(Elastic.Client.Constants.PhoneticPropertyName)
						 .Analyzer(Elastic.Client.Constants.AnalyzerName));
		}

		public static TextPropertyDescriptor<T> AddNameAnalyzer<T>(this TextPropertyDescriptor<T> propertiesDescriptor) where T : class
		{
			return propertiesDescriptor.Analyzer(Elastic.Client.Constants.AnalyzerName);
		}

		public static TextPropertyDescriptor<T> BuildMultiFieldWithKeyword<T>(this TextPropertyDescriptor<T> propertiesDescriptor) where T : class
		{
			return propertiesDescriptor.AddNameAnalyzer().Fields(ff => ff.AddKeyword());
		}

		public static TextPropertyDescriptor<T> BuildMultiFieldWithKeywordAndPhonetic<T>(this TextPropertyDescriptor<T> propertiesDescriptor) where T : class
		{
			return propertiesDescriptor.AddNameAnalyzer().Fields(ff => ff.AddKeyword().AddPhonetic());
		}

		public static TextPropertyDescriptor<T> BuildMultiFieldWithPhonetic<T>(this TextPropertyDescriptor<T> propertiesDescriptor) where T : class
		{
			return propertiesDescriptor.AddNameAnalyzer().Fields(ff => ff.AddPhonetic());
		}

		public static TextPropertyDescriptor<T> BuildTextField<T>(this TextPropertyDescriptor<T> propertiesDescriptor, PropertyInfo propertyInfo) where T : class
		{
			TextAttribute textAttribute = (TextAttribute)Attribute.GetCustomAttribute(propertyInfo, typeof(TextAttribute));
			return propertiesDescriptor.Analyzer(textAttribute.Analyzer).Fields(ff => ff.AddKeywordIfSet(propertyInfo).AddKeywordIfSet(propertyInfo));
		}

		public static PropertiesDescriptor<T> AddKeywordIfSet<T>(this PropertiesDescriptor<T> propertiesDescriptor, PropertyInfo propertyInfo) where T : class
		{
			if (!Attribute.IsDefined(propertyInfo, typeof(KeywordPropertyAttribute))) return propertiesDescriptor;
			return propertiesDescriptor.AddKeyword();
		}

		public static PropertiesDescriptor<T> AddPhoneticIfSet<T>(this PropertiesDescriptor<T> propertiesDescriptor, PropertyInfo propertyInfo) where T : class
		{
			if (!Attribute.IsDefined(propertyInfo, typeof(PhoneticPropertyAttribute))) return propertiesDescriptor;
			return propertiesDescriptor.AddPhonetic();
		}
	}

}
