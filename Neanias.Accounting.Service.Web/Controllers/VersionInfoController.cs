using Neanias.Accounting.Service.Model;
using Neanias.Accounting.Service.Service.Version;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cite.Tools.Logging.Extensions;

namespace Neanias.Accounting.Service.Web.Controllers
{
	[Route("api/accounting-service/version-info")]
	public class VersionInfoController : ControllerBase
	{
		private readonly IVersionInfoService _versionInfoService;
		private readonly ILogger<VersionInfoController> _logger;

		public VersionInfoController(
			IVersionInfoService versionInfoService,
			ILogger<VersionInfoController> logger)
		{
			this._logger = logger;
			this._versionInfoService = versionInfoService;
		}

		[HttpGet("current")]
		public async Task<List<VersionInfo>> GetCurrent()
		{
			this._logger.Debug("current");

			List<VersionInfo> current = await this._versionInfoService.CurrentAsync();
			return current;
		}

		[HttpGet("history")]
		public async Task<List<VersionInfo>> GetHistory()
		{
			this._logger.Debug("history");

			List<VersionInfo> history = await this._versionInfoService.HistoryAsync();
			return history;
		}
	}
}
