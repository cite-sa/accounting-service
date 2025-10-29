using Cite.Accounting.Service.Elastic.Base.Client;

namespace Cite.Accounting.Service.Elastic.Client
{
	public class AppElasticClientConfig : BaseElasticClientConfig
	{
		public Index AccountingEntryIndex { get; set; }
		public Index OrphanFolderIndex { get; set; }
		public Index UserInfoIndex { get; set; }
	}
}
