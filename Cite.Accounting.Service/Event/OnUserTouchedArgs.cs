using System;

namespace Cite.Accounting.Service.Event
{
	public struct OnUserTouchedArgs
	{
		public OnUserTouchedArgs(Guid tenantId, Guid userId, String subject, String issuer, String prevSubject, String prevIssuer)
		{
			this.TenantId = tenantId;
			this.UserId = userId;
			this.Subject = subject;
			this.Issuer = issuer;
			this.PreviousSubject = prevSubject;
			this.PreviousIssuer = prevIssuer;
		}

		public Guid TenantId { get; private set; }
		public Guid UserId { get; private set; }
		public String Subject { get; private set; }
		public String Issuer { get; private set; }
		public String PreviousSubject { get; private set; }
		public String PreviousIssuer { get; private set; }
	}
}
