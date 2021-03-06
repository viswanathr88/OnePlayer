﻿using Mirage.ViewModel;
using Riff.Authentication;
using Riff.Data;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Riff.UWP.ViewModel
{
    public sealed class FirstRunExperienceViewModel : DataViewModel
    {
        private readonly ILoginManager loginManager;
        private bool loginRequired = false;
        private string providerUrl;
        private bool profileAndPhotoFetched = false;
        private UserProfile profile = null;
        private Stream photo = null;

        public FirstRunExperienceViewModel(ILoginManager loginManager)
        {
            this.loginManager = loginManager ?? throw new ArgumentNullException(nameof(loginManager));
            ProviderUrl = this.loginManager.GetAuthorizeUrl();
        }

        public override async Task LoadAsync()
        {
            LoginRequired = !(await loginManager.LoginExistsAsync());
            if (LoginRequired)
            {
                await LoginAsync();
            }
        }

        public bool LoginRequired
        {
            get { return this.loginRequired; }
            private set
            {
                SetProperty(ref this.loginRequired, value);
            }
        }

        public string ProviderUrl
        {
            get
            {
                return this.providerUrl;
            }
            private set
            {
                SetProperty(ref this.providerUrl, value);
            }
        }

        public bool ProfileAndPhotoFetched
        {
            get
            {
                return this.profileAndPhotoFetched;
            }
            private set
            {
                SetProperty(ref this.profileAndPhotoFetched, value);
            }
        }

        public UserProfile Profile
        {
            get
            {
                return this.profile;
            }
            private set
            {
                SetProperty(ref this.profile, value);
            }
        }

        public Stream Photo
        {
            get
            {
                return this.photo;
            }
            private set
            {
                SetProperty(ref this.photo, value);
            }
        }

        public async Task LoginAsync()
        {
            IsLoading = true;
            var token = await this.loginManager.LoginAsync(null);

            if (token != null)
            {
                LoginRequired = false;
                Profile = await this.loginManager.GetUserAsync();
                Photo = await this.loginManager.GetUserPhotoAsync();
                ProfileAndPhotoFetched = true;
            }
            else
            {
                Application.Current.Exit();
            }

            IsLoading = false;
        }
    }
}
