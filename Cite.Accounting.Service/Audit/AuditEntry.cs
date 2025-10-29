using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Audit
{
	public class AuditEntry
	{
		private int _tieBreaker = 0;

		public AuditEntry() { }

		[JsonProperty("msg")]
		protected String Text { get; set; }
		[JsonProperty("d")]
		protected Dictionary<String, Object> Data { get; set; }

		protected virtual int NextTieBreaker() { return this._tieBreaker++; }

		protected virtual String ToSafeKey(String key)
		{
			String basekey = key;
			if (String.IsNullOrEmpty(key)) basekey = this.NextTieBreaker().ToString();
			String keySafe = basekey;
			while (this.Data.ContainsKey(keySafe)) keySafe = $"{basekey}_{this.NextTieBreaker()}";
			return keySafe;
		}

		public virtual AuditEntry Message(String message)
		{
			this.Text = message;
			return this;
		}

		public virtual AuditEntry And(String key, Object data)
		{
			if (this.Data == null) this.Data = new Dictionary<string, object>();
			String keySafe = this.ToSafeKey(key);
			this.Data[keySafe] = data;
			return this;
		}
	}
}
