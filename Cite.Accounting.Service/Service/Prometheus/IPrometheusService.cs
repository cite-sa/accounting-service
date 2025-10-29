
using Prometheus;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.Prometheus
{
	public interface IPrometheusService
	{
        void InitializeGauges();

    }
}
