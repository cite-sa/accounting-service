using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Cite.Accounting.Service.Web.Tasks.QueuePublisher.MessageStorage
{
	public class InverseLookupMessageStorage<TKey, TValue> : IMessageStorage<TKey, TValue>
	{
		private readonly ConcurrentDictionary<TKey, TValue> _lookup = new ConcurrentDictionary<TKey, TValue>();
		private readonly ConcurrentDictionary<TValue, TKey> _inverseLookup = new ConcurrentDictionary<TValue, TKey>();
		public void Add(TKey key, TValue value)
		{
			this._lookup.TryAdd(key, value);
			this._inverseLookup.TryAdd(value, key);
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return this._lookup.GetEnumerator();
		}

		public TValue LookupKey(TKey key)
		{
			this._lookup.TryGetValue(key, out TValue value);
			return value;
		}

		public IEnumerable<KeyValuePair<TKey, TValue>> LookupKeyRange(IEnumerable<TKey> keys)
		{
			return this._lookup.Where(x => keys.Contains(x.Key));
		}

		public TKey LookupValue(TValue value)
		{
			this._inverseLookup.TryGetValue(value, out TKey key);
			return key;
		}

		public IEnumerable<KeyValuePair<TKey, TValue>> LookupValueRange(IEnumerable<TValue> values)
		{
			var keys = this._inverseLookup.Where(x => values.Contains(x.Key)).Select(x => x.Value);
			return this._lookup.Where(x => keys.Contains(x.Key));
		}

		public TValue PurgeByKey(TKey key)
		{
			this._lookup.Remove(key, out TValue value);
			this._inverseLookup.Remove(value, out TKey inverseKey);
			return value;
		}

		public TKey PurgeByValue(TValue value)
		{
			this._inverseLookup.Remove(value, out TKey key);
			this._lookup.Remove(key, out TValue lookupValue);
			return key;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}
	}
}
