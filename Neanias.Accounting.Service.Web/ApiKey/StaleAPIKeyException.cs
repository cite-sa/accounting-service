using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Web.APIKey
{
	public class StaleAPIKeyException : System.Exception
	{
		public int Code { get; set; }

		public StaleAPIKeyException() : base() { }
		public StaleAPIKeyException(int code) : this() { this.Code = code; }
		public StaleAPIKeyException(String message) : base(message) { }
		public StaleAPIKeyException(int code, String message) : this(message) { this.Code = code; }
		public StaleAPIKeyException(String message, System.Exception innerException) : base(message, innerException) { }
		public StaleAPIKeyException(int code, String message, System.Exception innerException) : this(message, innerException) { this.Code = code; }
	}
}
