//using Neanias.Accounting.Service.Data;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Neanias.Accounting.Service.Query.Extensions
//{
//	public static class Extensions
//	{
//		public static bool Like(this DbFunctions _, string matchExpression, string pattern, DbProviderConfig.DbProvider provider)
//		{
//			switch (provider)
//			{
//				case DbProviderConfig.DbProvider.PostgreSQL: return EF.Functions.ILike(matchExpression, pattern);
//				case DbProviderConfig.DbProvider.SQLServer:
//				default: return EF.Functions.Like(matchExpression, pattern);
//			}
//		}

//		public static bool ILike(this DbFunctions _, string matchExpression, string pattern, string escapeCharacter, DbProviderConfig.DbProvider provider)
//		{
//			switch (provider)
//			{
//				case DbProviderConfig.DbProvider.PostgreSQL: return EF.Functions.ILike(matchExpression, pattern, escapeCharacter);
//				case DbProviderConfig.DbProvider.SQLServer:
//				default: return EF.Functions.Like(matchExpression, pattern, escapeCharacter);
//			}
//		}
//	}
//}
