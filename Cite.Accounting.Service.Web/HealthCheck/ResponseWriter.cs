using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.HealthCheck
{
	public static class ResponseWriter
	{
		public static Task WriteResponse(HttpContext context, HealthReport healthReport)
		{
			context.Response.ContentType = "application/json; charset=utf-8";

			JsonWriterOptions options = new JsonWriterOptions { Indented = true };

			using MemoryStream memoryStream = new MemoryStream();
			using (Utf8JsonWriter jsonWriter = new Utf8JsonWriter(memoryStream, options))
			{
				jsonWriter.WriteStartObject();
				jsonWriter.WriteString("status", healthReport.Status.ToString());
				jsonWriter.WriteString("duration", healthReport.TotalDuration.ToString());
				jsonWriter.WriteStartObject("results");

				foreach (KeyValuePair<string, HealthReportEntry> healthReportEntry in healthReport.Entries)
				{
					jsonWriter.WriteStartObject(healthReportEntry.Key);

					jsonWriter.WriteString("status", healthReportEntry.Value.Status.ToString());
					jsonWriter.WriteString("description", healthReportEntry.Value.Description);
					jsonWriter.WriteString("duration", healthReportEntry.Value.Duration.ToString());
					jsonWriter.WriteString("tags", healthReportEntry.Value.Tags == null || !healthReportEntry.Value.Tags.Any() ? null : string.Join(", ", healthReportEntry.Value.Tags));
					jsonWriter.WriteString("exception", healthReportEntry.Value.Exception?.Message);

					jsonWriter.WriteStartObject("data");
					foreach (var item in healthReportEntry.Value.Data)
					{
						jsonWriter.WritePropertyName(item.Key);

						JsonSerializer.Serialize(jsonWriter, item.Value,
							item.Value?.GetType() ?? typeof(object));
					}
					jsonWriter.WriteEndObject();

					jsonWriter.WriteEndObject();
				}

				jsonWriter.WriteEndObject();
				jsonWriter.WriteEndObject();
			}

			return context.Response.WriteAsync(Encoding.UTF8.GetString(memoryStream.ToArray()));
		}
	}
}
