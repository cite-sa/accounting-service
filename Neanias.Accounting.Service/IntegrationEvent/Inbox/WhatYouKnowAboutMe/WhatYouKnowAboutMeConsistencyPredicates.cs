using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public class WhatYouKnowAboutMeConsistencyPredicates : IConsistencyPredicates
	{
		public Guid UserId { get; set; }
	}
}
