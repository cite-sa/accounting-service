using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.ErrorCode;
using Cite.Tools.Exception;
using Cite.Tools.Logging.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Data.Context
{
	public class TenantDbContext : AppDbContext
	{
		private readonly TenantScope _scope;
		private readonly ILogger<TenantDbContext> _logger;

		public TenantDbContext(
			DbContextOptions<TenantDbContext> options,
			DbProviderConfig config,
			ILogger<TenantDbContext> logger,
			ErrorThesaurus errors,
			TenantScope scope) : base(options, config, errors)
		{
			this._scope = scope;
			this._logger = logger;
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			if (!this._scope.IsSet)
			{
				this._logger.Critical("scope not set");
				throw new MyForbiddenException(this._errors.MissingTenant.Code, this._errors.MissingTenant.Message);
			}
			if (this._scope.IsMultitenant)
			{
				modelBuilder.Entity<ForgetMe>().HasQueryFilter(x => x.TenantId == this._scope.Tenant);
				modelBuilder.Entity<TenantConfiguration>().HasQueryFilter(x => x.TenantId == this._scope.Tenant);
				modelBuilder.Entity<Tenant>().HasQueryFilter(x => x.Id == this._scope.Tenant);
				modelBuilder.Entity<User>().HasQueryFilter(x => x.TenantId == this._scope.Tenant);
				modelBuilder.Entity<UserProfile>().HasQueryFilter(x => x.TenantId == this._scope.Tenant);
				modelBuilder.Entity<StorageFile>().HasQueryFilter(x => x.TenantId == this._scope.Tenant);
				modelBuilder.Entity<ServiceResource>().HasQueryFilter(x => x.TenantId == this._scope.Tenant);
				modelBuilder.Entity<ServiceUser>().HasQueryFilter(x => x.TenantId == this._scope.Tenant);
				modelBuilder.Entity<UserRole>().HasQueryFilter(x => x.TenantId == this._scope.Tenant);
				modelBuilder.Entity<Service>().HasQueryFilter(x => x.TenantId == this._scope.Tenant);
				modelBuilder.Entity<Metric>().HasQueryFilter(x => x.TenantId == this._scope.Tenant);
				modelBuilder.Entity<ServiceAction>().HasQueryFilter(x => x.TenantId == this._scope.Tenant);
				modelBuilder.Entity<ServiceSync>().HasQueryFilter(x => x.TenantId == this._scope.Tenant);
				modelBuilder.Entity<ServiceResetEntrySync>().HasQueryFilter(x => x.TenantId == this._scope.Tenant);
				modelBuilder.Entity<UserSettings>().HasQueryFilter(x => x.TenantId == this._scope.Tenant);
			}
		}

		public override int SaveChanges(bool acceptAllChangesOnSuccess)
		{
			this.EnforceTenant();
			return base.SaveChanges(acceptAllChangesOnSuccess);
		}

		public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
		{
			this.EnforceTenant();
			return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
		}

		private void EnforceTenant()
		{
			if (this.ChangeTracker.Entries<Tenant>().Any(x => x.State == EntityState.Added || x.State == EntityState.Deleted || x.State == EntityState.Modified))
			{
				this._logger.Critical("somebody tried to set a tenant explicitly");
				throw new MyForbiddenException("tenant tampering");
			}

			foreach (var entry in this.ChangeTracker.Entries())
			{
				if (!(entry.Entity is ITenantScoped)) continue;
				switch (entry.State)
				{
					case EntityState.Added:
						{
							if (this._scope.IsMultitenant) entry.Property("TenantId").CurrentValue = this._scope.Tenant;
							else entry.Property("TenantId").CurrentValue = null;
							break;
						}
					case EntityState.Deleted:
					case EntityState.Modified:
						{
							Guid? currentValue = entry.Property("TenantId").CurrentValue as Guid?;
							if (this._scope.IsMultitenant)
							{
								if (!currentValue.HasValue)
								{
									this._logger.Critical("somebody tried to set null tenant");
									throw new MyForbiddenException("tenant tampering");
								}
								if (currentValue.Value != this._scope.Tenant)
								{
									this._logger.Critical("somebody tried to change an entries tenant");
									throw new MyForbiddenException("tenant tampering");
								}
							}
							else
							{
								if (currentValue.HasValue)
								{
									this._logger.Critical("somebody tried to set non null tenant");
									throw new MyForbiddenException("tenant tampering");
								}
							}
							break;
						}
					default: break;
				}
			}
		}
	}
}
