using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using Discord;
using Newtonsoft.Json.Linq;

namespace DiscordBot
{
    internal class Twitch
    {
        private System.Timers.Timer streamCheck; 
        public Twitch()
        {
            streamCheck = new System.Timers.Timer((Bot.Config.TwitchCheckInterval * 1000));
            streamCheck.Elapsed += StreamCheck_Elapsed;
            streamCheck.Start();
        }
        
        private void StreamCheck_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CheckForStartedStreams();
        }

        #region Twitch

        #region Essential Commands

        public void RegisterTwitchChannel(string name, Channel channel = null)
        {
            using (var client = new HttpClient())
            {
                string request = string.Format("https://api.twitch.tv/kraken/channels/{0}?client_id={1}"
                   , name
                   , Bot.Config.TwitchAPIKey);
                try
                {
                    var response = client.GetStringAsync(request);
                    response.Wait();
                    var channelObject = JObject.Parse(response.Result);
                    Jenkins.Twitch.AddTwitchChannel(channelObject);
                }
                catch (Exception e)
                {
                    Bot.NotifyDevs(Supporter.BuildExceptionMessage(e, "RegisterTwitchChannel()", request));
                    if (channel != null)
                        Bot.SendMessage("There's no Twitch-Channel called **" + name + "** :(", channel);
                }
            }
        }

