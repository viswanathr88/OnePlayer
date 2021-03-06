﻿using Android.App;
using Android.Content;
using Riff.Authentication;
using Riff.Data;
using Riff.Droid.Storage;
using Riff.Sync;
using System;
using System.Net.Http;

namespace Riff.Droid
{
    [Application]
    public class App : Android.App.Application, IRiffApp
    {
        private ILoginManager loginManager;
        private ITokenCache tokenCache;
        private IProfileCache profileCache;
        private IPreferences appPreferences;
        private IMusicLibrary musicLibrary;
        private SyncEngine syncEngine;
        private const string tokenCachePreferenceFile = "com.riff.droid.tokencache.preferences";
        private const string appPreferenceFile = "com.riff.droid.app.preferences";
        private readonly static HttpClient httpClient = new HttpClient();
        private static readonly string DefaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

        public App(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
        }

        public IMusicLibrary MusicLibrary
        {
            get
            {
                if (this.musicLibrary == null)
                {
                    this.musicLibrary = new MusicLibrary(DefaultPath, "Riff.db");
                }

                return this.musicLibrary;
            }
        }


        public ILoginManager LoginManager
        {
            get
            {
                if (this.loginManager == null)
                {
                    this.loginManager = new CacheReadyLoginManager(httpClient, TokenCache, ProfileCache);
                }

                return this.loginManager;
            }
        }

        public HttpClient WebClient => httpClient;

        private ITokenCache TokenCache
        {
            get
            {
                if (tokenCache == null)
                {
                    this.tokenCache = new TokenCache(GetSharedPreferences(tokenCachePreferenceFile, FileCreationMode.Private));
                }

                return this.tokenCache;
            }
        }

        private IProfileCache ProfileCache
        {
            get
            {
                if (profileCache == null)
                {
                    profileCache = new ProfileCache(DefaultPath);
                }

                return profileCache;
            }
        }

        public IPreferences Preferences
        {
            get
            {
                if (appPreferences == null)
                {
                    appPreferences = new AppPreferences(GetSharedPreferences(appPreferenceFile, FileCreationMode.Private));
                }

                return appPreferences;
            }
        }

        public SyncEngine SyncEngine
        {
            get
            {
                if (syncEngine == null)
                {
                    syncEngine = new SyncEngine(Preferences, LoginManager, WebClient, MusicLibrary);
                }

                return syncEngine;
            }
        }
    }
}