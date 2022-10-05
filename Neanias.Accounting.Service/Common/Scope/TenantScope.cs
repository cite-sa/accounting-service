using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Common
{
	public class TenantScope
	{
		public TenantScope(MultitenancyMode multitenancy)
		{
			this._multitenancy = multitenancy;
		}

		private MultitenancyMode _multitenancy { get; set; }
		private Guid? _tenant { get; set; }

		public Boolean IsMultitenant { get { return this._multitenancy.IsMultitenant; } }

		public Guid Tenant
		{
			get
			{
				if (!this.IsMultitenant) return Guid.Empty;
				if (!this._tenant.HasValue) throw new InvalidOperationException("tenant not set");
				return this._tenant.Value;
			}
		}

		public Boolean IsSet
		{
			get
			{
				if (!this.IsMultitenant) return true;
				return this._tenant.HasValue;
			}
		}

		public void Set(Guid tenant)
		{
			if (!this.IsMultitenant) this._tenant = Guid.Empty;
			else this._tenant = tenant;
		}
	}
}
