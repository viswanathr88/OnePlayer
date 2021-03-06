﻿using Newtonsoft.Json;
using Riff.Authentication;
using Riff.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Riff.Sync
{
    public enum SyncState
    {
        NotStarted,
        Started,
        Syncing,
        NotSyncing,
        Uptodate,
        Stopped
    }

    public sealed class SyncStatus
    {
        public DateTime SuccessfulSyncTime
        {
            get;
            set;
        }

        public DateTime SyncTime
        {
            get;
            set;
        }

        public Exception Error
        {
            get;
            set;
        }

        public SyncState State
        {
            get;
            set;
        }

        public int ItemsAdded
        {
            get;
            set;
        } = 0;
    }

    public sealed class SyncEngine : ITrackUrlDownloader
    {
        public event EventHandler<SyncState> StateChanged;
        public event EventHandler<SyncStatus> Checkpoint;
        private SyncState state = SyncState.NotStarted;
        private static readonly string baseUrl = "https://graph.microsoft.com/v1.0";
        private static readonly string baseDeltaUrl = $"{baseUrl}/drive/special/music/delta?$expand=thumbnails";
        private readonly IPreferences preferences;
        private readonly ILoginManager loginManager;
        private readonly HttpClient webClient;
        private readonly IMusicLibrary library;

        public SyncEngine(IPreferences preferences, ILoginManager loginManager, HttpClient webClient, IMusicLibrary library)
        {
            this.preferences = preferences;
            this.loginManager = loginManager;
            this.webClient = webClient;
            this.library = library;

            if (!string.IsNullOrEmpty(preferences.LastSyncTime))
            {
                Status.SuccessfulSyncTime = DateTime.Parse(preferences.LastSyncTime);
            }

            Start();
        }

        private void Library_ItemAdded(object sender, Data.DriveItem e)
        {
            Status.ItemsAdded++;
        }

        public SyncState State
        {
            get
            {
                return this.state;
            }
            private set
            {
                if (this.state != value)
                {
                    this.state = value;
                    Status.State = this.state;
                    StateChanged?.Invoke(this, this.state);
                }
            }
        }

        public SyncStatus Status
        {
            get;
            private set;
        } = new SyncStatus();

        public void Stop()
        {
            if (State != SyncState.Stopped)
            {
                // TODO: Kill any sync sessions

                State = SyncState.Stopped;
                preferences.IsSyncPaused = true;
            }
        }

        public void Start()
        {
            if (preferences.IsSyncPaused)
            {
                State = SyncState.Stopped;
            }
            else if (State == SyncState.NotStarted || State == SyncState.Stopped)
            {
                State = SyncState.Started;
            }
        }

        public async Task RunAsync()
        {
            try
            {
                Status.SyncTime = DateTime.Now;
                Status.ItemsAdded = 0;
                Status.Error = null;

                await RunInternalAsync();
            }
            catch (Exception ex)
            {
                Status.Error = ex;
                State = SyncState.NotSyncing;
            }
        }

        public async Task<Uri> GetDownloadUrlAsync(string driveItemId)
        {
            if (string.IsNullOrEmpty(driveItemId))
            {
                throw new ArgumentNullException(nameof(driveItemId));
            }

            var url = $"{baseUrl}/me/drive/items/{driveItemId}/content";
            var token = await loginManager.AcquireTokenSilentAsync();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            var response = await webClient.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.Redirect)
            {
                return response.Headers.Location;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                Uri uri;
                if (Uri.TryCreate(content, UriKind.Absolute, out uri))
                {
                    return uri;
                }
            }

            throw new Exception("Failed to fetch download url");
        }

        private async Task RunInternalAsync()
        {
            if (State == SyncState.Syncing || State == SyncState.Stopped)
            {
                // We are already syncing.
                return;
            }

            State = SyncState.Syncing;

            // Fire a checkpoint event
            Checkpoint?.Invoke(this, Status);

            string deltaUrl = string.Empty;

            bool completed = false;

            using (var session = library.Edit())
            {
                do
                {
                    var token = await loginManager.AcquireTokenSilentAsync();

                    if (deltaUrl == string.Empty)
                    {
                        // Read delta url from a previous sync
                        deltaUrl = string.IsNullOrEmpty(preferences.DeltaUrl) ? baseDeltaUrl : preferences.DeltaUrl;
                    }

                    var request = new HttpRequestMessage(HttpMethod.Get, deltaUrl);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

                    var response = await webClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    if (response.IsSuccessStatusCode)
                    {
                        Data.Json.DeltaQueryResponse deltaQueryResponse = null;
                        using (var responseStream = await response.Content.ReadAsStreamAsync())
                        {
                            using (var streamReader = new System.IO.StreamReader(responseStream))
                            {
                                using (var jsonReader = new JsonTextReader(streamReader))
                                {
                                    var serializer = new JsonSerializer();
                                    deltaQueryResponse = serializer.Deserialize<Data.Json.DeltaQueryResponse>(jsonReader);
                                }
                            }
                        }


                        if (deltaQueryResponse.value.Length > 0)
                        {
                            Add(deltaQueryResponse.value, session);
                        }

                        if (!string.IsNullOrEmpty(deltaQueryResponse.nextLink))
                        {
                            deltaUrl = deltaQueryResponse.nextLink;
                        }
                        else if (!string.IsNullOrEmpty(deltaQueryResponse.deltaLink))
                        {
                            deltaUrl = deltaQueryResponse.deltaLink;
                            completed = true;
                        }

                        // Download thumbnails
                        bool thumbnailsDownloaded = await DownloadThumbnailsAsync(session);

                        if (deltaQueryResponse.value.Length > 0 && thumbnailsDownloaded)
                        {
                            session.Save();
                        }

                        preferences.DeltaUrl = deltaUrl;


                        if (completed)
                        {
                            DateTime successTime = DateTime.Now;
                            Status.SuccessfulSyncTime = successTime;
                            preferences.LastSyncTime = successTime.ToString();
                            State = SyncState.Uptodate;
                        }

                        // Fire a checkpoint event
                        Checkpoint?.Invoke(this, Status);
                    }
                    else
                    {
                        // Read the error message
                        string errorMessage = await response.Content.ReadAsStringAsync();
                        if (response.StatusCode == System.Net.HttpStatusCode.Gone)
                        {
                            // Look for the location header
                            // deltaUrl = response.Headers.Location.ToString();
                            deltaUrl = baseDeltaUrl;
                            preferences.DeltaUrl = string.Empty;
                        }
                    }
                }
                while (!completed);
            }
        }

        private void Add(IEnumerable<Data.Json.DriveItem> items, IEditSession session)
        {
            foreach (var item in items)
            {
                if (item.audio != null)
                {
                    ProcessAndSaveItem(item, session);
                }
            }
        }

        private void ProcessAndSaveItem(Data.Json.DriveItem item, IEditSession session)
        {
            string genreName = item.audio.genre?.Trim();
            string albumArtistName = item.audio.albumArtist?.Trim();
            string albumName = item.audio.album?.Trim();

            DriveItem driveItem = session.DriveItems.Get(item.id);

            Track track = (driveItem != null) ? session.Tracks.Get(driveItem.Track.Id.Value) : null;
            IndexedTrack indexedTrack = (track != null) ? session.Index.Get(track.Id.Value) : null;
            Artist artist = session.Artists.Find(albumArtistName);
            Album album = artist != null ? session.Albums.FindByArtist(artist.Id.Value, albumName) : null;
            ThumbnailInfo thumbnailInfo = (album != null) ? session.Thumbnails.Get(album.Id.Value) : null;
            Genre genre = session.Genres.Find(genreName);

            // If we already have a genre, do nothing. If not create a new one
            bool isGenreNew = (genre == null);
            genre = genre ?? new Genre() { Name = genreName };
            if (isGenreNew)
            {
                session.Genres.Add(genre);
            }

            // If we already have an artist, do nothing. If not create a new one
            bool isArtistNew = (artist == null);
            artist = artist ?? new Artist() { Name = albumArtistName };
            if (isArtistNew)
            {
                session.Artists.Add(artist);
            }

            // If we already have an album, do nothing. If not create a new one
            bool isAlbumNew = (album == null);
            album = album ?? new Album()
            {
                Name = albumName,
                Artist = new Artist() { Id = artist.Id },
                Genre = new Genre() { Id = genre.Id }
            };
            bool albumNeedsUpdate = (album.ReleaseYear != item.audio.year) || (album.Genre.Id != genre.Id);
            album.ReleaseYear = item.audio.year;
            album.Genre.Id = genre.Id;
            _ = isAlbumNew ? session.Albums.Add(album) : albumNeedsUpdate ? session.Albums.Update(album) : null;

            // If we have a track, update its properties. If not create a new one
            bool isTrackNew = (track == null);
            track = track ?? new Track();
            track.Title = item.audio.title?.Trim();
            track.Number = item.audio.track;
            track.Artist = item.audio.artist?.Trim();
            track.Bitrate = item.audio.bitrate;
            track.Composers = item.audio.composers?.Trim();
            track.Duration = TimeSpan.FromMilliseconds(item.audio.duration);
            track.ReleaseYear = item.audio.year;
            track.Genre = new Genre() { Id = genre.Id };
            track.Album = new Album() { Id = album.Id };
            _ = isTrackNew ? session.Tracks.Add(track) : session.Tracks.Update(track);

            // If we have a driveItem already, update its properties. If not create a new one
            bool isDriveItemNew = (driveItem == null);
            driveItem = driveItem ?? new DriveItem();
            driveItem.Id = item.id;
            driveItem.Name = item.name?.Trim();
            driveItem.Description = item.description?.Trim();
            driveItem.CTag = item.cTag;
            driveItem.ETag = item.eTag;
            driveItem.AddedDate = item.createdDateTime;
            driveItem.LastModified = item.lastModifiedDateTime;
            driveItem.DownloadUrl = item.DownloadUrl;
            driveItem.Size = (int)item.size;
            driveItem.Track = new Track() { Id = track.Id };
            driveItem.Source = DriveItemSource.OneDrive;
            _ = isDriveItemNew ? session.DriveItems.Add(driveItem) : session.DriveItems.Update(driveItem);

            // Replicate whatever changes had to be done to the Track to IndexedTrack
            bool isIndexedTrackNew = (indexedTrack == null);
            indexedTrack = indexedTrack ?? new IndexedTrack();
            indexedTrack.Id = track.Id;
            indexedTrack.FileName = driveItem.Name;
            indexedTrack.AlbumName = album.Name;
            indexedTrack.SetAlbumId(album.Id.Value);
            indexedTrack.ArtistName = artist.Name;
            indexedTrack.SetArtistId(artist.Id.Value);
            indexedTrack.GenreName = genre.Name;
            indexedTrack.SetGenreId(genre.Id.Value);
            indexedTrack.TrackArtist = track.Artist;
            indexedTrack.TrackName = track.Title;
            _ = isIndexedTrackNew ? session.Index.Add(indexedTrack) : session.Index.Update(indexedTrack);

            // Add or update thumbnail
            bool isThumbnailInfoNew = (thumbnailInfo == null);
            thumbnailInfo = thumbnailInfo ?? new ThumbnailInfo();
            thumbnailInfo.Id = album.Id;
            thumbnailInfo.AttemptCount = 0;
            thumbnailInfo.SmallUrl = item.Thumbnails?.FirstOrDefault()?.Small?.Url;
            thumbnailInfo.MediumUrl = item.Thumbnails?.FirstOrDefault()?.Medium?.Url;
            thumbnailInfo.LargeUrl = item.Thumbnails?.FirstOrDefault()?.Large?.Url;
            thumbnailInfo.Cached = string.IsNullOrEmpty(thumbnailInfo.SmallUrl) && string.IsNullOrEmpty(thumbnailInfo.MediumUrl) ? true : false;
            _ = isThumbnailInfoNew ? session.Thumbnails.Add(thumbnailInfo) : session.Thumbnails.Update(thumbnailInfo);
        }

        public async Task<bool> DownloadThumbnailsAsync(IEditSession session)
        {
            // Get all thumbnails that have not been cached
            var thumbnails = session.Thumbnails.GetUncached();

            // Download and save the thumbnails
            await thumbnails.ForEachAsync(session, DownloadAndCacheThumbnailAsync);

            return thumbnails.Count > 0;
        }

        private async Task DownloadAndCacheThumbnailAsync(ThumbnailInfo info, IEditSession session)
        {
            try
            {
                string url = info.MediumUrl ?? info.SmallUrl;

                if (!string.IsNullOrEmpty(url))
                {
                    using (HttpResponseMessage message = await webClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        using (var stream = await message.Content.ReadAsStreamAsync())
                        {
                            await session.AlbumArts.SaveAsync(info.Id.Value, stream);
                        }
                    }
                }

                info.Cached = true;
            }
            catch (Exception)
            {
                info.AttemptCount++;
                info.Cached = false;
            }
            finally
            {
                session.Thumbnails.Update(info);
            }
        }
    }
}
