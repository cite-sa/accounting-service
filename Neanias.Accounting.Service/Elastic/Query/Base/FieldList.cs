using System;
using System.Collections.Generic;
using System.Linq;

namespace Neanias.Accounting.Service.Elastic.Query
{
	public class FieldList<T> 
	{
		public List<String> Fields { get; set; }

		public String Prefix { get; set; }

		public FieldList()
		{
			Prefix = String.Empty;
			Fields = new List<string>();
		}

		public FieldList(params string[] prefix)
		{
			Prefix = prefix == null || !prefix.Any()? String.Empty : String.Join(".", prefix.Select(x=> this.ToLowerFirstChar(x)));
			Fields = new List<string>();
		}

		public FieldList(List<String> fields, params string[] prefix)
		{
			Prefix = prefix == null || !prefix.Any() ? String.Empty : String.Join(".", prefix.Select(x => this.ToLowerFirstChar(x)));
			Fields = new List<string>();
			foreach (String field in fields) this.Add(field);
		}
		

		private string ToLowerFirstChar(string input)
		{
			string newString = input;
			if (!String.IsNullOrEmpty(newString) && Char.IsUpper(newString[0]))
				newString = Char.ToLower(newString[0]) + newString.Substring(1);
			return newString;
		}

		public FieldList<T> Add(String field)
		{
			this.Fields.Add(field);
			return this;
		}

		public String GetFieldWithPath(String field)
		{
			return String.IsNullOrWhiteSpace(this.Prefix) ? this.ToLowerFirstChar(field) : $"{this.Prefix}.{this.ToLowerFirstChar(field)}";
		}
	}

	public class FieldItem<T>
	{
		public String Field { get; set; }

		public String Prefix { get; set; }

		public FieldItem()
		{
			Prefix = String.Empty;
			Field = String.Empty;
		}

		public FieldItem(string field)
		{
			Field = field;
		}

		public FieldItem(string[] prefix, string field)
		{
			Prefix = prefix == null || !prefix.Any() ? String.Empty : String.Join(".", prefix.Select(x => this.ToLowerFirstChar(x)));
			Field = field;
		}

		private string ToLowerFirstChar(string input)
		{
			string newString = input;
			if (!String.IsNullOrEmpty(newString) && Char.IsUpper(newString[0]))
				newString = Char.ToLower(newString[0]) + newString.Substring(1);
			return newString;
		}

		public String GetFieldWithPath()
		{
			return String.IsNullOrWhiteSpace(this.Prefix) ? this.ToLowerFirstChar(this.Field) : $"{this.Prefix}.{this.ToLowerFirstChar(this.Field)}";
		}
	}

}
