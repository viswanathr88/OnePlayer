﻿using Riff.Data;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Riff.Authentication
{
    public sealed class CacheReadyLoginManager : ILoginManager
    {
        private readonly ILoginManager loginManagerImpl;
        private readonly IProfileCache profileCache;
        private UserProfile inMemoryProfile = null;

        public CacheReadyLoginManager(ILoginManager loginManager, string path)
            : this(loginManager, new ProfileCache(path))
        {
        }

        public CacheReadyLoginManager(HttpClient client, ITokenCache tokenCache, IProfileCache profileCache)
            : this(new OAuthLoginManager(client, tokenCache), profileCache)
        { }

        public CacheReadyLoginManager(ILoginManager loginManager, IProfileCache profileCache)
        {
            this.loginManagerImpl = loginManager;
            this.profileCache = profileCache;
        }

        public async Task<bool> LoginExistsAsync()
        {
            return await this.loginManagerImpl.LoginExistsAsync();
        }

        public async Task<Token> AcquireTokenSilentAsync()
        {
            return await this.loginManagerImpl.AcquireTokenSilentAsync();
        }

        public async Task<Token> LoginAsync(object code)
        {
            return await this.loginManagerImpl.LoginAsync(code);
        }

        public string GetAuthorizeUrl()
        {
            return this.loginManagerImpl.GetAuthorizeUrl();
        }

        public string GetRedirectUrl()
        {
            return this.loginManagerImpl.GetRedirectUrl();
        }

        public async Task<UserProfile> GetUserAsync()
        {
            UserProfile profile = await GetCachedProfileAsync();
            if (profile == null)
            {
                profile = await this.loginManagerImpl.GetUserAsync();
                await this.profileCache.SetProfileAsync(profile);
            }

            inMemoryProfile = profile;
            return profile;
        }

        public async Task<Stream> GetUserPhotoAsync()
        {
            var stream = await this.profileCache.GetPhotoAsync();

            if (stream == null)
            {
                using (var photostream = await this.loginManagerImpl.GetUserPhotoAsync())
                {
                    if (photostream != null)
                    {
                        await this.profileCache.SetPhotoAsync(photostream);
                    }
                }

                stream = await this.profileCache.GetPhotoAsync();
            }

            return stream;
        }

        private async Task<UserProfile> GetCachedProfileAsync()
        {
            return inMemoryProfile ?? await this.profileCache.GetProfileAsync();
        }

        public Task SignOutAsync()
        {
            return loginManagerImpl.SignOutAsync();
        }
    }
}
