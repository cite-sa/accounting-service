using System.Collections.Generic;

namespace Cite.Accounting.Service.Web.Tasks.QueuePublisher.MessageStorage
{
	public interface IMessageStorage<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
	{
		void Add(TKey key, TValue value);
		TValue LookupKey(TKey key);
		IEnumerable<KeyValuePair<TKey, TValue>> LookupKeyRange(IEnumerable<TKey> keys);
		TKey LookupValue(TValue value);
		IEnumerable<KeyValuePair<TKey, TValue>> LookupValueRange(IEnumerable<TValue> values);
		TValue PurgeByKey(TKey key);
		TKey PurgeByValue(TValue value);
	}
}
