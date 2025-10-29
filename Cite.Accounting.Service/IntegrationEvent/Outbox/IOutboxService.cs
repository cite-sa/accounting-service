using System.Threading.Tasks;

namespace Cite.Accounting.Service.IntegrationEvent.Outbox
{
	public interface IOutboxService
	{
		Task PublishAsync(OutboxIntegrationEvent item);
	}
}
