using System;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class WhatYouKnowAboutMeRevokeConsistencyPredicates : IConsistencyPredicates
	{
		public Guid Id { get; set; }
	}
}
