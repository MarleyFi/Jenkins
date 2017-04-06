using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot
{
    internal static class Jenkins
    {
        #region Internal Variables

        public static string filePath;

        private static System.Threading.Timer backupTimer;

        public static DataSet Database = new DataSet("JENKINSDB")
        {
            EnforceConstraints = false
        };

        #region Modules

        public static Users Users = new Users();

        public static Quotes Quotes = new Quotes();

        public static Insults Insults = new Insults();

        public static Twitch Twitch = new Twitch();

        #endregion Modules

        #endregion Internal Variables

        #region Essential methods

        public static void Init()
        {
            if (Directory.Exists(@"\Users\Administrator\Desktop\Dropbox\Projects\Discord.NET\DiscordBot\bin\Debug\files\"))
            {
                filePath = @"C:\Users\Administrator\Desktop\Dropbox\Projects\Discord.NET\DiscordBot\bin\Debug\files\jenkins.xml";
            }
            else
            {
                filePath = Path.Combine(Environment.CurrentDirectory, "files", "jenkins.xml");
            }
            Database.Tables.Add(CreateUsersTable());
            Database.Tables.Add(CreateConfigTable());
            Database.Tables.Add(CreateUserStatsTable());
            Database.Tables.Add(CreateInsultsTable());
            Database.Tables.Add(CreateAdminTable());
            Database.Tables.Add(CreateQuotesTable());
            Database.Tables.Add(CreateInsultVictimsTable());
            Database.Tables.Add(CreateTwitchChannelsTable());
            Database.Tables.Add(CreateTwitchDiscordChannelsTable());
            Database.Tables.Add(CreateTwitchStreamsTable());
            Database.Tables.Add(CreateFoodOptionsTable());
            Database.Tables.Add(CreateObserveTable());
            Read();
            CheckAndScheduleBackUp(Bot.Config.DailyBackupEnabled);
            if (Bot.Config.DailyVote)
                Food.ScheduleNextVote();
        }

        public static void Write()
        {
            Database.WriteXml(filePath);
        }

        public static void Read()
        {
            Database.ReadXml(filePath);
        }

        #region Backup

        public static string CheckAndScheduleBackUp(bool scheduleBackup = false)
        {
            string answer;
            //var DailyTime = Supporter.ValidateTime(Bot.Config.DailyBackupTime) ? Bot.Config.DailyBackupTime : "02:00:00";
            //var timeParts = DailyTime.Split(new char[1] { ':' });

            //var dateNow = DateTime.Now;
            //var date = new DateTime(dateNow.Year, dateNow.Month, dateNow.Day,
            //           int.Parse(timeParts[0]), int.Parse(timeParts[1]), int.Parse(timeParts[2]));
            //TimeSpan ts;
            //if (date > dateNow)
            //    ts = date - dateNow; // Backup scheduled
            //else
            //{
            //    date = date.AddDays(1); // Backup made
            //    ts = date - dateNow;
            //}

            DateTime dateNow = DateTime.Now;
            DateTime backupDate = Supporter.GetParsedDateTime(Supporter.ValidateTime(Bot.Config.DailyBackupTime) ? Bot.Config.DailyBackupTime : "02:00:00");
            TimeSpan ts;
            if (backupDate > dateNow)
                ts = backupDate - dateNow;
            else
            {
                backupDate = backupDate.AddDays(1);
                ts = backupDate - dateNow;
            }

            var backupDirectory = new DirectoryInfo(Path.Combine(filePath.Replace("jenkins.xml", ""), "backup"));
            var myFile = backupDirectory.GetFiles()
             .OrderByDescending(f => f.LastWriteTime)
             .First();
            if (Bot.Config.DailyBackupEnabled)
            {
                answer = string.Format("Last backup :floppy_disk: was **{0}**, next one is scheduled for **{1}** in **{2}**",
                Supporter.GetDuration(dateNow - myFile.LastWriteTime) + "ago",
                backupDate.ToLongTimeString(),
                Supporter.GetDuration(ts));
            }
            else
            {
                answer = "Daily-backup :floppy_disk: is currently not activated.";
            }


            //waits certan time and run the code
            //Task.Delay(ts).ContinueWith((x) => DoBackup());
            if (scheduleBackup)
            {
                SetUpTimer(ts);
            }
            return answer;
        }

        private static void SetUpTimer(TimeSpan timeToGo)
        {
            if (timeToGo < TimeSpan.Zero)
            {
                Bot.NotifyDevs("Unhandled exception(?) in SetUpTimer()\r\n" + timeToGo.ToString());
                //SetUpTimer((new TimeSpan(1,0,0,0) - timeToGo.Negate()));
                return;//time already passed
            }
            backupTimer = new System.Threading.Timer(x =>
            {
                DoBackup();
            }, null, timeToGo, Timeout.InfiniteTimeSpan);
        }

        private static void DoBackup()
        {
            Bot.NotifyDevs("Running scheduled backup...");
            string path = filePath.Replace("jenkins.xml", "");
            string backupPath = Path.Combine(path, "backup");
            if (!Directory.Exists(backupPath))
                Directory.CreateDirectory(backupPath);
            string fileName = string.Format("jenkins_{0}.xml",
                Supporter.GetFileDateString(DateTime.Now));
            string combinedFilePath = Path.Combine(backupPath, fileName);
            //            if (!File.Exists(combinedFilePath))
            try
            {
                File.Copy(filePath, combinedFilePath, true);
            }
            catch (Exception e)
            {
                Bot.NotifyDevs(Supporter.BuildExceptionMessage(e, "DoBackup()", filePath + "\r\n" + combinedFilePath));
            }
            Bot.NotifyDevs(string.Format("Backup for **{0}** complete. :floppy_disk::white_check_mark:",
                Supporter.GetFileDateString(DateTime.Now)));

            CheckAndScheduleBackUp();
        }

        #endregion Backup

        #endregion Essential methods

        #region Tables

        /// <summary>
        /// Userstable
        /// </summary>
        private static DataTable CreateUsersTable()
        {
            // Here we create a DataTable with four columns.
            DataTable usersTable = new DataTable("USERS");
            usersTable.Columns.Add("NAME", typeof(string));
            usersTable.Columns.Add("ID", typeof(ulong));
            usersTable.PrimaryKey = new DataColumn[] { usersTable.Columns["ID"] };

            return usersTable;
        }

        private static DataTable CreateConfigTable()
        {
            // Here we create a DataTable with four columns.
            DataTable usersTable = new DataTable("CONFIGS");
            usersTable.Columns.Add("PROFILENAME", typeof(string));
            usersTable.Columns.Add("INSULTCHANCE", typeof(int));
            usersTable.Columns.Add("RANDOMTALKCHANCE", typeof(int));
            usersTable.Columns.Add("TWITCHCHECKINTERVAL", typeof(int));
            usersTable.Columns.Add("MUTED", typeof(bool));
            usersTable.Columns.Add("DEBUG", typeof(bool));
            usersTable.Columns.Add("CLEVERBOTAPIUSER", typeof(string));
            usersTable.Columns.Add("CLEVERBOTAPIKEY", typeof(string));
            usersTable.Columns.Add("DARKSKYAPIKEY", typeof(string));
            usersTable.Columns.Add("TWITCHAPIKEY", typeof(string));
            usersTable.Columns.Add("GIPHYAPIKEY", typeof(string));
            usersTable.Columns.Add("LASTEDIT", typeof(DateTime));
            usersTable.PrimaryKey = new DataColumn[] { usersTable.Columns["PROFILENAME"] };

            return usersTable;
        }

        /// <summary>
        /// Userstable
        /// </summary>
        private static DataTable CreateUserStatsTable()
        {
            // Here we create a DataTable with four columns.
            DataTable userStatsTable = new DataTable("USERSTATS");
            userStatsTable.Columns.Add("USERID", typeof(ulong));
            userStatsTable.Columns.Add("MESSAGECOUNT", typeof(int));
            userStatsTable.Columns.Add("TALKEDTOMECOUNT", typeof(int));
            userStatsTable.Columns.Add("LASTACTIVITY", typeof(DateTime));
            userStatsTable.Columns.Add("COMMANDCOUNT", typeof(int));
            userStatsTable.Columns.Add("MORONPERC", typeof(int));
            userStatsTable.Columns.Add("REGISTERDATE", typeof(DateTime));
            userStatsTable.PrimaryKey = new DataColumn[] { userStatsTable.Columns["USERID"] };
            return userStatsTable;
        }

        private static DataTable CreateInsultsTable()
        {
            // Here we create a DataTable with four columns.
            DataTable insultsTable = new DataTable("INSULTS");
            insultsTable.Columns.Add("USERID", typeof(ulong));
            insultsTable.Columns.Add("MESSAGE", typeof(string));
            insultsTable.Columns.Add("REGISTERDATE", typeof(DateTime));
            insultsTable.PrimaryKey = new DataColumn[] { insultsTable.Columns["USERID"] };
            return insultsTable;
        }

        private static DataTable CreateAdminTable()
        {
            DataTable adminTable = new DataTable("ADMINS");
            adminTable.Columns.Add("USERID", typeof(ulong));
            adminTable.Columns.Add("SERVERID", typeof(ulong));
            adminTable.Columns.Add("ISDEV", typeof(bool));
            adminTable.PrimaryKey = new DataColumn[] { adminTable.Columns["USERID"] };
            return adminTable;
        }

        private static DataTable CreateQuotesTable()
        {
            DataTable quotesTable = new DataTable("QUOTES");
            quotesTable.Columns.Add("USERID", typeof(ulong));
            quotesTable.Columns.Add("MESSAGE", typeof(string));
            quotesTable.Columns.Add("OWNER", typeof(string));
            quotesTable.Columns.Add("REGISTERDATE", typeof(DateTime));
            quotesTable.PrimaryKey = new DataColumn[] { quotesTable.Columns["USERID"] };
            return quotesTable;
        }

        private static DataTable CreateInsultVictimsTable()
        {
            DataTable insultVictimTable = new DataTable("INSULTVICTIMS");
            insultVictimTable.Columns.Add("USERID", typeof(ulong));
            insultVictimTable.PrimaryKey = new DataColumn[] { insultVictimTable.Columns["USERID"] };
            return insultVictimTable;
        }

        private static DataTable CreateTwitchChannelsTable()
        {
            DataTable twitchChannelsTable = new DataTable("TWITCHCHANNELS");
            twitchChannelsTable.Columns.Add("ID", typeof(int));
            twitchChannelsTable.Columns.Add("NAME", typeof(string));
            twitchChannelsTable.Columns.Add("LOGO", typeof(string));
            twitchChannelsTable.Columns.Add("DATEREGISTERED", typeof(DateTime));
            twitchChannelsTable.PrimaryKey = new DataColumn[] { twitchChannelsTable.Columns["ID"] };
            return twitchChannelsTable;
        }

        private static DataTable CreateTwitchDiscordChannelsTable()
        {
            DataTable twitchChannelsTable = new DataTable("TWITCHDISCORDCHANNELS");
            twitchChannelsTable.Columns.Add("TWITCHCHANNELID", typeof(int));
            twitchChannelsTable.Columns.Add("DISCORDCHANNELID", typeof(ulong));
            twitchChannelsTable.Columns.Add("USERID", typeof(ulong));
            twitchChannelsTable.Columns.Add("DATEREGISTERED", typeof(DateTime));
            twitchChannelsTable.PrimaryKey = new DataColumn[] { twitchChannelsTable.Columns["TWITCHCHANNELID"] };
            return twitchChannelsTable;
        }

        private static DataTable CreateTwitchStreamsTable()
        {
            DataTable twitchStreamsTable = new DataTable("TWITCHSTREAMS");
            twitchStreamsTable.Columns.Add("ID", typeof(ulong));
            twitchStreamsTable.Columns.Add("CHANNEL", typeof(string));
            twitchStreamsTable.Columns.Add("GAME", typeof(string));
            twitchStreamsTable.Columns.Add("URL", typeof(string));
            twitchStreamsTable.Columns.Add("DATEPOSTED", typeof(DateTime));
            twitchStreamsTable.PrimaryKey = new DataColumn[] { twitchStreamsTable.Columns["ID"] };
            return twitchStreamsTable;
        }

        private static DataTable CreateFoodOptionsTable()
        {
            DataTable usersTable = new DataTable("FOODOPTIONS");
            usersTable.Columns.Add("KRZ", typeof(string));
            usersTable.Columns.Add("NAME", typeof(string));
            usersTable.Columns.Add("DESC", typeof(string));
            usersTable.Columns.Add("DAYS", typeof(string));
            //usersTable.Columns.Add("DAYS", typeof(IEnumerable<Food.Day>));
            usersTable.Columns.Add("INFO", typeof(string));
            usersTable.PrimaryKey = new DataColumn[] { usersTable.Columns["KRZ"] };
            return usersTable;
        }

        private static DataTable CreateObserveTable()
        {
            DataTable usersTable = new DataTable("OBSERVE");
            usersTable.Columns.Add("SERVERID", typeof(ulong));
            usersTable.Columns.Add("SERVERNAME", typeof(string));
            usersTable.PrimaryKey = new DataColumn[] { usersTable.Columns["SERVERID"] };
            return usersTable;
        }

        #endregion Tables

        #region Others

        //public static DataTable JoinDataTable(DataTable dataTable1, DataTable dataTable2, string joinField)
        //{
        //    var dt = new DataTable();
        //    var joinTable = from t1 in dataTable1.AsEnumerable()
        //                    join t2 in dataTable2.AsEnumerable()
        //                        on t1[joinField] equals t2[joinField]
        //                    select new { t1, t2 };

        //    foreach (DataColumn col in dataTable1.Columns)
        //        dt.Columns.Add(col.ColumnName, typeof(string));

        //    dt.Columns.Remove(joinField);

        //    foreach (DataColumn col in dataTable2.Columns)
        //        dt.Columns.Add(col.ColumnName, typeof(string));

        //    foreach (var row in joinTable)
        //    {
        //        var newRow = dt.NewRow();
        //        newRow.ItemArray = row.t1.ItemArray.Union(row.t2.ItemArray).ToArray();
        //        dt.Rows.Add(newRow);
        //    }
        //    return dt;
        //}

        private static void FillExampleData()
        {
            Read();
            //JenkinsDBSet.Tables["USERS"].Rows.Add("Marlz", 111794715690549248);
            //JenkinsDBSet.Tables["USERSTATS"].Rows.Add(111794715690549248, 97, 24, DateTime.Now.AddDays(-1.5), 42, 24);
            Database.Tables["CONFIG"].Rows.Add("StandardCfg", 9, 10, "uIb9o6e9VYNgLSBQ", "N67iXe8rBr5hwGXjfN8tANKQ0NeYNvJf", "a6e1a984fca6e6be6a2fc56e7e1b377c", DateTime.Now);
            //JenkinsDBSet.Tables["USERS"].Rows.Add("Gerrie", 208232302965293066);
            //JenkinsDBSet.Tables["USERSTATS"].Rows.Add(208232302965293066, 0, 0, DateTime.Now.AddDays(-0.5), 0, 98);
            Write();
        }

        #endregion Others
    }
}