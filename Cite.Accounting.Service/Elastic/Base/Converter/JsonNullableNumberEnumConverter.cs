using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cite.Accounting.Service.Elastic.Base.Converter
{
	public class JsonNullableNumberEnumConverter<TEnum> : JsonConverter<TEnum?> where TEnum : struct, Enum
	{
		public override bool HandleNull => true;
		public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			string value = reader.GetString();

			if (string.IsNullOrEmpty(value)) return null;
			return Enum.Parse<TEnum>(value);
		}

		public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options) =>
			writer.WriteStringValue(value != null ? Convert.ToUInt32(value).ToString() : null);
	}
}
