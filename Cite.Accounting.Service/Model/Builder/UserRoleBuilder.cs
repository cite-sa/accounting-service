using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Convention;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Model
{
	public class UserRoleBuilder : Builder<UserRole, Data.UserRole>
	{
		private Authorization.AuthorizationFlags _authorize = Authorization.AuthorizationFlags.None;
		public UserRoleBuilder(
			IConventionService conventionService,
			ILogger<UserRoleBuilder> logger,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
		}
		public UserRoleBuilder Authorize() { return this; }
		public UserRoleBuilder Authorize(Authorization.AuthorizationFlags authorize) { this._authorize = authorize; return this; }
		public override Task<List<UserRole>> Build(IFieldSet fields, IEnumerable<Data.UserRole> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Task.FromResult(Enumerable.Empty<UserRole>().ToList());

			List<UserRole> models = new List<UserRole>();
			foreach (Data.UserRole d in datas ?? new List<Data.UserRole>())
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
