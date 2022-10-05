using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.ExternalIdentityInfoProvider
{
	public class FakeExternalIdentityInfoProviderService : IExternalIdentityInfoProvider
	{
		private readonly ILogger<FakeExternalIdentityInfoProviderService> _logger;

		public FakeExternalIdentityInfoProviderService(
			ILogger<FakeExternalIdentityInfoProviderService> logger
			)
		{
			this._logger = logger;
		}

		public Task<Dictionary<string, ExternalIdentityInfoResult>> Resolve(IEnumerable<string> subjects)
		{
			Dictionary<string, ExternalIdentityInfoResult> result = new Dictionary<string, ExternalIdentityInfoResult>();
			foreach (string subject in subjects)
			{
				result[subject] = new ExternalIdentityInfoResult() { Email = "", Name = subject, Issuer = "fake", Subject = subject };
			}
			return Task.FromResult(result);
		}
	}
}
