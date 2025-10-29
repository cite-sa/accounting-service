using System.Threading.Tasks;

namespace Cite.Accounting.Service.IntegrationEvent.Outbox
{
	public interface IForgetMeCompletedIntegrationEventHandler
	{
		Task HandleAsync(ForgetMeCompletedIntegrationEvent @event);
	}
}
