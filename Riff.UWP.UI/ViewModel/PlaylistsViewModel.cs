﻿using Mirage.ViewModel;
using Mirage.ViewModel.Commands;
using Riff.Data;
using Riff.UWP.UI.Extensions;
using Riff.UWP.ViewModel.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Riff.UWP.ViewModel
{
    class PlaylistComparer : IEqualityComparer<Playlist>
    {
        public bool Equals(Playlist x, Playlist y)
        {
            return x.Id == y.Id &&
                x.Name == y.Name &&
                x.LastModified == y.LastModified;
        }

        public int GetHashCode(Playlist obj)
        {
            return obj.GetHashCode();
        }
    }

    public class PlaylistsViewModel : DataViewModel, IPlaylistCommands
    {
        private readonly IMusicLibrary musicLibrary;

        private ObservableCollection<Playlist> playlists = new ObservableCollection<Playlist>();
        private bool isEmpty;
        private bool isSelectionMode;

        public PlaylistsViewModel(IPlayer player, IMusicLibrary library)
        {
            this.musicLibrary = library;
            this.Add = new AddPlaylistCommand(musicLibrary);
            Delete = new DeletePlaylistCommand(musicLibrary);
            DeleteMultiple = new DeletePlaylistsCommand(musicLibrary);
            Play = new PlayPlaylistCommand(player, musicLibrary);
            PlayMultiple = new PlayPlaylistsCommand(player, musicLibrary);
            PlayNext = new PlayPlaylistCommand(player, musicLibrary) { AddToNowPlayingList = true };
            PlayNextMultiple = new PlayPlaylistsCommand(player, musicLibrary) { AddToNowPlayingList = true };
            PlayItems = new PlayDriveItemsCommand(player);
            PlayItemsNext = new PlayDriveItemsCommand(player) { AddToNowPlayingList = true };
            Rename = new RenamePlaylistCommand(musicLibrary);
        }

        public ObservableCollection<Playlist> Playlists
        {
            get => playlists;
            private set => SetProperty(ref this.playlists, value);
        }

        public bool IsEmpty
        {
            get => isEmpty;
            private set => SetProperty(ref this.isEmpty, value);
        }

        public bool IsSelectionMode
        {
            get => isSelectionMode;
            set => SetProperty(ref this.isSelectionMode, value);
        }

        public AddPlaylistCommand Add { get; }

        public ICommand<Playlist> Delete { get; }

        public ICommand<IList<object>> DeleteMultiple { get; }

        public IAsyncCommand<Playlist> Play { get; }

        public IAsyncCommand<IList<object>> PlayMultiple { get; }

        public IAsyncCommand<Playlist> PlayNext { get; }

        public IAsyncCommand<IList<object>> PlayNextMultiple { get; }

        public RenamePlaylistCommand Rename { get; }

        public IAsyncCommand<IList<DriveItem>> PlayItems { get; }

        public IAsyncCommand<IList<DriveItem>> PlayItemsNext { get; }

        public async override Task LoadAsync()
        {
            var diffList = await Task.Run(() => {
                var playlists = musicLibrary.Playlists2.Get(new PlaylistAccessOptions());
                return Diff.Compare(Playlists, playlists, new PlaylistComparer());
            });
            Playlists.ApplyDiff(diffList);
            IsEmpty = (Playlists.Count == 0);
        }

        private async void PlaylistManager_StateChanged(object sender, System.EventArgs e)
        {
            await LoadAsync();
        }

        public async Task AddToPlaylist(Album album, Playlist playlist)
        {
            var options = new DriveItemAccessOptions()
            {
                IncludeTrack = true,
                IncludeTrackAlbum = true,
                IncludeTrackGenre = true,
                AlbumFilter = album.Id,
                SortType = TrackSortType.ReleaseYear,
                SortOrder = SortOrder.Descending
            };

            await AddToPlaylist(options, playlist);
        }

        public async Task AddToPlaylist(Artist artist, Playlist playlist)
        {
            var options = new DriveItemAccessOptions()
            {
                IncludeTrack = true,
                IncludeTrackAlbum = true,
                IncludeTrackGenre = true,
                AlbumArtistFilter = artist.Id,
                SortType = TrackSortType.ReleaseYear,
                SortOrder = SortOrder.Descending
            };

            await AddToPlaylist(options, playlist);
        }

        public async Task AddToPlaylist(IList<DriveItem> items, Playlist playlist)
        {
            await AddToPlaylist(() => items, playlist);
        }

        private async Task AddToPlaylist(DriveItemAccessOptions options, Playlist playlist)
        {
            await AddToPlaylist(() => musicLibrary.DriveItems.Get(options), playlist);
        }

        private async Task AddToPlaylist(Func<IList<DriveItem>> itemFetcher, Playlist playlist)
        {
            await Task.Run(() =>
            {
                var items = itemFetcher();
                using (var session = musicLibrary.Edit())
                {
                    IList<PlaylistItem> playlistItems = new List<PlaylistItem>();
                    foreach (var driveItem in items)
                    {
                        playlistItems.Add(new PlaylistItem()
                        {
                            DriveItem = driveItem,
                            PlaylistId = playlist.Id.Value
                        });
                    }

                    session.PlaylistItems.Add(playlist, playlistItems);
                }
            });
        }
    }
}
