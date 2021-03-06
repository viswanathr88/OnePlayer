﻿using Android.Support.V7.Widget;
using Android.Views;
using Riff.Data;
using System;
using System.Collections.Generic;

namespace Riff.Droid.UI.MusicLibrary
{
    class TrackListAdapter : Android.Support.V7.Widget.RecyclerView.Adapter
    {
        private readonly IMusicLibrary library;
        private readonly IList<Track> tracks;
        private readonly Action<Track> selectionHandler;

        public TrackListAdapter(IMusicLibrary library, Action<Track> selectionHandler)
        {
            this.library = library;
            this.tracks = this.library.Tracks.Get();
            this.selectionHandler = selectionHandler;
        }
        public override int ItemCount => tracks.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var viewHolder = holder as TrackItemViewHolder;
            viewHolder.TrackName.Text = tracks[position].Title;
            viewHolder.TrackArtist.Text = tracks[position].Artist;
            viewHolder.TrackDuration.Text = tracks[position].Duration.ToString("mm':'ss");

            if (this.library.AlbumArts.Exists(tracks[position].Album.Id.Value))
            {
                using var stream = this.library.AlbumArts.Get(tracks[position].Album.Id.Value);
                viewHolder.TrackArt.SetImageBitmap(Android.Graphics.BitmapFactory.DecodeStream(stream));
            }
            else
            {
                viewHolder.TrackArt.SetImageDrawable(null);
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // Create a new view for the album item
            var view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.widget_track_item, parent, false);
            return new TrackItemViewHolder(view, OnItemClicked);
        }

        private void OnItemClicked(int index)
        {
            selectionHandler(tracks[index]);
        }
    }
}