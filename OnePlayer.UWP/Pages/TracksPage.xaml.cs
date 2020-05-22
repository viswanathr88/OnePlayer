﻿using OnePlayer.UWP.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace OnePlayer.UWP.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TracksPage : LibraryPageBase, ISupportViewModel<TracksViewModel>
    {
        public TracksPage()
        {
            this.InitializeComponent();
        }

        public TracksViewModel ViewModel => (Application.Current.Resources["VMLocator"] as Locator).MusicLibrary.Tracks;

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (!ViewModel.IsLoaded)
            {
                await ViewModel.LoadAsync(VoidType.Empty);
            }
        }

        private async void SortFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadAsync(VoidType.Empty);
        }

        private async void SortOrderFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadAsync(VoidType.Empty);
        }

        private void TracksList_ItemClick(object sender, ItemClickEventArgs e)
        {

        }
    }
}
