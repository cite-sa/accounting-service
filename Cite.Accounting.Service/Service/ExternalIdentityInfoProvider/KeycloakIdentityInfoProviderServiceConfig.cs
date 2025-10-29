namespace Cite.Accounting.Service.Service.ExternalIdentityInfoProvider
{
	public class KeycloakIdentityInfoProviderServiceConfig
	{
		public string IdpBaseUtrl { get; set; }
		public string Realm { get; set; }
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
		public string Issuer { get; set; }
	}
}
