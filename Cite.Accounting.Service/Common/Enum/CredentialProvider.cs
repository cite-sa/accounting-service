namespace Cite.Accounting.Service.Common
{
	public enum CredentialProvider : short
	{
		UserPass = 0,
		Google = 1,
		Facebook = 2,
		Twitter = 3,
		LinkedIn = 4,
		GitHub = 5,
		Saml = 6,
		APIKey = 7,
		Totp = 8,
		DirectLink = 9,
		Transient = 10,
		CAS = 11,
	}
}
