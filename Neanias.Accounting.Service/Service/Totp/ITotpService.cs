using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.Totp
{
	public interface ITotpService
	{
		Boolean Enabled();
		Task<TotpValidateResponse> ValidateAsync(Guid tenantId, Guid userId, String totp);
	}
}
