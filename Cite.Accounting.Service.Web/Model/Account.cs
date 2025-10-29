using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Query;
using Cite.Tools.Auth.Claims;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.Model
{
	public class Account
	{
		public class ProfileInfo
		{
			public Guid? Tenant { get; set; }
			public String Timezone { get; set; }
			public String Culture { get; set; }
			public String Language { get; set; }
		}

		public class ClaimInfo
		{
			public List<String> Roles { get; set; }
		}

		public class PrincipalInfo
		{
			public Guid? Subject { get; set; }
			public Guid? UserId { get; set; }
			[LogSensitive]
			public String Name { get; set; }
			public Boolean CanManageAnySevice { get; set; }
			public List<String> Scope { get; set; }
			public String Client { get; set; }
			public DateTime? NotBefore { get; set; }
			public DateTime? AuthenticatedAt { get; set; }
			public DateTime? ExpiresAt { get; set; }
			[LogSensitive]
			public Dictionary<String, List<String>> More { get; set; }
		}

		public Boolean IsAuthenticated { get; set; }
		public PrincipalInfo Principal { get; set; }
		public ClaimInfo Claims { get; set; }
		public List<String> Permissions { get; set; }
		public ProfileInfo Profile { get; set; }
	}

	public class AccountBuilder
	{
		private readonly IQueryingService _queryingService;
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private readonly IPermissionPolicyService _permissionPolicyService;
		private readonly IAuthorizationService _authorizationService;
		private readonly ClaimExtractor _extractor;

		public AccountBuilder(
			IQueryingService queryingService,
			QueryFactory queryFactory,
			BuilderFactory builderFactory,
			IPermissionPolicyService permissionPolicyService,
			IAuthorizationService authorizationService,
			ClaimExtractor extractor)
		{
			this._queryingService = queryingService;
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
			this._permissionPolicyService = permissionPolicyService;
			this._authorizationService = authorizationService;
			this._extractor = extractor;
		}

		private List<String> _additionalClaimKeys;
		private List<String> AdditionalClaimKeys
		{
			get
			{
				if (this._additionalClaimKeys == null)
				{
					this._additionalClaimKeys = this._extractor.KnownPublicKeys.Except(new String[] {
						ClaimExtractorKeys.Subject,
						ClaimExtractorKeys.Name,
						ClaimExtractorKeys.Scope,
						ClaimExtractorKeys.Client,
						ClaimExtractorKeys.NotBefore,
						ClaimExtractorKeys.AuthenticatedAt,
						ClaimExtractorKeys.ExpiresAt,
						ClaimExtractorKeys.Roles,
						ClaimExtractorKeys.Tenant
					}).ToList();
				}
				return this._additionalClaimKeys;
			}
		}

		//TODO: depending on the requested fields, we might be able to remove some data collection. add some field checking conditions
		public async Task<Account> Build(IFieldSet fields, ClaimsPrincipal principal)
		{
			Account model = new Account();
			if (principal == null)
			{
				model.IsAuthenticated = false;
				return model;
			}
			else model.IsAuthenticated = true;

			Cite.Accounting.Service.Model.User user = null;

			Guid? subjectId = this._extractor.SubjectGuid(principal);

			if (subjectId.HasValue)
			{
				IFieldSet userFields = new FieldSet(
					nameof(Cite.Accounting.Service.Model.User.Id),
					new String[] { nameof(Cite.Accounting.Service.Model.User.Profile), nameof(Cite.Accounting.Service.Model.UserProfile.Timezone) }.AsIndexer(),
					new String[] { nameof(Cite.Accounting.Service.Model.User.Profile), nameof(Cite.Accounting.Service.Model.UserProfile.Language) }.AsIndexer(),
					new String[] { nameof(Cite.Accounting.Service.Model.User.Profile), nameof(Cite.Accounting.Service.Model.UserProfile.Culture) }.AsIndexer());
				user = await this._queryingService.FirstAsAsync(
					this._queryFactory.Query<UserQuery>()
						.DisableTracking()
						.Subject(subjectId.Value.ToString()),
					this._builderFactory.Builder<Cite.Accounting.Service.Model.UserBuilder>(),
					userFields);
			}

			IFieldSet principalFields = fields.ExtractPrefixed(nameof(Account.Principal).AsIndexerPrefix());
			IFieldSet profileFields = fields.ExtractPrefixed(nameof(Account.Profile).AsIndexerPrefix());
			IFieldSet claimFields = fields.ExtractPrefixed(nameof(Account.Claims).AsIndexerPrefix());

			if (!principalFields.IsEmpty()) model.Principal = new Account.PrincipalInfo();
			if (principalFields.HasField(nameof(Account.Principal.Subject))) model.Principal.Subject = subjectId;
			if (principalFields.HasField(nameof(Account.Principal.UserId))) model.Principal.UserId = user?.Id;
			if (principalFields.HasField(nameof(Account.Principal.Name))) model.Principal.Name = this._extractor.Name(principal);
			if (principalFields.HasField(nameof(Account.Principal.Scope))) model.Principal.Scope = this._extractor.Scope(principal);
			if (principalFields.HasField(nameof(Account.Principal.Client))) model.Principal.Client = this._extractor.Client(principal);
			if (principalFields.HasField(nameof(Account.Principal.NotBefore))) model.Principal.NotBefore = this._extractor.NotBefore(principal);
			if (principalFields.HasField(nameof(Account.Principal.AuthenticatedAt))) model.Principal.AuthenticatedAt = this._extractor.AuthenticatedAt(principal);
			if (principalFields.HasField(nameof(Account.Principal.ExpiresAt))) model.Principal.ExpiresAt = this._extractor.ExpiresAt(principal);
			if (principalFields.HasField(nameof(Account.Principal.More)))
			{
				model.Principal.More = new Dictionary<string, List<string>>();
				foreach (String key in this.AdditionalClaimKeys)
				{
					if (!model.Principal.More.ContainsKey(key)) model.Principal.More.Add(key, new List<string>());
					model.Principal.More[key].AddRange(this._extractor.AsStrings(principal, key));
				}
			}
			if (principalFields.HasField(nameof(Account.PrincipalInfo.CanManageAnySevice)))
			{
				if (await this._authorizationService.Authorize(Permission.EditService))
				{
					model.Principal.CanManageAnySevice = true;
				}
				else
				{
					int allowedToEditServices = await this._queryFactory.Query<ServiceQuery>().IsActive(Accounting.Service.Common.IsActive.Active).DisableTracking().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice).Permissions(Permission.EditService).CountAsync();
					model.Principal.CanManageAnySevice = allowedToEditServices > 0;
				}
			}
			if (!claimFields.IsEmpty()) model.Claims = new Account.ClaimInfo();
			if (claimFields.HasField(nameof(Account.Claims.Roles))) model.Claims.Roles = this._extractor.Roles(principal);

			if (fields.HasField(nameof(Account.Permissions))) model.Permissions = new List<string>(this._permissionPolicyService.PermissionsOf(this._extractor.Roles(principal)));

			if (!profileFields.IsEmpty()) model.Profile = new Account.ProfileInfo();
			if (profileFields.HasField(nameof(Account.Profile.Tenant))) model.Profile.Tenant = this._extractor.TenantGuid(principal);
			if (profileFields.HasField(nameof(Account.Profile.Language))) model.Profile.Language = user?.Profile?.Language;
			if (profileFields.HasField(nameof(Account.Profile.Timezone))) model.Profile.Timezone = user?.Profile?.Timezone;
			if (profileFields.HasField(nameof(Account.Profile.Culture))) model.Profile.Culture = user?.Profile?.Culture;

			return model;
		}
	}
}
