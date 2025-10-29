using Cite.Tools.FieldSet;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.WhatYouKnowAboutMe
{
	public interface IWhatYouKnowAboutMeService
	{
		Task<Model.WhatYouKnowAboutMe> PersistAsync(Model.WhatYouKnowAboutMeIntegrationPersist model, IFieldSet fields = null);
		Task DeleteAndSaveAsync(Guid id);
	}
}
