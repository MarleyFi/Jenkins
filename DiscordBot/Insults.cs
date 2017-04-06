using System;
using System.Data;
using System.Linq;
using System.Text;
using Discord;

namespace DiscordBot
{
    internal class Insults
    {
        #region Methods

        public void AddInsult(User user, string message)
        {
            Jenkins.Database.Tables["INSULTS"].Rows.Add(user.Id, message, DateTime.Now);
            Jenkins.Write();
        }

        public string GetRandomInsult(bool withName = false)
        {
            DataTable insultsTable = Jenkins.Database.Tables["INSULTS"];
            var insults = insultsTable.AsEnumerable();

            if (withName)
            {
                insults = insults.Where(r => r.Field<string>("MESSAGE").Contains("*"));
            }
            Random rnd = new Random();
            var insult = insults.ElementAt<DataRow>(rnd.Next(0, insults.Count()));
            return insult["MESSAGE"].ToString();
        }

        public string GetInsult(int index)
        {
            DataRow[] userRows = Jenkins.Database.Tables["INSULTS"].Select("");
            var insult = userRows.ElementAt<DataRow>(index);
            return insult["MESSAGE"].ToString();
        }

        public string ListInsults()
        {
            var insultRows = Jenkins.Database.Tables["INSULTS"].AsEnumerable();
            var userRows = Jenkins.Database.Tables["USERS"].Rows;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<- - - **All Insults** - - ->");
            foreach (var insult in insultRows)
            {
                string userName = userRows.Find(insult.Field<ulong>("USERID")).Field<string>("NAME").ToString();
                sb.Append("- '");
                sb.Append(insult.Field<string>("MESSAGE"));
                sb.AppendLine("'");
                sb.AppendLine(" by **" + userName + "**");
            }
            return sb.ToString();
        }

        public string ListInsultsForUser(User user, ulong serverId)
        {
            DataRow[] userRows;
            if (Jenkins.Users.IsUserAdmin(user.Id, serverId))
            {
                userRows = Jenkins.Database.Tables["INSULTS"].Select("");
            }
            else
            {
                string userEXP = string.Format("USERID = '{0}'", user.Id);
                userRows = Jenkins.Database.Tables["INSULTS"].Select(userEXP);
            }

            int index = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<- - - **Insults** - - ->");
            foreach (var insult in userRows)
            {
                sb.Append("- [");
                sb.Append(index);
                sb.Append("] '");
                sb.Append(insult.Field<string>("MESSAGE"));
                sb.Append("'");
                sb.AppendLine();
                index++;
            }
            return sb.ToString();
        }

        public void DelInsult(User user, int index, ulong serverId)
        {
            DataRow[] userRows;
            if (Jenkins.Users.IsUserAdmin(user.Id, serverId))
            {
                userRows = Jenkins.Database.Tables["INSULTS"].Select("");
            }
            else
            {
                string userEXP = string.Format("USERID = '{0}'", user.Id);
                userRows = Jenkins.Database.Tables["INSULTS"].Select(userEXP);
            }
            userRows.ElementAt<DataRow>(index).Delete();
            Jenkins.Write();
        }

        public bool IsUserVictim(ulong userID)
        {
            string userEXP = string.Format("USERID = '{0}'", userID);
            DataRow[] userRows = Jenkins.Database.Tables["INSULTVICTIMS"].Select(userEXP);
            return (userRows.Length == 1);
        }

        public string ListVictims()
        {
            DataRow[] victimRows = Jenkins.Database.Tables["INSULTVICTIMS"].Select("");

            //var joinTable = from victims in JenkinsDBSet.Tables["INSULTVICTIMS"].AsEnumerable()
            //                join users in JenkinsDBSet.Tables["USERS"].AsEnumerable()
            //                on victims["USERID"] equals users["ID"]
            //                select new { victims, users };

            //joinTable
            int index = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<- - - **Victims** - - ->");
            foreach (var victim in victimRows)
            {
                DataRow row = Jenkins.Database.Tables["USERS"].Rows.Find(victim.Field<ulong>("USERID"));

                sb.Append("- [");
                sb.Append(index);
                sb.Append("] '");
                sb.Append(row.Field<string>("NAME"));
                sb.Append("'");
                sb.AppendLine();
                index++;
            }
            return sb.ToString();
        }

        public void AddVictim(ulong userId)
        {
            Jenkins.Database.Tables["INSULTVICTIMS"].Rows.Add(userId);
            Jenkins.Write();
        }

        public void DelVictim(ulong userId)
        {
            DataRow victimRow = Jenkins.Database.Tables["INSULTVICTIMS"].Rows.Find(userId);
            victimRow.Delete();
            Jenkins.Write();
        }

        #endregion Methods
    }
}