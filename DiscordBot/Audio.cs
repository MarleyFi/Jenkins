using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Discord;
using Discord.Audio;
using NAudio.Wave;
using YoutubeExtractor;

namespace DiscordBot
{
    internal class Audio
    {
        #region Variables

        public static bool IsPlaying = true;
        private static string audioPath;
        private static string mp3Path;
        public static Message MediaMessage;
        private static string mediaName;
        private static User User;
        private static Channel TextChannel;
        private static Channel vChannelToStream;

        #endregion Variables

        #region Methods

        public static void DownloadAndPlayURL(string URL, Message botMsg, User user, Channel vChannel)
        {
            TextChannel = botMsg.Channel;
            MediaMessage = botMsg;
            User = user;
            vChannelToStream = vChannel;
            try
            {
                DownloadAudio(URL);
            }
            catch (Exception e)
            {
                Bot.NotifyDevs(Supporter.BuildExceptionMessage(e));
            }
        }

        private async static void DownloadAudio(string URL)
        {
            string link = URL;
            IEnumerable<YoutubeExtractor.VideoInfo> videoInfos = null;
            try
            {
                videoInfos = DownloadUrlResolver.GetDownloadUrls(link);
                MediaMessage = await TextChannel.SendMessage("Downloading audio...");
            }
            catch (Exception)
            {
                await MediaMessage.Delete();
                await User.SendMessage("Invalid link :(");
                return;
            }

            VideoInfo video = videoInfos.First(info => info.VideoType == VideoType.Mp4 && info.Resolution == 0);
            mediaName = video.Title;
            audioPath = Path.Combine(Environment.CurrentDirectory, "files", "audio", video.Title + video.AudioExtension);
            mp3Path = Path.Combine(Environment.CurrentDirectory, "files", "mp3", video.Title + ".mp3");
            var audioDownloader = new VideoDownloader(video, audioPath);

            audioDownloader.DownloadFinished += AudioDownloader_DownloadFinished;
            try
            {
                audioDownloader.Execute();
            }
            catch (Exception e)
            {
                Bot.NotifyDevs(Supporter.BuildExceptionMessage(e, "DownloadAudio()", URL));
                await MediaMessage.Edit("Failed downloading audio");
            }
        }

        /// <summary>
        /// Download finished. Starting to stream after this
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async static void AudioDownloader_DownloadFinished(object sender, EventArgs e)
        {
            var waveProvider = new MediaFoundationReader(audioPath);
            MediaFoundationEncoder.EncodeToMp3(waveProvider, mp3Path);
            await MediaMessage.Edit(string.Format("Now playing **{0}**\r\nby **{1}**", mediaName, User.Name));
            StreamFileToVoiceChannel(mp3Path, vChannelToStream);
        }

        public static async void StreamFileToVoiceChannel(string path, Channel vChannel)
        {
            var vClient = await Bot.Client.GetService<AudioService>().Join(vChannel);
            var channelCount = Bot.Client.GetService<AudioService>().Config.Channels;
            var OutFormat = new WaveFormat(48000, 16, channelCount);
            using (var MP3Reader = new Mp3FileReader(path))
            using (var resampler = new MediaFoundationResampler(MP3Reader, OutFormat))
            {
                resampler.ResamplerQuality = 60;
                int blockSize = OutFormat.AverageBytesPerSecond / 50;
                byte[] buffer = new byte[blockSize];
                int byteCount;

                while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0/* && playing*/)
                {
                    if (byteCount < blockSize)
                    {
                        for (int i = byteCount; i < blockSize; i++)
                            buffer[i] = 0;
                    }
                    vClient.Send(buffer, 0, blockSize);
                }
                vClient.Wait();
            }
            await vClient.Disconnect();
        }

        public static async void StreamFileOrUrlTovChannel(string pathOrUrl, Channel vChannel)
        {
            var vClient = await Bot.Client.GetService<AudioService>().Join(vChannel);

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i {pathOrUrl} " +
            "-f s16le -ar 48000 -ac 2 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
            Thread.Sleep(2000);
            int blockSize = 3840;
            byte[] buffer = new byte[blockSize];
            int byteCount;
            while (true)
            {
                byteCount = process.StandardOutput.BaseStream
                .Read(buffer, 0, blockSize);
                if (byteCount == 0)
                    break;
                vClient.Send(buffer, 0, byteCount);
            }
            vClient.Wait();
        }

        #endregion Methods
    }
}