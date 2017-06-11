using System;
using System.Collections.Generic;
using System.Linq;
using Discord.Commands;
using SpotifyAPI.Local;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace DiscordBot
{
    internal static class Spotify // fkin' Spotify lul
    {
        public static SpotifyWebAPI spotify;
        public static SpotifyLocalAPI API;
        public static PrivateProfile profile;
        public static List<FullTrack> savedTracks;
        public static List<SimplePlaylist> playlists;

        public static void Init()
        {
            if (Bot.Config.SpotifyEnabled)
                RunAuthentication();

            //savedTracks = new List<FullTrack>();
        }

        public static void InitialSetup()
        {
            profile = spotify.GetPrivateProfile();
            playlists = GetPlaylists();
            savedTracks = GetSavedTracks();
            //API = new SpotifyLocalAPI();
            //API.Connect();

            //if (InvokeRequired)
            //{
            //    Invoke(new Action(InitialSetup));
            //    return;
            //}

            //savedTracksCountLabel.Text = savedTracks.Count.ToString();
            //savedTracks.ForEach(track => savedTracksListView.Items.Add(new ListViewItem()
            //{
            //    Text = track.Name,
            //    SubItems = { string.Join(",", track.Artists.Select(source => source.Name)), track.Album.Name }
            //}));

            //playlistsCountLabel.Text = playlists.Count.ToString();
            //playlists.ForEach(playlist => playlistsListBox.Items.Add(playlist.Name));

            //displayNameLabel.Text = profile.DisplayName;
            //countryLabel.Text = profile.Country;
            //emailLabel.Text = profile.Email;
            //accountLabel.Text = profile.Product;

            //if (profile.Images != null && profile.Images.Count > 0)
            //{
            //    using (WebClient wc = new WebClient())
            //    {
            //        byte[] imageBytes = await wc.DownloadDataTaskAsync(new Uri(profile.Images[0].Url));
            //        using (MemoryStream stream = new MemoryStream(imageBytes))
            //            //avatarPictureBox.Image = Image.FromStream(stream);
            //    }
            //}
        }

        public static SimplePlaylist GetPlaylist(string name)
        {
            IEnumerable<SimplePlaylist> results = playlists.Where(r => r.Name.ToLower().Contains(name.ToLower()));
            return results.First();
        }

        public static FullTrack GetTrack(string name)
        {
            IEnumerable<FullTrack> results = savedTracks.Where(r => r.Name.ToLower().Contains(name.ToLower()));
            var song = results.First();
            return song;
        }

        public static List<FullTrack> GetSavedTracks()
        {
            Paging<SavedTrack> savedTracks = spotify.GetSavedTracks();
            List<FullTrack> list = savedTracks.Items.Select(track => track.Track).ToList();

            while (savedTracks.Next != null)
            {
                savedTracks = spotify.GetSavedTracks(20, savedTracks.Offset + savedTracks.Limit);
                list.AddRange(savedTracks.Items.Select(track => track.Track));
            }

            return list;
        }

        public static List<SimplePlaylist> GetPlaylists()
        {
            Paging<SimplePlaylist> playlists = spotify.GetUserPlaylists(profile.Id);
            List<SimplePlaylist> list = playlists.Items.ToList();

            while (playlists.Next != null)
            {
                playlists = spotify.GetUserPlaylists(profile.Id, 20, playlists.Offset + playlists.Limit);
                list.AddRange(playlists.Items);
            }

            return list;
        }

        public static async void RunAuthentication()
        {
            WebAPIFactory webApiFactory = new WebAPIFactory(
                "http://localhost",
                8000,
                Bot.Config.SpotifyAPIKey, Scope.Streaming | Scope.UserLibraryRead | Scope.PlaylistReadPrivate);

            try
            {
                spotify = await webApiFactory.GetWebApi();
            }
            catch (Exception ex)
            {
                Console.WriteLine("SpotifyWebAPI-Init failed. Message:");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Writing log...");
                DiscordBotLog.AppendLog(DiscordBotLog.BuildRuntimeExceptionMessage(ex));
            }

            if (spotify == null)
                return;

            InitialSetup();
        }
    }
}