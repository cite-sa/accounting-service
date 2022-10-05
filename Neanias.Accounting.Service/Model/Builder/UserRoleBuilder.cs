using Neanias.Accounting.Service.Convention;
using Neanias.Accounting.Service.Data.Context;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Query;
using Neanias.Accounting.Service.Authorization;

namespace Neanias.Accounting.Service.Model
{
	public class UserRoleBuilder : Builder<UserRole, Data.UserRole>
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private readonly TenantDbContext _dbContext;
		private Authorization.AuthorizationFlags _authorize = Authorization.AuthorizationFlags.None;

		public UserRoleBuilder(
			TenantDbContext dbContext,
			QueryFactory queryFactory,
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ILogger<UserRoleBuilder> logger,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
		}
		public UserRoleBuilder Authorize(Authorization.AuthorizationFlags authorize) { this._authorize = authorize; return this; }

		public override Task<List<UserRole>> Build(IFieldSet fields, IEnumerable<Data.UserRole> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Task.FromResult(Enumerable.Empty<UserRole>().ToList());

			List<UserRole> models = new List<UserRole>();
			foreach (Data.UserRole d in datas)
			{
				UserRole m = new UserRole();
				if (fields.HasField(this.AsIndexer(nameof(UserRole.Hash)))) m.Hash = this.HashValue(d.UpdatedAt);
				if (fields.HasField(this.AsIndexer(nameof(UserRole.Id)))) m.Id = d.Id;
				if (fields.HasField(this.AsIndexer(nameof(UserRole.Rights)))) m.Rights = d.Rights;
				if (fields.HasField(this.AsIndexer(nameof(UserRole.Propagate)))) m.Propagate = d.Propagate;
				if (fields.HasField(this.AsIndexer(nameof(UserRole.Name)))) m.Name = d.Name;
				if (fields.HasField(this.AsIndexer(nameof(UserRole.IsActive)))) m.IsActive = d.IsActive;
				if (fields.HasField(this.AsIndexer(nameof(UserRole.CreatedAt)))) m.CreatedAt = d.CreatedAt;
				if (fields.HasField(this.AsIndexer(nameof(UserRole.UpdatedAt)))) m.UpdatedAt = d.UpdatedAt;

				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return Task.FromResult(models);
		}
	}
}
