using System;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class WhatYouKnowAboutMeConsistencyPredicates : IConsistencyPredicates
	{
		public Guid UserId { get; set; }
	}
}
