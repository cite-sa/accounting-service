using Cite.Accounting.Service.Elastic.Base.Client;

namespace Cite.Accounting.Service.Elastic.Client
{

	public class AppElasticCertificateProvider : BaseElasticCertificateProvider
	{
		public AppElasticCertificateProvider(
			CertificateConfig config
			) : base(config)
		{
		}
	}

}
