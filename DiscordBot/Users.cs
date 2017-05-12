using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Discord;

namespace DiscordBot
{
    internal class Users
    {
        #region Users

        #region Admins

        public bool IsUserAdmin(ulong userID, ulong serverId)
        {
            var admins = Jenkins.Database.Tables["ADMINS"].AsEnumerable();
            var admin = admins
                .Where(r => r.Field<ulong>("USERID").Equals(userID))
                .Where(r => r.Field<ulong>("SERVERID").Equals(serverId));
            if (admin.Count() == 1)
            {
                return true;
            }
            Server server = Bot.Client.GetServer(serverId);
            var user = server.Users.Where(r => r.Id.Equals(userID)).ElementAt(0);
            var adminRole = user.Roles.Where(r => r.Name.Equals(Bot.Config.AdminRoleName));
            return (adminRole.Count() == 1);
        }

        public bool IsUserDev(ulong userID)
        {
            var admins = Jenkins.Database.Tables["ADMINS"].AsEnumerable();
            var dev = admins
                .Where(r => r.Field<bool>("ISDEV").Equals(true))
                .Where(r => r.Field<ulong>("USERID").Equals(userID)); ;
            return (dev.Count() >= 1);
        }

        public void PromoteToAdmin(ulong userId, ulong serverId)
        {
            var admin = Jenkins.Database.Tables["ADMINS"].AsEnumerable()
                .Where(r => r.Field<ulong>("USERID").Equals(userId))
                .Where(r => r.Field<ulong>("SERVERID").Equals(serverId));
            if (admin.Count() >= 1)
                return;
            Jenkins.Database.Tables["ADMINS"].Rows.Add(userId, serverId, false);
            Jenkins.Write();
        }

        public void DegradeToUser(ulong userId, ulong serverId)
        {
            var admins = Jenkins.Database.Tables["ADMINS"].AsEnumerable();
            var admin = admins
                .Where(r => r.Field<ulong>("USERID").Equals(userId))
                .Where(r => r.Field<ulong>("SERVERID").Equals(serverId));
            admin.ElementAt(0).Delete();
            Jenkins.Write();
        }

        public string[] GetAdminNames(ulong serverId)
        {
            List<string> adminNameList = new List<string>();
            DataTable adminsTable = Jenkins.Database.Tables["ADMINS"];
            var admins = adminsTable.AsEnumerable()
                .Where(r => r.Field<ulong>("SERVERID").Equals(serverId)); ;
            var userRows = Jenkins.Database.Tables["USERS"].Rows;
            foreach (var item in admins)
            {
                var rw = userRows.Find(item.Field<ulong>("USERID"));
                adminNameList.Add(rw.Field<string>("NAME"));
            }
            return adminNameList.ToArray();
        }

        public ulong[] GetDevIDs()
        {
            List<ulong> devList = new List<ulong>();
            DataTable adminsTable = Jenkins.Database.Tables["ADMINS"];
            var devs = adminsTable.AsEnumerable();
            devs = devs.Where(r => r.Field<bool>("ISDEV").Equals(true));
            foreach (var dev in devs)
            {
                ulong userId = dev.Field<ulong>("USERID");
                if (!devList.Contains(userId))
                    devList.Add(userId);
            }
            return devList.ToArray();
        }

        #endregion Admins

        #region Basic User Methods

        public bool TryGetUserId(string username, out ulong userId)
        {
            string userEXP = string.Format("NAME = '{0}'", username);
            DataRow[] userRows = Jenkins.Database.Tables["USERS"].Select(userEXP);
            if (userRows.Length == 1)
            {
                userId = userRows[0].Field<ulong>("ID");
                return true;
            }
            userId = 0;
            return false; // not found
        }

        public bool IsUserRegistered(ulong userID)
        {
            string userEXP = string.Format("ID = '{0}'", userID);
            DataRow[] userRows = Jenkins.Database.Tables["USERS"].Select(userEXP);
            return (userRows.Length == 1);
        }

