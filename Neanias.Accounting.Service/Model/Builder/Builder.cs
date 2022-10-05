using Neanias.Accounting.Service.Convention;
using Cite.Tools.Common.Extensions;
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
using Neanias.Accounting.Service.Authorization;
using System.Reflection;

namespace Neanias.Accounting.Service.Model
{
	public abstract class Builder<M, D> : IBuilder where D : class
	{
		public Builder(
			IConventionService conventionService,
			ILogger logger,
			IPermissionProvider permissionProvider)
		{
			this._conventionService = conventionService;
			this._logger = logger;
			this._permissionProvider = permissionProvider;
		}

		protected readonly IConventionService _conventionService;
		protected readonly ILogger _logger;
		protected readonly IPermissionProvider _permissionProvider;

		public async Task<M> Build(IFieldSet directives, D data)
		{
			if (data == null)
			{
				this._logger.Debug(new MapLogEntry("requested build for null item requesting fields").And("fields", directives));
				return default(M);
			}
			List<M> models = await this.Build(directives, new D[] { data });
			return models.FirstOrDefault();
		}

		public abstract Task<List<M>> Build(IFieldSet directives, IEnumerable<D> datas);

		public async Task<Dictionary<K, M>> AsForeignKey<K>(Query<D> query, IFieldSet directives, Func<M, K> keySelector)
		{
			this._logger.Trace("Building references from query");
			List<D> datas = await query.CollectAsAsync(directives);
			this._logger.Debug("collected {count} items to build", datas?.Count);
			return await this.AsForeignKey(datas, directives, keySelector);
		}

		public async Task<Dictionary<K, M>> AsForeignKey<K>(IEnumerable<D> datas, IFieldSet directives, Func<M, K> keySelector)
		{
			this._logger.Trace("building references");
			List<M> models = await this.Build(directives, datas);
			this._logger.Debug("mapping {count} build items from {countdata} requested", models?.Count, datas?.Count());
			Dictionary<K, M> map = models.ToDictionary(keySelector);
			return map;
		}

		public async Task<Dictionary<K, List<M>>> AsMasterKey<K>(Query<D> query, IFieldSet directives, Func<M, K> keySelector)
		{
			this._logger.Trace("Building details from query");
			List<D> datas = await query.CollectAsAsync(directives);
			this._logger.Debug("collected {count} items to build", datas?.Count);
			return await this.AsMasterKey(datas, directives, keySelector);
		}

		public async Task<Dictionary<K, List<M>>> AsMasterKey<K>(IEnumerable<D> datas, IFieldSet directives, Func<M, K> keySelector)
		{
			this._logger.Trace("building details");
			List<M> models = await this.Build(directives, datas);
			this._logger.Debug("mapping {count} build items from {countdata} requested", models?.Count, datas?.Count());
			Dictionary<K, List<M>> map = new Dictionary<K, List<M>>();
			foreach (M model in models)
			{
				K key = keySelector.Invoke(model);
				if (!map.ContainsKey(key)) map.Add(key, new List<M>());
				map[key].Add(model);
			}
			return map;
		}

		public Dictionary<FK, FM> AsEmpty<FK, FM>(IEnumerable<FK> keys, Func<FK, FM> mapper, Func<FM, FK> keySelector)
		{
			this._logger.Trace("building static references");
			IEnumerable<FM> models = keys.Select(mapper);
			this._logger.Debug("mapping {count} build items from {countdata} requested", models?.Count(), keys?.Count());
			Dictionary<FK, FM> map = models.ToDictionary(keySelector);
			return map;
		}

		protected String HashValue(DateTime value)
		{
			return this._conventionService.HashValue(value);
		}

		protected String AsPrefix(String name)
		{
			return name.AsIndexerPrefix();
		}

		protected String AsIndexer(params String[] names)
		{
			return names.AsIndexer();
		}

		protected HashSet<String> ExtractAuthorizationFlags(IFieldSet fields, String propertyName)
		{
			IFieldSet authorizationFlags = fields.ExtractPrefixed(this.AsPrefix(propertyName));
			List<String> allPermission = this._permissionProvider.GetPermissionValues();
			HashSet<String> authorizationPermissionFlags = allPermission.Where(x => authorizationFlags.Fields.Contains(x.ToLowerInvariant())).ToHashSet();
			return authorizationPermissionFlags;
		}

		protected async Task<List<String>> EvaluateAuthorizationFlags(IAuthorizationService authorizationService, IEnumerable<String> authorizationFlags, AffiliatedResource affiliatedResource)
		{
			List<String> allowed = new List<String>();
			foreach (String permission in authorizationFlags)
			{
				Boolean isAllowed = await authorizationService.AuthorizeOrAffiliated(affiliatedResource, permission);
				if (isAllowed) allowed.Add(permission);
			}
			return allowed;
		}
	}
}
