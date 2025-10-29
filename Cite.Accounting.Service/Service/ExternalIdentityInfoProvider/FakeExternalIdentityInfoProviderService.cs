using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.ExternalIdentityInfoProvider
{
	public class FakeExternalIdentityInfoProviderService : IExternalIdentityInfoProvider
	{

		public FakeExternalIdentityInfoProviderService()
		{
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
