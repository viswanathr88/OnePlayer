﻿using Mirage.ViewModel;
using Riff.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Riff.UWP.ViewModel
{
    sealed class AlbumComparer : IEqualityComparer<Album>
    {
        public bool Equals(Album x, Album y)
        {
            return x.Id.Equals(y.Id);
        }

        public int GetHashCode(Album obj)
        {
            return obj.Id.GetHashCode();
        }
    }

    public sealed class ExpandedAlbumItem : IGrouping<Album, DriveItem>
    {
        private readonly IList<DriveItem> tracks;
        private readonly IThumbnailReadOnlyCache cache;

        public ExpandedAlbumItem(IThumbnailReadOnlyCache cache, Album key, IEnumerable<DriveItem> tracks)
        {
            this.cache = cache;
            Key = key;
            this.tracks = new List<DriveItem>(tracks);
        }

        public Album Key { get; }

        public IEnumerator<DriveItem> GetEnumerator() => tracks.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => tracks.GetEnumerator();

        public string AlbumArt => cache.Exists(Key.Id.Value) ? cache.GetPath(Key.Id.Value) : " ";
    }

    public sealed class ArtistViewModel : DataViewModel<Artist>
    {
        private ObservableCollection<ExpandedAlbumItem> albumTracks;
        private IList<DriveItem> tracks;
        private readonly IMusicLibrary library;

        public ArtistViewModel(IMusicLibrary library)
        {
            this.library = library ?? throw new ArgumentNullException(nameof(library));
        }

        public ObservableCollection<ExpandedAlbumItem> AlbumTracks
        {
            get => albumTracks;
            set  
            {
                if (SetProperty(ref this.albumTracks, value))
                {
                    Tracks = AlbumTracks.SelectMany(item => item).ToList();
                }
            }
        }

        public IList<DriveItem> Tracks
        {
            get => tracks;
            private set => SetProperty(ref this.tracks, value);
        }

        public async override Task LoadAsync(Artist artist)
        {
            var options = new DriveItemAccessOptions()
            {
                IncludeTrack = true,
                IncludeTrackAlbum = true,
                AlbumArtistFilter = artist.Id,
                SortType = TrackSortType.ReleaseYear,
                SortOrder = SortOrder.Descending
            };

            var groups = await Task.Run(() =>
            {
                var items = library.DriveItems.Get(options);
                var groupedItems = items.GroupBy(item => item.Track.Album, (key, list) => new ExpandedAlbumItem(library.AlbumArts, key, list), new AlbumComparer());
                return groupedItems;
            });

            AlbumTracks = new ObservableCollection<ExpandedAlbumItem>(groups);
            IsLoaded = true;
        }
    }
}
