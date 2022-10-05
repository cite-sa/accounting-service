using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.Locale;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neanias.Accounting.Service.Authorization
{
	public class UserScope
	{
		private readonly ILocaleService _localeService;
		private readonly TenantDbContext _dbContext;
		public UserScope(TenantDbContext dbContext, ILocaleService localeService)
		{
			this._localeService = localeService ?? throw new ArgumentNullException(nameof(localeService));
			this._dbContext = dbContext;
		}

		private Guid? _userId { get; set; }
		private Data.UserProfile _profile { get; set; }
		private Data.User _user { get; set; }


		public Guid? UserId
		{
			get
			{
				return this._userId;
			}
		}

		public Boolean IsSet
		{
			get
			{
				return this._userId.HasValue;
			}
		}

		public void Set(Guid userId)
		{
			this._userId = userId;
		}

		private Data.User GetUser()
		{
			if (_user == null && this.IsSet)
			{
				_user = this._dbContext.Users.Find(this._userId.Value);
			}
			return _user;
		}

		private Data.UserProfile GetUserProfile()
		{
			if (_profile == null && this.IsSet)
			{
				Data.User user = this.GetUser();
				_profile = user != null ? this._dbContext.UserProfiles.Find(user.ProfileId) : null;
			}
			return _profile;
		}


		public string Timezone()
		{
			if (this.IsSet) 
			{
				Data.UserProfile profile = this.GetUserProfile();
				if (profile != null) return profile.Timezone;
			} 
			return this._localeService.TimezoneName();
		}

		public string Culture()
		{
			if (this.IsSet)
			{
				Data.UserProfile profile = this.GetUserProfile();
				if (profile != null) return profile.Culture;
			}
			return this._localeService.CultureName();
		}

		public string Language()
		{
			if (this.IsSet)
			{
				Data.UserProfile profile = this.GetUserProfile();
				if (profile != null) return profile.Language;
			}
			return this._localeService.Language();
		}
	}
}
