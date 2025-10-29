using System;

namespace Cite.Accounting.Service.Authorization
{
	public static class Permission
	{
		//Tenant
		public const String BrowseTenant = "BrowseTenant";
		public const String EditTenant = "EditTenant";
		public const String DeleteTenant = "DeleteTenant";
		//Tenant Configuration
		public const String BrowseTenantConfiguration = "BrowseTenantConfiguration";
		public const String BrowseTenantConfigurationUserLocale = "BrowseTenantConfigurationUserLocale";
		public const String DeleteTenantConfiguration = "DeleteTenantConfiguration";
		public const String EditTenantConfiguration = "EditTenantConfiguration";
		//User
		public const String BrowseUser = "BrowseUser";
		public const String EditUser = "EditUser";
		public const String DeleteUser = "DeleteUser";
		//Profile
		public const String BrowseUserProfile = "BrowseUserProfile";
		//ForgetMe
		public const String BrowseForgetMe = "BrowseForgetMe";
		public const String EditForgetMe = "EditForgetMe";
		public const String DeleteForgetMe = "DeleteForgetMe";
		//Deferred
		public const String DeferredAffiliation = "DeferredAffiliation";

		//WhatYouKnowAboutMe
		public const String BrowseWhatYouKnowAboutMe = "BrowseWhatYouKnowAboutMe";
		public const String EditWhatYouKnowAboutMe = "EditWhatYouKnowAboutMe";
		public const String DeleteWhatYouKnowAboutMe = "DeleteWhatYouKnowAboutMe";
		//Service
		public const String BrowseService = "BrowseService";
		public const String EditService = "EditService";
		public const String EditServiceCode = "EditServiceCode";
		public const String DeleteService = "DeleteService";
		public const String NewService = "NewService";
		//Metric
		public const String BrowseMetric = "BrowseMetric";
		public const String EditMetric = "EditMetric";
		public const String DeleteMetric = "DeleteMetric";
		//ServiceResource
		public const String BrowseServiceResource = "BrowseServiceResource";
		public const String EditServiceResource = "EditServiceResource";
		public const String DeleteServiceResource = "DeleteServiceResource";
		public const String NewServicResource = "NewServiceResource";
		public const String EditServiceResourceCode = "EditServiceResourceCode";
		//ServiceSync
		public const String BrowseServiceSync = "BrowseServiceSync";
		public const String EditServiceSync = "EditServiceSync";
		public const String DeleteServiceSync = "DeleteServiceSync";
		public const String EnforceServiceSync = "EnforceServiceSync";
		public const String ServiceCleanUp = "ServiceCleanUp";
		//ServiceResetEntrySync
		public const String BrowseServiceResetEntrySync = "BrowseServiceResetEntrySync";
		public const String EditServiceResetEntrySync = "EditServiceResetEntrySync";
		public const String DeleteServiceResetEntrySync = "DeleteServiceResetEntrySync";

		//ServiceUser
		public const String BrowseServiceUser = "BrowseServiceUser";
		public const String EditServiceUser = "EditServiceUser";
		public const String DeleteServiceUser = "DeleteServiceUser";
		public const String NewServiceUser = "NewServiceUser";
		//UserRole
		public const String BrowseUserRole = "BrowseUserRole";
		public const String EditUserRole = "EditUserRole";
		public const String DeleteUserRole = "DeleteUserRole";

		//ServiceAction
		public const String BrowseServiceAction = "BrowseServiceAction";
		public const String EditServiceAction = "EditServiceAction";
		public const String DeleteServiceAction = "DeleteServiceAction";
		public const String NewServiceAction = "NewServiceAction";
		public const String EditServiceActionCode = "EditServiceActionCode";
		//UserInfo
		public const String BrowseUserInfo = "BrowseUserInfo";
		public const String EditUserInfo = "EditUserInfo";
		public const String DeleteUserInfo = "DeleteUserInfo";
		public const String EditUserInfoUser = "EditUserInfoUser";

		public const String BrowseAccountingEntry = "BrowseAccountingEntry";
		public const String AddDummyAccountingEntry = "AddDummyAccountingEntry";
		public const String CalculateAccountingInfo = "CalculateAccountingInfo";
		public const String CalculateServiceAccountingInfo = "CalculateServiceAccountingInfo";

		public const String ViewUsersPage = "ViewUsersPage";
		public const String ViewUserProfilePage = "ViewUserProfilePage";
		public const String ViewServicePage = "ViewServicePage";
		public const String ViewServiceResourcePage = "ViewServiceResourcePage";
		public const String ViewServiceActionPage = "ViewServiceActionPage";
		public const String ViewUserRolePage = "ViewUserRolePage";
		public const String ViewServiceSyncPage = "ViewServiceSyncPage";
		public const String ViewServiceAccountingInfoPage = "ViewServiceAccountingInfoPage";
		public const String ViewMyAccountingInfoPage = "ViewMyAccountingInfoPage";
	}
}
