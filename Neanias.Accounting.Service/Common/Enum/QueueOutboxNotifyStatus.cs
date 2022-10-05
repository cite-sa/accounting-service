using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Common
{
	public enum QueueOutboxNotifyStatus : short
	{
		Pending = 0,
		Processing = 1,
		WaitingConfirmation = 2,
		Confirmed = 3,
		ConfirmedTimeout = 4,
		Error = 5,
		Omitted = 6
	}
}
