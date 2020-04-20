﻿using Newtonsoft.Json;
using OnePlayer.Authentication;
using OnePlayer.Data;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Windows.Storage;

namespace OnePlayer.UWP.Authentication
{
    public sealed class LoginProfile
    {
        public LoginProfile(WebAccountProvider provider, WebAccount account)
        {
            Provider = provider;
            Account = account;
        }

        public WebAccountProvider Provider 
        {
            get;
            private set;
        }

        public WebAccount Account
        {
            get;
            private set;
        }
    }

    public sealed class LoginManager : ILoginManager
    {
        private const string appId = "19b11b92-7fc8-44c9-b794-8ae1d41cebed";
        private static readonly string[] scopes = new string[] { "User.Read", "files.read", "offline_access" };
        private const string authorizeUri = @"https://login.microsoft.com";
        private const string profileUri = @"https://graph.microsoft.com/V1.0/me/";
        private const string profilePhotoUri = @"https://graph.microsoft.com/beta/me/photo/$value";
        private const string currentProviderIdKey = "CurrentUserProviderId";
        private const string currentUserIdKey = "CurrentUserId";


        private readonly HttpClient webClient;

        public LoginManager(HttpClient client)
        {
            this.webClient = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<Token> AcquireTokenSilentAsync()
        {
            var profile = await GetLoginProfileAsync();

            var request = new WebTokenRequest(profile.Provider, string.Join(" ", scopes));
            var result = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(request);

            if (result.ResponseStatus == WebTokenRequestStatus.Success)
            {
                return new Token() { AccessToken = result.ResponseData[0].Token };
            }
            else
            {
                return null;
            }
        }

        public async Task<Token> EndLoginAsync(string data)
        {
            var providerId = data;
            var provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(providerId);

            WebTokenRequest request = new WebTokenRequest(provider, string.Join(" ", scopes), appId);
            request.Properties.Add("resource", "https://graph.microsoft.com");
            WebTokenRequestResult result = await WebAuthenticationCoreManager.RequestTokenAsync(request);

            if (result.ResponseStatus == WebTokenRequestStatus.Success)
            {
                StoreLoginProfileAsync(new LoginProfile(provider, result.ResponseData[0].WebAccount));
                return new Token() { AccessToken = result.ResponseData[0].Token };
            }

            throw new Exception($"Login failed with status {result.ResponseStatus}, Error Code: {result.ResponseError?.ErrorCode}, ErrorMessage: {result.ResponseError?.ErrorMessage}");
        }

        public string GetAuthorizeUrl()
        {
            return authorizeUri;
        }

        public string GetRedirectUrl()
        {
            throw new NotSupportedException("Redirect is not support by Windows Login Manager");
        }

        public async Task<UserProfile> GetUserAsync()
        {
            var token = await AcquireTokenSilentAsync();

            var request = new HttpRequestMessage(HttpMethod.Get, profileUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            var response = await this.webClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string body = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<UserProfile>(body);
        }

        public async Task<Stream> GetUserPhotoAsync()
        {
            var token = await AcquireTokenSilentAsync();

            var request = new HttpRequestMessage(HttpMethod.Get, profilePhotoUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            var response = await this.webClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync();
        }

        public async Task<bool> LoginExistsAsync()
        {
            bool loginExists = false;
            try
            {
                var loginProfile = await GetLoginProfileAsync();
                loginExists = (loginProfile != null);
            }
            catch (Exception)
            {
            }

            return loginExists;
        }

        private async Task<LoginProfile> GetLoginProfileAsync()
        {
            string providerId = ApplicationData.Current.LocalSettings.Values["CurrentUserProviderId"]?.ToString();
            string accountId = ApplicationData.Current.LocalSettings.Values["CurrentUserId"]?.ToString();

            if (string.IsNullOrEmpty(providerId) || string.IsNullOrEmpty(accountId))
            {
                throw new NotSupportedException("Account is not available");
            }

            var provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(providerId);
            var account = await WebAuthenticationCoreManager.FindAccountAsync(provider, accountId);
            return new LoginProfile(provider, account);
        }

        private void StoreLoginProfileAsync(LoginProfile profile)
        {
            ApplicationData.Current.LocalSettings.Values[currentProviderIdKey] = profile.Provider.Id;
            ApplicationData.Current.LocalSettings.Values[currentUserIdKey] = profile.Account.Id;
        }

        public async Task SignOutAsync()
        {
            var profile = await GetLoginProfileAsync();

            await profile.Account.SignOutAsync();

            ApplicationData.Current.LocalSettings.Values.Remove(currentProviderIdKey);
            ApplicationData.Current.LocalSettings.Values.Remove(currentUserIdKey);
        }
    }
}
