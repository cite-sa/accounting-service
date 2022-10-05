using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.ForgetMe
{
	public interface IEraserService
	{
		Task<Boolean> Erase(Data.ForgetMe request);
	}
}
