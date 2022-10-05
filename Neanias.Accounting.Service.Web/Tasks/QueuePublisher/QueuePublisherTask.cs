using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Common.Enum;
using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.IntegrationEvent.Outbox;
using Neanias.Accounting.Service.Query;
using Neanias.Accounting.Service.Web.Tasks.QueuePublisher.MessageStorage;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Query;
using Cite.Tools.Json;
using Cite.Tools.Logging.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Neanias.Accounting.Service.Web.Tasks.QueuePublisher
{
	public class QueuePublisherTask : Microsoft.Extensions.Hosting.BackgroundService
	{
		private readonly ILogger<QueuePublisherTask> _logging;
		private readonly QueuePublisherConfig _config;
		private readonly IServiceProvider _serviceProvider;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly IMessageStorage<UInt64, Guid> _unconfirmedPublishedMessages = new InverseLookupMessageStorage<UInt64, Guid>();
		private IConnection _queueConnection;
		private IModel _queueChannel;
		private readonly Random _random = new Random();

		public QueuePublisherTask(
			ILogger<QueuePublisherTask> logging,
			QueuePublisherConfig config,
			IServiceProvider serviceProvider,
			JsonHandlingService jsonHandlingService)
		{
			this._logging = logging;
			this._config = config;
			this._serviceProvider = serviceProvider;
			this._jsonHandlingService = jsonHandlingService;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			this._logging.Debug("starting...");

			if (!this._config.Enable)
			{
				this._logging.Information("Publisher disabled. exiting");
				return;
			}

			this.BootstrapQueue();

			stoppingToken.Register(() => this._logging.Information($"requested to stop..."));

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					this._logging.Debug($"going to sleep for {this._config.IntervalSeconds} seconds...");
					await Task.Delay(TimeSpan.FromSeconds(this._config.IntervalSeconds.Value), stoppingToken);
				}
				catch (TaskCanceledException ex)
				{
					this._logging.Information($"Task canceled: {ex.Message}");
					break;
				}
				catch (System.Exception ex)
				{
					this._logging.Error(ex, "Error while delaying to process notification. Continuing");
				}

				if (this._config.Enable) await this.Process();
			}

			this._logging.Information("stoping...");
		}

		private void BootstrapQueue()
		{
			ConnectionFactory factory = new ConnectionFactory
			{
				HostName = this._config.HostName,
				Port = this._config.Port.Value,
				UserName = this._config.Username,
				Password = this._config.Password,
				DispatchConsumersAsync = true
			};

			factory.AutomaticRecoveryEnabled = this._config.ConnectionRecovery.Enabled;
			if (this._config.ConnectionRecovery.Enabled)
			{
				factory.NetworkRecoveryInterval = TimeSpan.FromMilliseconds(this._config.ConnectionRecovery.NetworkRecoveryInterval);
			}
			this._queueConnection = this.CreateConnection(factory);

			this._queueChannel = this._queueConnection.CreateModel();
			this._queueChannel.ExchangeDeclare(
				exchange: this._config.Exchange,
				type: "topic",
				durable: this._config.Durable,
				autoDelete: false,
				arguments: null);

			this._queueChannel.ConfirmSelect();
			this._queueConnection.ConnectionShutdown += OnConnectionShutdown;
			this._queueChannel.BasicAcks += OnBasicAcks;
			this._queueChannel.BasicNacks += OnBasicNacks;
		}

		public IConnection CreateConnection(ConnectionFactory factory)
		{
			while (true)
			{
				try
				{
					return factory.CreateConnection();
				}
				catch (System.Exception ex)
				{
					this._logging.Error(ex, $"problem connecting to Queue.Will retry in {TimeSpan.FromMilliseconds(this._config.ConnectionRecovery.UnreachableRecoveryInterval).Seconds} seconds...");
					Thread.Sleep(this._config.ConnectionRecovery.UnreachableRecoveryInterval);
				}
			}
		}

		private async void OnBasicAcks(object sender, BasicAckEventArgs @event)
		{
			this._logging.Information($"Received confirm for delivery tag {@event.DeliveryTag}, multiple {@event.Multiple}");
			List<Guid> confirmedMessages = new List<Guid>();
			if (@event.Multiple)
			{
				var confirmedTags = this._unconfirmedPublishedMessages.Where(x => x.Key <= @event.DeliveryTag);
				foreach (var tag in confirmedTags.Select(x => x.Key))
				{
					var key = this._unconfirmedPublishedMessages.PurgeByKey(tag);
					if (key == Guid.Empty)
					{
						this._logging.Information($"Could not find message for confirm {@event.DeliveryTag}");
						continue;
					}
					confirmedMessages.Add(key);
				}
			}
			else
			{
				var key = this._unconfirmedPublishedMessages.PurgeByKey(@event.DeliveryTag);
				if (key == Guid.Empty)
				{
					this._logging.Information($"Could not find message for confirm {@event.DeliveryTag}");
					return;
				}
				confirmedMessages = key.AsList();
			}
			await this.HandleConfirm(confirmedMessages);
		}

		private async void OnBasicNacks(object sender, BasicNackEventArgs @event)
		{
			this._logging.Information($"Received nack for delivery tag {@event.DeliveryTag}, multiple {@event.Multiple}");
			List<Guid> nackedMessages = new List<Guid>();
			if (@event.Multiple)
			{
				var nackedTags = this._unconfirmedPublishedMessages.Where(x => x.Key <= @event.DeliveryTag);
				foreach (var tag in nackedTags.Select(x => x.Key))
				{
					var key = this._unconfirmedPublishedMessages.PurgeByKey(tag);
					if (key == Guid.Empty)
					{
						this._logging.Error($"Could not find message for confirm {@event.DeliveryTag}");
						continue;
					}
					nackedMessages.Add(key);
				}
			}
			else
			{
				var key = this._unconfirmedPublishedMessages.PurgeByKey(@event.DeliveryTag);
				if (key == Guid.Empty)
				{
					this._logging.Error($"Could not find message for confirm {@event.DeliveryTag}");
					return;
				}
				nackedMessages = key.AsList();
			}
			await this.HandleNack(nackedMessages);
		}

		public Boolean Publish(Data.QueueOutbox queueOutboxMessage)
		{
			try
			{
				OutboxIntegrationEvent @event = this._jsonHandlingService.FromJsonSafe<OutboxIntegrationEvent>(queueOutboxMessage.Message);
				byte[] body = Encoding.UTF8.GetBytes(@event.Message);

				IBasicProperties properties = this._queueChannel.CreateBasicProperties();
				properties.Persistent = true;
				properties.MessageId = @event.Id;
				properties.AppId = this._config.AppId;
				properties.Headers = new Dictionary<string, object>
				{
					{ "x-tenant", queueOutboxMessage.TenantId.ToString() }
				};
				var deliveryTag = this._queueChannel.NextPublishSeqNo;
				this._queueChannel.BasicPublish(
					exchange: queueOutboxMessage.Exchange,
					routingKey: queueOutboxMessage.Route,
					basicProperties: properties,
					body: body);
				this._unconfirmedPublishedMessages.Add(deliveryTag, queueOutboxMessage.Id);
				return true;
			}
			catch (System.Exception ex)
			{
				this._logging.Error(ex, "unable to publish event. Continuing...");
				return false;
			}
		}

		private async Task Process()
		{
			try
			{
				//GOTCHA: Notifications with same createdat are ignored util other set to final state
				DateTime? lastCandidateCreationTimestamp = null;
				while (true)
				{
					CandidateInfo candidate = await this.CandidateToNotify(lastCandidateCreationTimestamp);
					if (candidate == null) break;
					lastCandidateCreationTimestamp = candidate.MessageCreatedAt;

					this._logging.Debug($"Processing notify message: {candidate.MessageId}");

					Boolean shouldOmit = await this.ShouldOmitNotify(candidate.MessageId);
					if (shouldOmit)
					{
						this._logging.Debug($"Omitting message {candidate.MessageId}");
						//skipping reprocessing for this iteration
						continue;
					}

					Boolean shouldWait = await this.ShouldWait(candidate);
					if (shouldWait)
					{
						this._logging.Debug($"Will no retry message {candidate.MessageId}");
						//skipping reprocessing for this iteration
						continue;
					}

					Boolean successfulyProcessed = await this.Notify(candidate.MessageId);
					if (!successfulyProcessed)
					{
						//skipping reprocessing for this iteration
					}
				}
			}
			catch (System.Exception ex)
			{
				this._logging.Error(ex, $"Problem processing messages. Breaking for next interval");
			}
		}

		private async Task<Boolean> ShouldOmitNotify(Guid outboxMessageId)
		{
			using (var serviceScope = this._serviceProvider.CreateScope())
			{
				using (AppDbContext dbContext = serviceScope.ServiceProvider.GetService<AppDbContext>())
				{
					using (var transaction = await dbContext.Database.BeginTransactionAsync())
					{
						try
						{
							Data.QueueOutbox queueMessage = await dbContext.QueueOutboxes.FirstOrDefaultAsync(x => x.Id == outboxMessageId);


							TimeSpan age = DateTime.UtcNow - queueMessage.CreatedAt;
							if (!this._config.Options.TooOldToSendSeconds.HasValue)
							{
								transaction.Commit();
								return false;
							}
							TimeSpan omitThreshold = TimeSpan.FromSeconds(this._config.Options.TooOldToSendSeconds.Value);
							if (age < omitThreshold)
							{
								transaction.Commit();
								return false;
							}

							queueMessage.NotifyStatus = QueueOutboxNotifyStatus.Omitted;
							queueMessage.UpdatedAt = DateTime.UtcNow;
							dbContext.Update(queueMessage);
							await dbContext.SaveChangesAsync();

							transaction.Commit();
							return true;
						}
						catch (System.Exception ex)
						{
							this._logging.Error(ex, $"Could not mark message {outboxMessageId} as omitted");
							//Still want to skip it from processing
							transaction.Rollback();
							return true;
						}
					}
				}
			}
		}

		private class CandidateInfo
		{
			public Guid MessageId { get; set; }
			public QueueOutboxNotifyStatus PreviousState { get; set; }
			public DateTime MessageCreatedAt { get; set; }
		}

		private async Task<CandidateInfo> CandidateToNotify(DateTime? lastCandidateCreationTimestamp)
		{
			using (var serviceScope = this._serviceProvider.CreateScope())
			{
				using (AppDbContext dbContext = serviceScope.ServiceProvider.GetService<AppDbContext>())
				{
					using (var transaction = await dbContext.Database.BeginTransactionAsync())
					{
						try
						{
							QueryFactory queryFactory = serviceScope.ServiceProvider.GetService<QueryFactory>();

							List<Data.QueueOutbox> candidates = new List<Data.QueueOutbox>();

							Data.QueueOutbox item = await queryFactory.Query<QueueOutboxQuery>()
								.IsActive(IsActive.Active)
								.Status(new QueueOutboxNotifyStatus[] { QueueOutboxNotifyStatus.Pending, QueueOutboxNotifyStatus.WaitingConfirmation, QueueOutboxNotifyStatus.Error })
								.RetryThreshold(this._config.Options.RetryThreashold)
								.CreatedAfter(lastCandidateCreationTimestamp)
								.Ordering(new Ordering().AddAscending(nameof(Data.QueueOutbox.CreatedAt)))
								.FirstAsync();

							if (item != null) candidates.Add(item);

							Data.QueueOutbox outboxMessage = candidates.OrderBy(x => x.CreatedAt).FirstOrDefault();

							if (outboxMessage == null)
							{
								transaction.Commit();
								return null;
							}

							bool confirmTimeout = (outboxMessage.PublishedAt.HasValue && !outboxMessage.ConfirmedAt.HasValue)
								&& outboxMessage.PublishedAt.Value.AddSeconds(this._config.Options.ConfirmTimeoutSeconds) > DateTime.UtcNow;
							if (confirmTimeout) this.ClearFromCache(outboxMessage.Id);

							QueueOutboxNotifyStatus prevState = outboxMessage.NotifyStatus;
							outboxMessage.NotifyStatus = QueueOutboxNotifyStatus.Processing;
							outboxMessage.UpdatedAt = DateTime.UtcNow;

							dbContext.Update(outboxMessage);
							await dbContext.SaveChangesAsync();

							transaction.Commit();

							return new CandidateInfo() { MessageId = outboxMessage.Id, MessageCreatedAt = outboxMessage.CreatedAt, PreviousState = prevState };
						}
						catch (DbUpdateConcurrencyException ex)
						{
							// we get this if/when someone else already modified the notifications. We want to essentially ignore this, and keep working
							this._logging.Debug($"Concurrency exception getting list of notifications. Skipping: {ex.Message}");
							transaction.Rollback();
							return null;
						}
						catch (System.Exception ex)
						{
							this._logging.Error(ex, $"Problem getting list of notifications. Skipping: {ex.Message}");
							transaction.Rollback();
							return null;
						}
					}
				}
			}
		}

		private async Task<Boolean> ShouldWait(CandidateInfo candidate)
		{
			using (var serviceScope = this._serviceProvider.CreateScope())
			{
				using (AppDbContext dbContext = serviceScope.ServiceProvider.GetService<AppDbContext>())
				{
					using (var transaction = await dbContext.Database.BeginTransactionAsync())
					{
						try
						{
							Data.QueueOutbox notification = await dbContext.QueueOutboxes.FirstOrDefaultAsync(x => x.Id == candidate.MessageId);

							if (!notification.RetryCount.HasValue || notification.RetryCount.Value < 1)
							{
								transaction.Commit();
								return false;
							}

							int accumulatedRetry = 0;
							int pastAccumulateRetry = 0;
							for (int i = 1; i <= notification.RetryCount + 1; i += 1) accumulatedRetry += (i * this._config.Options.RetryDelayStepSeconds);
							for (int i = 1; i <= notification.RetryCount; i += 1) pastAccumulateRetry += (i * this._config.Options.RetryDelayStepSeconds);
							int randAccumulatedRetry = this._random.Next((int)(accumulatedRetry / 2), accumulatedRetry + 1);
							int additionalTime = randAccumulatedRetry > this._config.Options.MaxRetryDelaySeconds ? this._config.Options.MaxRetryDelaySeconds : randAccumulatedRetry;
							int retry = pastAccumulateRetry + additionalTime;

							DateTime retryOn = notification.CreatedAt.AddSeconds(retry);
							Boolean itIsTime = retryOn <= DateTime.UtcNow;

							if (!itIsTime)
							{
								notification.NotifyStatus = candidate.PreviousState;
								notification.UpdatedAt = DateTime.UtcNow;
								dbContext.Update(notification);
								await dbContext.SaveChangesAsync();
							}

							transaction.Commit();

							return !itIsTime;
						}
						catch (System.Exception ex)
						{
							this._logging.Error(ex, $"Could not check message {candidate.MessageId} for retry");
							//Still want to skip it from processing
							transaction.Rollback();
							return false;
						}
					}
				}
			}
		}

		private async Task<Boolean> Notify(Guid outboxMessageId)
		{
			using (var serviceScope = this._serviceProvider.CreateScope())
			{
				using (AppDbContext dbContext = serviceScope.ServiceProvider.GetService<AppDbContext>())
				{
					Data.QueueOutbox queueOutboxMessage = null;
					try
					{
						queueOutboxMessage = await dbContext.QueueOutboxes.FirstOrDefaultAsync(x => x.Id == outboxMessageId);
					}
					catch (System.Exception ex)
					{
						this._logging.Warning(ex, $"Could not lookup message {outboxMessageId} to process. Continuing...");
						return false;
					}

					Boolean success = false;
					using (var transaction = await dbContext.Database.BeginTransactionAsync())
					{
						try
						{
							//The tracker must porperly update the Notify State so that the notification does not remain locked
							success = this.Publish(queueOutboxMessage);
							if (success)
							{
								queueOutboxMessage.NotifyStatus = QueueOutboxNotifyStatus.WaitingConfirmation;
								queueOutboxMessage.PublishedAt = DateTime.UtcNow;
								queueOutboxMessage.UpdatedAt = DateTime.UtcNow;
							}
							else
							{
								queueOutboxMessage.NotifyStatus = QueueOutboxNotifyStatus.Error;
								queueOutboxMessage.RetryCount = queueOutboxMessage.RetryCount.HasValue ? queueOutboxMessage.RetryCount.Value + 1 : 0;
								queueOutboxMessage.PublishedAt = null;

							}
							queueOutboxMessage.UpdatedAt = DateTime.UtcNow;
							dbContext.Update(queueOutboxMessage);
							await dbContext.SaveChangesAsync();
							transaction.Commit();
						}
						catch (System.Exception ex)
						{
							this._logging.Error(ex, $"Problem sending message. rolling back any message emit db changes and marking error. If message was send it might cause multiple emits. Continuing...");
							transaction.Rollback();
							success = false;
						}
					}
					return success;
				}
			}
		}

		private async Task HandleConfirm(IEnumerable<Guid> confirmedMessages)
		{
			using (var serviceScope = this._serviceProvider.CreateScope())
			{
				using (AppDbContext dbContext = serviceScope.ServiceProvider.GetService<AppDbContext>())
				{
					List<Data.QueueOutbox> queueOutboxMessages = null;
					try
					{
						queueOutboxMessages = await dbContext.QueueOutboxes.Where(x => confirmedMessages.Contains(x.Id)).ToListAsync();
					}
					catch (System.Exception ex)
					{
						this._logging.Warning(ex, $"Could not lookup messages {String.Join(",", confirmedMessages)} to process. Continuing...");
						return;
					}

					using (var transaction = await dbContext.Database.BeginTransactionAsync())
					{
						try
						{
							foreach (Data.QueueOutbox queueOutboxMessage in queueOutboxMessages)
							{
								queueOutboxMessage.NotifyStatus = QueueOutboxNotifyStatus.Confirmed;
								queueOutboxMessage.UpdatedAt = DateTime.UtcNow;
								queueOutboxMessage.ConfirmedAt = DateTime.UtcNow;
							}

							dbContext.UpdateRange(queueOutboxMessages);
							await dbContext.SaveChangesAsync();
							transaction.Commit();
						}
						catch (System.Exception ex)
						{
							this._logging.Error(ex, $"Problem sending message. rolling back any message emit db changes and marking error. If message was send it might cause multiple emits. Continuing...");
							transaction.Rollback();
						}
					}
				}
			}
		}

		private async Task HandleNack(IEnumerable<Guid> nackedMessages)
		{
			using (var serviceScope = this._serviceProvider.CreateScope())
			{
				using (AppDbContext dbContext = serviceScope.ServiceProvider.GetService<AppDbContext>())
				{
					List<Data.QueueOutbox> nackedQueueOutboxMessages = null;
					try
					{
						nackedQueueOutboxMessages = await dbContext.QueueOutboxes.Where(x => nackedMessages.Contains(x.Id)).ToListAsync();
					}
					catch (System.Exception ex)
					{
						this._logging.Warning(ex, $"Could not lookup messages {String.Join(",", nackedMessages)} to process. Continuing...");
						return;
					}

					using (var transaction = await dbContext.Database.BeginTransactionAsync())
					{
						try
						{
							foreach (Data.QueueOutbox queueOutboxMessage in nackedQueueOutboxMessages)
							{
								queueOutboxMessage.NotifyStatus = QueueOutboxNotifyStatus.Error;
								queueOutboxMessage.RetryCount = queueOutboxMessage.RetryCount.HasValue ? queueOutboxMessage.RetryCount.Value + 1 : 0;
								queueOutboxMessage.UpdatedAt = DateTime.UtcNow;
							}

							dbContext.UpdateRange(nackedQueueOutboxMessages);
							await dbContext.SaveChangesAsync();
							transaction.Commit();
						}
						catch (System.Exception ex)
						{
							this._logging.Error(ex, $"Problem sending message. rolling back any message emit db changes and marking error. If message was send it might cause multiple emits. Continuing...");
							transaction.Rollback();
						}
					}
				}
			}
		}

		private void ClearFromCache(Guid messageId)
		{
			this._unconfirmedPublishedMessages.PurgeByValue(messageId);
		}

		private void OnConnectionShutdown(object sender, ShutdownEventArgs @event)
		{
			this._logging.Information("Queue event {0} with args {1}", nameof(OnConnectionShutdown), @event);
		}

		public override void Dispose()
		{
			if (this._queueChannel != null)
			{
				this._queueChannel.Close();
				this._queueChannel.Dispose();
			}
			if (this._queueConnection != null)
			{
				this._queueConnection.Close();
				this._queueConnection.Dispose();
			}
			base.Dispose();
		}
	}
}
