using Elastic.Clients.Elasticsearch;

namespace Cite.Accounting.Service.Elastic.Base.Query.Models
{
	public class OrderingField
	{
		public Field Field { get; set; }
		public FieldSort FieldSort { get; set; }
	}
}
