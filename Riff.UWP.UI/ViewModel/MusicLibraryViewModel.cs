﻿using Mirage.ViewModel;
using Riff.Data;
using System;
using System.Threading.Tasks;

namespace Riff.UWP.ViewModel
{
    public enum MusicLibrarySection
    {
        Albums,
        Artists,
        Tracks
    };

    public sealed class MusicLibraryViewModel : DataViewModel
    {
        private readonly IMusicLibrary library;
        private readonly Lazy<AlbumsViewModel> albums;
        private readonly Lazy<ArtistsViewModel> artists;
        private readonly Lazy<TracksViewModel> tracks;
        private MusicLibrarySection section = MusicLibrarySection.Albums;

        public MusicLibraryViewModel(IMusicLibrary library, IPlayer player, PlaylistsViewModel playlistsVM)
        {
            this.library = library ?? throw new ArgumentNullException(nameof(library));
            albums = new Lazy<AlbumsViewModel>(() => new AlbumsViewModel(this.library, player, playlistsVM));
            artists = new Lazy<ArtistsViewModel>(() => new ArtistsViewModel(this.library, player, playlistsVM));
            tracks = new Lazy<TracksViewModel>(() => new TracksViewModel(this.library));
        }

        public AlbumsViewModel Albums => albums.Value;

        public ArtistsViewModel Artists => artists.Value;

        public TracksViewModel Tracks => tracks.Value;

        public MusicLibrarySection CurrentSection
        {
            get => section;
            set => SetProperty(ref this.section, value);
        }

        public override Task LoadAsync()
        {
            Load();
            return Task.CompletedTask;
        }

        public void Load()
        {
            IsLoaded = true;
        }
    }
}
