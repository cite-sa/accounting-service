using Cite.Tools.Exception;
using Cite.Tools.Json;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.ExternalIdentityInfoProvider
{

	public class KeycloakIdentityInfoProviderService : IExternalIdentityInfoProvider
	{
		private readonly ILogger<KeycloakIdentityInfoProviderService> _logger;
		private readonly IHttpClientFactory _clientFactory;
		private readonly KeycloakIdentityInfoProviderServiceConfig _config;
		private readonly JsonHandlingService _jsonHandlingService;

		public KeycloakIdentityInfoProviderService(
			ILogger<KeycloakIdentityInfoProviderService> logger,
			KeycloakIdentityInfoProviderServiceConfig config,
			JsonHandlingService jsonHandlingService,
			IHttpClientFactory clientFactory
			)
		{
			this._logger = logger;
			this._config = config;
			this._jsonHandlingService = jsonHandlingService;
			this._clientFactory = clientFactory;
		}

		public async Task<Dictionary<string, ExternalIdentityInfoResult>> Resolve(IEnumerable<string> subjects)
		{
			Dictionary<string, ExternalIdentityInfoResult> result = new Dictionary<string, ExternalIdentityInfoResult>();
			foreach (string subject in subjects)
			{
				UserResponse userResponse = await this.GetUserInfo(subject);
				if (userResponse != null)
				{
					String name = $"{userResponse.LastName} {userResponse.FirstName}";
					if (String.IsNullOrWhiteSpace(name)) name = userResponse.Username;
					if (String.IsNullOrWhiteSpace(name)) name = subject;
					result[subject] = new ExternalIdentityInfoResult() { Email = userResponse.Email, Name = name, Issuer = this._config.Issuer, Subject = subject };
				}
			}
			return result;
		}

		private async Task<string> GetAccessToken()
		{
			if (String.IsNullOrWhiteSpace(this._accessToken))
			{
				Uri tokenUrl = new Uri($"{this._config.IdpBaseUtrl}/auth/realms/{this._config.Realm}/protocol/openid-connect/token");

				var postData = new Dictionary<string, string>();
				postData.Add("grant_type", "client_credentials");
				postData.Add("client_id", this._config.ClientId);
				postData.Add("client_secret", this._config.ClientSecret);

				HttpContent content = new FormUrlEncodedContent(postData);

				HttpClient client = _clientFactory.CreateClient();

				this._logger.Debug(new MapLogEntry("geting access token ").And("tokenUrl", tokenUrl.ToString()));
				using (HttpResponseMessage result = await client.PostAsync(tokenUrl, content))
				{
					if (result.IsSuccessStatusCode)
					{
						TokenResponse tokenResponse = this._jsonHandlingService.FromJsonSafe<TokenResponse>(await result.Content.ReadAsStringAsync());
						if (tokenResponse != null && !String.IsNullOrWhiteSpace(tokenResponse.access_token))
						{
							this._accessToken = tokenResponse.access_token;
							return tokenResponse.access_token;
						}
					}
				}

				this._logger.Debug(new MapLogEntry("failed to get access token").And("tokenUrl", tokenUrl.ToString()));
				throw new MyUnauthorizedException("Idp access token not resolved");
			}
			return this._accessToken;
		}

		private void ResetAccessToken() => this._accessToken = null;

		private async Task<UserResponse> GetUserInfo(string id)
		{
			Uri tokenUrl = new Uri($"{this._config.IdpBaseUtrl}/auth/admin/realms/{this._config.Realm}/users/{id}");

			string accessToken = await this.GetAccessToken();

			HttpClient client = _clientFactory.CreateClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
			using (HttpResponseMessage result = await client.GetAsync(tokenUrl))
			{
				if (result.IsSuccessStatusCode)
				{
					return this._jsonHandlingService.FromJsonSafe<UserResponse>(await result.Content.ReadAsStringAsync());
				}
				else if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
				{
					this.ResetAccessToken();
				}
			}

			return null;
		}

		private string _accessToken = null;

	}

	public class TokenResponse
	{
		public String access_token { get; set; }
	}

	public class UserResponse
	{
		public String Username { get; set; }
		public String FirstName { get; set; }
		public String LastName { get; set; }
		public String Email { get; set; }
	}
}
