using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.ErrorCode
{
	public class ErrorThesaurus
	{
		public struct ErrorDescription
		{
			public int Code { get; set; }
			public String Message { get; set; }
		}

		public ErrorDescription HashConflict { get; set; }
		public ErrorDescription Forbidden { get; set; }
		public ErrorDescription SystemError { get; set; }
		public ErrorDescription MissingTenant { get; set; }
		public ErrorDescription InvalidAPIKey { get; set; }
		public ErrorDescription StaleAPIKey { get; set; }
		public ErrorDescription ModelValidation { get; set; }
		public ErrorDescription SensitiveInfo { get; set; }
		public ErrorDescription NonPersonPrincipal { get; set; }
		public ErrorDescription BlockingConsent { get; set; }
		public ErrorDescription WhatYouKnowAboutMeIncompatibleState { get; set; }
		public ErrorDescription SingleTenantConfigurationPerTypeSupported { get; set; }
		public ErrorDescription IncompatibleTenantConfigurationTypes { get; set; }
		public ErrorDescription MissingTotpToken { get; set; }
		public ErrorDescription CycleDetected { get; set; }
		public ErrorDescription ActionNotSupported { get; set; }
		public ErrorDescription ServiceSyncIsNotAvailable { get; set; }
		public ErrorDescription MaxCalculateResultLimit { get; set; }
		
	}
}
