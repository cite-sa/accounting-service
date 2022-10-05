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
	public class ServiceUserBuilder : Builder<ServiceUser, Data.ServiceUser>
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private readonly TenantDbContext _dbContext;
		private Authorization.AuthorizationFlags _authorize = Authorization.AuthorizationFlags.None;

		public ServiceUserBuilder(
			TenantDbContext dbContext,
			QueryFactory queryFactory,
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ILogger<ServiceUserBuilder> logger,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
		}
		public ServiceUserBuilder Authorize(Authorization.AuthorizationFlags authorize) { this._authorize = authorize; return this; }

		public override async Task<List<ServiceUser>> Build(IFieldSet fields, IEnumerable<Data.ServiceUser> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Enumerable.Empty<ServiceUser>().ToList();

			IFieldSet serviceFields = fields.ExtractPrefixed(this.AsPrefix(nameof(ServiceUser.Service)));
			Dictionary<Guid, Service> serviceMap = await this.CollectServices(serviceFields, datas);

			IFieldSet userFields = fields.ExtractPrefixed(this.AsPrefix(nameof(ServiceUser.User)));
			Dictionary<Guid, User> userMap = await this.CollectUsers(userFields, datas);

			IFieldSet roleFields = fields.ExtractPrefixed(this.AsPrefix(nameof(ServiceUser.Role)));
			Dictionary<Guid, UserRole> roleMap = await this.CollectRoles(roleFields, datas);

			List<ServiceUser> models = new List<ServiceUser>();
			foreach (Data.ServiceUser d in datas)
			{
				ServiceUser m = new ServiceUser();
				if (fields.HasField(this.AsIndexer(nameof(ServiceUser.Hash)))) m.Hash = this.HashValue(d.UpdatedAt);
				if (fields.HasField(this.AsIndexer(nameof(ServiceUser.Id)))) m.Id = d.Id;
				if (fields.HasField(this.AsIndexer(nameof(ServiceUser.CreatedAt)))) m.CreatedAt = d.CreatedAt;
				if (fields.HasField(this.AsIndexer(nameof(ServiceUser.UpdatedAt)))) m.UpdatedAt = d.UpdatedAt;
				if (!serviceFields.IsEmpty() && serviceMap != null && serviceMap.ContainsKey(d.ServiceId)) m.Service = serviceMap[d.ServiceId];
				if (!userFields.IsEmpty() && userMap != null && userMap.ContainsKey(d.UserId)) m.User = userMap[d.UserId];
				if (!roleFields.IsEmpty() && roleMap != null && roleMap.ContainsKey(d.RoleId)) m.Role = roleMap[d.RoleId];

				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		private async Task<Dictionary<Guid, Service>> CollectServices(IFieldSet fields, IEnumerable<Data.ServiceUser> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(Service));

			Dictionary<Guid, Service> itemMap = null;
			if (!fields.HasOtherField(this.AsIndexer(nameof(Service.Id)))) itemMap = this.AsEmpty(datas.Select(x => x.ServiceId).Distinct(), x => new Service() { Id = x }, x => x.Id.Value);
			else
			{
				IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(Service.Id));
				ServiceQuery q = this._queryFactory.Query<ServiceQuery>().Authorize(this._authorize).DisableTracking().Ids(datas.Select(x => x.ServiceId).Distinct());
				itemMap = await this._builderFactory.Builder<ServiceBuilder>().Authorize(this._authorize).AsForeignKey(q, clone, x => x.Id.Value);
			}
			if (!fields.HasField(nameof(Service.Id))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Id = null);

			return itemMap;
		}

		private async Task<Dictionary<Guid, User>> CollectUsers(IFieldSet fields, IEnumerable<Data.ServiceUser> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(User));

			Dictionary<Guid, User> itemMap = null;
			if (!fields.HasOtherField(this.AsIndexer(nameof(User.Id)))) itemMap = this.AsEmpty(datas.Select(x => x.UserId).Distinct(), x => new User() { Id = x }, x => x.Id.Value);
			else
			{
				IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(User.Id));
				UserQuery q = this._queryFactory.Query<UserQuery>().DisableTracking().Ids(datas.Select(x => x.UserId).Distinct());
				itemMap = await this._builderFactory.Builder<UserBuilder>().Authorize(this._authorize).AsForeignKey(q, clone, x => x.Id.Value);
			}
			if (!fields.HasField(nameof(User.Id))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Id = null);

			return itemMap;
		}

		private async Task<Dictionary<Guid, UserRole>> CollectRoles(IFieldSet fields, IEnumerable<Data.ServiceUser> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(UserRole));

			Dictionary<Guid, UserRole> itemMap = null;
			if (!fields.HasOtherField(this.AsIndexer(nameof(UserRole.Id)))) itemMap = this.AsEmpty(datas.Select(x => x.RoleId).Distinct(), x => new UserRole() { Id = x }, x => x.Id.Value);
			else
			{
				IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(UserRole.Id));
				UserRoleQuery q = this._queryFactory.Query<UserRoleQuery>().DisableTracking().Ids(datas.Select(x => x.RoleId).Distinct());
				itemMap = await this._builderFactory.Builder<UserRoleBuilder>().Authorize(this._authorize).AsForeignKey(q, clone, x => x.Id.Value);
			}
			if (!fields.HasField(nameof(UserRole.Id))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Id = null);

			return itemMap;
		}
	}
}
