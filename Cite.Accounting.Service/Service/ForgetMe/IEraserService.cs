using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.ForgetMe
{
	public interface IEraserService
	{
		Task<Boolean> Erase(Data.ForgetMe request);
	}
}
