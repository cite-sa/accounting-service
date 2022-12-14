using Neanias.Accounting.Service.Authorization;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Censor;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Cite.Tools.Logging.Extensions;

namespace Neanias.Accounting.Service.Model
{
	public class MetricCensor : Censor
	{
		private readonly CensorFactory _censorFactory;
		private readonly IAuthorizationService _authService;
		private readonly ILogger<MetricCensor> _logger;

		public MetricCensor(
			CensorFactory censorFactory,
			IAuthorizationService authService,
			ILogger<MetricCensor> logger)
		{
			this._logger = logger;
			this._censorFactory = censorFactory;
			this._authService = authService;
		}

		public async Task Censor(IFieldSet fields, Guid? userId = null)
		{
			this._logger.Debug(new DataLogEntry("censoring fields", fields));
			if (this.IsEmpty(fields)) return;
			await this._authService.AuthorizeOrOwnerForce(userId.HasValue ? new OwnedResource(userId.Value) : null, Permission.BrowseMetric, Permission.DeferredAffiliation);
			IFieldSet serviceFields = fields.ExtractPrefixed(nameof(Metric.Service).AsIndexerPrefix());
			await this._censorFactory.Censor<ServiceCensor>().Censor(serviceFields, userId);
		}
	}

}