        public void AddUser(User user)
        {
            Random rnd = new Random();
            Jenkins.Database.Tables["USERS"].Rows.Add(user.Name, user.Id);
            Jenkins.Database.Tables["USERSTATS"].Rows.Add(user.Id, 0, 0, DateTime.Now, 0, rnd.Next(0, 100), DateTime.Now);
            Jenkins.Write();
        }

        public string ListUsers() // ToDo: Deprecated
        {
            DataRow[] userRows = Jenkins.Database.Tables["USERS"].Select("");

            int index = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<- - - **Users** - - ->");
            foreach (var insult in userRows)
            {
                sb.Append("- [");
                sb.Append(index);
                sb.Append("] '");
                sb.Append(insult.Field<string>("NAME"));
                sb.Append("'");
                sb.AppendLine();
                index++;
            }
            return sb.ToString();
        }

        #endregion Basic User Methods

        #region Update stats

        public void CheckUser(User user)
        {
            if (!IsUserRegistered(user.Id) && !user.IsBot)
            {
                AddUser(user);
            }
        }

        public string GetUserStats(ulong userID)
        {
            string userEXP = string.Format("ID = '{0}'", userID);
            DataRow[] userRows = Jenkins.Database.Tables["USERS"].Select(userEXP);

            string statsEXP = string.Format("USERID = '{0}'", userID);
            DataRow[] statsRows = Jenkins.Database.Tables["USERSTATS"].Select(statsEXP);

            string name = userRows[0]["NAME"].ToString();
            ulong id = ulong.Parse(userRows[0]["ID"].ToString());
            string msgCount = statsRows[0]["MESSAGECOUNT"].ToString();
            string talkedToMeCount = statsRows[0]["TALKEDTOMECOUNT"].ToString();
            object lastActivity = statsRows[0]["LASTACTIVITY"];
            string commandCount = statsRows[0]["COMMANDCOUNT"].ToString();
            string registerDate = statsRows[0]["REGISTERDATE"].ToString();
            string moronPerc = statsRows[0]["MORONPERC"].ToString();

            return Supporter.BuildStats(name.ToString(), int.Parse(msgCount), int.Parse(talkedToMeCount), int.Parse(commandCount), DateTime.Parse(lastActivity.ToString()), DateTime.Parse(registerDate), int.Parse(moronPerc));
        }

        public void CountUpMessages(User user)
        {
            string userEXP = string.Format("USERID = '{0}'", user.Id);
            DataRow row = Jenkins.Database.Tables["USERSTATS"].Rows.Find(user.Id);
            int msgCount = int.Parse(row["MESSAGECOUNT"].ToString());
            msgCount++;
            row["MESSAGECOUNT"] = msgCount;
            row.AcceptChanges();
            UpdateLastActivity(user);
        }

        public void CountUpCommands(User user)
        {
            string userEXP = string.Format("USERID = '{0}'", user.Id);
            DataRow row = Jenkins.Database.Tables["USERSTATS"].Rows.Find(user.Id);
            int cmdCount = int.Parse(row["COMMANDCOUNT"].ToString());
            cmdCount++;
            row["COMMANDCOUNT"] = cmdCount;
            row.AcceptChanges();
            UpdateLastActivity(user); // is this a activity?
        }

        public void CountUpTalkedToMe(User user)
        {
            string userEXP = string.Format("USERID = '{0}'", user.Id);
            DataRow row = Jenkins.Database.Tables["USERSTATS"].Rows.Find(user.Id);
            int talkedToMeCount = int.Parse(row["TALKEDTOMECOUNT"].ToString());
            talkedToMeCount++;
            row["TALKEDTOMECOUNT"] = talkedToMeCount;
            row.AcceptChanges();
            UpdateLastActivity(user);
        }

        public void UpdateLastActivity(User user)
        {
            string userEXP = string.Format("USERID = '{0}'", user.Id);
            DataRow row = Jenkins.Database.Tables["USERSTATS"].Rows.Find(user.Id);
            DateTime lastActivity = DateTime.Parse(row["LASTACTIVITY"].ToString());
            lastActivity = DateTime.Now;
            row["LASTACTIVITY"] = lastActivity;
            row.AcceptChanges();
            Jenkins.Write();
        }

        #endregion Update stats

        #endregion Users
    }
}