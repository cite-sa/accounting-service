using System;

namespace Cite.Accounting.Service.Web.APIKey
{
	public class StaleApiKeyException : System.Exception
	{
		public int Code { get; set; }

		public StaleApiKeyException() : base() { }
		public StaleApiKeyException(int code) : this() { this.Code = code; }
		public StaleApiKeyException(String message) : base(message) { }
		public StaleApiKeyException(int code, String message) : this(message) { this.Code = code; }
		public StaleApiKeyException(String message, System.Exception innerException) : base(message, innerException) { }
		public StaleApiKeyException(int code, String message, System.Exception innerException) : this(message, innerException) { this.Code = code; }
	}
}
