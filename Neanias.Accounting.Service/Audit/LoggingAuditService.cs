using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text;
using Cite.Tools.Auth.Claims;
using Cite.Tools.Auth.Extensions;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Common.Types;
using Cite.Tools.Logging.Extensions;
using Cite.WebTools.CurrentPrincipal;
using Cite.WebTools.InvokerContext;
using Microsoft.Extensions.Logging;

namespace Neanias.Accounting.Service.Audit
{
	public class LoggingAuditService : IAuditService
	{
		private const String _invokerKey = "invoker";
		private const String _principalKey = "usr";
		private const String _timestampKey = "ts";
		private const String _varKey = "var";

		protected readonly ICurrentPrincipalResolverService _currentPrincipalResolverService;
		protected readonly IInvokerContextResolverService _invokerContextResolverService;
		protected readonly ILogger _logger;
		protected readonly LoggingAuditConfig _config;
		protected readonly ClaimExtractor _extractor;

		public LoggingAuditService(
			ICurrentPrincipalResolverService currentPrincipalResolverService,
			IInvokerContextResolverService invokerContextResolverService,
			ILogger logger,
			LoggingAuditConfig config,
			ClaimExtractor extractor)
		{
			this._currentPrincipalResolverService = currentPrincipalResolverService;
			this._invokerContextResolverService = invokerContextResolverService;
			this._logger = logger;
			this._config = config;
			this._extractor = extractor;
		}

		#region Enrichers

		protected virtual void InvokerContextEnricher(AuditEntry entry)
		{
			if (!this._config.InvokerContextEnricher) return;

			IPAddress ipAddress = this._invokerContextResolverService.RemoteIpAddress();
			entry.And(LoggingAuditService._invokerKey, new AuditableInvokerContext
			{
				IPAddress = ipAddress?.ToString(),
				IPAddressFamily = ipAddress?.AddressFamily.ToString(),
				RequestScheme = this._invokerContextResolverService.RequestScheme(),
				ClientCertificateSubjectName = this._invokerContextResolverService.ClientCertificateSubjectName(),
				ClientCertificateThumbpint = this._invokerContextResolverService.ClientCertificateThumbprint()
			});

		}

		protected virtual void PrincipalEnricher(AuditEntry entry)
		{
			if (!this._config.PrincipalEnricher) return;

			ClaimsPrincipal principal = this._currentPrincipalResolverService.CurrentPrincipal();
			entry.And(LoggingAuditService._principalKey, new AuditablePrincipal
			{
				Subject = this._extractor.SubjectGuid(principal),
				Name = this._extractor.Name(principal),
				Client = this._extractor.Client(principal)
			});
		}

		protected virtual void TimestampEnricher(AuditEntry entry)
		{
			if (!this._config.TimestampEnricher) return;
			entry.And(LoggingAuditService._timestampKey, DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture));
		}

		protected virtual AuditEntry Enrich(AuditEntry entry)
		{
			this.PrincipalEnricher(entry);
			this.TimestampEnricher(entry);
			return entry;
		}

		protected virtual AuditEntry EnrichIdentity(AuditEntry entry)
		{
			this.InvokerContextEnricher(entry);
			return entry;
		}

		#endregion

		#region Entry Builders

		protected virtual AuditEntry Build()
		{
			return this.Enrich(new AuditEntry());
		}

		protected virtual AuditEntry Build(string message)
		{
			return this.Enrich(new AuditEntry())
				.Message(message);
		}

		protected virtual AuditEntry Build(IEnumerable<Pair<string, object>> data)
		{
			AuditEntry entry = this.Enrich(new AuditEntry());
			if (data != null) foreach (Pair<String, Object> pair in data) entry.And(pair.Key, pair.Value);
			return entry;
		}

		protected virtual AuditEntry Build(IDictionary<string, object> data)
		{
			AuditEntry entry = this.Enrich(new AuditEntry());
			if (data != null) foreach (KeyValuePair<String, Object> pair in data) entry.And(pair.Key, pair.Value);
			return entry;
		}

		protected virtual AuditEntry Build(string message, IEnumerable<Pair<string, object>> data)
		{
			AuditEntry entry = this.Enrich(new AuditEntry())
				.Message(message);
			if (data != null) foreach (Pair<String, Object> pair in data) entry.And(pair.Key, pair.Value);
			return entry;
		}

		protected virtual AuditEntry Build(string message, IDictionary<string, object> data)
		{
			AuditEntry entry = this.Enrich(new AuditEntry())
				.Message(message);
			if (data != null) foreach (KeyValuePair<String, Object> pair in data) entry.And(pair.Key, pair.Value);
			return entry;
		}

		protected virtual AuditEntry Build(string message, object data)
		{
			return this.Enrich(new AuditEntry())
				.Message(message)
				.And(LoggingAuditService._varKey, data);
		}

		#endregion

		public Boolean IsEnabled { get { return this._config.Enable; } }

		public Boolean IsIdentityTrackingEnabled { get { return this._config.EnableIdentityTracking; } }

		public void Track(EventId action)
		{
			if (!this.IsEnabled) return;
			using (Serilog.Context.LogContext.PushProperty(this._config.AuditPropertyName, this._config.AuditPropertyValue))
			{
				this._logger.LogSafe(this._config.Level, action, this.Build());
			}
		}

		public void TrackIdentity(EventId action)
		{
			if (!this.IsEnabled) return;
			using (Serilog.Context.LogContext.PushProperty(this._config.IdentityTrackingPropertyName, this._config.IdentityTrackingPropertyValue))
			{
				this._logger.LogSafe(this._config.Level, action, this.EnrichIdentity(this.Build()));
			}
		}

