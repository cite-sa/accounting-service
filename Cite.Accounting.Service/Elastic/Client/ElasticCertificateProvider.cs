using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Cite.Accounting.Service.Elastic.Client
{
	public class CertificateInfo
	{
		public String Issuer { get; set; }
		public String SerialNumber { get; set; }
		public String CertHash { get; set; }
	}

	public class ElasticCertificateProvider
	{


		private readonly CertificateConfig _config;
		private readonly List<CertificateInfo> _validCertificates;

		public ElasticCertificateProvider(
			CertificateConfig config
			)
		{
			this._config = config;
			this._validCertificates = new List<CertificateInfo>();
			this.Init();
		}

		private void Init()
		{
			if (!this._config.LoadAdditionalSslCertificates) return;

			foreach (string path in this._config.Paths ?? new List<string>())
			{
				X509Certificate2 cert = new X509Certificate2(path);
				this._validCertificates.Add(new CertificateInfo() { CertHash = cert.GetCertHashString(), Issuer = cert.Issuer, SerialNumber = cert.GetSerialNumberString() });
			}
		}

		public IEnumerable<CertificateInfo> GetIssuerCertificateInfos(string issuer) => this._validCertificates.Where(x => x.Issuer == issuer);
	}

}
