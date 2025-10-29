using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Cite.Accounting.Service.Elastic.Base.Client
{
	public class CertificateInfo
	{
		public string Issuer { get; set; }
		public string SerialNumber { get; set; }
		public string CertHash { get; set; }
	}

	public abstract class BaseElasticCertificateProvider
	{
		private readonly CertificateConfig _config;
		private List<CertificateInfo> _validCertificates;

		protected BaseElasticCertificateProvider(
			CertificateConfig config
			)
		{
			this._config = config;
			this.Init();
		}

		private void Init()
		{
			this._validCertificates = new List<CertificateInfo>();
			foreach (string path in this._config?.Paths)
			{
				if (string.IsNullOrWhiteSpace(path)) continue;
				X509Certificate2 cert = new X509Certificate2(path);
				this._validCertificates.Add(new CertificateInfo() { CertHash = cert.GetCertHashString(), Issuer = cert.Issuer, SerialNumber = cert.GetSerialNumberString() });
			}
		}

		public IEnumerable<CertificateInfo> GetIssuerCertificateInfos(string issuer) => this._validCertificates.Where(x => x.Issuer == issuer);
	}

}
