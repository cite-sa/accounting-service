using Cite.Tools.Exception;
using Cite.Tools.Time;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Neanias.Accounting.Service.ErrorCode;

namespace Neanias.Accounting.Service.Convention
{
	public class ConventionService : IConventionService
	{
		//This is a value used to mask secrets 
		private const String secretValue = "1784E159-C809-4F3F-AC8F-90F98B04B01D";
		private const String LogTrackingHeaderValue = "x-log-tracking";
		private readonly ILogger<ConventionService> _logger;
		private readonly ErrorThesaurus _errors;

		public ConventionService(
			ErrorThesaurus errors,
			ILogger<ConventionService> logger)
		{
			this._logger = logger;
			this._errors = errors;
		}

		public Boolean IsValidId(int? id)
		{
			return id.HasValue && id.Value > 0;
		}

		public Boolean IsValidGuid(Guid? guid)
		{
			return guid.HasValue && guid.Value != Guid.Empty;
		}

		public Boolean IsValidHash(String hash)
		{
			return !String.IsNullOrEmpty(hash);
		}

		//GOTCHA: This will only cover conflicts with a range up to a second. To go bellow that consider using datetime2 (sql server) or a different conflict resolution technique
		public String HashValue(Object value)
		{
			if (value == null) return String.Empty;
			if (value is DateTime) return ((DateTime)value).ToEpoch().ToString();
			throw new MyApplicationException(this._errors.SystemError.Code, this._errors.SystemError.Message);
		}

		public String Limit(String text, int maxLength)
		{
			if (String.IsNullOrEmpty(text)) return text;
			if (text.Length > maxLength) return String.Format("{0}...", text.Substring(0, maxLength));
			else return text;
		}

		public String Truncate(String text, int maxLength)
		{
			string truncated = text;
			if (text.Length < maxLength) return text;

			truncated = truncated.Trim();
			truncated = Regex.Replace(truncated, @"\s+", " ");//remove multiple spaces
			if (truncated.Length < maxLength) return truncated;
			truncated = Regex.Replace(truncated, @"([.!@#$%^&-=':;,<>?*\\""/|])+", "");//remove special chars
			if (truncated.Length < maxLength) return truncated;
			truncated = Regex.Replace(truncated, @"([aeiou])+", ""); //remove non capital vowel letters
			if (truncated.Length < maxLength) return truncated;
			truncated = Regex.Replace(truncated, @"([AEIOU])+", ""); //remove capital vowel letters
			if (truncated.Length < maxLength) return truncated;

			if (text.Length > maxLength) return String.Format("{0}...", text.Substring(0, maxLength));
			return text;
		}

		public string WireValue()
		{
			return secretValue;
		}

		public String LogTrackingHeader()
		{
			return LogTrackingHeaderValue;
		}

		public string ToLowerFirstChar(string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			return char.ToLower(input[0]) + input.Substring(1);
		}
	}
}
