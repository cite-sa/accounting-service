using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Neanias.Accounting.Service.Common.Validation.Extensions
{
	public static class Extensions
	{
		public static Boolean IsValidEmail(this String value)
		{
			if (String.IsNullOrEmpty(value)) return false;
			try
			{
				new System.Net.Mail.MailAddress(value);
				return true;
			}
			catch (System.Exception)
			{
				return false;
			}
		}

		public static Boolean IsValidE164Phone(this String value)
		{
			if (String.IsNullOrEmpty(value)) return false;
			try
			{
				return Regex.IsMatch(value, "^\\+?[1-9]\\d{1,14}$");
			}
			catch (System.Exception)
			{
				return false;
			}
		}
	}
}
