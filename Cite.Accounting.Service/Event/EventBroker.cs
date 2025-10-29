using Cite.Accounting.Service.Common;
using Cite.Tools.Common.Extensions;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Event
{
	public class EventBroker
	{
		#region User Touched

		private EventHandler<OnUserTouchedArgs> _userTouched;
		public event EventHandler<OnUserTouchedArgs> UserTouched
		{
			add { this._userTouched += value; }
			remove { this._userTouched -= value; }
		}

		public void EmitUserTouched(Guid tenantId, Guid userId, String subject, String issuer, String prevSubject, String prevIssuer)
		{
			this.EmitUserTouched(this, tenantId, userId, subject, issuer, prevSubject, prevIssuer);
		}

		public void EmitUserTouched(Object sender, Guid tenantId, Guid userId, String subject, String issuer, String prevSubject, String prevIssuer)
		{
			this._userTouched?.Invoke(sender, new OnUserTouchedArgs(tenantId, userId, subject, issuer, prevSubject, prevIssuer));
		}

		#endregion

		#region Api Key Removed

		private EventHandler<OnApiKeyRemovedArgs> _apiKeyRemoved;
		public event EventHandler<OnApiKeyRemovedArgs> ApiKeyRemoved
		{
			add { this._apiKeyRemoved += value; }
			remove { this._apiKeyRemoved -= value; }
		}

		public void EmitApiKeyRemoved(Guid tenantId, Guid userId, String apiKeyHash)
		{
			this.EmitApiKeyRemoved(this, tenantId, userId, apiKeyHash);
		}

		public void EmitApiKeyRemoved(IEnumerable<OnApiKeyRemovedArgs> events)
		{
			this.EmitApiKeyRemoved(this, events);
		}

		public void EmitApiKeyRemoved(Object sender, Guid tenantId, Guid userId, String apiKeyHash)
		{
			this._apiKeyRemoved?.Invoke(sender, new OnApiKeyRemovedArgs(tenantId, userId, apiKeyHash));
		}

		public void EmitApiKeyRemoved(Object sender, IEnumerable<OnApiKeyRemovedArgs> events)
		{
			if (events == null) return;
			foreach (OnApiKeyRemovedArgs ev in events) this._apiKeyRemoved?.Invoke(sender, ev);
		}

		#endregion

		#region Tenant Configuration Touched

		private EventHandler<OnTenantConfigurationTouchedArgs> _tenantConfigurationTouch;
		public event EventHandler<OnTenantConfigurationTouchedArgs> TenantConfigurationTouched
		{
			add { this._tenantConfigurationTouch += value; }
			remove { this._tenantConfigurationTouch -= value; }
		}

		public void EmitTenantConfigurationTouched(Guid tenantId, TenantConfigurationType type)
		{
			this.EmitTenantConfigurationTouched(this, tenantId, type);
		}

		public void EmitTenantConfigurationTouched(Object sender, Guid tenantId, TenantConfigurationType type)
		{
			this._tenantConfigurationTouch?.Invoke(sender, new OnTenantConfigurationTouchedArgs(tenantId, type));
		}

		#endregion

		#region Tenant Configuration Deleted

		private EventHandler<OnTenantConfigurationDeletedArgs> _tenantConfigurationDeleted;
		public event EventHandler<OnTenantConfigurationDeletedArgs> TenantConfigurationDeleted
		{
			add { this._tenantConfigurationDeleted += value; }
			remove { this._tenantConfigurationDeleted -= value; }
		}

		public void EmitTenantConfigurationDeleted(Guid tenantId, TenantConfigurationType type)
		{
			this.EmitTenantConfigurationDeleted(this, tenantId, type);
		}

		public void EmitTenantConfigurationDeleted(Object sender, Guid tenantId, TenantConfigurationType type)
		{
			this._tenantConfigurationDeleted?.Invoke(sender, new OnTenantConfigurationDeletedArgs(tenantId, type));
		}

		#endregion

		#region Tenant Code Touched

		private EventHandler<OnTenantCodeTouchedArgs> _tenantCodeTouched;
		public event EventHandler<OnTenantCodeTouchedArgs> TenantCodeTouched
		{
			add { this._tenantCodeTouched += value; }
			remove { this._tenantCodeTouched -= value; }
		}

		public void EmitTenantCodeTouched(Guid tenantId, String existingTenanetCode, String updatedTenanetCode)
		{
			this.EmitTenantCodeTouched(this, tenantId, existingTenanetCode, updatedTenanetCode);
		}

		public void EmitTenantCodeTouched(OnTenantCodeTouchedArgs events)
		{
			this.EmitTenantCodeTouched(this, events.AsList());
		}

		public void EmitTenantCodeTouched(IEnumerable<OnTenantCodeTouchedArgs> events)
		{
			this.EmitTenantCodeTouched(this, events);
		}

		public void EmitTenantCodeTouched(Object sender, Guid tenantId, String existingTenanetCode, String updatedTenanetCode)
		{
			this._tenantCodeTouched?.Invoke(sender, new OnTenantCodeTouchedArgs(tenantId, existingTenanetCode, updatedTenanetCode));
		}

		public void EmitTenantCodeTouched(Object sender, IEnumerable<OnTenantCodeTouchedArgs> events)
		{
			if (events == null) return;
			foreach (OnTenantCodeTouchedArgs ev in events) this._tenantCodeTouched?.Invoke(sender, ev);
		}

		#endregion

		#region Role Touched

		private EventHandler<OnUserRoleTouchedArgs> _userRoleTouched;
		public event EventHandler<OnUserRoleTouchedArgs> UserRoleTouched
		{
			add { this._userRoleTouched += value; }
			remove { this._userRoleTouched -= value; }
		}

		public void EmitUserRoleTouched(Guid tenantId, Guid roleId)
		{
			this.EmitUserRoleTouched(this, tenantId, roleId);
		}

		public void EmitUserRoleTouched(OnUserRoleTouchedArgs events)
		{
			this.EmitUserRoleTouched(this, events.AsList());
		}

		public void EmitUserRoleTouched(IEnumerable<OnUserRoleTouchedArgs> events)
		{
			this.EmitUserRoleTouched(this, events);
		}

		public void EmitUserRoleTouched(Object sender, Guid tenantId, Guid roleId)
		{
			this._userRoleTouched?.Invoke(sender, new OnUserRoleTouchedArgs(tenantId, roleId));
		}

		public void EmitUserRoleTouched(Object sender, IEnumerable<OnUserRoleTouchedArgs> events)
		{
			if (events == null) return;
			foreach (OnUserRoleTouchedArgs ev in events) this._userRoleTouched?.Invoke(sender, ev);
		}

		#endregion

		#region Tenant Deleted

		private EventHandler<OnTenantDeletedArgs> _tenantDeleted;
		public event EventHandler<OnTenantDeletedArgs> TenantDeleted
		{
			add { this._tenantDeleted += value; }
			remove { this._tenantDeleted -= value; }
		}

		public void EmitTenantDeleted(Guid tenantId)
		{
			this.EmitTenantDeleted(this, tenantId);
		}

		public void EmitTenantDeleted(IEnumerable<OnTenantDeletedArgs> events)
		{
			this.EmitTenantDeleted(this, events);
		}

		public void EmitTenantDeleted(Object sender, Guid tenantId)
		{
			this._tenantDeleted?.Invoke(sender, new OnTenantDeletedArgs(tenantId));
		}

		public void EmitTenantDeleted(Object sender, IEnumerable<OnTenantDeletedArgs> events)
		{
			if (events == null) return;
			foreach (OnTenantDeletedArgs ev in events) this._tenantDeleted?.Invoke(sender, ev);
		}

		#endregion
	}
}
