﻿using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Data.Context;
using Cite.Tools.Data.Deleter;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Model
{
	public class WhatYouKnowAboutMeDeleter : IDeleter
	{
		private readonly TenantDbContext _dbContext;
		private readonly ILogger<WhatYouKnowAboutMeDeleter> _logger;

		public WhatYouKnowAboutMeDeleter(
			TenantDbContext dbContext,
			ILogger<WhatYouKnowAboutMeDeleter> logger)
		{
			this._logger = logger;
			this._dbContext = dbContext;
		}

		public async Task DeleteAndSave(IEnumerable<Data.WhatYouKnowAboutMe> datas)
		{
			this._logger.Debug("will delete {0} items", datas?.Count());
			this.Delete(datas);
			this._logger.Trace("saving changes");
			await this._dbContext.SaveChangesAsync();
			this._logger.Trace("changes saved");
		}

		public void Delete(IEnumerable<Data.WhatYouKnowAboutMe> datas)
		{
			this._logger.Debug("will delete {0} items", datas?.Count());
			if (datas == null || !datas.Any()) return;

			foreach (Data.WhatYouKnowAboutMe item in datas)
			{
				this._logger.Trace("deleting item {id}", item.Id);
				item.UpdatedAt = DateTime.UtcNow;
				item.IsActive = IsActive.Inactive;
				this._logger.Trace("updating item");
				this._dbContext.Update(item);
				this._logger.Trace("updated item");
			}
		}
	}
}
