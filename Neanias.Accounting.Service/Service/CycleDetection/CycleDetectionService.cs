using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cite.Tools.Exception;
using Microsoft.Extensions.Logging;
using Cite.Tools.Data.Query;
using Neanias.Accounting.Service.ErrorCode;

namespace Neanias.Accounting.Service.Service.CycleDetection
{
	public class CycleDetectionService : ICycleDetectionService
	{
		private readonly ILogger<CycleDetectionService> _logger;
		private readonly ErrorThesaurus _errors;

		public CycleDetectionService(
			ILogger<CycleDetectionService> logger,
			ErrorThesaurus errors)
		{
			_logger = logger;
			this._errors = errors;
		}

		public async Task<HashSet<Guid>> EnsureNoCycleForce<T>(T item, Func<T, Guid> getItemId, Func<Guid, Query<T>> buildParentQuery, HashSet<Guid> visited = null) where T : class
		{
			visited = visited ?? new HashSet<Guid>();
			Guid itemId = getItemId(item);
			if (visited.Contains(itemId)) throw new MyApplicationException(this._errors.CycleDetected.Code, this._errors.CycleDetected.Message);
			visited.Add(itemId);
			IEnumerable<T> childs = await buildParentQuery(itemId).CollectAsync();
			foreach (T child in childs) visited = await this.EnsureNoCycleForce(child, getItemId, buildParentQuery, visited);
			return visited;
		}

		public async Task<HashSet<Guid>> EnsureNoCycleForce<T>(T item, Func<T, Guid> getItemId, Func<Guid, Task<IEnumerable<T>>> getChilds, HashSet<Guid> visited = null) where T : class
		{
			visited = visited ?? new HashSet<Guid>();
			Guid itemId = getItemId(item);
			if (visited.Contains(itemId)) throw new MyApplicationException(this._errors.CycleDetected.Code, this._errors.CycleDetected.Message);
			visited.Add(itemId);
			IEnumerable<T> childs = await getChilds(itemId);
			foreach (T child in childs) visited = await this.EnsureNoCycleForce(child, getItemId, getChilds, visited);
			return visited;
		}
	}
}
