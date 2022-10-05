using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Common.Enum;
using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.ErrorCode;
using Neanias.Accounting.Service.IntegrationEvent;
using Neanias.Accounting.Service.IntegrationEvent.Inbox;
using Neanias.Accounting.Service.Query;
using Neanias.Accounting.Service.Service.LogTracking;
using Neanias.Accounting.Service.Web.Tasks.QueueListener.Extensions;
using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Cite.Tools.Json;
using Cite.Tools.Logging.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Tasks.QueueListener
{
	public class QueueListenerTask : Microsoft.Extensions.Hosting.BackgroundService
	{
		private readonly ILogger<QueueListenerTask> _logging;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly QueueListenerConfig _config;
		private readonly LogTrackingConfig _logTrackingConfig;
		private readonly IServiceProvider _serviceProvider;
		private readonly ErrorThesaurus _errors;
		private IConnection _queueConnection;
		private IModel _queueChannel;
		private readonly Random _random = new Random();


		public QueueListenerTask(
			ILogger<QueueListenerTask> logging,
			JsonHandlingService jsonHandlingService,
			QueueListenerConfig config,
			LogTrackingConfig logTrackingConfig,
			IServiceProvider serviceProvider,
			ErrorThesaurus errors)
		{
			this._logging = logging;
			this._jsonHandlingService = jsonHandlingService;
			this._config = config;
			this._logTrackingConfig = logTrackingConfig;
			this._serviceProvider = serviceProvider;
			this._errors = errors;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			this._logging.Debug("starting...");

			if (!this._config.Enable)
			{
				this._logging.Information("Listener disabled. exiting");
				return;
			}

			this.BootstrapQueue();

			stoppingToken.Register(() => this._logging.Information($"requested to stop..."));
			stoppingToken.ThrowIfCancellationRequested();

			AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(this._queueChannel);
			consumer.Received += OnConsumerReceived;
			consumer.Shutdown += OnConsumerShutdown;
			consumer.Registered += OnConsumerRegistered;
			consumer.Unregistered += OnConsumerUnregistered;
			consumer.ConsumerCancelled += OnConsumerCancelled;

			this._queueChannel.BasicConsume(
				queue: this._config.QueueName,
				autoAck: false,
				consumer: consumer);

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

			this._logging.Information("returning...");

			return;
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
			this._queueChannel.BasicQos((uint)this._config.QosPrefetchSize.Value, (ushort)this._config.QosPrefetchCount.Value, this._config.QosGlobal);

			this._queueChannel.ExchangeDeclare(
				exchange: this._config.Exchange,
				type: "topic",
				durable: this._config.Durable,
				autoDelete: false,
				arguments: null);

			this._queueChannel.QueueDeclare(
				queue: this._config.QueueName,
				durable: this._config.Durable,
				exclusive: false,
				autoDelete: false,
				arguments: null);

			this.BindTopics(this._queueChannel, this._config.QueueName, this._config.TenantCreationTopic);
			this.BindTopics(this._queueChannel, this._config.QueueName, this._config.TenantRemovalTopic);
			this.BindTopics(this._queueChannel, this._config.QueueName, this._config.UserTouchedTopic);
			this.BindTopics(this._queueChannel, this._config.QueueName, this._config.UserRemovalTopic);
			this.BindTopics(this._queueChannel, this._config.QueueName, this._config.ForgetMeRequestTopic);
			this.BindTopics(this._queueChannel, this._config.QueueName, this._config.ForgetMeRevokeTopic);
			this.BindTopics(this._queueChannel, this._config.QueueName, this._config.WhatYouKnowAboutMeRequestTopic);
			this.BindTopics(this._queueChannel, this._config.QueueName, this._config.WhatYouKnowAboutMeRevokeTopic);
			this.BindTopics(this._queueChannel, this._config.QueueName, this._config.DefaultUserLocaleChangedTopic);
			this.BindTopics(this._queueChannel, this._config.QueueName, this._config.DefaultUserLocaleRemovedTopic);
			this.BindTopics(this._queueChannel, this._config.QueueName, this._config.APIKeyStaleTopic);

			this._queueConnection.ConnectionShutdown += OnConnectionShutdown;
			this._queueConnection.RecoverySucceeded += OnRecoverySucceeded;
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
					this._logging.Error(ex, "problem connecting to Queue.Will retry in {seconds} seconds...", TimeSpan.FromMilliseconds(this._config.ConnectionRecovery.UnreachableRecoveryInterval).Seconds);
					Thread.Sleep(this._config.ConnectionRecovery.UnreachableRecoveryInterval);
				}
			}
		}

		private void BindTopics(IModel channel, String queueName, List<String> topics)
		{
			if (topics == null) return;
			foreach (String topic in topics)
			{
				channel.QueueBind(
					queue: queueName,
					exchange: this._config.Exchange,
					routingKey: topic);
			}
		}

		private async Task OnConsumerReceived(object sender, BasicDeliverEventArgs @event)
		{
			Boolean ack = false;
			try
			{
				ack = await this.ConsumeMessage(@event);
			}
			catch (System.Exception ex)
			{
				this._logging.Warning(ex, "there was a problem consuming the queued message. continuing...");
			}
			if (ack) this._queueChannel.BasicAck(@event.DeliveryTag, false);
			else this._queueChannel.BasicNack(@event.DeliveryTag, false, false);
		}

		private void OnConnectionShutdown(object sender, ShutdownEventArgs @event)
		{
			this._logging.Information("Queue event {0} with args {1}", nameof(OnConnectionShutdown), @event);
		}
		private void OnRecoverySucceeded(object sender, EventArgs @event)
		{
			this._logging.Information("Queue event {0} with args {1}", nameof(OnRecoverySucceeded), @event);
		}

		private Task OnConsumerCancelled(object sender, RabbitMQ.Client.Events.ConsumerEventArgs @event)
		{
			this._logging.Information("Queue event {0} with args {1}", nameof(OnConsumerCancelled), @event);
			return Task.CompletedTask;
		}

		private Task OnConsumerUnregistered(object sender, RabbitMQ.Client.Events.ConsumerEventArgs @event)
		{
			this._logging.Information("Queue event {0} with args {1}", nameof(OnConsumerUnregistered), @event);
			return Task.CompletedTask;
		}

		private Task OnConsumerRegistered(object sender, RabbitMQ.Client.Events.ConsumerEventArgs @event)
		{
			this._logging.Information("Queue event {0} with args {1}", nameof(OnConsumerRegistered), @event);
			return Task.CompletedTask;
		}

		private Task OnConsumerShutdown(object sender, ShutdownEventArgs @event)
		{
			this._logging.Information("Queue event {0} with args {1}", nameof(OnConsumerShutdown), @event);
			return Task.CompletedTask;
		}

		private async Task<Boolean> ConsumeMessage(BasicDeliverEventArgs @event)
		{
			using (var serviceScope = this._serviceProvider.CreateScope())
			{
				TenantScope scope = serviceScope.ServiceProvider.GetRequiredService<TenantScope>();
				if (!scope.TryExtractTenant(@event))
				{
					this._logging.Error($"Could not extract tenant for event {@event.BasicProperties.MessageId} from {@event.BasicProperties.AppId}");
					return false;
				}

				using (AppDbContext dbContext = serviceScope.ServiceProvider.GetService<AppDbContext>())
				{
					IBasicProperties properties = @event.BasicProperties;
					if (@event.Redelivered)
					{
						if (!Guid.TryParse(properties.MessageId, out Guid mId))
						{
							this._logging.Error($"Could not extract message id for event from {@event.BasicProperties.AppId}");
							throw new MyApplicationException(this._errors.SystemError.Code, this._errors.SystemError.Message);
						}
						if (dbContext.QueueInboxes.Any(x => x.MessageId == mId)) return true;
					}

					using (var transaction = await dbContext.Database.BeginTransactionAsync())
					{
						try
						{
							String message = Encoding.UTF8.GetString(@event.Body);
							Data.QueueInbox queueMessage = new Data.QueueInbox
							{
								Id = Guid.NewGuid(),
								TenantId = scope.Tenant == Guid.Empty ? (Guid?)null : scope.Tenant,
								Exchange = this._config.Exchange,
								Queue = this._config.QueueName,
								Route = @event.RoutingKey,
								ApplicationId = properties.AppId,
								MessageId = Guid.Parse(properties.MessageId),
								Message = message,
								IsActive = IsActive.Active,
								Status = QueueInboxStatus.Pending,
								RetryCount = 0,
								CreatedAt = DateTime.UtcNow,
								UpdatedAt = DateTime.UtcNow
							};

							dbContext.Add(queueMessage);
							await dbContext.SaveChangesAsync();
							transaction.Commit();
							return true;
						}
						catch (System.Exception ex)
						{
							this._logging.Error(ex, $"Could not save message {properties.MessageId}");
							return false;
						}
					}
				}
			}
		}

		private async Task Process()
		{
			try
			{
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

					Boolean successfulyProcessed = await this.Emit(candidate.MessageId);
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
							Data.QueueInbox queueMessage = await dbContext.QueueInboxes.FirstOrDefaultAsync(x => x.Id == outboxMessageId);

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

							queueMessage.Status = QueueInboxStatus.Omitted;
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
			public QueueInboxStatus PreviousState { get; set; }
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

							List<Data.QueueInbox> candidates = new List<Data.QueueInbox>();

							Data.QueueInbox item = await queryFactory.Query<QueueInboxQuery>()
								.IsActive(IsActive.Active)
								.Status(new QueueInboxStatus[] { QueueInboxStatus.Pending, QueueInboxStatus.Parked, QueueInboxStatus.Error })
								.RetryThreshold(this._config.Options.RetryThreashold)
								.CreatedAfter(lastCandidateCreationTimestamp)
								.Ordering(new Ordering().AddAscending(nameof(Data.QueueInbox.CreatedAt)))
								.FirstAsync();

							if (item != null) candidates.Add(item);

							Data.QueueInbox outboxMessage = candidates.OrderBy(x => x.CreatedAt).FirstOrDefault();

							if (outboxMessage == null)
							{
								transaction.Commit();
								return null;
							}

							QueueInboxStatus prevState = outboxMessage.Status;
							outboxMessage.Status = QueueInboxStatus.Processing;
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
							Data.QueueInbox notification = await dbContext.QueueInboxes.FirstOrDefaultAsync(x => x.Id == candidate.MessageId);

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
								notification.Status = candidate.PreviousState;
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

		private async Task<Boolean> Emit(Guid outboxMessageId)
		{
			using (var serviceScope = this._serviceProvider.CreateScope())
			{
				using (AppDbContext dbContext = serviceScope.ServiceProvider.GetService<AppDbContext>())
				{
					Data.QueueInbox queueInboxMessage = null;
					try
					{
						queueInboxMessage = await dbContext.QueueInboxes.FirstOrDefaultAsync(x => x.Id == outboxMessageId);
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
							EventProcessingStatus status = await this.ProcessMessage(queueInboxMessage.Route, queueInboxMessage.MessageId.ToString(), queueInboxMessage.ApplicationId, queueInboxMessage.Message);
							if (status == EventProcessingStatus.Success)
							{
								queueInboxMessage.Status = QueueInboxStatus.Successful;
								queueInboxMessage.UpdatedAt = DateTime.UtcNow;
								success = true;
							}
							else if (status == EventProcessingStatus.Postponed)
							{
								queueInboxMessage.Status = QueueInboxStatus.Parked;
								queueInboxMessage.UpdatedAt = DateTime.UtcNow;
							}
							else
							{
								queueInboxMessage.Status = QueueInboxStatus.Error;
								queueInboxMessage.RetryCount = queueInboxMessage.RetryCount.HasValue ? queueInboxMessage.RetryCount.Value + 1 : 0;
							}
							queueInboxMessage.UpdatedAt = DateTime.UtcNow;
							dbContext.Update(queueInboxMessage);
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

		private async Task<EventProcessingStatus> ProcessMessage(String routingKey, String messageId, String appId, String message)
		{
			IIntegrationEventHandler handler;
			if (this.RoutingKeyMatched(routingKey, this._config.TenantCreationTopic)) handler = this._serviceProvider.GetRequiredService<ITenantTouchedIntegrationEventHandler>();
			else if (this.RoutingKeyMatched(routingKey, this._config.TenantRemovalTopic)) handler = this._serviceProvider.GetRequiredService<ITenantRemovalIntegrationEventHandler>();
			else if (this.RoutingKeyMatched(routingKey, this._config.UserTouchedTopic)) handler = this._serviceProvider.GetRequiredService<IUserTouchedIntegrationEventHandler>();
			else if (this.RoutingKeyMatched(routingKey, this._config.UserRemovalTopic)) handler = this._serviceProvider.GetRequiredService<IUserRemovalIntegrationEventHandler>();
			else if (this.RoutingKeyMatched(routingKey, this._config.ForgetMeRequestTopic)) handler = this._serviceProvider.GetRequiredService<IForgetMeIntegrationEventHandler>();
			else if (this.RoutingKeyMatched(routingKey, this._config.ForgetMeRevokeTopic)) handler = this._serviceProvider.GetRequiredService<IForgetMeRevokeIntegrationEventHandler>();
			else if (this.RoutingKeyMatched(routingKey, this._config.WhatYouKnowAboutMeRequestTopic)) handler = this._serviceProvider.GetRequiredService<IWhatYouKnowAboutMeIntegrationEventHandler>();
			else if (this.RoutingKeyMatched(routingKey, this._config.WhatYouKnowAboutMeRevokeTopic)) handler = this._serviceProvider.GetRequiredService<IWhatYouKnowAboutMeRevokeIntegrationEventHandler>();
			else if (this.RoutingKeyMatched(routingKey, this._config.DefaultUserLocaleChangedTopic)) handler = this._serviceProvider.GetRequiredService<IDefaultUserLocaleChangedIntegrationEventHandler>();
			else if (this.RoutingKeyMatched(routingKey, this._config.DefaultUserLocaleRemovedTopic)) handler = this._serviceProvider.GetRequiredService<IDefaultUserLocaleDeletedIntegrationEventHandler>();
			else if (this.RoutingKeyMatched(routingKey, this._config.APIKeyStaleTopic)) handler = this._serviceProvider.GetRequiredService<IAPIKeyStaleIntegrationEventHandler>();
			else handler = null;

			if (handler == null) return EventProcessingStatus.Error;

			IntegrationEventProperties properties = new IntegrationEventProperties
			{
				AppId = appId,
				MessageId = messageId
			};

			TrackedEvent @event = this._jsonHandlingService.FromJsonSafe<TrackedEvent>(message);
			using (LogContext.PushProperty(this._logTrackingConfig.LogTrackingContextName, @event.TrackingContextTag))
			{
				try
				{
					return await handler.Handle(properties, message);
				}
				catch (System.Exception ex)
				{
					this._logging.Error(ex, "problem handling event from routing key {0}. Setting nack and continuing...", routingKey);
					return EventProcessingStatus.Error;
				}
			}
		}

		private Boolean RoutingKeyMatched(String routingKey, List<String> topics)
		{
			if (topics == null || topics.Count == 0) return false;
			return topics.Any(x => x.Equals(routingKey));
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
