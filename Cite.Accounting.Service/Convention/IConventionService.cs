using System;

namespace Cite.Accounting.Service.Convention
{
	public interface IConventionService
	{
		Boolean IsValidId(int? id);
		Boolean IsValidGuid(Guid? guid);
		Boolean IsValidHash(String hash);
		String HashValue(Object value);
		String WireValue();
		String Limit(String text, int maxLength);
		String Truncate(String text, int maxLength);
		String LogTrackingHeader();
		string ToLowerFirstChar(string input);
	}
}
