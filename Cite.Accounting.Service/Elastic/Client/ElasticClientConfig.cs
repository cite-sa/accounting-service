using System;

namespace Cite.Accounting.Service.Elastic.Client
{
	public class CertificateConfig : Base.Client.CertificateConfig
	{
		public Boolean LoadAdditionalSslCertificates { get; set; }
	}
}
