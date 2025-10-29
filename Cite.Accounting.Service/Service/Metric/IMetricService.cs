using Cite.Tools.FieldSet;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.Metric
{
	public interface IMetricService
	{
		Task<Model.Metric> PersistAsync(Model.MetricPersist model, IFieldSet fields = null);
		Task DeleteAndSaveAsync(Guid id);
	}
}