        private void CheckForStartedStreams()
        {
            if (!Bot.Config.ParseSuccessfull)
            {
                return;
            }
            string[] twitchChannels = Jenkins.Twitch.GetGlobalObservingTwitchChannels();

            Dictionary<ulong, string> broadcastList = new Dictionary<ulong, string>();
            foreach (var twitchChannel in twitchChannels)
            {
                string request = "";
                ulong[] discordChannelsToBroadcast = Jenkins.Twitch.GetFollowingDiscordChannelsForTwitchChannel(twitchChannel);

                if (discordChannelsToBroadcast.Length == 0 && false) // ToDo Exception #1
                {
                    Bot.NotifyDevs("Incontinence in database for Twitch-Channel **" + twitchChannel + "**");
                    return;
                }

                request = string.Format("https://api.twitch.tv/kraken/streams/{0}?client_id={1}"
               , twitchChannel
               , Bot.Config.TwitchAPIKey);
                try
                {
                    using (var client = new HttpClient())
                    {
                        var response = client.GetStringAsync(request);
                        response.Wait();
                        var requestObject = JObject.Parse(response.Result);
                        var streamString = requestObject.GetValue("stream").ToString();
                        if ((streamString != null && streamString != string.Empty))
                        {
                            var streamObject = JObject.Parse(streamString);
                            ulong streamId = ulong.Parse(streamObject.GetValue("_id").ToString());
                            if (Jenkins.Twitch.IsStreamNotPosted(streamId))
                            {
                                var channelString = streamObject.GetValue("channel").ToString();
                                var channelObject = JObject.Parse(channelString);
                                Jenkins.Twitch.AddStream(streamObject, channelObject);
                                string broadcast = Supporter.BuildStreamBroadcast(Jenkins.Twitch.GetStream(streamId));
                                foreach (var discordChannel in discordChannelsToBroadcast)
                                {
                                    broadcastList.Add(discordChannel, broadcast);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Bot.NotifyDevs(Supporter.BuildExceptionMessage(e, "CheckForStartedStreams()", request));
                }
            }
            BroadcastStreamUpdates(broadcastList);
        }

        private async void BroadcastStreamUpdates(Dictionary<ulong, string> broadcasts)
        {
            foreach (var broadcast in broadcasts)
            {
                try
                {
                    Channel channelToPost = Bot.Client.GetChannel(broadcast.Key);
                    await channelToPost.SendMessage(broadcast.Value);
                }
                catch (Exception e)
                {
                    Bot.NotifyDevs(Supporter.BuildExceptionMessage(e, "BroadcastStreamUpdates\r\nInconsistence for parametred channelId", new object[] { broadcast.Key }));
                }
            }
        }

        #endregion 

        public string[] GetGlobalObservingTwitchChannels() // For broadcast or Admin-View
        {
            List<string> channelList = new List<string>();
            DataTable channelsTable = Jenkins.Database.Tables["TWITCHCHANNELS"];
            var channels = channelsTable.AsEnumerable();
            // ToDo: GroupBy
            foreach (var channel in channels)
            {
                string channelName = channel.Field<string>("NAME");
                channelList.Add(channelName);
            }
            return channelList.ToArray();
        }

        public string[] GetObservingTwitchChannelsForDiscordChannel(ulong channelId)
        {
            List<string> channelList = new List<string>();
            var twitchChannelRows = Jenkins.Database.Tables["TWITCHCHANNELS"].Rows;
            var twitchChannels = Jenkins.Database.Tables["TWITCHCHANNELS"].AsEnumerable();
            var discordChannels = Jenkins.Database.Tables["TWITCHDISCORDCHANNELS"].AsEnumerable();

            discordChannels = discordChannels
                .Where(r => r.Field<ulong>("DISCORDCHANNELID").Equals(channelId));

            foreach (var discordChannel in discordChannels)
            {
                int twitchChannelId = discordChannel.Field<int>("TWITCHCHANNELID");
                if (twitchChannelRows.Contains(twitchChannelId))
                {
                    channelList.Add(twitchChannelRows.Find(twitchChannelId).Field<string>("NAME"));
                }
            }
            return channelList.ToArray();
        }

        public ulong[] GetFollowingDiscordChannelsForTwitchChannel(string name)
        {
            List<ulong> discordChannelList = new List<ulong>();
            int twitchChannelId = GetTwitchChannelIdByName(name);
            var discordChannels = Jenkins.Database.Tables["TWITCHDISCORDCHANNELS"].AsEnumerable();
            discordChannels = discordChannels
                .Where(r => r.Field<int>("TWITCHCHANNELID").Equals(twitchChannelId));

            foreach (var discordChannel in discordChannels)
            {
                discordChannelList.Add(discordChannel.Field<ulong>("DISCORDCHANNELID"));
            }

            return discordChannelList.ToArray();
        }

        public void AddTwitchChannelToWatchlistOfDiscordChannel(string twitchChannelName, User user, Channel txtChannel)
        {
            int id = GetTwitchChannelIdByName(twitchChannelName);
            ulong userId = user.Id;
            ulong channelId = txtChannel.Id;
            Jenkins.Database.Tables["TWITCHDISCORDCHANNELS"].Rows.Add(id, channelId, userId, DateTime.Now);
            Jenkins.Write();
        }

        public void RemoveTwitchChannelFromWatchlistOfDiscordChannel(string channelName, Channel txtChannel)
        {
            var twitchChannels = Jenkins.Database.Tables["TWITCHCHANNELS"].AsEnumerable();
            var twitchChannel = twitchChannels.Where(r => r.Field<string>("NAME").Equals(channelName));
            int twitchChannelId = twitchChannel.FirstOrDefault().Field<int>("ID");
            ulong channelId = txtChannel.Id;
            Jenkins.Database.Tables["TWITCHDISCORDCHANNELS"].Rows.Find(twitchChannelId).Delete();
            if (GetFollowingDiscordChannelsForTwitchChannel(channelName).Length == 0 && false) // ToDo: Exception wenn Datensatz gelöscht wird während Requests für den entsprechenden Twitch-Channel ausgeführt werden
            {
                DelTwitchChannel(channelName);
            }
            Jenkins.Write();
        }

        public bool IsTwitchChannelRegiseredByObject(JObject channelObject)
        {
            int id = int.Parse(channelObject.GetValue("_id").ToString());
            string name = channelObject.GetValue("display_name").ToString();
            var twitchChannel = Jenkins.Database.Tables["TWITCHCHANNELS"].Rows.Find(id);
            return (twitchChannel != null);
        }

        public bool IsTwitchChannelRegiseredByName(string channelName)
        {
            var twitchChannels = Jenkins.Database.Tables["TWITCHCHANNELS"].AsEnumerable();
            var twitchChannel = twitchChannels.Where(r => r.Field<string>("NAME").Equals(channelName));
            return (twitchChannel.Count() == 1);
        }

        public bool IsDiscordChannelFollowingTwitchChannel(string twitchChannelName, ulong discordChannelId)
        {
            var discordChannels = Jenkins.Database.Tables["TWITCHDISCORDCHANNELS"].AsEnumerable();
            var discordChannel = discordChannels.Where(r => r.Field<ulong>("DISCORDCHANNELID").Equals(discordChannelId))
                .Where(r => r.Field<int>("TWITCHCHANNELID").Equals(GetTwitchChannelIdByName(twitchChannelName)));

            return (discordChannel.Count() == 1);
        }

        private static int GetTwitchChannelIdByName(string twitchChannelName)
        {
            var twitchChannels = Jenkins.Database.Tables["TWITCHCHANNELS"].AsEnumerable();
            var twitchChannel = twitchChannels.Where(r => r.Field<string>("NAME").Equals(twitchChannelName));
            var twitchChannelId = twitchChannel.FirstOrDefault().Field<int>("ID"); // Know error while deleting a

            return twitchChannelId;
        }

        public void AddTwitchChannel(JObject channelObject)
        {
            int id = int.Parse(channelObject.GetValue("_id").ToString());
            string name = channelObject.GetValue("display_name").ToString();
            string logo = channelObject.GetValue("logo").ToString();
            Jenkins.Database.Tables["TWITCHCHANNELS"].Rows.Add(id, name, logo, DateTime.Now);
            Jenkins.Write();
        }

        public void DelTwitchChannel(string channelName)
        {
            if (GetFollowingDiscordChannelsForTwitchChannel(channelName).Length != 0) // Should not happen
            {
                Console.WriteLine("Detected (system-architecture ?) failure in Twitch.cs:");
                Console.WriteLine("Tried to delete Twitch-Channel " + channelName + " while it still got following Discord-Channels:");
                foreach (var discordChannel in GetFollowingDiscordChannelsForTwitchChannel(channelName))
                {
                    Console.WriteLine(discordChannel);
                }
            }
            int twitchChannelId = GetTwitchChannelIdByName(channelName);
            Jenkins.Database.Tables["TWITCHCHANNELS"].Rows.Find(twitchChannelId).Delete();
            Jenkins.Write();
        }

        public bool IsStreamNotPosted(ulong streamId)
        {
            DataTable streamsTable = Jenkins.Database.Tables["TWITCHSTREAMS"];
            var streams = streamsTable.AsEnumerable();
            streams = streams
                .Where(r => r.Field<ulong>("ID").Equals(streamId));

            return ((streams.Count() == 0));
        }

        public void AddStream(JObject streamObject, JObject channelObject)
        {
            ulong id = ulong.Parse(streamObject.GetValue("_id").ToString());
            string channel = channelObject.GetValue("display_name").ToString(); ;
            string game = streamObject.GetValue("game").ToString();
            string url = channelObject.GetValue("url").ToString();
            Jenkins.Database.Tables["TWITCHSTREAMS"].Rows.Add(id, channel, game, url, DateTime.Now);
            Jenkins.Write();
        }

        public DataRow GetStream(ulong streamId)
        {
            DataTable streamsTable = Jenkins.Database.Tables["TWITCHSTREAMS"];
            var streams = streamsTable.AsEnumerable();
            DataRow stream = streams
                .Where(r => r.Field<ulong>("ID").Equals(streamId)).First();
            return stream;
        }

        #endregion Twitch
    }
}