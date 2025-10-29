using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.Totp
{
	public interface ITotpService
	{
		Boolean Enabled();
		Task<TotpValidateResponse> ValidateAsync(Guid tenantId, Guid userId, String totp);
	}
}
