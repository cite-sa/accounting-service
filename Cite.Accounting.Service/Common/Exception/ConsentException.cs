using System;

namespace Cite.Accounting.Service.Exception
{
	public class ConsentException : System.Exception
	{
		public int Code { get; set; }

		public ConsentException() : base() { }
		public ConsentException(int code) : this() { this.Code = code; }
		public ConsentException(String message) : base(message) { }
		public ConsentException(int code, String message) : this(message) { this.Code = code; }
		public ConsentException(String message, System.Exception innerException) : base(message, innerException) { }
		public ConsentException(int code, String message, System.Exception innerException) : this(message, innerException) { this.Code = code; }
	}
}
