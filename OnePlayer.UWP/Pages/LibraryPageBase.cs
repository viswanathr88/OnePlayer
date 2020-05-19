﻿using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace OnePlayer.UWP.Pages
{
    public class LibraryPageBase : NavViewPageBase
    {
        public bool ShowSortMenu
        {
            get { return (bool)GetValue(ShowSortMenuProperty); }
            set { SetValue(ShowSortMenuProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowSortMenu.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowSortMenuProperty =
            DependencyProperty.Register("ShowSortMenu", typeof(bool), typeof(LibraryPageBase), new PropertyMetadata(true));

        public MenuFlyout SortMenu
        {
            get { return (MenuFlyout)GetValue(SortMenuProperty); }
            set { SetValue(SortMenuProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SortMenu.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SortMenuProperty =
            DependencyProperty.Register("SortMenu", typeof(MenuFlyout), typeof(LibraryPageBase), new PropertyMetadata(null));

        public bool ShowLayoutMenu
        {
            get { return (bool)GetValue(ShowLayoutMenuProperty); }
            set { SetValue(ShowLayoutMenuProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowLayoutMenu.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowLayoutMenuProperty =
            DependencyProperty.Register("ShowLayoutMenu", typeof(bool), typeof(LibraryPageBase), new PropertyMetadata(true));


        public MenuFlyout LayoutMenu
        {
            get { return (MenuFlyout)GetValue(LayoutMenuProperty); }
            set { SetValue(LayoutMenuProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LayoutMenu.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LayoutMenuProperty =
            DependencyProperty.Register("LayoutMenu", typeof(MenuFlyout), typeof(LibraryPageBase), new PropertyMetadata(null));

    }
}
