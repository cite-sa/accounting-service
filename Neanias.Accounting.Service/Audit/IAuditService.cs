using Cite.Tools.Common.Types;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Audit
{
	public interface IAuditService
	{
		void Track(EventId action);
		void TrackIdentity(EventId action);
		void Track(EventId action, string message);
		void TrackIdentity(EventId action, string message);
		void Track(EventId action, string key, object data);
		void TrackIdentity(EventId action, string key, object data);
		void Track(EventId action, IEnumerable<Pair<string, object>> data);
		void TrackIdentity(EventId action, IEnumerable<Pair<string, object>> data);
		void Track(EventId action, IDictionary<string, object> data);
		void TrackIdentity(EventId action, IDictionary<string, object> data);
		void Track(EventId action, string message, string key, object data);
		void TrackIdentity(EventId action, string message, string key, object data);
		void Track(EventId action, string message, IEnumerable<Pair<string, object>> data);
		void TrackIdentity(EventId action, string message, IEnumerable<Pair<string, object>> data);
		void Track(EventId action, string message, IDictionary<string, object> data);
		void TrackIdentity(EventId action, string message, IDictionary<string, object> data);
	}
}