		public void Track(EventId action, string message)
		{
			if (!this.IsEnabled) return;
			using (Serilog.Context.LogContext.PushProperty(this._config.AuditPropertyName, this._config.AuditPropertyValue))
			{
				this._logger.LogSafe(this._config.Level, action, this.Build(message));
			}
		}

		public void TrackIdentity(EventId action, string message)
		{
			if (!this.IsEnabled || !this.IsIdentityTrackingEnabled) return;
			using (Serilog.Context.LogContext.PushProperty(this._config.IdentityTrackingPropertyName, this._config.IdentityTrackingPropertyValue))
			{
				this._logger.LogSafe(this._config.Level, action, this.EnrichIdentity(this.Build(message)));
			}
		}

		public void Track(EventId action, string key, object data)
		{
			if (!this.IsEnabled) return;
			using (Serilog.Context.LogContext.PushProperty(this._config.AuditPropertyName, this._config.AuditPropertyValue))
			{
				this._logger.LogSafe(this._config.Level, action, this.Build(new Pair<String, Object>(key, data).AsArray()));
			}
		}

		public void TrackIdentity(EventId action, string key, object data)
		{
			if (!this.IsEnabled || !this.IsIdentityTrackingEnabled) return;
			using (Serilog.Context.LogContext.PushProperty(this._config.IdentityTrackingPropertyName, this._config.IdentityTrackingPropertyValue))
			{
				this._logger.LogSafe(this._config.Level, action, this.EnrichIdentity(this.Build(new Pair<String, Object>(key, data).AsArray())));
			}
		}

		public void Track(EventId action, IEnumerable<Pair<string, object>> data)
		{
			if (!this.IsEnabled) return;
			using (Serilog.Context.LogContext.PushProperty(this._config.AuditPropertyName, this._config.AuditPropertyValue))
			{
				this._logger.LogSafe(this._config.Level, action, this.Build(data));
			}
		}

		public void TrackIdentity(EventId action, IEnumerable<Pair<string, object>> data)
		{
			if (!this.IsEnabled || !this.IsIdentityTrackingEnabled) return;
			using (Serilog.Context.LogContext.PushProperty(this._config.IdentityTrackingPropertyName, this._config.IdentityTrackingPropertyValue))
			{
				this._logger.LogSafe(this._config.Level, action, this.EnrichIdentity(this.Build(data)));
			}
		}

		public void Track(EventId action, IDictionary<string, object> data)
		{
			if (!this.IsEnabled) return;
			using (Serilog.Context.LogContext.PushProperty(this._config.AuditPropertyName, this._config.AuditPropertyValue))
			{
				this._logger.LogSafe(this._config.Level, action, this.Build(data));
			}
		}

		public void TrackIdentity(EventId action, IDictionary<string, object> data)
		{
			if (!this.IsEnabled || !this.IsIdentityTrackingEnabled) return;
			using (Serilog.Context.LogContext.PushProperty(this._config.IdentityTrackingPropertyName, this._config.IdentityTrackingPropertyValue))
			{
				this._logger.LogSafe(this._config.Level, action, this.EnrichIdentity(this.Build(data)));
			}
		}

		public void Track(EventId action, string message, string key, object data)
		{
			if (!this.IsEnabled) return;
			using (Serilog.Context.LogContext.PushProperty(this._config.AuditPropertyName, this._config.AuditPropertyValue))
			{
				this._logger.LogSafe(this._config.Level, action, this.Build(message, new Pair<String, Object>(key, data).AsArray()));
			}
		}

		public void TrackIdentity(EventId action, string message, string key, object data)
		{
			if (!this.IsEnabled || !this.IsIdentityTrackingEnabled) return;
			using (Serilog.Context.LogContext.PushProperty(this._config.IdentityTrackingPropertyName, this._config.IdentityTrackingPropertyValue))
			{
				this._logger.LogSafe(this._config.Level, action, this.EnrichIdentity(this.Build(message, new Pair<String, Object>(key, data).AsArray())));
			}
		}

		public void Track(EventId action, string message, IEnumerable<Pair<string, object>> data)
		{
			if (!this.IsEnabled) return;
			using (Serilog.Context.LogContext.PushProperty(this._config.AuditPropertyName, this._config.AuditPropertyValue))
			{
				this._logger.LogSafe(this._config.Level, action, this.Build(message, data));
			}
		}

		public void TrackIdentity(EventId action, string message, IEnumerable<Pair<string, object>> data)
		{
			if (!this.IsEnabled || !this.IsIdentityTrackingEnabled) return;
			using (Serilog.Context.LogContext.PushProperty(this._config.IdentityTrackingPropertyName, this._config.IdentityTrackingPropertyValue))
			{
				this._logger.LogSafe(this._config.Level, action, this.EnrichIdentity(this.Build(message, data)));
			}
		}

		public void Track(EventId action, string message, IDictionary<string, object> data)
		{
			if (!this.IsEnabled) return;
			using (Serilog.Context.LogContext.PushProperty(this._config.AuditPropertyName, this._config.AuditPropertyValue))
			{
				this._logger.LogSafe(this._config.Level, action, this.Build(message, data));
			}
		}

		public void TrackIdentity(EventId action, string message, IDictionary<string, object> data)
		{
			if (!this.IsEnabled || !this.IsIdentityTrackingEnabled) return;
			using (Serilog.Context.LogContext.PushProperty(this._config.IdentityTrackingPropertyName, this._config.IdentityTrackingPropertyValue))
			{
				this._logger.LogSafe(this._config.Level, action, this.EnrichIdentity(this.Build(message, data)));
			}
		}
	}
}
