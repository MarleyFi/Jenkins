using System;
using System.IO;
using System.Linq;
using System.Text;
using IniParser;
using IniParser.Model;

namespace DiscordBot
{
    internal class Configuration
    {
        private const string General = "General";

        private const string Admin = "Admin";

        private const string Apis = "Apis";

        public bool ParseSuccessfull = false;

        private FileIniDataParser fileIniData = new FileIniDataParser();

        private IniData parsedData;

        public string filePath;

        public string directoryPath;

        public Configuration()
        {
            fileIniData.Parser.Configuration.CommentString = "#";
            if (Directory.Exists(@"\Users\Administrator\Desktop\Dropbox\Projects\Discord.NET\DiscordBot\bin\Debug\files\"))
            {
                directoryPath = @"\Users\Administrator\Desktop\Dropbox\Projects\Discord.NET\DiscordBot\bin\Debug\files\";
                filePath = @"C:\Users\Administrator\Desktop\Dropbox\Projects\Discord.NET\DiscordBot\bin\Debug\files\config.ini";
            }
            else
            {
                directoryPath = Path.Combine(Environment.CurrentDirectory, "files");
                filePath = Path.Combine(Environment.CurrentDirectory, "files", "config.ini");
            }

            if (!File.Exists(filePath))
            {
                File.Create(filePath);
            }
            LoadConfig();
            UpdateConfigTimestamp();
        }

        #region Variables

        public string DiscordToken { get; set; }

        public bool Muted { get; set; }

        public bool TrollVictims { get; set; }

        public bool TTSEnabled { get; set; }

        public int RandomTalkChance { get; set; }

        public int RandomActionChance { get; set; }

        public int TwitchCheckInterval { get; set; }

        public int TwitchChannelLimit { get; set; }

        public int GiphySearchLimit { get; set; }

        public string CleverbotNick { get; set; }

        public bool CleverbotEnabled { get; set; }

        public bool GamesSyncEnabled { get; set; }

        public int CreateNewRoleLimit { get; set; }

        public bool GamesSyncNotifyUsers { get; set; }

        public bool DailyVote { get; set; }

        public string DailyVoteStart { get; set; }

        public string DailyVoteEnd { get; set; }

        public ulong DailyVoteChannel { get; set; }

        public bool SpotifyEnabled { get; set; }

        public DateTime LastLoad { get; set; }

        public bool Debug { get; set; }

        public ulong OwnerID { get; set; }

        public string AdminRoleName { get; set; }

        public int NukeLimit { get; set; }

        public bool DailyBackupEnabled { get; set; }

        public string DailyBackupTime { get; set; }

        public string GoogleAPIKey { get; set; }

        public string GoogleSearchEngineKey { get; set; }

        public string CleverbotAPIUser { get; set; }

        public string CleverbotAPIKey { get; set; }

        public string DarkskyAPIKey { get; set; }

        public string TwitchAPIKey { get; set; }

        public string GiphyAPIKey { get; set; }

        public string SpotifyAPIKey { get; set; }

        #endregion Variables

        public void LoadConfig()
        {
            try
            {
                parsedData = fileIniData.ReadFile(filePath);
                parsedData.Configuration.ThrowExceptionsOnError = false;
                this.Muted = bool.Parse(parsedData[General]["Muted"]);
                this.TrollVictims = bool.Parse(parsedData[General]["TrollVictims"]);
                this.TTSEnabled = bool.Parse(parsedData[General]["TTSEnabled"]);
                this.RandomTalkChance = int.Parse(parsedData[General]["RandomTalkChance"]);
                this.RandomActionChance = int.Parse(parsedData[General]["RandomActionChance"]);
                this.TwitchCheckInterval = int.Parse(parsedData[General]["TwitchCheckInterval"]);
                this.TwitchChannelLimit = int.Parse(parsedData[General]["TwitchChannelLimit"]);
                this.GiphySearchLimit = int.Parse(parsedData[General]["GiphySearchLimit"]);
                this.CleverbotNick = parsedData[General]["CleverbotNick"];
                this.CleverbotEnabled = bool.Parse(parsedData[General]["CleverbotEnabled"]);
                this.GamesSyncEnabled = bool.Parse(parsedData[General]["GamesSyncEnabled"]);
                this.CreateNewRoleLimit = int.Parse(parsedData[General]["CreateNewRoleLimit"]);
                this.GamesSyncNotifyUsers = bool.Parse(parsedData[General]["GamesSyncNotifyUsers"]);
                this.DailyVote = bool.Parse(parsedData[General]["DailyVote"]);
                this.DailyVoteStart = parsedData[General]["DailyVoteStart"];
                this.DailyVoteEnd = parsedData[General]["DailyVoteEnd"];
                this.DailyVoteChannel = ulong.Parse(parsedData[General]["DailyVoteChannel"]);
                this.SpotifyEnabled = bool.Parse(parsedData[General]["SpotifyEnabled"]);
                this.Debug = bool.Parse(parsedData[Admin]["Debug"]);
                this.OwnerID = ulong.Parse(parsedData[Admin]["OwnerID"]);
                this.AdminRoleName = parsedData[Admin]["AdminRoleName"];
                this.NukeLimit = int.Parse(parsedData[Admin]["NukeLimit"]);
                this.DailyBackupEnabled = bool.Parse(parsedData[Admin]["DailyBackupEnabled"]);
                this.DailyBackupTime = parsedData[Admin]["DailyBackupTime"];
                this.DiscordToken = parsedData[Apis]["DiscordToken"];
                this.GoogleAPIKey = parsedData[Apis]["GoogleAPIKey"];
                this.GoogleSearchEngineKey = parsedData[Apis]["GoogleSearchEngineKey"];
                this.CleverbotAPIUser = parsedData[Apis]["CleverbotAPIUser"];
                this.CleverbotAPIKey = parsedData[Apis]["CleverbotAPIKey"];
                this.DarkskyAPIKey = parsedData[Apis]["DarkskyAPIKey"];
                this.TwitchAPIKey = parsedData[Apis]["TwitchAPIKey"];
                this.GiphyAPIKey = parsedData[Apis]["GiphyAPIKey"];
                this.SpotifyAPIKey = parsedData[Apis]["SpotifyAPIKey"];
                ParseSuccessfull = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while loading configuration");
                Console.WriteLine(e.Message);
                Console.WriteLine("Loading default config...");
                Console.WriteLine("Please enter DiscordBot-token:");
                string discordToken = Console.ReadLine();
                this.Muted = true;
                this.TrollVictims = false;
                this.TTSEnabled = false;
                this.RandomTalkChance = 10;
                this.RandomActionChance = 12;
                this.TwitchCheckInterval = 120;
                this.TwitchChannelLimit = 5;
                this.GiphySearchLimit = 100;
                this.CleverbotNick = "JenkinsBOT";
                this.CleverbotEnabled = false;
                this.GamesSyncEnabled = false;
                this.CreateNewRoleLimit = 2;
                this.GamesSyncNotifyUsers = false;
                this.DailyVote = false;
                this.DailyVoteStart = "00:00:00";
                this.DailyVoteEnd = "00:01:00";
                this.DailyVoteChannel = 0;
                this.SpotifyEnabled = false;
                this.Debug = true;
                this.OwnerID = 0;
                this.AdminRoleName = "Admin";
                this.NukeLimit = 50;
                this.DailyBackupEnabled = true;
                this.DailyBackupTime = "02:00:00";
                this.DiscordToken = discordToken;
                this.GoogleAPIKey = string.Empty;
                this.GoogleSearchEngineKey = string.Empty;
                this.CleverbotAPIUser = string.Empty;
                this.CleverbotAPIKey = string.Empty;
                this.DarkskyAPIKey = string.Empty;
                this.TwitchAPIKey = string.Empty;
                this.GiphyAPIKey = string.Empty;
                this.SpotifyAPIKey = string.Empty;
            }
        }

        public string GetConfiguration(bool censor = true)
        {
            if (!ParseSuccessfull)
            {
                return "Config is damaged.\r\nDefault.cfg was loaded.";
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<- - - **Configuration** - - ->");
            foreach (var section in parsedData.Sections)
            {
                sb.AppendLine();
                sb.AppendLine(string.Format("**[{0}]**"
                    , section.SectionName
                    ));
                foreach (var key in section.Keys)
                {
                    sb.AppendLine(string.Format("- > {0}: **{1}**"
                        , key.KeyName
                        , (censor && section.SectionName == "Apis") ? "*<censored>*" : key.Value));
                }
            }

            sb.AppendLine();
            sb.AppendLine("I'm online for **" + Supporter.GetTimeSince(DateTime.Parse(parsedData.Sections[General]["LastLoad"])) + "** right now");

            return sb.ToString();
        }

        private void UpdateConfigTimestamp()
        {
            if (!ParseSuccessfull)
                return;

            parsedData[General]["LastLoad"] = DateTime.Now.ToString();
            fileIniData.WriteFile(filePath, parsedData);
        }
    }
}