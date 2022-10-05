using Cite.Tools.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Neanias.Accounting.Service.Authorization
{
	public class PermissionProvider : IPermissionProvider
	{
		private List<String> _permissionValues = null;

		public List<String> GetPermissionValues()
		{
			if (_permissionValues == null)
				_permissionValues = typeof(Permission).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(String)).Select(x => (String)x.GetRawConstantValue()).ToList();
			return _permissionValues;
		}
	}
}
