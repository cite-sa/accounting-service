using Cite.Tools.Auth.Claims;
using Cite.WebTools.CurrentPrincipal;
using Cite.WebTools.InvokerContext;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Audit
{
	public class AuditService : LoggingAuditService
	{
		//private const String _tenantKey = "t";

		//private readonly TenantScope _scope;

		public AuditService(
			ICurrentPrincipalResolverService currentPrincipalResolverService,
			IInvokerContextResolverService invokerContextResolverService,
			ILoggerFactory logger,
			//TenantScope scope,
			LoggingAuditConfig config,
			ClaimExtractor extractor) : base(currentPrincipalResolverService, invokerContextResolverService, logger.CreateLogger("audit"), config, extractor)
		{
			//this._scope = scope;
		}

		//protected override AuditEntry Enrich(AuditEntry entry)
		//{
		//	base.Enrich(entry);
		//	return entry.And(AuditService._tenantKey, this._scope.IsSet ? (Guid?)this._scope.Tenant : null);
		//}
	}
}
